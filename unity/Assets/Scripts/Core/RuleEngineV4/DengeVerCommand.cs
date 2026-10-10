namespace Dovus.Core.RuleEngineV4
{
    public sealed class DengeVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DengeVer;
        public float Amount { get; init; }
    }
}
