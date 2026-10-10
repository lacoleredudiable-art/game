namespace Dovus.Core.RuleEngineV4
{
    public sealed class IsaretKoyCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.IsaretKoy;
        public float PlaceRangeM { get; init; }
        public float LifeSec { get; init; }
    }
}
