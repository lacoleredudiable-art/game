using Dovus.Core.Equipment;
using Dovus.Game.Composition;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// ui_rules.swap_indicator: weapon_icons — aktif silah ikonu, yedek rozeti ve radial bekleme.
    /// </summary>
    public sealed partial class HexagonView
    {
        RectTransform _swap;
        Image _swapFace;
        Image _swapFill;
        Image _swapReserveIcon;
        Text _swapActive;
        Text _swapReserve;
        ManifestationDirector _swapSource;
        GameClockHost _swapClock;
        bool _wasSwapping;

        public void BindWeaponSwap(ManifestationDirector source, GameClockHost clock)
        {
            _swapSource = source;
            _swapClock = clock;
            RefreshWeaponSwapButton();
        }

        void BuildWeaponSwapButton(Sprite disc, Transform parent)
        {
            _swap = CreateLayeredDisc(
                "WeaponSwapButton", disc, disc, _theme.PanelColor, parent,
                out _swapFace, new Color(0.95f, 0.8f, 0.45f, 0.88f));

            var reserveGo = new GameObject("ReserveWeaponIcon");
            reserveGo.transform.SetParent(_swap, false);
            var reserveRt = reserveGo.AddComponent<RectTransform>();
            reserveRt.anchorMin = reserveRt.anchorMax = new Vector2(0.14f, 0.12f);
            reserveRt.pivot = new Vector2(0.5f, 0.5f);
            reserveRt.sizeDelta = new Vector2(
                HexagonLayoutScreen.DpToPixels(_theme.WeaponReserveIconDp),
                HexagonLayoutScreen.DpToPixels(_theme.WeaponReserveIconDp));
            _swapReserveIcon = reserveGo.AddComponent<Image>();
            _swapReserveIcon.preserveAspect = true;
            _swapReserveIcon.raycastTarget = false;

            var fillGo = new GameObject("SwapCooldown");
            fillGo.transform.SetParent(_swap, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _swapFill = fillGo.AddComponent<Image>();
            _swapFill.sprite = disc;
            _swapFill.type = Image.Type.Filled;
            _swapFill.fillMethod = Image.FillMethod.Radial360;
            _swapFill.fillOrigin = (int)Image.Origin360.Top;
            _swapFill.fillClockwise = false;
            _swapFill.color = _theme.CooldownOverlayColor;
            _swapFill.raycastTarget = false;
            _swapFill.fillAmount = 0f;

            _swapActive = CreateLabel(_swap, "—");
            _swapActive.fontSize = 12;
            _swapActive.fontStyle = FontStyle.Bold;
            _swapActive.color = Color.white;
            _swapActive.rectTransform.offsetMin = new Vector2(0f, 6f);
            var outline = _swapActive.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            _swapReserve = CreateLabel(_swap, string.Empty);
            _swapReserve.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(_theme.ControlCaptionDp));
            _swapReserve.color = _theme.ControlCaptionColor;
            _swapReserve.alignment = TextAnchor.UpperCenter;
            _swapReserve.rectTransform.anchorMin = new Vector2(0f, 0f);
            _swapReserve.rectTransform.anchorMax = new Vector2(1f, 0f);
            _swapReserve.rectTransform.pivot = new Vector2(0.5f, 1f);
            _swapReserve.rectTransform.anchoredPosition = new Vector2(
                0f, -HexagonLayoutScreen.DpToPixels(_theme.WeaponCaptionGapDp));
            _swapReserve.rectTransform.sizeDelta = new Vector2(
                0f, HexagonLayoutScreen.DpToPixels(_theme.WeaponCaptionHeightDp));

            _swap.SetAsLastSibling();
        }

        void LayoutWeaponSwapButton(int w, int h)
        {
            if (_swap == null)
                return;
            Vector2 p = HexagonLayoutScreen.WeaponSwapButtonPx(_tuning, w, h);
            float diameter = HexagonLayoutScreen.WeaponSwapButtonRadiusPx(_tuning) * 2f;
            Place(_swap, p, diameter, w, h);
            if (_swapFace != null)
            {
                float inset = diameter * (1f - Mathf.Clamp01(_tuning.Input.WeaponSwapIconScale)) * 0.5f;
                _swapFace.rectTransform.offsetMin = new Vector2(inset, inset);
                _swapFace.rectTransform.offsetMax = new Vector2(-inset, -inset);
            }
        }

        void RefreshWeaponSwapButton()
        {
            if (_swap == null)
                return;
            WeaponSwapState swap = _swapSource != null ? _swapSource.WeaponSwap : null;
            bool visible = swap != null && swap.Reserve != null;
            _swap.gameObject.SetActive(visible);
            if (!visible)
                return;

            double worldMs = _swapClock != null ? _swapClock.Director.WorldTimeMs : 0;
            Sprite activeIcon = WeaponIconCatalog.Get(swap.Active);
            Sprite reserveIcon = WeaponIconCatalog.Get(swap.Reserve);
            _swapFace.sprite = activeIcon != null ? activeIcon : CreateCircleSprite();
            _swapFace.color = activeIcon != null ? _theme.RuneFaceTint : _theme.PanelColor;
            _swapActive.text = activeIcon != null ? string.Empty : ShortName(swap.Active?.Name);
            _swapReserve.text = "SWAP  ·  " + ShortName(swap.Reserve?.Name);
            _swapReserveIcon.sprite = reserveIcon;
            _swapReserveIcon.enabled = reserveIcon != null;
            _swapFill.fillAmount = swap.Cooldown01(worldMs);
            if (activeIcon != null && swap.IsSwapping)
                _swapFace.color = _theme.WeaponSwapAccentColor;

            if (_centerFace != null)
            {
                _centerFace.sprite = activeIcon != null ? activeIcon : CreateCircleSprite();
                _centerFace.color = activeIcon != null ? _theme.RuneFaceTint : _tuning.Visuals.HexagonCenterColor;
            }
            if (_centerLabel != null)
                _centerLabel.enabled = activeIcon == null;

            if (swap.IsSwapping && !_wasSwapping)
                UiJuice.PunchScale(_swap, _theme.ReadyPopScale, _theme.JuiceSec);
            _wasSwapping = swap.IsSwapping;
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "—";
            return name.Length <= 7 ? name : name.Substring(0, 6) + ".";
        }
    }
}
