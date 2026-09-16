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
            public Result(bool knockback, bool cleansed, IReadOnlyList<StatusReactionRule> triggeredReactions)
            {
                Knockback = knockback;
                Cleansed = cleansed;
                TriggeredReactions = triggeredReactions;
            }

            public bool Knockback { get; }
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
            }
            finally
            {
                board.ReactionTriggered -= OnReaction;
            }

            return new Result(knockback, cleansed, triggered);
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
