namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Gözcü kural anahtarları: log satırındaki ilk sözcük (veri sözcüğü, çevrilmez).</summary>
    public static class GozcuRule
    {
        public const string FootBelowGround = "ayak_zemin_alti";
        public const string BodyInCollider = "govde_collider_icinde";
        public const string WrongClip = "uyumsuz_klip";
        public const string Fallback = "v4_fallback";
        public const string CameraAngularSpeed = "kamera_aci_hizi";
        public const string CameraInCollider = "kamera_collider_icinde";
        public const string PlayerOffScreen = "oyuncu_ekran_disi";
        public const string EffectScreenCover = "efekt_ekran_kaplama";
        public const string CameraFovInvalid = "kamera_fov_gecersiz";
    }
}
