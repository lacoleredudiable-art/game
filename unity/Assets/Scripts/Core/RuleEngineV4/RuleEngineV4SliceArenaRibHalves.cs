using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Kemer = sol + sağ yarım kaburga (aynı mesh, sol yarım X aynası). Her yarım taç pivotu etrafında dışa
    /// lean_deg eğilir ve scale_mult ile büyür → ayaklar arenanın dışına basar, kemik yalnız tepeden geçer.
    /// Uzunluklar kaburga yerel metresinde (kemer ölçeğiyle çarpılır); pivot_y_m modelin kendi metresinde.
    /// crown_overlap_m: yarım kesimi taç çizgisinin bu kadar ötesinden yapılır → eğik yarımlar tepede kaynaşır.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaRibHalves
    {
        public bool Enabled { get; private set; }
        public float LeanDeg { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHalfLeanDeg;
        public float ScaleMult { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHalfScaleMult;
        public float PivotYM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHalfPivotYM;
        public float CrownYM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHalfCrownYM;
        public float CrownOverlapM { get; private set; } = RuleEngineV4SliceArenaVisualsDefaults.RibHalfCrownOverlapM;

        public static RuleEngineV4SliceArenaRibHalves FromJson(JsonValue node)
        {
            var h = new RuleEngineV4SliceArenaRibHalves();
            if (node.IsNull)
                return h;
            h.Enabled = true;
            h.LeanDeg = node["lean_deg"].AsFloat(h.LeanDeg);
            h.ScaleMult = node["scale_mult"].AsFloat(h.ScaleMult);
            h.PivotYM = node["pivot_y_m"].AsFloat(h.PivotYM);
            h.CrownYM = node["crown_y_m"].AsFloat(h.CrownYM);
            h.CrownOverlapM = node["crown_overlap_m"].AsFloat(h.CrownOverlapM);
            return h;
        }
    }
}
