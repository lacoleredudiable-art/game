namespace Dovus.Core.Combat
{
    /// <summary>
    /// Dodge, sert CC (sersem / donma / yere yıkılma) ve ölüm dışında her an serbesttir.
    /// Skill ve düz vuruş bu yüzden kesilir; casting kapısı dodge'u tutmaz.
    /// </summary>
    public static class DodgeCancelRules
    {
        public static bool HardLock(bool stun, bool freeze, bool knockdown) =>
            stun || freeze || knockdown;

        public static bool Allowed(bool dead, bool stun, bool freeze, bool knockdown) =>
            !dead && !HardLock(stun, freeze, knockdown);

        /// <summary>İzin varsa kalıbın konumunu hemen bırakır. İzin yoksa kira olduğu gibi kalır.</summary>
        public static bool TryCancel(SkillCastLease lease, bool dead, bool stun, bool freeze, bool knockdown)
        {
            if (lease == null || !Allowed(dead, stun, freeze, knockdown))
                return false;
            lease.CancelForDodge();
            return true;
        }
    }

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
