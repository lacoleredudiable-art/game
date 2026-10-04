namespace Dovus.Game.Diagnostics
{
    /// <summary><see cref="IDebugFlags"/> varsayılanları DebugConfig ile aynı.</summary>
    public sealed class DebugFlags : IDebugFlags
    {
        public bool DevHp { get; set; } = true;
        public bool HalfHpStart { get; set; } = false;

        public bool DevHpActive => DebugConfig.Enabled && DevHp && !HalfHpStart;

        public float StartHpRatio => DebugConfig.Enabled && HalfHpStart ? 0.5f : 1f;
    }
}
