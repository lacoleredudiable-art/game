namespace Dovus.Core.RuleEngineV4
{
    public sealed class AlanAcCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.AlanAc;
        public string Shape { get; init; } = "daire";
        public float RadiusM { get; init; }
        public int MaxTargets { get; init; }
        public float PowerMult { get; init; }
    }
}
