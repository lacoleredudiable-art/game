namespace Dovus.Game.Skills.State
{
    /// <summary>Mekanik JSON / buff zamanlayıcıları — tek kopya (ManifestationDirector tutar).</summary>
    public sealed class SkillWorldState
    {
        public float SelfDamageBuff { get; set; }
        public double SelfDamageBuffUntilMs { get; set; }
        public int LastStatusTransferMoved { get; set; }
        public bool LastFriendlyWasAlly { get; set; }
        public double TasarShieldUntilMs { get; set; }
        public float OverflowNextHitBonus { get; set; }
        public double LastBasicStrikeMs { get; set; } = -1;
        public bool JsonTickDamage { get; set; }
        public string CardEffect { get; set; } = string.Empty;
    }
}
