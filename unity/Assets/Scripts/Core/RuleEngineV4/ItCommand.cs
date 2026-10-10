namespace Dovus.Core.RuleEngineV4
{
    public sealed class ItCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.It;
        public float DistanceM { get; init; }
        public bool ClipToWeaponRange { get; init; }
    }
}
