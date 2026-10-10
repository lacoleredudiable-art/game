namespace Dovus.Core.RuleEngineV4
{
    /// <summary>JSON yüklendikten sonra aktif dünya fiziği sayıları.</summary>
    public static class RuleEngineV4WorldPhysicsRuntime
    {
        public static RuleEngineV4WorldPhysics Active { get; private set; } = RuleEngineV4WorldPhysics.Default;

        public static void Bind(RuleEngineV4WorldPhysics physics) =>
            Active = physics ?? RuleEngineV4WorldPhysics.Default;
    }
}
