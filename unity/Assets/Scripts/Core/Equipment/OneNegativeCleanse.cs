using Dovus.Core.Boss;
using Dovus.Core.Shared;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;

namespace Dovus.Core.Equipment
{
    /// <summary>Tılsım değiştirme bonusu: hedefteki bir kötü durumu siler, diğerleri kalır.</summary>
    public static class OneNegativeCleanse
    {
        public static bool TryRemove(StatusBoard board)
        {
            if (board == null)
                return false;
            StatusKind pick = StatusKind.None;
            foreach (StatusKind kind in board.ActiveKinds)
            {
                if (!IsNegative(kind))
                    continue;
                pick = kind;
                break;
            }

            if (pick == StatusKind.None)
                return false;
            board.RemoveKinds(new[] { pick });
            return true;
        }

        public static bool IsNegative(StatusKind kind) =>
            StatusKindUtil.IsHardCc(kind) || StatusKindUtil.IsSoftCc(kind) || StatusKindUtil.IsDebuff(kind);
    }
}
