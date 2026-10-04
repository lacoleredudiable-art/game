using Dovus.Game.Casting;
using TMPro;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// HUD'un tek tema kaynağı: font, palet, boyut ve juice zamanlamaları.
    /// <c>Resources/HudTheme.asset</c> varsa o okunur; yoksa bu varsayılanlar.
    /// "Önerilen" işaretli değerler uydurmadır (docs/durum.md his turu Faz 3).
    /// </summary>
    [CreateAssetMenu(menuName = "Dovus/UI/Hud Theme", fileName = "HudTheme")]
    public sealed class HudTheme : ScriptableObject
    {
        public const string FontResource = "Fonts/HudFont";
        public const string TmpFontResource = "Fonts/HudFont SDF";

        [Header("Palet")]
        public Color DamageTextColor = new(0.95f, 0.93f, 0.88f, 1f);
        public Color CritTextColor = new(0.95f, 0.88f, 0.55f, 1f);
        public Color HealColor = new(0.45f, 0.9f, 0.75f, 1f);
        public Color PlayerHitColor = new(0.95f, 0.28f, 0.22f, 1f);
        /// <summary>Gecikmeli hasar barı. Önerilen.</summary>
        public Color GhostColor = new(1f, 0.93f, 0.78f, 0.9f);
        public Color FlashColor = Color.white;
        /// <summary>Boss cast barı dolgusu. Önerilen.</summary>
        public Color CastBarColor = new(1f, 0.56f, 0.24f, 0.95f);
        public Color BannerColor = new(1f, 0.9f, 0.72f, 1f);
        public Color VictoryColor = new(1f, 0.86f, 0.4f, 1f);
        public Color DefeatColor = new(0.95f, 0.35f, 0.35f, 1f);
        public Color LowHpVignetteColor = new(0.55f, 0f, 0.04f, 1f);
        public Color DamageDirectionColor = new(1f, 0.25f, 0.2f, 1f);
        public Color OffscreenArrowColor = new(1f, 0.55f, 0.3f, 0.95f);
        public Color DisabledTint = new(0.42f, 0.42f, 0.46f, 1f);
        public Color PhaseNotchColor = new(1f, 1f, 1f, 0.75f);

        [Header("Combat chrome paleti")]
        [Tooltip("Alt savaş tepsisi ve kartların ortak koyu cam yüzeyi.")]
        public Color PanelColor = new(0.018f, 0.032f, 0.065f, 0.82f);
        public Color PanelSoftColor = new(0.035f, 0.065f, 0.11f, 0.62f);
        public Color PanelEdgeColor = new(0.35f, 0.78f, 1f, 0.22f);
        public Color RuneFaceTint = Color.white;
        public Color PrimaryTextColor = new(0.95f, 0.98f, 1f, 1f);
        public Color SecondaryTextColor = new(0.72f, 0.82f, 0.91f, 0.94f);
        /// <summary>Rün düğmesi üstündeki "PASİF" rozeti (bekleyen / etkin). Önerilen.</summary>
        public Color PassiveBadgeColor = new(0.02f, 0.05f, 0.09f, 0.88f);
        public Color PassiveBadgeActiveColor = new(0.20f, 0.62f, 0.44f, 0.95f);
        public Color ControlCaptionColor = new(0.72f, 0.84f, 0.94f, 0.92f);
        public Color SkillCompatibleColor = new(0.33f, 0.95f, 0.68f, 1f);
        public Color SkillMismatchColor = new(1f, 0.76f, 0.22f, 1f);
        public Color SkillNeutralColor = new(0.42f, 0.90f, 1f, 1f);
        public Color PlayerHpColor = new(0.86f, 0.22f, 0.31f, 0.98f);
        public Color PlayerManaColor = new(0.24f, 0.62f, 1f, 0.98f);
        public Color AllyHpColor = new(0.30f, 0.86f, 0.56f, 0.98f);
        public Color BossLowHpColor = new(1f, 0.23f, 0.16f, 1f);
        public Color CastUrgentColor = new(1f, 0.20f, 0.10f, 1f);
        public Color WeaponSwapAccentColor = new(1f, 0.80f, 0.34f, 1f);
        public Color CooldownOverlayColor = new(0.01f, 0.025f, 0.05f, 0.68f);

        [Header("Rün kimlik renkleri")]
        [Tooltip("Yalnız rim/vurgu; ikon yüzleri v6 fiil kimliğini taşır, element rengi değildir.")]
        public Color Rune1 = new(0.28f, 0.86f, 1f, 1f);
        public Color Rune2 = new(0.32f, 0.92f, 0.65f, 1f);
        public Color Rune3 = new(0.36f, 0.73f, 1f, 1f);
        public Color Rune4 = new(0.58f, 0.57f, 1f, 1f);
        public Color Rune5 = new(1f, 0.42f, 0.32f, 1f);
        public Color Rune6 = new(0.36f, 0.84f, 0.93f, 1f);
        public Color Rune7 = new(0.79f, 0.42f, 1f, 1f);
        public Color Rune8 = new(1f, 0.75f, 0.29f, 1f);
        public Color Rune9 = new(0.44f, 0.93f, 0.83f, 1f);
        public Color Rune10 = new(0.73f, 0.61f, 1f, 1f);
        public Color Rune11 = new(0.47f, 0.54f, 1f, 1f);
        public Color Rune12 = new(0.35f, 0.85f, 1f, 1f);

        [Header("Boyut (dp)")]
        public float BossNameDp = 18f;
        public float BossSubtitleDp = 11f;
        public float PlayerIdentityDp = 11f;
        public float BannerDp = 30f;
        public float CastLabelDp = 12f;
        public float CastBarHeightDp = 9f;
        public float CastBarWidthFrac = 0.72f;
        public float OffscreenArrowDp = 26f;
        public float OffscreenMarginDp = 34f;
        public float DamageDirectionDp = 120f;
        public float RuneTrayTitleDp = 10f;
        public float ControlCaptionDp = 10f;
        public float SkillTitleDp = 19f;
        public float SkillDetailDp = 11f;
        public float SkillAccentWidthDp = 6f;
        public float SkillCardPaddingDp = 12f;
        /// <summary>Pasif rozeti rün diskinin çapına oranla (genişlik, yükseklik). Önerilen.</summary>
        public float PassiveBadgeWidthFrac = 0.92f;
        public float PassiveBadgeHeightFrac = 0.30f;
        public float PanelShadowDp = 4f;
        public float PanelOutlineDp = 1f;
        public float TrayTitleInsetDp = 14f;
        public float TrayTitleTopDp = 5f;
        public float TrayAccentHeightDp = 2f;
        public float WeaponReserveIconDp = 22f;
        public float WeaponCaptionGapDp = 5f;
        public float WeaponCaptionHeightDp = 18f;
        public float BarInsetDp = 2f;
        public float BarLabelInsetDp = 8f;

        [Header("Bar juice (sn)")]
        /// <summary>Ghost bar düşmeden önce bekler. Önerilen.</summary>
        public float GhostHoldSec = 0.35f;
        /// <summary>Ghost bar erime hızı (bar oranı / sn). Önerilen.</summary>
        public float GhostDrainPerSec = 0.6f;
        public float FlashSec = 0.12f;
        public float FlashAlpha = 0.7f;
        public float HealFlashAlpha = 0.55f;
        /// <summary>Bu oranın altında oyuncu barı + vinyet nabız atar. Önerilen.</summary>
        public float LowHpFrac = 0.3f;
        public float LowHpPulseHz = 2.2f;
        public float LowHpPulseStrength = 0.45f;
        public float LowHpVignetteMaxAlpha = 0.42f;

        [Header("Banner / yön (sn)")]
        public float BannerHoldSec = 1.6f;
        public float BannerFadeSec = 0.4f;
        public float DamageDirectionSec = 0.6f;
        public float BannerPunchScale = 1.25f;
        public float CastPopScale = 1.08f;
        public float BossBarShakeDp = 4f;
        public float BossBarShakeSec = 0.35f;
        [Range(0f, 1f)] public float BossLowHpFrac = 0.18f;
        public float BossLowHpPulseHz = 3.2f;
        [Range(0f, 1f)] public float BossLowHpPulseStrength = 0.35f;
        [Range(0f, 1f)] public float CastUrgencyStart01 = 0.68f;
        public float CastUrgencyPulseHz = 6f;
        [Range(0f, 1f)] public float CastUrgencyPulseStrength = 0.42f;

        [Header("Altıgen buton juice")]
        public float PressScale = 0.88f;
        public float ReadyPopScale = 1.18f;
        public float JuiceSec = 0.14f;
        public float RuneHighlightSec = 0.42f;
        public float RuneHighlightScale = 1.10f;
        public float SkillCardPopScale = 1.08f;
        public float SkillCardFadeSec = 0.18f;

        [Header("Status ikonları")]
        /// <summary>Kalan süre bunun altına inince ikon yanıp söner. Önerilen.</summary>
        public float StatusBlinkUnderSec = 1.5f;
        public float StatusBlinkHz = 4f;

        static Font _font;
        static TMP_FontAsset _tmpFont;

        /// <summary>uGUI Text fontu (Türkçe glifli OFL); yoksa Unity yerleşik fontu.</summary>
        public static Font LegacyFont
        {
            get
            {
                if (_font == null)
                    _font = AssetLoader.Load<Font>(FontResource, null);
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>TMP font varlığı (editörde <c>Dovus/UI/Build HUD Font Asset</c>); yoksa TMP varsayılanı.</summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null)
                    _tmpFont = AssetLoader.Load<TMP_FontAsset>(TmpFontResource, null);
                if (_tmpFont == null)
                    _tmpFont = TMP_Settings.defaultFontAsset;
                return _tmpFont;
            }
        }

        public Color RuneAccent(int runeId) => runeId switch
        {
            1 => Rune1,
            2 => Rune2,
            3 => Rune3,
            4 => Rune4,
            5 => Rune5,
            6 => Rune6,
            7 => Rune7,
            8 => Rune8,
            9 => Rune9,
            10 => Rune10,
            11 => Rune11,
            12 => Rune12,
            _ => SkillNeutralColor
        };

        /// <summary>Kod-kurulu TMP etiketi (raycast kapalı, taşma serbest).</summary>
        public static TextMeshProUGUI CreateTmp(
            Transform parent, string name, float sizeDp, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (parent != null)
                go.layer = parent.gameObject.layer;
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = TmpFont;
            t.fontSize = HexagonLayoutScreen.DpToPixels(sizeDp);
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.outlineWidth = 0.18f;
            t.outlineColor = new Color32(0, 0, 0, 200);
            return t;
        }
    }
}
