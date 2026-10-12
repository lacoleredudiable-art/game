namespace Dovus.Core.RuleEngineV4
{
    /// <summary>"gozcu" yoksa geçici değerler; nihai ayar kural-motoru-v4.json.</summary>
    public static class RuleEngineV4GozcuDefaults
    {
        /// <summary>Görev: en alçak ayak kemiği zeminin 3 cm altına inemez.</summary>
        public const float FootBelowGroundMaxM = 0.03f;
        /// <summary>Görev: gövde statik collider'a en çok 5 cm gömülür.</summary>
        public const float BodyPenetrationMaxM = 0.05f;
        /// <summary>10-olculer O-5.6: kamera açısal hızı en çok 120°/s.</summary>
        public const float CameraAngularSpeedMaxDps = 120f;
        /// <summary>10-olculer O-9.1: tek efekt ekranın en çok %25'ini kaplar.</summary>
        public const float EffectScreenCoverMaxFrac = 0.25f;
        public const string FallbackLogTag = "[V4-FALLBACK]";
        public const int FallbackLogMax = 0;
        /// <summary>Görev: aynı kural saniyede en çok bir kez yazılır.</summary>
        public const float LogIntervalSec = 1f;
        /// <summary>spec'te yok: kırmızı satır ekranda kalma süresi.</summary>
        public const float LineShowSec = 3f;
        /// <summary>spec'te yok: klip "oynuyor" sayılan en düşük ağırlık (PlayMode testleriyle aynı).</summary>
        public const float ClipWeightMin = 0.5f;
        /// <summary>spec'te yok: kamera "collider içinde" yoklama küresi.</summary>
        public const float CameraProbeRadiusM = 0.1f;
        /// <summary>Görev: zemin yalnız arena zemini (Y = 0); sabit yapı, duvar, koza zemin sayılmaz.</summary>
        public const float ArenaFloorYM = 0f;
        /// <summary>Görev: ana kamera FOV'u bunun altındaysa (0 dahil) kamera_fov_gecersiz; ekran ölçümü yapılmaz.</summary>
        public const float CameraFovMinDeg = 10f;
    }
}
