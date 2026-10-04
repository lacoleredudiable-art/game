namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionPresentation
    {
        public SkillResolutionPresentation(
            VerbFamilyWire verbFamily,
            SkillActionWire action,
            HitboxWire hitbox,
            AnimationTypeWire animationType,
            string silhouetteAxis)
        {
            VerbFamily = verbFamily;
            Action = action;
            Hitbox = hitbox;
            AnimationType = animationType;
            SilhouetteAxis = silhouetteAxis ?? string.Empty;
        }

        public VerbFamilyWire VerbFamily { get; }
        public SkillActionWire Action { get; }
        public HitboxWire Hitbox { get; }
        public AnimationTypeWire AnimationType { get; }
        public string SilhouetteAxis { get; }
    }
}
