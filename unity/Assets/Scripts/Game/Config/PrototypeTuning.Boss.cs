using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        // Telegraf silüeti: sayı değil poz. Spec §11 pozu tarif ediyor, oran vermiyor.
        [Header("Boss telegrafı (T8.1, §11)")]
        public float TelegraphStretch = 0.28f;
        public float TelegraphSquash = 0.18f;
        public float TelegraphSlamSquash = 0.22f;
        public float TelegraphTonePitchMin = 0.55f;
        public float TelegraphTonePitchMax = 1.8f;
        public float TelegraphToneVolumeMin = 0.12f;
        public float TelegraphToneVolumeMax = 0.40f;
        // Bossun oyuncuya yaklaşırken bıraktığı boşluk. Etki yarıçapı (5.4 m) ile birlikte
        // hangi derecelerin erişilebilir olduğunu BU sayı belirliyor — bkz. durum.md T8.1.
        public float BossApproachStopPadM = 0.35f;
        // Değer aynı, yeri BossReactor'dan taşındı.
        [Header("Boss tepki fiziği (T7.2, BossReactor)")]
        public float BossGravityMps2 = 22f;
        public float BossRecoilEaseDecayPerSec = 3.2f;
        public float BossShakeAmpBaseM = 0.12f;
        public float BossShakeAmpPerKnockbackM = 0.05f;
        public float BossPinShakeAmpM = 0.04f;
        public float BossLiftVelocityPerM = 4.5f;

        [Header("Boss karşılaşma")]
        /// <summary>Resources/Bosses/{id}.json — varsayılan Ağların Kraliçesi; karadul yedek.</summary>
        public string ActiveBossId = "aglarin_kralicesi";

        /// <summary><c>Resources.Load</c> yolu: <c>Bosses/</c> + id (<c>_</c> → <c>-</c>).</summary>
        public string ActiveBossResourcePath =>
            "Bosses/" + (string.IsNullOrWhiteSpace(ActiveBossId) ? "karadul" : ActiveBossId.Replace('_', '-'));

        // Boss animasyon sunumu (his turu Faz 1). Hepsi önerilen — klip değişince göz kontrolü.
        [Header("Boss animasyon (his turu)")]
        public float BossTurnRateDegPerSec = 240f;
        /// <summary>Saldırı klibinde darbe karesinin normalize zamanı; windup sonuna hizalanır.</summary>
        public float BossSlamImpactNorm = 0.42f;
        public float BossConeImpactNorm = 0.40f;
        public float BossStaggerMinGapSec = 0.6f;
        public float BossAnimCrossFadeSec = 0.15f;
        /// <summary>Yürüme klibinin kök hızı bulunamazsa (in-place klip) ölçek-1 adım hızı.</summary>
        public float BossWalkClipMps = 1.4f;

        // Boss ölümündeki çökme pozu; süre mevcut his varsayılanıdır.
        [Header("Boss ölüm pozu (T12, §11)")]
        public float BossDeathCollapseSec = 0.85f;
        public float BossDeathSquashY = 0.28f;
        public float BossDeathSpreadXz = 1.35f;
    }
}
