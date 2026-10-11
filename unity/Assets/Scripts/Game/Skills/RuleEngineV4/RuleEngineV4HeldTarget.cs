using Dovus.Game.Actors;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Basılı tutarak seçilen tek seferlik hedef; kalıcı kilidi (PlayerTargetingController.Selected) değiştirmez.</summary>
    public readonly struct RuleEngineV4HeldTarget
    {
        public enum Kind
        {
            /// <summary>Mevcut H kuralı: kilitli hedef, yoksa otomatik.</summary>
            Default,
            /// <summary>Kilit yok sayılır, otomatik hedef.</summary>
            Auto,
            /// <summary>Parmağın altındaki canlı hedef.</summary>
            Target,
            /// <summary>Parmağın altındaki MarkOnly yüzey (işaret koyan planlar için).</summary>
            Surface,
        }

        public RuleEngineV4HeldTarget(Kind mode, TargetableHost target = null, RuleEngineV4SurfaceAim surface = default)
        {
            Mode = mode;
            Target = target;
            SurfaceAim = surface;
        }

        public Kind Mode { get; }
        public TargetableHost Target { get; }
        public RuleEngineV4SurfaceAim SurfaceAim { get; }
    }
}
