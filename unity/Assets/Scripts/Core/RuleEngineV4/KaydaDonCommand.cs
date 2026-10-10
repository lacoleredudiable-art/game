namespace Dovus.Core.RuleEngineV4
{
    public sealed class KaydaDonCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KaydaDon;
        public float PowerMult { get; init; }
        public bool Revive { get; init; }
    }
}
