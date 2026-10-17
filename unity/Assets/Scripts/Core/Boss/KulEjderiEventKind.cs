namespace Dovus.Core.Boss
{
    public enum KulEjderiEventKind
    {
        Awakened,
        TelegraphStarted,
        /// <summary>Etkin süre başladı: vuruş bu karede çözülür.</summary>
        Struck,
        RecoveryStarted,
        /// <summary>Ön işaret sırasında Kontrol ile kesildi.</summary>
        Interrupted,
        /// <summary>Saldırı sendeleme/faz geçişi/ölümle iptal oldu; ön işaretler silinir.</summary>
        Cancelled,
        StaggerStarted,
        StaggerEnded,
        /// <summary>KE-7.8 Kor Mührü başarısı: plakalar açılır, ejderha çöker.</summary>
        CollapseStarted,
        CollapseEnded,
        TransitionStarted,
        PhaseChanged,
        Enraged,
        Dying,
        Died
    }
}
