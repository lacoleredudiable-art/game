using System.Collections.Generic;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Core.Shared;

namespace Dovus.App.Boss
{
    public static class BossMechanicStatusMap
    {
        public static void ApplyFireCone(StatusBoard board, StatusTuning t, IReadOnlyList<string> mechanics)
        {
            if (board == null || t == null)
                return;
            if (mechanics == null || mechanics.Count == 0)
            {
                board.Apply(StatusKind.Burn, t.BurnMs, t.BurnDamagePerSec);
                board.Apply(StatusKind.GrievousWounds, t.GrievousMs, t.GrievousHealMult);
                return;
            }

            ApplyMechanicTags(board, mechanics, t);
        }

        public static void ApplyMechanicTags(StatusBoard board, IReadOnlyList<string> mechanics, StatusTuning t)
        {
            if (board == null || t == null || mechanics == null)
                return;
            for (int i = 0; i < mechanics.Count; i++)
            {
                if (!StatusKindUtil.TryParse(mechanics[i], out StatusKind kind) || kind == StatusKind.None)
                    continue;
                ApplyKind(board, kind, t, durationSec: 0f);
            }
        }

        public static void ApplyKind(StatusBoard board, StatusKind kind, StatusTuning t, float durationSec)
        {
            if (board == null || t == null)
                return;
            double ms = durationSec > 0f ? durationSec * Units.SecToMs : 0;
            switch (kind)
            {
                case StatusKind.Burn:
                    board.Apply(kind, ms > 0 ? ms : t.BurnMs, t.BurnDamagePerSec);
                    break;
                case StatusKind.GrievousWounds:
                    board.Apply(kind, ms > 0 ? ms : t.GrievousMs, t.GrievousHealMult);
                    break;
                case StatusKind.Poison:
                    board.Apply(kind, ms > 0 ? ms : t.PoisonMs, t.PoisonDamagePerSec);
                    break;
                case StatusKind.Weaken:
                    board.Apply(kind, ms > 0 ? ms : t.WeakenMs, t.WeakenOutgoingMult);
                    break;
                case StatusKind.ArmorBreak:
                    board.Apply(kind, ms > 0 ? ms : t.ArmorBreakMs, t.ArmorBreakDamageTakenMult);
                    break;
                case StatusKind.Slow:
                    board.Apply(kind, ms > 0 ? ms : t.SlowMs, t.SlowSpeedMult);
                    break;
                case StatusKind.Blind:
                    board.Apply(kind, ms > 0 ? ms : t.BlindMs,
                        BossStatusMath.BlindChanceFromAccuracy(t.BlindMissChance));
                    break;
                case StatusKind.Root:
                    board.Apply(kind, ms > 0 ? ms : t.RootMs, 1f);
                    break;
                case StatusKind.Silence:
                    board.Apply(kind, ms > 0 ? ms : t.SilenceMs, 1f);
                    break;
                case StatusKind.Stun:
                    board.Apply(kind, ms > 0 ? ms : t.StunMs, 1f);
                    break;
                case StatusKind.Disarm:
                    board.Apply(kind, ms > 0 ? ms : t.DisarmMs, 1f);
                    break;
            }
        }
    }
}
