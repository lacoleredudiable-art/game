namespace Dovus.Core.RuleEngineV4
{
    public sealed class KayitAlCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KayitAl;
        public float LookbackSec { get; init; }
    }
}
