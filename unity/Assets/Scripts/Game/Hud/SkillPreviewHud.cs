using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// Binding sıra 7a: salt-okunur preview. Seçim/cast üretmez; canlı SentenceEngine ve
    /// SkillFactory sonucunu altıgenin hemen üstünde gösterir. Çizim yokken gizlenir;
    /// cast sonrası <see cref="GameTuning.SkillPreviewHoldSec"/> kadar kalır.
    /// </summary>
    public sealed class SkillPreviewHud : MonoBehaviour
    {
        SentenceEngine _engine;
        SkillMotor _motor;
        SkillFactory _factory;
        ManifestationDirector _manifestation;
        GameTuning _tuning;
        HudTheme _theme;
        RectTransform _rect;

        public void BindTheme(HudTheme theme) => _theme = theme;
        CanvasGroup _group;
        Text _title;
        Text _detail;
        Text _compatibility;
        Image _accent;
        string _lastSignature = string.Empty;
        float _visibleUntil = float.NegativeInfinity;
        Skill _lastCast;
        bool _wasVisible;

        public void Configure(
            SentenceEngine engine,
            SkillMotor motor,
            SkillFactory factory,
            ManifestationDirector manifestation,
            GameTuning tuning,
            Transform canvasRoot)
        {
            _engine = engine;
            _motor = motor;
            _factory = factory;
            _manifestation = manifestation;
            _tuning = tuning;

            var root = new GameObject("SkillPreviewReadOnly");
            root.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                root.layer = canvasRoot.gameObject.layer;
            _rect = root.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.zero;
            _rect.pivot = new Vector2(0.5f, 0f);

            _group = root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var bg = root.AddComponent<Image>();
            HudTheme theme = _theme;
            bg.color = theme.PanelColor;
            bg.raycastTarget = false;
            var shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(
                0f, -HexagonLayoutScreen.DpToPixels(theme.PanelShadowDp));
            var outline = root.AddComponent<Outline>();
            outline.effectColor = theme.PanelEdgeColor;
            float outlinePx = HexagonLayoutScreen.DpToPixels(theme.PanelOutlineDp);
            outline.effectDistance = new Vector2(outlinePx, -outlinePx);

            var accentGo = new GameObject("CompatibilityAccent");
            accentGo.transform.SetParent(root.transform, false);
            var accentRt = accentGo.AddComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(theme.SkillAccentWidthDp), 0f);
            _accent = accentGo.AddComponent<Image>();
            _accent.raycastTarget = false;

            _title = CreateText(root.transform, "Title", new Vector2(0f, 0.46f), new Vector2(0.72f, 1f), theme.SkillTitleDp);
            _title.alignment = TextAnchor.MiddleLeft;
            _title.fontStyle = FontStyle.Bold;
            _detail = CreateText(root.transform, "Detail", Vector2.zero, new Vector2(1f, 0.46f), theme.SkillDetailDp);
            _detail.alignment = TextAnchor.MiddleLeft;
            _compatibility = CreateText(root.transform, "Compatibility", new Vector2(0.72f, 0.48f), Vector2.one, theme.SkillDetailDp);
            _compatibility.alignment = TextAnchor.MiddleRight;
            _compatibility.fontStyle = FontStyle.Bold;
            Layout();
        }

        void LateUpdate()
        {
            if (_engine == null || _motor == null || _factory == null)
                return;

            Layout();

            SentenceState state = _engine.State;
            bool drawing = state.Phase == SentencePhase.Building && state.Words.Count > 0;
            Skill cast = _manifestation?.LastFactorySkill;
            bool newCast = cast != null && !ReferenceEquals(cast, _lastCast);
            _lastCast = cast;
            float hold = _tuning != null ? _tuning.Hud.SkillPreviewHoldSec : HudDefaults.SkillPreviewHoldSec;
            if (drawing || newCast)
                _visibleUntil = Time.unscaledTime + hold;
            HudTheme theme = _theme;
            bool visible = _visibleUntil > Time.unscaledTime;
            if (visible && !_wasVisible)
                UiJuice.PunchScale(_rect, theme.SkillCardPopScale, theme.JuiceSec * HudDefaults.SkillCardJuiceDurationMult);
            _wasVisible = visible;
            _group.alpha = Mathf.Clamp01(
                (_visibleUntil - Time.unscaledTime) / Mathf.Max(HudDefaults.BannerFadeMinSec, theme.SkillCardFadeSec));

            int elementId = _manifestation?.SelectedElementPaint?.Id ?? 0;
            string weaponId = _manifestation?.EquippedWeapon?.Id ?? string.Empty;
            string signature = state.Phase + ":" + state.Words.Count + ":" + elementId
                + ":weapon:" + weaponId;
            for (int i = 0; i < state.Words.Count; i++)
                signature += ":" + (int)state.Words[i].Rune;
            if (state.Words.Count == 0 && _manifestation?.LastFactorySkill != null)
                signature += ":last:" + _manifestation.LastFactorySkill.Id;
            if (signature == _lastSignature)
                return;
            _lastSignature = signature;

            if (state.Phase == SentencePhase.Building && state.Words.Count == 1)
            {
                int verbId = (int)state.Words[0].Rune;
                SkillResolution preview = _motor.Resolve(new[] { verbId });
                SetPreview(preview.VerbName, "İkinci rünü çiz: sıfat davranışı ve silüeti değiştirir.",
                    theme.SkillNeutralColor, "FİİL HAZIR");
                return;
            }

            Skill skill = null;
            if (state.Words.Count == 2)
            {
                try
                {
                    skill = _factory.CreateFromWords(
                        state.Words,
                        _manifestation?.EquippedWeapon,
                        elementId);
                }
                catch
                {
                    skill = null;
                }
            }
            skill ??= _manifestation?.LastFactorySkill;
            if (skill == null)
                return;

            Color color = skill.Weapon.Compatible ? theme.SkillCompatibleColor : theme.SkillMismatchColor;
            string compatibility = skill.Weapon.Compatible
                ? "UYUMLU"
                : SkillTextNumbers.FormatIncompatibleCompatibilityLabel(skill.Weapon.DamageMult);
            string prose = !string.IsNullOrEmpty(skill.Resolution.SkillJob)
                ? skill.Resolution.SkillJob
                : skill.Resolution.ProseFeel;
            string detail = skill.Weapon.Compatible
                ? prose
                : SkillTextNumbers.FormatIncompatibleSkillDetail(
                    prose, skill.Weapon.CastTimeMult, skill.Weapon.DamageMult);
            SetPreview(skill.DisplayName, detail, color, compatibility);
        }

        void Layout()
        {
            if (_rect == null || _tuning == null)
                return;

            Vector2 center = HexagonLayoutScreen.CenterPx(_tuning, Screen.width, Screen.height);
            float top = center.y
                + HexagonLayoutScreen.RadiusPx(_tuning)
                + HexagonLayoutScreen.DotHitRadiusPx(_tuning)
                + HexagonLayoutScreen.DpToPixels(_tuning.Hud.SkillPreviewGapDp);
            float width = HexagonLayoutScreen.DpToPixels(_tuning.Hud.SkillPreviewWidthDp);
            float height = HexagonLayoutScreen.DpToPixels(_tuning.Hud.SkillPreviewHeightDp);

            Rect safe = HexagonLayoutScreen.SafeRectPx();
            float half = width * 0.5f;
            float x = Mathf.Clamp(center.x, safe.xMin + half, safe.xMax - half);
            float y = Mathf.Min(top, safe.yMax - height);

            _rect.sizeDelta = new Vector2(width, height);
            _rect.anchoredPosition = new Vector2(x, y);
        }

        void SetPreview(string title, string detail, Color color, string compatibility)
        {
            if (_title == null || _detail == null || _compatibility == null)
                return;
            _title.text = title ?? string.Empty;
            _detail.text = detail ?? string.Empty;
            _compatibility.text = compatibility ?? string.Empty;
            _title.color = _theme.PrimaryTextColor;
            _detail.color = _theme.SecondaryTextColor;
            _compatibility.color = color;
            if (_accent != null)
                _accent.color = color;
        }

        Text CreateText(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSizeDp)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            float pad = HexagonLayoutScreen.DpToPixels(_theme.SkillCardPaddingDp);
            rect.offsetMin = new Vector2(pad, HexagonLayoutScreen.DpToPixels(2f));
            rect.offsetMax = new Vector2(-pad, -HexagonLayoutScreen.DpToPixels(2f));
            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont;
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            int fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(fontSizeDp));
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }
    }
}
