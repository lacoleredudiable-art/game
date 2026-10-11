namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Görsel yerleşim: XZ konum, Y ekseni dönüşü, düzgün ölçek.</summary>
    public readonly struct RuleEngineV4SliceArenaPlacement
    {
        public RuleEngineV4SliceArenaPlacement(float xM, float zM, float yawDeg, float scale)
        {
            XM = xM;
            ZM = zM;
            YawDeg = yawDeg;
            Scale = scale;
        }

        public float XM { get; }
        public float ZM { get; }
        public float YawDeg { get; }
        public float Scale { get; }
    }
}
