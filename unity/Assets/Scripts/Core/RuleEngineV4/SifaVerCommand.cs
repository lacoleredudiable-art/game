namespace Dovus.Core.RuleEngineV4
{
    public sealed class SifaVerCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.SifaVer;
        public float Amount { get; init; }
        public float PowerMult { get; init; }
    }
}
