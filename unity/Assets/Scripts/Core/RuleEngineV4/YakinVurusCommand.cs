namespace Dovus.Core.RuleEngineV4
{
    public sealed class YakinVurusCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YakinVurus;
        public float RangeM { get; init; }
        public int HitParts { get; init; }
    }
}
