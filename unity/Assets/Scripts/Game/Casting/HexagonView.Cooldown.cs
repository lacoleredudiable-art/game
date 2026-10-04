using Dovus.Core.Input;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonView
    {
        /// <summary>
        /// Kozmetik soğuma — cast'i engellemez. ui_rules.cooldown_display (radial_overlay + sayı).
        /// EnforceCooldown=false yolunda ManifestationDirector bunu çağırır.
        /// </summary>
        public void BeginCosmeticCooldown(int dot, float durationSec)
        {
            if (_cdRemainingSec == null || dot < 1 || dot >= _cdRemainingSec.Length)
                return;
            if (durationSec <= 0f)
                return;

            _cdTracked[dot] = false;
            _cdComboKeys[dot] = null;
            _cdDurationSec[dot] = durationSec;
            _cdRemainingSec[dot] = durationSec;
            Layout();
            RefreshCooldownVisuals();
        }

        /// <summary>
        /// Bağlama 4: gerçek CooldownTracker kalanı — radial fillAmount = rem/duration.
        /// </summary>
        public void BeginTrackedCooldown(int dot, string comboKey, float durationSec, PlayerCooldownHost source, GameClockHost clock)
        {
            if (_cdRemainingSec == null || dot < 1 || dot >= _cdRemainingSec.Length)
                return;
            if (string.IsNullOrEmpty(comboKey) || durationSec <= 0f || source == null)
                return;

            _cdSource = source;
            _cdClock = clock;
            _cdTracked[dot] = true;
            _cdComboKeys[dot] = comboKey;
            _cdDurationSec[dot] = durationSec;
            double worldMs = clock != null ? clock.Director.WorldTimeMs : 0;
            _cdRemainingSec[dot] = source.ComboRemainingSec(comboKey, worldMs);
            Layout();
            RefreshCooldownVisuals();
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;

            Layout();
            TickCooldowns();
            RefreshWeaponSwapButton();
            LayoutDrawCaption(Screen.width, Screen.height);
            TickDrawCaption();
        }

        /// <summary>Kapalı rün ya da soğumada: gri ton.</summary>
        bool IsDotUnavailable(int dot) =>
            !_tuning.IsDotOpen(dot)
            || (_cdRemainingSec != null && dot < _cdRemainingSec.Length && _cdRemainingSec[dot] > 0f);

        /// <summary>Dokunuş kabul edildi: basınca küçülme (0 = merkez).</summary>
        public void NotifyPressed(int dot)
        {
            RectTransform target = dot == 0 ? _center : (_dots != null && dot > 0 && dot < _dots.Length ? _dots[dot] : null);
            HudTheme th = _theme;
            _uiJuice?.PunchScale(target, th.PressScale, th.JuiceSec);
            if (dot > 0 && _dotHighlightUntil != null && dot < _dotHighlightUntil.Length)
            {
                _dotHighlightUntil[dot] = Time.unscaledTime + th.RuneHighlightSec;
                _uiJuice?.PunchScale(target, th.RuneHighlightScale, th.JuiceSec * HexagonViewDefaults.RuneHighlightJuiceMult);
            }
        }

        void TickCooldowns()
        {
            if (_cdRemainingSec == null)
                return;

            float dt = Time.unscaledDeltaTime;
            double worldMs = _cdClock != null ? _cdClock.Director.WorldTimeMs : 0;
            bool any = false;
            for (int i = 1; i < _cdRemainingSec.Length; i++)
            {
                bool wasCooling = _cdRemainingSec[i] > 0f;
                TickCooldown(i, dt, worldMs, ref any);
                if (wasCooling && _cdRemainingSec[i] <= 0f && _dots != null && _dots[i] != null)
                {
                    HudTheme th = _theme;
                    _uiJuice?.PunchScale(_dots[i], th.ReadyPopScale, th.JuiceSec * HexagonViewDefaults.ReadyPopJuiceMult);
                }
            }

            if (any || (_cdFills != null && AnyCooldownVisible()))
                RefreshCooldownVisuals();
        }

        void TickCooldown(int i, float dt, double worldMs, ref bool any)
        {
            if (_cdTracked != null && _cdTracked[i] && _cdSource != null && !string.IsNullOrEmpty(_cdComboKeys[i]))
            {
                float rem = _cdSource.ComboRemainingSec(_cdComboKeys[i], worldMs);
                if (!Mathf.Approximately(rem, _cdRemainingSec[i]))
                    any = true;
                _cdRemainingSec[i] = rem;
                if (rem <= 0f)
                {
                    _cdTracked[i] = false;
                    _cdComboKeys[i] = null;
                }
                return;
            }

            if (_cdRemainingSec[i] <= 0f)
                return;
            _cdRemainingSec[i] = Mathf.Max(0f, _cdRemainingSec[i] - dt);
            any = true;
        }

        bool AnyCooldownVisible()
        {
            for (int i = 1; i < _cdFills.Length; i++)
            {
                if (_cdFills[i] != null && _cdFills[i].enabled)
                    return true;
            }

            return false;
        }

        void RefreshCooldownVisuals()
        {
            if (_cdFills == null || _tuning == null)
                return;

            Color accent = _tuning.Visuals.InkCyan;
            for (int dot = 1; dot < _cdFills.Length; dot++)
            {
                float rem = _cdRemainingSec[dot];
                float dur = _cdDurationSec[dot];
                bool active = rem > 0f && dur > 0f;
                Image fill = _cdFills[dot];
                Text label = _cdLabels[dot];
                if (fill == null)
                    continue;

                fill.enabled = active;
                if (label != null)
                    label.enabled = active;

                if (!active)
                    continue;

                fill.fillAmount = Mathf.Clamp01(rem / dur);
                // Koyu radial örtü — ikon üstünde net okunur.
                fill.color = _theme.CooldownOverlayColor;
                fill.SetAllDirty();
                if (label != null)
                {
                    int shown = Mathf.Max(1, Mathf.CeilToInt(rem));
                    label.text = shown.ToString();
                    label.color = new Color(accent.r, accent.g, accent.b, 1f);
                    label.fontSize = Mathf.Max(22, Mathf.RoundToInt(DotFontPx(dot)));
                    label.SetAllDirty();
                }
            }
        }

        float DotFontPx(int dot)
        {
            if (_dots == null || _dots[dot] == null)
                return HexagonViewDefaults.CooldownLabelMinFontPx;
            return _dots[dot].sizeDelta.x * 0.42f;
        }

        void CreateCooldownOverlay(int dot, Sprite ringSprite, Transform parent, bool underDots)
        {
            var ringGo = new GameObject($"CooldownRing{dot}");
            ringGo.transform.SetParent(parent, false);
            if (underDots)
                ringGo.transform.SetSiblingIndex(0);
            _cdRings[dot] = ringGo.AddComponent<RectTransform>();
            var fill = ringGo.AddComponent<Image>();
            fill.sprite = ringSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.raycastTarget = false;
            fill.fillAmount = 0f;
            fill.enabled = false;
            _cdFills[dot] = fill;
        }

        void CreateCooldownLabel(int dot, Transform parent)
        {
            var labelGo = new GameObject($"CooldownLabel{dot}");
            labelGo.transform.SetParent(parent, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.zero;
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            var label = labelGo.AddComponent<Text>();
            label.font = HudTheme.LegacyFont;
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.enabled = false;
            var outline = labelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(HexagonViewDefaults.LabelOutlineOffsetPx, -HexagonViewDefaults.LabelOutlineOffsetPx);
            _cdLabels[dot] = label;
        }
    }
}
