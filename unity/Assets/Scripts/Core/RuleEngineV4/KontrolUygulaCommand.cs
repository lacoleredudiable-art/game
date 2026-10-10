namespace Dovus.Core.RuleEngineV4
{
    public sealed class KontrolUygulaCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KontrolUygula;
        public float DurationSec { get; init; }
    }
}
