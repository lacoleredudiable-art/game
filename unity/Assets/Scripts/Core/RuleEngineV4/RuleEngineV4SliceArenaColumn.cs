namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4SliceArenaColumn
    {
        public RuleEngineV4SliceArenaColumn(float xM, float zM, float radiusM, float heightM)
        {
            XM = xM;
            ZM = zM;
            RadiusM = radiusM;
            HeightM = heightM;
        }

        public float XM { get; }
        public float ZM { get; }
        public float RadiusM { get; }
        public float HeightM { get; }
    }
}
