namespace Dovus.Core.RuleEngineV4
{
    public sealed class YorungeKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YorungeKur;
        public float RadiusM { get; init; }
        public int PartCount { get; init; }
        public float LifeSec { get; init; }
    }
}
