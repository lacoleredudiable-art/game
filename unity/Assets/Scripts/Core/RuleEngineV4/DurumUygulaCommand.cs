namespace Dovus.Core.RuleEngineV4
{
    public sealed class DurumUygulaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.DurumUygula;
        public float Magnitude { get; init; }
        public float DurationSec { get; init; }
    }
}
