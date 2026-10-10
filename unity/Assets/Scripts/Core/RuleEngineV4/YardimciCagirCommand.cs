namespace Dovus.Core.RuleEngineV4
{
    public sealed class YardimciCagirCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YardimciCagir;
        public int Count { get; init; }
        public float LifeSec { get; init; }
    }
}
