namespace Dovus.Core.Boss
{
    /// <summary>Bir boss saldırısının veri kaydı (18-kul-ejderi.md §5-§7). Değerler <see cref="KulEjderiDefaults"/>'tan.</summary>
    public sealed class KulEjderiAttackSpec
    {
        public KulEjderiAttackSpec(
            KulEjderiAttackId id,
            string designId,
            KulEjderiPhase phase,
            float weight,
            int telegraphMs,
            int activeMs,
            int recoveryMs,
            float damagePct,
            KulEjderiShape shape)
        {
            Id = id;
            DesignId = designId;
            Phase = phase;
            Weight = weight;
            TelegraphMs = telegraphMs;
            ActiveMs = activeMs;
            RecoveryMs = recoveryMs;
            DamagePct = damagePct;
            Shape = shape;
        }

        public KulEjderiAttackId Id { get; }
        /// <summary>Tasarım belgesi kimliği (ör. KE-5.4): PR ve test adında yazılır.</summary>
        public string DesignId { get; }
        public KulEjderiPhase Phase { get; }
        /// <summary>Normal havuz ağırlığı; duyurulu büyük saldırıda 0 (ağırlıkla seçilmez, KE-4.3).</summary>
        public float Weight { get; }
        public int TelegraphMs { get; }
        public int ActiveMs { get; }
        /// <summary>Açık kalma (vurma penceresi).</summary>
        public int RecoveryMs { get; }
        /// <summary>Vuruş (ya da periyodik vuruş başına) hasar, oyuncu azami canının yüzdesi.</summary>
        public float DamagePct { get; }
        public KulEjderiShape Shape { get; }
        public float RadiusM { get; set; }
        public float AngleDeg { get; set; }
        public float KnockbackM { get; set; }
        /// <summary>0 = tek vuruş; &gt;0 = etkin süre boyunca bu aralıkla tekrar (nefes saldırıları).</summary>
        public int TickMs { get; set; }
        /// <summary>Kontrol ile kesilebilir mi (§0).</summary>
        public bool Interruptible { get; set; }
        /// <summary>Yalnız menzilli Kontrol ulaşır (KE-6.5).</summary>
        public bool InterruptNeedsRange { get; set; }
        /// <summary>Duyurulu büyük saldırı: yazı + geri sayım, her zaman kesilemez (KE-4.3).</summary>
        public bool Announced { get; set; }
        /// <summary>Aynı saldırının iki kullanımı arasındaki en kısa süre; 0 = sınırsız (KE-6.6).</summary>
        public int MinRepeatGapMs { get; set; }
    }
}
