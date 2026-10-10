namespace Dovus.Core.RuleEngineV4
{
    public sealed class HasarVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.HasarVer;
        public float Amount { get; init; }
        public float Poise { get; init; }
        public float PowerMult { get; init; }
    }
}
