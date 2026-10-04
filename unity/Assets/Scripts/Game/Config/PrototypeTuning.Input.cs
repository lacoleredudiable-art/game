using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        [Header("Sanal çubuk")]
        public float JoystickMaxRadiusDp = 72f;
        public float JoystickDeadZone = 0.12f;

        // Altıgen ekrana sabit (§2). Yarıçap/konum spec'te sayı yok — varsayılan; durum.md'ye geçildi.
        // T6.2: merkez X, dodge düğmesi sağ kenardan taşmasın diye 0.78'den içeri alındı.
        [Header("Altıgen (§2)")]
        /// <summary>
        /// Ekranın kısa kenarı bu dp'den azsa bütün savaş HUD'u orantılı küçülür (0 = kapalı).
        /// Yatay telefonda kısa kenar ~440 dp; tam ölçekte altıgen tepsisi yüksekliğin %84'ünü
        /// kaplıyordu (29 Eyl telefon). Önerilen.
        /// </summary>
        public float HudFitShortSideDp = 600f;
        // Sağ-alt köşeye yaslı: boss ekran ortasında görünür kalsın.
        public float HexagonCenterXNorm = 0.78f;
        // Sağ-alt; FittedRadiusPx alt rünleri safe içinde tutar.
        public float HexagonCenterYNorm = 0.30f;
        // Komşu rün kenar boşluğu ≈ radius − 2·dotR (≥40dp çizim koridoru).
        public float HexagonRadiusDp = 112f;
        public float DotHitRadiusDp = 34f;
        public float CenterHitRadiusDp = 38f;
        public bool MirrorForLeftHand = false;
        public float InkLingerSec = 0.40f;
        public float InkWidthDp = 3.5f;
        /// <summary>Ham çizim izi genişlik çarpanı (InkWidthDp tabanı). spec'te yok — varsayılan</summary>
        public float InkRawWidthScale = 0.9f;
        /// <summary>Ham iz baş ucunda cyan→beyaz parlaklık (1 = tam). spec'te yok — varsayılan</summary>
        public float InkRawGlow = 1f;

        // §2: dodge hex dışı (sağ-alt); offset ek kaydırma (varsayılan 0).
        [Header("Dodge düğmesi (§2, T6.2)")]
        public float DodgeButtonOffsetXDp = 0f;
        public float DodgeButtonOffsetYDp = 0f;
        public float DodgeButtonRadiusDp = 40f;
        public float DodgeButtonScreenMarginDp = 10f;
        /// <summary>Hex kenarı ile dodge yüzeyi arası (dp).</summary>
        public float DodgeClearanceDp = 40f;

        // weapon_skill_interaction.swap: dodge'un altıgene göre simetriği (sol-alt).
        [Header("Silah swap düğmesi (v6 swap)")]
        public float WeaponSwapButtonRadiusDp = 36f;
        public float WeaponSwapButtonOffsetXDp = 0f;
        public float WeaponSwapButtonOffsetYDp = 0f;
        [Range(0.4f, 1f), Tooltip("Swap yüzü içindeki silah ikonunun çap oranı.")]
        public float WeaponSwapIconScale = 0.72f;

        [Header("Lock-on düğmesi (mobil)")]
        public float LockOnButtonRadiusDp = 36f;
        public float LockOnButtonOffsetXDp = 0f;
        public float LockOnButtonOffsetYDp = 52f;
        public Color LockOnButtonColor = new Color(0.55f, 0.72f, 0.88f, 0.9f);
        public Color LockOnButtonActiveColor = new Color(0.95f, 0.82f, 0.45f, 0.95f);

        [Header("Element radial (v6.1.1)")]
        /// <summary>JSON yalnız 6 konum/300 ms verir; ekran yarıçapı için prototip varsayılanı.</summary>
        public float ElementMenuRadiusDp = 92f;
        public float ElementMenuChipWidthDp = 144f;
        public float ElementMenuChipHeightDp = 48f;
        public float ElementMenuItemWidthDp = 84f;
        public float ElementMenuItemHeightDp = 48f;
        // Sağ başparmak: altıgen tepsisinin üstü (sol yarı hareket çubuğunun).
        [Range(0f, 1f)] public float ElementMenuAnchorXNorm = 0.86f;
        [Range(0f, 1f)] public float ElementMenuAnchorYNorm = 0.72f;

        // §5: merkez bir kelime değil düğme; hangi fiille vurduğu veridir (prototipte 1/Ateş).
        [Header("Düz vuruş (§5, T6.2)")]
        public int BasicStrikeDot = 1;

        [Header("Altıgen ikon")]
        // 1.0 — görsel disk hit'ten şişmesin, komşu boşluğu yemesin.
        public float IconDisplayScale = 1.0f;
        [Tooltip("Altıgeni gruplayan cam tepsinin rune halkasına ek boşluğu.")]
        public float CombatTrayPaddingDp = 22f;
        public float CombatTrayHeaderHeightDp = 22f;
        public float CombatTrayCornerRadiusDp = 20f;
        public float CombatTrayLinkWidthDp = 1.5f;

        // Spec'te sayı yok — ayrık onay tıkırtısı (§2); Handheld.Vibrate ~500 ms üst üste biniyordu.
        [Header("Dokunsal (§2)")]
        public long DotVibrationMs = 30;
    }
}
