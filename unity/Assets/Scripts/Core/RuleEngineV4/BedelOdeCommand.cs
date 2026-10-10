namespace Dovus.Core.RuleEngineV4
{
    public sealed class BedelOdeCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.BedelOde;
        public float HpRatio { get; init; }
    }
}
