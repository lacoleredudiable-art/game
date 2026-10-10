namespace Dovus.Core.RuleEngineV4
{
    public sealed class YansitCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.Yansit;
        public float Ratio { get; init; }
        public float DurationSec { get; init; }
    }
}
