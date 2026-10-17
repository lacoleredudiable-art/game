namespace Dovus.Core.Boss
{
    /// <summary>
    /// Kül Ejderi (docs/tasarim/18-kul-ejderi.md) sayıları. Kimlik KE-x.y. Belgedeki sayıların çoğu ÖNERİ
    /// (ölçüm sınıfı): gri kutu denemesinde telefonda onaylanır ya da değişir. Süreler ms, hasar oyuncu
    /// azami canının yüzdesi (Normal zorluk), uzunluklar metre.
    /// "Türetilmiş" yazan satırlar belgedeki bir sayıdan hesaplanmıştır; belgede doğrudan yazmaz.
    /// </summary>
    public static class KulEjderiDefaults
    {
        // KE-3.3 faz eşikleri (can oranı)
        public const float Phase2HpRatio = 0.60f;
        public const float Phase3HpRatio = 0.25f;
        // KE-3.4 can katmanı sayısı (her biri canın %20'si)
        public const int HealthLayerCount = 5;

        // KE-5.3 uyanış sineması, ilk saldırı 2 s içinde
        public const int AwakenMs = 4000;
        public const int FirstAttackAfterAwakenMs = 2000;
        // KE-6.1 / KE-7.1 faz geçiş sineması
        public const int TransitionMs = 4000;
        // KE-7.12 ölüm: 3 s'de yığılır
        public const int DyingMs = 3000;

        // KE-4.2 saldırılar arası bekleme (açık kalma bittikten sonra)
        public const int AttackGapMinMs = 1500;
        public const int AttackGapMaxMs = 3000;
        // KE-4.3 duyurulu büyük saldırı: fazın 30. saniyesinde ilki, sonra 45-60 s'de bir
        public const int FirstAnnouncedAfterPhaseStartMs = 30000;
        public const int AnnouncedIntervalMinMs = 45000;
        public const int AnnouncedIntervalMaxMs = 60000;

        // Kontrol ile kesme (§0): sendeleme 1,5 s, sonra 45 s Dirençli
        public const int InterruptStaggerMs = 1500;
        public const int InterruptResistanceMs = 45000;
        // KE-8.1 denge çubuğu dolunca sendeleme 7 s, bütün bölgeler ×1,5
        public const int PoiseStaggerMs = 7000;
        public const float StaggerDamageMultiplier = 1.5f;
        // KE-5.12 takım görevi başarısı: 4 s sendeleme
        public const int TeamTaskStaggerMs = 4000;

        // D-7 (Mustafa, 16 Ekim: B): iki "uzun" saldırının vuruş anları arası en az dodge hak dolumu kadar.
        // Dolum süresi DodgeTuning'in varsayılanından gelir (6 s, O-4.7); "uzun" tanımı belgede yok:
        // etkin süresi bu eşikten uzun olan saldırı uzun sayılır (tahmin, ölçümle ayarlanır).
        public const int LongAttackActiveMs = 2000;
        public static readonly int DodgeRechargeMs = new Tuning.DodgeTuning().ChargeRechargeMs;

        // KE-3.7 öfke: dövüşün 765. saniyesi; açık kalma yarıya, küçük tehdit aralığı 2 s
        public const int EnrageAtMs = 765000;
        public const float EnrageRecoveryFactor = 0.5f;
        public const int EnrageSmallThreatIntervalMs = 2000;

        // Tek Tick çağrısında en çok kaç durum adımı (çok uzun karelerde sonsuz döngüye karşı)
        public const int MaxStepsPerTick = 16;

        // KE-6.6 Kanat Savuruşu en az 20 s arayla
        public const int WingSweepMinGapMs = 20000;

        // KE-2.7 baş dönüşü (bakış yönü): en çok 60°, en çok 30 °/s
        public const float HeadYawLimitDeg = 60f;
        public const float HeadTurnDegPerSec = 30f;

        // Faz 1 (KE-5.4 - KE-5.7)
        public const int ClawSlamTelegraphMs = 1000;
        public const int ClawSlamActiveMs = 200;
        public const int ClawSlamRecoveryMs = 1200;
        public const float ClawSlamDamagePct = 25f;
        public const float ClawSlamRadiusM = 4f;
        public const float ClawSlamReachM = 10f;
        public const float ClawSlamWeight = 45f;

        public const int HeadSweepTelegraphMs = 1200;
        public const int HeadSweepActiveMs = 300;
        public const int HeadSweepRecoveryMs = 1000;
        public const float HeadSweepDamagePct = 30f;
        public const float HeadSweepRadiusM = 8f;
        public const float HeadSweepArcDeg = 150f;
        public const float HeadSweepKnockbackM = 4f;
        public const float HeadSweepWeight = 30f;

        public const int EmberBreathTelegraphMs = 1500;
        public const int EmberBreathActiveMs = 2000;
        public const int EmberBreathRecoveryMs = 1500;
        public const int EmberBreathTickMs = 250;
        public const float EmberBreathTickDamagePct = 8f;
        public const float EmberBreathMaxDamagePct = 64f;
        public const float EmberBreathConeDeg = 40f;
        public const float EmberBreathRangeM = 14f;
        public const float EmberBreathWeight = 25f;
        public const float EmberBreathHeadTurnDegPerSec = 15f;

        public const int EmberWaveTelegraphMs = 3000;
        public const int EmberWaveActiveMs = 3200;   // türetilmiş: halka 8 m/s ile güney kenara ~3,2 s
        public const int EmberWaveRecoveryMs = 2000;
        public const float EmberWaveDamagePct = 45f;
        public const float EmberWaveSpeedMps = 8f;
        public const float EmberWaveThicknessM = 1.5f;

        // Faz 2 (KE-6.4 - KE-6.7)
        public const int ClawSlamHighTelegraphMs = 1200;
        public const int ClawSlamHighActiveMs = 200;
        public const int ClawSlamHighRecoveryMs = 1200;
        public const float ClawSlamHighDamagePct = 30f;
        public const float ClawSlamHighRadiusM = 5f;
        public const float ClawSlamHighReachM = 11f;
        public const float ClawSlamHighWeight = 35f;

        public const int SkyEmberBreathTelegraphMs = 2000;
        public const int SkyEmberBreathActiveMs = 3000;
        public const int SkyEmberBreathRecoveryMs = 1500;
        public const int SkyEmberBreathTickMs = 250;
        public const float SkyEmberBreathTickDamagePct = 10f;
        public const float SkyEmberBreathStripWidthM = 4f;
        public const float SkyEmberBreathSweepSpeedMps = 6f;
        public const float SkyEmberBreathWeight = 35f;

        public const int WingSweepTelegraphMs = 2000;
        public const int WingSweepActiveMs = 400;
        public const int WingSweepRecoveryMs = 1500;
        public const float WingSweepDamagePct = 50f;
        public const float WingSweepKnockbackM = 6f;
        public const float WingSweepSideEdgeX = 4f;
        public const float WingSweepWeight = 30f;

        public const int SkyCollapseTelegraphMs = 3000;
        public const int SkyCollapseRockCount = 8;
        public const int SkyCollapseRockIntervalMs = 600;
        public const int SkyCollapseRockTelegraphMs = 1200;
        // türetilmiş: son kaya (7 aralık) + kendi ön işareti
        public const int SkyCollapseActiveMs = 5400;
        public const int SkyCollapseRecoveryMs = 2000;
        public const float SkyCollapseDamagePct = 40f;
        public const float SkyCollapseRadiusM = 3f;

        // Faz 3 (KE-7.2 - KE-7.5)
        public const int ClawSweepTelegraphMs = 1600;
        public const int ClawSweepActiveMs = 400;
        public const int ClawSweepRecoveryMs = 1500;
        public const float ClawSweepDamagePct = 35f;
        public const float ClawSweepRadiusM = 10f;
        public const float ClawSweepArcDeg = 180f;
        public const float ClawSweepKnockbackM = 5f;
        public const float ClawSweepWeight = 40f;

        public const int EmberRingTelegraphMs = 1400;
        public const int EmberRingActiveMs = 1500;   // türetilmiş: 12 m / 8 m/s
        public const int EmberRingRecoveryMs = 1200;
        public const float EmberRingDamagePct = 30f;
        public const float EmberRingSpeedMps = 8f;
        public const float EmberRingMaxRadiusM = 12f;
        public const float EmberRingThicknessM = 1.5f;
        public const float EmberRingWeight = 30f;

        public const int WideEmberBreathTelegraphMs = 1800;
        public const int WideEmberBreathActiveMs = 2500;
        public const int WideEmberBreathRecoveryMs = 1500;
        public const int WideEmberBreathTickMs = 250;
        public const float WideEmberBreathTickDamagePct = 8f;
        public const float WideEmberBreathConeDeg = 70f;
        public const float WideEmberBreathRangeM = 12f;
        public const float WideEmberBreathHeadTurnDegPerSec = 10f;
        public const float WideEmberBreathWeight = 30f;

        public const int CraterHeartTelegraphMs = 3000;
        public const int CraterHeartWaveGapMs = 1500;
        public const int CraterHeartActiveMs = 3000; // türetilmiş: iki dalga x 1,5 s
        public const int CraterHeartRecoveryMs = 2000;
        public const float CraterHeartDamagePct = 45f;
        public const float CraterHeartInnerRadiusM = 8f;
        public const float CraterHeartArenaRadiusM = 18f;

        // KE-4.1 küçük tehditler
        public const int SmallThreatIntervalMinMs = 3000;
        public const int SmallThreatIntervalMaxMs = 4000;
        public const int SmallThreatKindCount = 3;

        public const int CrackFlameTelegraphMs = 1000;
        public const float CrackFlameDamagePct = 15f;
        public const float CrackFlameWidthM = 1.5f;
        public const float CrackFlameLengthM = 4f;
        public const float CrackFlameMaxDistM = 6f;

        public const int FallingAshTelegraphMs = 1200;
        public const float FallingAshDamagePct = 12f;
        public const float FallingAshRadiusM = 1.5f;

        public const int TremblingGroundTelegraphMs = 900;
        public const float TremblingGroundDamagePct = 10f;
        public const float TremblingGroundRadiusM = 3f;
        public const float TremblingGroundMinNearM = 2f;
        public const float TremblingGroundMaxNearM = 4f;

        // KE-2.8 - KE-2.11 vurulabilir bölge çarpanları
        public const float HeadMultiplier = 1.0f;
        public const float ClawMultiplier = 0.6f;
        public const float ChestMultiplier = 1.0f;
        public const float HeartOpenMultiplier = 2.0f;
        // KE-2.8 faz 2'de baş 15 m yüksekte: menzil >= 14 m silahlar ulaşır
        public const float Phase2HeadMinWeaponRangeM = 14f;
    }
}
