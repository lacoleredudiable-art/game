namespace Dovus.Core.RuleEngineV4
{
    public sealed class OnSureCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.OnSure;
        public float PrefireSec { get; init; }
        public float ChargeSec { get; init; }
        public float TotalCapSec { get; init; }
        public float RecoverySec { get; init; }
        public float DamageScale { get; init; }
        public bool PrefireMoves { get; init; }
    }
}
