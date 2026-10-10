namespace Dovus.Core.RuleEngineV4
{
    public sealed class KendiniTasiCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.KendiniTasi;
        public float MaxDistanceM { get; init; }
        public float SpeedMps { get; init; }
        public bool SkillWhileMoving { get; init; }
    }
}
