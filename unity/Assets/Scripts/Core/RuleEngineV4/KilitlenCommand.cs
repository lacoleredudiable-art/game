namespace Dovus.Core.RuleEngineV4
{
    public sealed class KilitlenCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Kilitlen;
        public float LockCapSec { get; init; }
        public float BlockInputSec { get; init; }
    }
}
