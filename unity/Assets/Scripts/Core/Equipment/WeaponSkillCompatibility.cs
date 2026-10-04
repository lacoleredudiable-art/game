namespace Dovus.Core.Equipment
{
    public readonly struct WeaponSkillCompatibility
    {
        public WeaponSkillCompatibility(
            bool compatible,
            float damageMult,
            float castTimeMult,
            float poiseMult,
            bool passiveEnabled,
            string uiColor,
            string uiLabel)
        {
            Compatible = compatible;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            CastTimeMult = castTimeMult > 0f ? castTimeMult : 1f;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
            PassiveEnabled = passiveEnabled;
            UiColor = uiColor ?? string.Empty;
            UiLabel = uiLabel ?? string.Empty;
        }

        public static WeaponSkillCompatibility Neutral { get; } =
            new WeaponSkillCompatibility(true, 1f, 1f, 1f, true, string.Empty, string.Empty);

        public bool Compatible { get; }
        public float DamageMult { get; }
        public float CastTimeMult { get; }
        public float PoiseMult { get; }
        public bool PassiveEnabled { get; }
        public string UiColor { get; }
        public string UiLabel { get; }
    }
}
