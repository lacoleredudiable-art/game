namespace Dovus.Core.Boss
{
    /// <summary>Kül Ejderi durum makinesinin anlık durumu.</summary>
    public enum KulEjderiState
    {
        /// <summary>KE-5.1: uyuyor, saldırmaz.</summary>
        Sleeping,
        /// <summary>KE-5.3: uyanış sineması, hasar yok.</summary>
        Awakening,
        /// <summary>Saldırılar arası bekleme (KE-4.2).</summary>
        Idle,
        /// <summary>Ön işaret süresi; kesilebilir saldırı burada kesilir.</summary>
        Telegraph,
        /// <summary>Etkin süre: vuruş çözülür.</summary>
        Active,
        /// <summary>Açık kalma: boss savunmasız pencere.</summary>
        Recovery,
        /// <summary>Kesilme, denge ya da görev başarısı sendelemesi.</summary>
        Staggered,
        /// <summary>KE-6.1 / KE-7.1 faz geçiş sineması, hasar yok.</summary>
        Transition,
        /// <summary>KE-7.12: yığılma, 3 s.</summary>
        Dying,
        Dead
    }
}
