namespace Dovus.Core.RuleEngineV4
{
    public sealed class YapiKurCommand : PhysicsCommand
    {
        public override PhysicsCommandKind Kind => PhysicsCommandKind.YapiKur;
        public float LifeSec { get; init; }
    }
}
