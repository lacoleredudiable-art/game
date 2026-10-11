namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Statik yüzey (sütun / dış duvar) yalnız işaret içindir (MarkOnly). Karar komut türlerinden doğar:
    /// işaret koyan plan yüzeye işaret bırakabilir; yüzey işaretini yalnız oyuncuyu taşıyan ve
    /// hasar / şifa taşımayan plan kullanır. Otomatik hedef seçici yüzeyi hiç görmez.
    /// </summary>
    public static class RuleEngineV4MarkOnlyRules
    {
        public static bool PlacesMark(CommandPlan plan) => MarkPlaceRangeM(plan) > 0;

        /// <summary>İşaret koyma menzili; plan işaret koymuyorsa 0.</summary>
        public static float MarkPlaceRangeM(CommandPlan plan)
        {
            if (plan == null)
                return 0;
            for (int i = 0; i < plan.Commands.Count; i++)
            {
                if (plan.Commands[i] is IsaretKoyCommand mark)
                    return mark.PlaceRangeM;
            }
            return 0;
        }

        /// <summary>Hedef çözümü en yakın takım işaretine gidiyor (İşaretli sıfat, işaret koymayan fiil).</summary>
        public static bool TargetsMark(RuleEngineV4TargetResolution resolution) =>
            resolution?.AutoProfileKey == "isaretli_nearest_mark";

        public static bool MayUseSurfaceMark(CommandPlan plan) =>
            (Has(plan, PhysicsCommandKind.KendiniTasi) || Has(plan, PhysicsCommandKind.Isinla))
            && !Has(plan, PhysicsCommandKind.HasarVer)
            && !Has(plan, PhysicsCommandKind.SifaVer);

        /// <summary>Nişandaki yüzey, menzildeki canlı hedeften yakınsa ya da canlı hedef menzilde değilse kazanır.</summary>
        public static bool SurfaceBeatsLiving(float surfaceDistanceM, bool livingInRange, float livingDistanceM) =>
            !livingInRange || surfaceDistanceM < livingDistanceM;

        static bool Has(CommandPlan plan, PhysicsCommandKind kind)
        {
            if (plan == null)
                return false;
            for (int i = 0; i < plan.Commands.Count; i++)
            {
                if (plan.Commands[i].Kind == kind)
                    return true;
            }
            return false;
        }
    }
}
