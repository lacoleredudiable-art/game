namespace Dovus.Core.RuleEngineV4
{
    public sealed class MenzileYuruCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.MenzileYuru;
        public float RangeM { get; init; }
    }
}
