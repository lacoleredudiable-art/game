namespace Dovus.Core.RuleEngineV4
{
    public sealed class EtkiSokCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.EtkiSok;
        public int Count { get; init; }
    }
}
