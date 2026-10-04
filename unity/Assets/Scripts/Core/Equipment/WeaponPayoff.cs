using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;

namespace Dovus.Core.Equipment
{
    /// <summary>T─▒ls─▒m de─şi┼ştirme bonusu: hedefteki bir k├Ât├╝ durumu siler, di─şerleri kal─▒r.</summary>
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

    /// <summary>B├╝y├╝ Kitab─▒ de─şi┼ştirme bonusu: o vuru┼ş mana yemez.</summary>
    public static class WeaponManaWaiver
    {
        public static void Charge(ResourceTracker tracker, float cost, bool freeCast)
        {
            if (freeCast || tracker == null || cost <= 0f)
                return;
            tracker.Consume(cost);
        }
    }

    /// <summary>
    /// K├╝re HUD d├╝─şmesi. ├çizim alan─▒ndaki bas─▒l─▒ tutma ve ├ğift dokunu┼ş yoktur.
    /// Eldeyse se├ğili hedefe gider, d─▒┼şar─▒daysa geri ├ğa─şr─▒l─▒r. Yolculuk s├╝resi OrbAnchor'da.
    /// </summary>
    public static class OrbHudCommand
    {
        public static OrbGestureResult Tap(bool atHand) =>
            atHand ? OrbGestureResult.Place : OrbGestureResult.Recall;
    }

    public enum OrbGestureResult
    {
        None = 0,
        Place = 1,
        Recall = 2
    }
}