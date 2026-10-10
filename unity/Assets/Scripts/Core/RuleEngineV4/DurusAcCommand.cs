namespace Dovus.Core.RuleEngineV4
{
    public sealed class DurusAcCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DurusAc;
        public float DurationSec { get; init; }
        public float BlockRatio { get; init; }
    }
}
