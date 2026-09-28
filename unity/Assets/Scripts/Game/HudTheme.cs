using TMPro;
using UnityEngine;

namespace Dovus.Game
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

        [Header("Boyut (dp)")]
        public float BossNameDp = 15f;
        public float BossSubtitleDp = 10f;
        public float BannerDp = 30f;
        public float CastLabelDp = 11f;
        public float CastBarHeightDp = 7f;
        public float CastBarWidthFrac = 0.62f;
        public float OffscreenArrowDp = 26f;
        public float OffscreenMarginDp = 34f;
        public float DamageDirectionDp = 120f;

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

        [Header("Altıgen buton juice")]
        public float PressScale = 0.88f;
        public float ReadyPopScale = 1.18f;
        public float JuiceSec = 0.14f;

        [Header("Status ikonları")]
        /// <summary>Kalan süre bunun altına inince ikon yanıp söner. Önerilen.</summary>
        public float StatusBlinkUnderSec = 1.5f;
        public float StatusBlinkHz = 4f;

        static HudTheme _current;
        static Font _font;
        static TMP_FontAsset _tmpFont;

        public static HudTheme Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<HudTheme>("HudTheme");
                    if (_current == null)
                    {
                        _current = CreateInstance<HudTheme>();
                        _current.hideFlags = HideFlags.DontSave;
                    }
                }
                return _current;
            }
        }

        /// <summary>uGUI Text fontu (Türkçe glifli OFL); yoksa Unity yerleşik fontu.</summary>
        public static Font LegacyFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.Load<Font>(FontResource);
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
                    _tmpFont = Resources.Load<TMP_FontAsset>(TmpFontResource);
                if (_tmpFont == null)
                    _tmpFont = TMP_Settings.defaultFontAsset;
                return _tmpFont;
            }
        }

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
