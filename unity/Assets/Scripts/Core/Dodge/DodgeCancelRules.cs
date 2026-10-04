namespace Dovus.Core.Dodge
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
    }
}
