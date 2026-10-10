namespace Dovus.Core.RuleEngineV4
{
    public sealed class ZincirKullanCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.ZincirKullan;
        public float LinkWidthM { get; init; }
        public int MaxTargets { get; init; }
    }
}
