namespace Dovus.Core.RuleEngineV4
{
    public sealed class SekCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Sek;
        public int BounceCount { get; init; }
        public float BounceMult { get; init; }
        public float SearchRadiusM { get; init; }
    }
}
