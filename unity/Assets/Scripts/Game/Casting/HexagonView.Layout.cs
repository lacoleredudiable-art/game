using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonView
    {
        void Layout()
        {
            int w = Screen.width;
            int h = Screen.height;
            float dotR = HexagonLayoutScreen.DotHitRadiusPx(_tuning);
            float centerR = HexagonLayoutScreen.CenterHitRadiusPx(_tuning);
            int n = Dovus.Core.Input.HexagonLayout.DotCount;

            for (int dot = 1; dot <= n; dot++)
            {
                Vector2 px = HexagonLayoutScreen.DotPx(dot, _tuning, w, h);
                float mul = _dotIcons != null && _dotIcons[dot] != null
                    ? Mathf.Max(0.5f, _tuning.Input.IconDisplayScale)
                    : 1f;
                float diam = dotR * 2f * mul;
                Place(_dots[dot], px, diam, w, h);
                if (_dotLabels != null && _dotLabels[dot] != null)
                    _dotLabels[dot].fontSize = Mathf.RoundToInt(diam * 0.30f);
                if (_dotImages[dot] != null)
                {
                    Color col = _dotIcons != null && _dotIcons[dot] != null
                        ? DotColor(dot)
                        : RuneFallbackColor(dot);
                    _dotImages[dot].color = IsDotUnavailable(dot) ? col * _theme.DisabledTint : col;
                }
                RefreshDotRim(dot);

                if (_cdRings != null && _cdRings[dot] != null)
                    Place(_cdRings[dot], px, diam * 1.05f, w, h);
                if (_cdLabels != null && _cdLabels[dot] != null)
                {
                    var lrt = _cdLabels[dot].rectTransform;
                    Place(lrt, px, diam * 0.9f, w, h);
                }
            }

            Vector2 c = HexagonLayoutScreen.CenterPx(_tuning, w, h);
            Place(_center, c, centerR * 2f, w, h);
            if (_centerLabel != null)
                _centerLabel.fontSize = Mathf.RoundToInt(centerR * 0.95f);

            Vector2 d = HexagonLayoutScreen.DodgeButtonPx(_tuning, w, h);
            float dodgeR = HexagonLayoutScreen.DodgeButtonRadiusPx(_tuning);
            Place(_dodge, d, dodgeR * 2f, w, h);
            if (_dodgeLabel != null)
                _dodgeLabel.fontSize = Mathf.RoundToInt(dodgeR * 0.38f);
            _dodge.SetAsLastSibling();
            LayoutWeaponSwapButton(w, h);
            LayoutLockOnButton(w, h);
            RefreshLockOnVisual();
            LayoutCombatTray(c, dotR);
        }

        Color RimColorForDot(int dot)
        {
            Color c = _theme.RuneAccent(RuneIdAt(dot));
            c.a = _tuning.IsDotOpen(dot) ? HexagonViewDefaults.RimOpenAlpha : HexagonViewDefaults.RimClosedAlpha;
            return c;
        }

        Color RuneFallbackColor(int dot)
        {
            Color c = (dot & 1) == 0 ? _tuning.Visuals.InkPurple : _tuning.Visuals.InkCyan;
            c.a = _tuning.IsDotOpen(dot) ? HexagonViewDefaults.FallbackOpenAlpha : HexagonViewDefaults.FallbackClosedAlpha;
            return c;
        }

        string DotGlyph(int dot)
        {
            int runeId = RuneIdAt(dot);
            string name = _skills != null ? _skills.RuneName(runeId) : RuneInfo.DisplayName((Rune)runeId);
            if (string.IsNullOrEmpty(name))
                return runeId.ToString();
            string compact = name.Replace("İ", "I").Replace("ı", "i");
            return compact.Length <= 2 ? compact.ToUpperInvariant() : compact.Substring(0, 2).ToUpperInvariant();
        }

        Color DotColor(int dot)
        {
            if (_dotIcons != null && _dotIcons[dot] != null)
            {
                float a = _tuning.IsDotOpen(dot) ? 1f : HexagonViewDefaults.DotClosedAlpha;
                Color tint = _theme.RuneFaceTint;
                tint.a *= a;
                return tint;
            }

            Color c = _tuning.Visuals.HexagonDotColor;
            if (_tuning.IsDotOpen(dot))
                return c;
            return new Color(c.r, c.g, c.b, c.a * 0.28f);
        }

        int RuneIdAt(int dot) =>
            _loadout != null ? _loadout.RuneIdAtSlot(dot) : dot;

        void ApplyDotIdentity(int dot)
        {
            if (_dotImages == null || dot <= 0 || dot >= _dotImages.Length)
                return;
            _dotIcons[dot] = RuneIconCatalog.Get(RuneIdAt(dot));
            Image face = _dotImages[dot];
            if (face != null)
            {
                face.sprite = _dotIcons[dot] != null ? _dotIcons[dot] : CreateCircleSprite();
                face.color = _dotIcons[dot] != null ? DotColor(dot) : RuneFallbackColor(dot);
            }
            if (_dotLabels != null && _dotLabels[dot] != null)
            {
                _dotLabels[dot].enabled = _dotIcons[dot] == null;
                _dotLabels[dot].text = DotGlyph(dot);
            }
            RefreshDotRim(dot);
        }

        void RefreshDotRim(int dot)
        {
            if (_dotRims == null || dot <= 0 || dot >= _dotRims.Length || _dotRims[dot] == null)
                return;
            Color baseColor = RimColorForDot(dot);
            bool hot = _dotHighlightUntil != null && Time.unscaledTime < _dotHighlightUntil[dot];
            if (hot)
            {
                float pulse = UiJuice.Pulse01(HexagonViewDefaults.RimPulseHz);
                Color bright = Color.Lerp(baseColor, Color.white, HexagonViewDefaults.RimPulseLerpBase + pulse * HexagonViewDefaults.RimPulseLerpAmp);
                bright.a = HexagonViewDefaults.RimHotAlpha;
                _dotRims[dot].color = bright;
            }
            else
            {
                _dotRims[dot].color = baseColor;
            }
        }
    }
}
