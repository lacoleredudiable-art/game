using System.Collections.Generic;
using Dovus.Core.Combat;
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
            StatusTuning tuning,
            MobilityCcData? mobilityCc = null)
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

                    ApplyKind(board, kind, skill, tuning, mobilityCc, ParseAdjectiveId(skill.AdjectiveId));
                }

                // Sıfat engine_modifiers — fiil mechanics dışında ek durum (3’lü/4’lü farkı).
                ApplyAdjectiveModifiers(
                    skill, board, caster, target, self, ref knockback, ref pull, tuning, mechanics,
                    mobilityCc, ParseAdjectiveId(skill.AdjectiveId));
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
        /// Fiil mechanics’te zaten varsa tekrar uygulanmaz. Düşmanca sıfat durumları kendine
        /// yönelik fiilde de (Hareket/Çağırma/Yansıma) caster'a değil vurulan hedefe gider;
        /// hedef yoksa (hiçbir şeye değmedi) uygulanmaz.
        /// </summary>
        static void ApplyAdjectiveModifiers(
            SkillResolution skill,
            StatusBoard board,
            StatusBoard caster,
            StatusBoard target,
            bool self,
            ref bool knockback,
            ref bool pull,
            StatusTuning tuning,
            string[] mechanics,
            MobilityCcData? mobilityCc,
            int adjectiveId)
        {
            JsonValue mods = skill.EngineModifiers;
            if (mods.IsNull || mods.Kind != JsonKind.Object)
                return;

            bool HasMech(string id) => System.Array.IndexOf(mechanics, id) >= 0;
            bool hitsEnemy = CardEffectRules.HarmfulHitsEnemy(skill.TargetMode, skill.Action, skill.SkillJob);
            StatusBoard hostile = hitsEnemy ? (self ? target : board) : null;

            if (hostile != null)
                ApplyHostileAdjectiveModifiers(
                    mods, hostile, skill, tuning, mechanics, mobilityCc, adjectiveId);

            if (CardEffectRules.WantsSelfHaste(skill.SkillJob)
                && !string.Equals(skill.Action, "tempo", System.StringComparison.OrdinalIgnoreCase)
                && caster != null)
            {
                float haste = CardEffectRules.HasteMagnitude(
                    skill.SkillJob,
                    mods["self_haste"].AsFloat(0f),
                    mods["enemy_slow"].AsFloat(0f),
                    mods["self_damage_buff"].AsFloat(0f));
                double hasteMs = mods["buff_duration_sec"].AsFloat(0f) * 1000.0;
                if (hasteMs <= 0)
                    hasteMs = tuning.HasteMs;
                if (haste > 1f)
                    caster.Apply(StatusKind.Haste, hasteMs, haste);
            }

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
                    board.Apply(
                        StatusKind.Blind,
                        mobilityCc?.ResolveCcDurationMs(StatusKind.Blind, adjectiveId, tuning.BlindMs)
                            ?? tuning.BlindMs,
                        1f);
                if (!HasMech("slow"))
                    board.Apply(
                        StatusKind.Slow,
                        mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                            ?? tuning.SlowMs,
                        tuning.SlowSpeedMult);
            }
        }

        static void ApplyHostileAdjectiveModifiers(
            JsonValue mods,
            StatusBoard board,
            SkillResolution skill,
            StatusTuning tuning,
            string[] mechanics,
            MobilityCcData? mobilityCc,
            int adjectiveId)
        {
            bool HasMech(string id) => System.Array.IndexOf(mechanics, id) >= 0;
            bool keepEnemyLock = !CardEffectRules.WantsSelfHaste(skill.SkillJob)
                || CardEffectRules.Names(skill.SkillJob, "root");

            if (keepEnemyLock && mods.Has("apply_slow") && !HasMech("slow"))
            {
                float mult = mods["apply_slow"].AsFloat(0f);
                if (mult <= 0f)
                    mult = tuning.SlowSpeedMult;
                if (mult <= 0f || mult > 1f)
                    mult = tuning.SlowSpeedMult;
                board.Apply(
                    StatusKind.Slow,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                        ?? tuning.SlowMs,
                    mult);
            }

            if (keepEnemyLock && ModifierTruthy(mods, "apply_root") && !HasMech("root"))
                board.Apply(
                    StatusKind.Root,
                    ExplicitOrFallback(mods, "cc_duration_sec", StatusKind.Root, adjectiveId, tuning.RootMs, mobilityCc, skill.SkillJob),
                    1f,
                    RootSource(skill, "adj"));
            float rootSec = mods["apply_root_sec"].AsFloat(0f);
            if (keepEnemyLock && rootSec > 0f && !HasMech("root"))
                board.Apply(
                    StatusKind.Root,
                    rootSec * 1000.0,
                    1f,
                    RootSource(skill, "adj"));

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
                board.Apply(
                    StatusKind.Silence,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Silence, adjectiveId, tuning.SilenceMs)
                        ?? tuning.SilenceMs,
                    1f);

            // Kart "yavaşlatma" diyorsa isabet cezası kör değil yavaşlatmadır.
            // Kart "kör" veya "isabet" diyorsa eski kör eşlemesi kalır.
            float accuracy = mods["accuracy_debuff"].AsFloat(0f);
            if (accuracy > 0f && CardEffectRules.AccuracyIsSlow(skill.SkillJob) && !HasMech("slow"))
            {
                float mult = accuracy <= 1f ? accuracy : tuning.SlowSpeedMult;
                double slowMs = mods["lifetime_add"].AsFloat(0f) * 1000.0;
                if (slowMs <= 0)
                    slowMs = mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, adjectiveId, tuning.SlowMs)
                        ?? tuning.SlowMs;
                board.Apply(StatusKind.Slow, slowMs, mult);
            }
            else if (accuracy > 0f && !HasMech("blind"))
                board.Apply(
                    StatusKind.Blind,
                    mobilityCc?.ResolveCcDurationMs(StatusKind.Blind, adjectiveId, tuning.BlindMs)
                        ?? tuning.BlindMs,
                    1f);
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
            if (hit is "self" or "self_aura" or "target_ally" or "self_or_ally")
                return true;
            if (skill.TargetMode is "self_only" or "self_or_ally")
                return true;
            string family = skill.VerbFamily ?? string.Empty;
            return family is "mend" or "guard" or "purge";
        }

        static void ApplyKind(
            StatusBoard board,
            StatusKind kind,
            SkillResolution skill,
            StatusTuning t,
            MobilityCcData? mobilityCc,
            int adjectiveId)
        {
            double Duration(double fallback) =>
                ExplicitOrFallback(
                    skill.EngineModifiers, "cc_duration_sec", kind, adjectiveId, fallback, mobilityCc, skill.SkillJob);
            switch (kind)
            {
                case StatusKind.Stun:
                    board.Apply(kind, Duration(t.StunMs), 1f);
                    break;
                case StatusKind.Root:
                    board.Apply(kind, Duration(t.RootMs), 1f, RootSource(skill, "verb"));
                    break;
                case StatusKind.Silence:
                    board.Apply(kind, Duration(t.SilenceMs), 1f);
                    break;
                case StatusKind.Slow:
                    board.Apply(kind, Duration(t.SlowMs), t.SlowSpeedMult);
                    break;
                case StatusKind.Blind:
                    board.Apply(kind, Duration(t.BlindMs), 1f);
                    break;
                case StatusKind.Disarm:
                    board.Apply(kind, Duration(t.DisarmMs), 1f);
                    break;
                case StatusKind.Taunt:
                    board.Apply(kind, Duration(t.TauntMs), 1f);
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

        /// <summary>
        /// Skill engine'i kendi süresini yazdıysa o kullanılır.
        /// Yazmadıysa mobility_cc tablosu, o da yoksa tuning yedeği.
        /// </summary>
        static double ExplicitOrFallback(
            JsonValue engine,
            string secondsField,
            StatusKind kind,
            int adjectiveId,
            double fallbackMs,
            MobilityCcData? mobilityCc,
            string effectText = "")
        {
            if (!engine.IsNull && engine.Kind == JsonKind.Object)
            {
                string cc = CardEffectRules.CcKind(effectText, engine["cc_kind"].AsString());
                bool matches = string.IsNullOrEmpty(cc)
                    || (StatusKindUtil.TryParse(cc, out StatusKind ccKind) && ccKind == kind);
                float seconds = engine[secondsField].AsFloat(0f);
                if (matches && seconds > 0f)
                    return seconds * 1000.0;
            }

            return mobilityCc?.ResolveCcDurationMs(kind, adjectiveId, fallbackMs) ?? fallbackMs;
        }

        static string RootSource(SkillResolution skill, string part) =>
            "skill:" + (string.IsNullOrEmpty(skill.SkillId) ? "unknown" : skill.SkillId) + ":" + part;

        static int ParseAdjectiveId(string id) =>
            int.TryParse(id, out int value) ? value : 0;
    }
}
