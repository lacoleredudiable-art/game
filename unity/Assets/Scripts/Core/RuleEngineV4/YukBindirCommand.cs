namespace Dovus.Core.RuleEngineV4
{
    public sealed class YukBindirCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YukBindir;
        public float WaitSec { get; init; }
    }
}
