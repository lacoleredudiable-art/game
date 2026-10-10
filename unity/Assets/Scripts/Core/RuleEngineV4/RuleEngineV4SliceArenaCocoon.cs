namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4SliceArenaCocoon
    {
        public RuleEngineV4SliceArenaCocoon(float xM, float zM, float radiusM, int maxHp)
        {
            XM = xM;
            ZM = zM;
            RadiusM = radiusM;
            MaxHp = maxHp;
        }

        public float XM { get; }
        public float ZM { get; }
        public float RadiusM { get; }
        public int MaxHp { get; }
    }
}
