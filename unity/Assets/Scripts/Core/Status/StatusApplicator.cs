using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    /// <summary>
    /// SkillResolution.mechanics → StatusBoard. Knockback ayrı bayrak (anlık).
    /// </summary>
    public static class StatusApplicator
    {
        public readonly struct Result
        {
            public Result(bool knockback, bool cleansed, IReadOnlyList<StatusReactionRule> triggeredReactions, bool pull = false)
            {
                Knockback = knockback;
                Cleansed = cleansed;
                TriggeredReactions = triggeredReactions;
                Pull = pull;
            }

            public bool Knockback { get; }
            public bool Pull { get; }
            public bool Cleansed { get; }

            /// <summary>
            /// 16 Eylül: bu cast sırasında ateşlenen durum etkileşim kuralları (varsa) —
            /// Game katmanı bunu ekrana yazsın diye (bkz. StatusBoard.ReactionTriggered).
            /// </summary>
            public IReadOnlyList<StatusReactionRule> TriggeredReactions { get; }
        }

        static readonly IReadOnlyList<StatusReactionRule> EmptyReactions = System.Array.Empty<StatusReactionRule>();

        /// <summary>
        /// self hitbox → caster board; aksi halde target board.
        /// cleanse: hedefteki düşman durumları temizlenir (self cleanse = caster).
        /// </summary>
        public static Result ApplySkill(
            SkillResolution skill,
            StatusBoard caster,
            StatusBoard target,
            StatusTuning tuning)
        {
            if (skill.IsEmpty || tuning == null)
                return new Result(false, false, EmptyReactions);

            bool self = IsSelfTargeted(skill);
            StatusBoard board = self ? caster : target;
            if (board == null)
                return new Result(false, false, EmptyReactions);

            bool knockback = false;
            bool pull = false;
            bool cleansed = false;
            string[] mechanics = skill.Mechanics ?? System.Array.Empty<string>();

            // "Savrulma Sersemliği" (docs/element-sistemi.json status_interaction_table):
            // aynı vuruşta stun + knockback birlikteyse stun süresi uzar. Knockback kalıcı bir
            // status değil (anlık bayrak) — StatusBoard'un durum tablosu bunu göremez, o yüzden
            // burada, "aynı cast" bilgisiyle özel işleniyor.
            bool hasKnockbackThisCast = System.Array.IndexOf(mechanics, "knockback") >= 0;

            // 16 Eylül: "etkileşim göremiyorum" raporu — bu cast sırasında ateşlenen kuralları
            // topla, Game katmanı ekrana yazsın (StatusBoard mekanik olarak zaten uyguluyordu,
            // sadece görünmüyordu).
            var triggered = new List<StatusReactionRule>();
            void OnReaction(StatusReactionRule r) => triggered.Add(r);
            board.ReactionTriggered += OnReaction;

            try
            {
                for (int i = 0; i < mechanics.Length; i++)
                {
                    string id = mechanics[i];
                    if (id == "cleanse")
                    {
                        board.CleanseHostile();
                        cleansed = true;
                        continue;
                    }

                    if (!StatusKindUtil.TryParse(id, out StatusKind kind) || kind == StatusKind.None)
                        continue;

                    if (kind == StatusKind.Knockback)
                    {
                        knockback = !self;
                        continue;
                    }

                    if (kind == StatusKind.Stun && hasKnockbackThisCast)
                    {
                        board.Apply(kind, tuning.StunMs + tuning.StunKnockbackDurationAddMs, 1f);
                        continue;
                    }

                    ApplyKind(board, kind, tuning);
                }

                // Sıfat engine_modifiers — fiil mechanics dışında ek durum (3’lü/4’lü farkı).
                ApplyAdjectiveModifiers(skill, board, caster, self, ref knockback, ref pull, tuning, mechanics);
            }
            finally
            {
                board.ReactionTriggered -= OnReaction;
            }

            return new Result(knockback, cleansed, triggered, pull);
        }

        /// <summary>
        /// apply_slow / apply_root / apply_burn / apply_poison_on_hit / apply_silence /
        /// apply_knockback / apply_pull / apply_stealth / apply_confuse — JSON adjectives.
        /// Fiil mechanics’te zaten varsa tekrar uygulanmaz.
        /// </summary>
        static void ApplyAdjectiveModifiers(
            SkillResolution skill,
            StatusBoard board,
            StatusBoard caster,
            bool self,
            ref bool knockback,
            ref bool pull,
            StatusTuning tuning,
            string[] mechanics)
        {
            JsonValue mods = skill.EngineModifiers;
            if (mods.IsNull || mods.Kind != JsonKind.Object)
                return;

            bool HasMech(string id) => System.Array.IndexOf(mechanics, id) >= 0;

            if (mods.Has("apply_slow") && !HasMech("slow"))
            {
                float mult = mods["apply_slow"].AsFloat(0f);
                if (mult <= 0f)
                    mult = tuning.SlowSpeedMult;
                if (mult <= 0f || mult > 1f)
                    mult = tuning.SlowSpeedMult;
                board.Apply(StatusKind.Slow, tuning.SlowMs, mult);
            }

            if (ModifierTruthy(mods, "apply_root") && !HasMech("root"))
                board.Apply(StatusKind.Root, tuning.RootMs, 1f);

            if (ModifierTruthy(mods, "apply_burn") && !HasMech("burn"))
            {
                float burn = tuning.BurnDamagePerSec;
                float burnMult = mods["burn_damage_mult"].AsFloat(1f);
                if (burnMult > 0f)
                    burn *= burnMult;
                board.Apply(StatusKind.Burn, tuning.BurnMs, burn);
            }

            if (ModifierTruthy(mods, "apply_poison_on_hit") && !HasMech("poison"))
                board.Apply(StatusKind.Poison, tuning.PoisonMs, tuning.PoisonDamagePerSec);

            if (ModifierTruthy(mods, "apply_silence") && !HasMech("silence"))
                board.Apply(StatusKind.Silence, tuning.SilenceMs, 1f);

            if (ModifierTruthy(mods, "apply_knockback") && !self && !HasMech("knockback"))
                knockback = true;

            if (ModifierTruthy(mods, "apply_pull") && !self)
                pull = true;

            if (mods.Has("apply_damage_reduction") && !HasMech("damage_reduction"))
            {
                float mult = mods["apply_damage_reduction"].AsFloat(tuning.DamageReductionMult);
                if (mult > 0f && mult < 1f)
                    board.Apply(StatusKind.DamageReduction, tuning.DamageReductionMs, mult);
            }

            // gizleme: her zaman caster'a stealth (hedef board self olsa da caster aynı).
            if (ModifierTruthy(mods, "apply_stealth") && !HasMech("stealth") && caster != null)
                caster.Apply(StatusKind.Stealth, tuning.StealthMs, 1f);

            // sasirtma: Confuse kind yok → Blind + Slow (durum.md öncelik 2).
            if (ModifierTruthy(mods, "apply_confuse") && !self)
            {
                if (!HasMech("blind"))
                    board.Apply(StatusKind.Blind, tuning.BlindMs, 1f);
                if (!HasMech("slow"))
                    board.Apply(StatusKind.Slow, tuning.SlowMs, tuning.SlowSpeedMult);
            }
        }

        static bool ModifierTruthy(JsonValue mods, string key)
        {
            if (!mods.Has(key))
                return false;
            JsonValue v = mods[key];
            if (v.Kind == JsonKind.Bool)
                return v.AsBool(false);
            if (v.Kind == JsonKind.Number)
                return v.AsFloat(0f) > 0f;
            if (v.Kind == JsonKind.String)
                return !string.IsNullOrEmpty(v.AsString());
            return false;
        }

        public static bool IsSelfTargeted(SkillResolution skill)
        {
            string hit = skill.Hitbox ?? string.Empty;
            if (hit is "self" or "self_aura" or "target_ally")
                return true;
            string family = skill.VerbFamily ?? string.Empty;
            return family is "mend" or "guard" or "purge";
        }

        static void ApplyKind(StatusBoard board, StatusKind kind, StatusTuning t)
        {
            switch (kind)
            {
                case StatusKind.Stun:
                    board.Apply(kind, t.StunMs, 1f);
                    break;
                case StatusKind.Root:
                    board.Apply(kind, t.RootMs, 1f);
                    break;
                case StatusKind.Silence:
                    board.Apply(kind, t.SilenceMs, 1f);
                    break;
                case StatusKind.Slow:
                    board.Apply(kind, t.SlowMs, t.SlowSpeedMult);
                    break;
                case StatusKind.Blind:
                    board.Apply(kind, t.BlindMs, 1f);
                    break;
                case StatusKind.Disarm:
                    board.Apply(kind, t.DisarmMs, 1f);
                    break;
                case StatusKind.Taunt:
                    board.Apply(kind, t.TauntMs, 1f);
                    break;
                case StatusKind.Fear:
                    board.Apply(kind, t.FearMs, 1f);
                    break;
                case StatusKind.Stasis:
                    board.Apply(kind, t.StasisMs, 1f);
                    break;
                case StatusKind.Stealth:
                    board.Apply(kind, t.StealthMs, 1f);
                    break;
                case StatusKind.Burn:
                    board.Apply(kind, t.BurnMs, t.BurnDamagePerSec);
                    break;
                case StatusKind.ArmorBreak:
                    board.Apply(kind, t.ArmorBreakMs, t.ArmorBreakDamageTakenMult);
                    break;
                case StatusKind.Weaken:
                    board.Apply(kind, t.WeakenMs, t.WeakenOutgoingMult);
                    break;
                case StatusKind.GrievousWounds:
                    board.Apply(kind, t.GrievousMs, t.GrievousHealMult);
                    break;
                case StatusKind.Poison:
                    board.Apply(kind, t.PoisonMs, t.PoisonDamagePerSec);
                    break;
                case StatusKind.Shield:
                    board.Apply(kind, t.ShieldMs, t.ShieldAbsorb);
                    break;
                case StatusKind.Haste:
                    board.Apply(kind, t.HasteMs, t.HasteSpeedMult);
                    break;
                case StatusKind.DamageReduction:
                    board.Apply(kind, t.DamageReductionMs, t.DamageReductionMult);
                    break;
                case StatusKind.Regen:
                    board.Apply(kind, t.RegenMs, t.RegenPerSec);
                    break;
            }
        }
    }
}
