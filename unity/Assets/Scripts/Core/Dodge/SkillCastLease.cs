namespace Dovus.Core.Dodge
{
    /// <summary>
    /// Süren skill'in konum kirası. Dodge kesince kalıp konumu aynı anda bırakır,
    /// kalan etkiler durur, harcanmış bekleme geri gelmez.
    /// </summary>
    public sealed class SkillCastLease
    {
        public bool OwnsPosition { get; private set; }
        public bool EffectsLive { get; private set; }
        public bool CooldownSpent { get; private set; }

        public void Arm(bool templateOwnsPosition)
        {
            OwnsPosition = templateOwnsPosition;
            EffectsLive = true;
            CooldownSpent = true;
        }

        public void ReleasePosition() => OwnsPosition = false;

        public void CancelForDodge()
        {
            OwnsPosition = false;
            EffectsLive = false;
        }
    }
}
