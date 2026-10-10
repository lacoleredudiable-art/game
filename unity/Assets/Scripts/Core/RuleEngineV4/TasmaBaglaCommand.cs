namespace Dovus.Core.RuleEngineV4
{
    public sealed class TasmaBaglaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.TasmaBagla;
        public float DurationSec { get; init; }
        public float MaxLengthM { get; init; }
    }
}
