namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionScaling
    {
        public SkillResolutionScaling(float damageMult, float hitboxScaleMult, float poiseDamageMult)
        {
            DamageMult = damageMult;
            HitboxScaleMult = hitboxScaleMult;
            PoiseDamageMult = poiseDamageMult;
        }

        public float DamageMult { get; }
        public float HitboxScaleMult { get; }
        public float PoiseDamageMult { get; }
    }
}
