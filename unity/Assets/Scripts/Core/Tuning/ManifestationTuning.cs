namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Tezahür (yaşayan etki) sayıları. Spec §8'de hız/yarıçap yok — varsayılanlar;
    /// telefonda T11 his turunda ayarlanacak. Değişiklik durum.md'ye geçilir.
    /// </summary>
    public class ManifestationTuning
    {
        // Seyahat — fiil dünyada yaşasın diye (T2)
        public float WaveSpeedMps = 9f;
        public float WaveMaxRadiusM = 9f;
        public float NeedleSpeedMps = 16f;
        public float NeedleMaxRangeM = 12f;
        public float SwarmSpeedMps = 6.5f;
        public float SwarmMaxRadiusM = 7f;

        // Silüet eksenleri — sıfat sayıyı değil şekli değiştirir (T3)
        public float FocusPerIgne = 0.78f;
        public float PiercePerIgne = 0.28f;
        public float SpreadPerSuru = 0.85f;
        public float LiftPerSarsinti = 0.45f;
        public float DwellStackScale = 0.35f;

        // Görünür toplama: hedef silüete saniyede ne kadar yaklaşır
        public float MorphLerpPerSec = 3.5f;

        // Temas / kapanış (hasar sayısı YOK — yalnızca fiziksel tepki)
        public float TravelHitRadiusM = 1.15f;
        public float ClosingBangRadiusM = 3.6f;
        public float BossKnockbackM = 1.35f;
        public float BossLiftM = 1.1f;
        public float BossShakeSec = 0.28f;

        // Kalıcı iz boyutu
        public float ScarScaleM = 1.4f;
        public int MaxSwarmBlobs = 8;
    }
}
