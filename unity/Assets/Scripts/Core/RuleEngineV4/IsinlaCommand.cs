namespace Dovus.Core.RuleEngineV4
{
    public sealed class IsinlaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Isinla;
        public float MaxDistanceM { get; init; }
    }
}
