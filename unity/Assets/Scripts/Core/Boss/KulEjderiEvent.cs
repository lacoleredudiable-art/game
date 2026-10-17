namespace Dovus.Core.Boss
{
    /// <summary>Beyin çıktısı: sunum katmanı (ses, kamera, görünüm) bu olayları okur.</summary>
    public readonly struct KulEjderiEvent
    {
        public KulEjderiEvent(KulEjderiEventKind kind, double worldMs, KulEjderiPhase phase, KulEjderiAttackId? attack)
        {
            Kind = kind;
            WorldMs = worldMs;
            Phase = phase;
            Attack = attack;
        }

        public KulEjderiEventKind Kind { get; }
        public double WorldMs { get; }
        public KulEjderiPhase Phase { get; }
        /// <summary>Saldırıyla ilgili olaylarda saldırı kimliği; yoksa null.</summary>
        public KulEjderiAttackId? Attack { get; }
    }
}
