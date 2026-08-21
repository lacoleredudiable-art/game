using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T5 prototip kabuğu ayarları. Spec'te yürüme hızı yok; varsayılanlar durum.md'de kayıtlı.
    /// Sahnedeki tek örnek Bootstrap'ten paylaşılır; kopya tutulmaz (T10 canlı ayarı bunu bekliyor).
    /// </summary>
    [System.Serializable]
    public sealed class PrototypeTuning
    {
        [Header("Arena")]
        public float ArenaHalfSizeM = 12f;

        [Header("Oyuncu")]
        public float WalkSpeedMps = 4.5f;

        [Header("Sanal çubuk")]
        public float JoystickMaxRadiusDp = 72f;
        public float JoystickDeadZone = 0.12f;

        // Beşgen ekrana sabit (§2). Yarıçap/konum spec'te sayı yok — varsayılan; durum.md'ye geçildi.
        [Header("Beşgen (§2)")]
        public float PentagonCenterXNorm = 0.78f;
        public float PentagonCenterYNorm = 0.40f;
        public float PentagonRadiusDp = 100f;
        public float DotHitRadiusDp = 30f;
        public float CenterHitRadiusDp = 24f;
        public bool MirrorForLeftHand = false;
        public float InkLingerSec = 0.40f;
        public float InkWidthDp = 3.5f;

        // §4 ilk tur: yalnızca 1 (İĞNE), 2 (SÜRÜ), 5 (SARSINTI). Kombo tablosu değil — açık/kapalı bayrak.
        [Header("Açık rünler (§4)")]
        public bool OpenDot1 = true;
        public bool OpenDot2 = true;
        public bool OpenDot3 = false;
        public bool OpenDot4 = false;
        public bool OpenDot5 = true;

        // Spec'te sayı yok — ayrık onay tıkırtısı (§2); Handheld.Vibrate ~500 ms üst üste biniyordu.
        [Header("Dokunsal (§2)")]
        public long DotVibrationMs = 30;

        [Header("Kamera")]
        public float FollowSmoothTimeSec = 0.18f;
        public float LookAheadM = 1.4f;
        public Vector3 CameraOffset = new Vector3(0f, 7f, -6f);

        // Renkler dovus-sistemi.md §10'dan. Kırmızı-turuncu bossun TEHDİDİNE ayrılı olduğu için
        // bossun gövdesi nötr: telegraf (T8) yandığında kontrast kalsın.
        [Header("Renk dili (§10)")]
        public Color PlayerColor = new Color(0.373f, 0.941f, 1f);
        public Color BossColor = new Color(0.18f, 0.19f, 0.22f);
        public Color GroundColor = new Color(0.38f, 0.4f, 0.44f);
        public Color BackgroundColor = new Color(0.12f, 0.14f, 0.18f);
        public Color InkPurple = new Color(0.725f, 0.549f, 1f);   // #B98CFF
        public Color InkCyan = new Color(0.373f, 0.941f, 1f);     // #5FF0FF
        public Color PentagonDotColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        public Color PentagonCenterColor = new Color(0.75f, 0.78f, 0.85f, 0.9f);

        public bool IsDotOpen(int dot) => dot switch
        {
            1 => OpenDot1,
            2 => OpenDot2,
            3 => OpenDot3,
            4 => OpenDot4,
            5 => OpenDot5,
            _ => false
        };
    }
}
