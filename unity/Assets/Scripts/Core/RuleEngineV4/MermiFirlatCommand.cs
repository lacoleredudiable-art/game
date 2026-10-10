namespace Dovus.Core.RuleEngineV4
{
    public sealed class MermiFirlatCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.MermiFirlat;
        public float RangeM { get; init; }
        public float SpeedMps { get; init; }
    }
}
