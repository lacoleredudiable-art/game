namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4SliceArenaCocoon
    {
        public RuleEngineV4SliceArenaCocoon(
            float xM,
            float zM,
            float radiusM,
            int maxHp,
            float heightM = RuleEngineV4SliceArenaDefaults.CocoonHeightM)
        {
            XM = xM;
            ZM = zM;
            RadiusM = radiusM;
            MaxHp = maxHp;
            HeightM = heightM;
        }

        public float XM { get; }
        public float ZM { get; }
        public float RadiusM { get; }
        public int MaxHp { get; }
        /// <summary>Kapsül toplam yüksekliği (yarım küreler dahil).</summary>
        public float HeightM { get; }
    }
}
