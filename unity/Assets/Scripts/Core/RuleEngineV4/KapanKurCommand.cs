namespace Dovus.Core.RuleEngineV4
{
    public sealed class KapanKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KapanKur;
        public float WaitSec { get; init; }
    }
}
