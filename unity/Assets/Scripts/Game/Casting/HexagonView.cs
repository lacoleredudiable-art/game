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
    /// <summary>
    /// Ekrana sabit altıgen noktaları + merkez (Canvas Overlay).
    /// ui_rules.cooldown_display: her rün etrafında radial dolum + kalan sn.
    /// EnforceCooldown=false → kozmetik (yerel sayaç). true → PlayerCooldownHost / CooldownTracker.
    /// </summary>
    public sealed partial class HexagonView : MonoBehaviour
    {
        HudTheme _theme;
        GameTuning _tuning;
        SkillMotor _skills;

        public void BindTheme(HudTheme theme) => _theme = theme;
        RuneLoadout _loadout;
        RectTransform[] _dots;
        Image[] _dotImages;
        Image[] _dotRims;
        Text[] _dotLabels;
        Sprite[] _dotIcons;
        float[] _dotHighlightUntil;
        RectTransform[] _cdRings;
        Image[] _cdFills;
        Text[] _cdLabels;
        float[] _cdRemainingSec;
        float[] _cdDurationSec;
        string[] _cdComboKeys;
        bool[] _cdTracked;
        PlayerCooldownHost _cdSource;
        GameClockHost _cdClock;
        RectTransform _center;
        RectTransform _dodge;
        Image _centerFace;
        Text _centerLabel;
        Text _dodgeLabel;
        RectTransform _tray;
        RectTransform _trayEdge;
        Text _trayTitle;
        RectTransform[] _trayLinks;
        Canvas _canvas;

        public Canvas Canvas => _canvas;
        public Transform CanvasRoot => _canvas != null ? _canvas.transform : null;
        public RectTransform DodgeButtonRect => _dodge;

        public void Build(
            GameTuning tuning,
            Camera overlayCam,
            SkillMotor skills = null,
            RuneLoadout loadout = null)
        {
            _tuning = tuning;
            _skills = skills;
            _loadout = loadout ?? skills?.DefaultLoadout ?? RuneLoadout.Sequential;

            var canvasGo = new GameObject("HexagonCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            // Overlay canvas her kameranın üstüne biner ve telegrafı ezer (§10).
            // Tek mekanizma: Overlay kamera + Screen Space Camera.
            _canvas.renderMode = overlayCam != null
                ? RenderMode.ScreenSpaceCamera
                : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = overlayCam;
            _canvas.planeDistance = HexagonViewDefaults.CanvasPlaneDistanceM;
            _canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var fallback = CreateCircleSprite();
            // Solid disc — radial fillAmount ile klasik cooldown pie (halka sprite fill'de silik kalıyordu).
            var ringSprite = fallback;
            BuildCombatTrayBackdrop(canvasGo.transform);
            int n = Dovus.Core.Input.HexagonLayout.DotCount;
            _dots = new RectTransform[n + 1];
            _dotImages = new Image[n + 1];
            _dotRims = new Image[n + 1];
            _dotLabels = new Text[n + 1];
            _dotIcons = new Sprite[n + 1];
            _dotHighlightUntil = new float[n + 1];
            _cdRings = new RectTransform[n + 1];
            _cdFills = new Image[n + 1];
            _cdLabels = new Text[n + 1];
            _cdRemainingSec = new float[n + 1];
            _cdDurationSec = new float[n + 1];
            _cdComboKeys = new string[n + 1];
            _cdTracked = new bool[n + 1];
            for (int dot = 1; dot <= n; dot++)
            {
                int runeId = RuneIdAt(dot);
                _dotIcons[dot] = RuneIconCatalog.Get(runeId);
                Sprite icon = _dotIcons[dot] != null ? _dotIcons[dot] : fallback;
                Color col = _dotIcons[dot] != null ? DotColor(dot) : RuneFallbackColor(dot);
                _dots[dot] = CreateLayeredDisc(
                    $"Dot{dot}", icon, fallback, col, canvasGo.transform, out _dotImages[dot],
                    RimColorForDot(dot));
                _dotRims[dot] = _dots[dot].Find("Rim")?.GetComponent<Image>();
                var label = CreateLabel(_dots[dot], DotGlyph(dot));
                _dotLabels[dot] = label;
                label.enabled = _dotIcons[dot] == null;
                label.fontSize = 14;
                label.color = new Color(0.92f, 0.97f, 1f, 0.98f);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // Radial örtü ikon ÜSTÜNDE (klasik pie); sn sayısı en üstte.
            for (int dot = 1; dot <= n; dot++)
                CreateCooldownOverlay(dot, ringSprite, canvasGo.transform, underDots: false);

            _center = CreateLayeredDisc(
                "Center", fallback, fallback, _tuning.Visuals.HexagonCenterColor, canvasGo.transform, out _centerFace,
                new Color(0.85f, 0.98f, 1f, 0.55f));
            _centerLabel = CreateLabel(_center, "⚔");
            _centerLabel.fontSize = 28;
            _centerLabel.color = new Color(0.05f, 0.1f, 0.14f, 0.95f);

            _dodge = CreateLayeredDisc(
                "DodgeButton", fallback, fallback, _tuning.Visuals.DodgeButtonColor, canvasGo.transform, out Image dodgeImg,
                new Color(0.55f, 0.95f, 1f, 0.7f));
            dodgeImg.color = new Color(
                _tuning.Visuals.DodgeButtonColor.r,
                _tuning.Visuals.DodgeButtonColor.g,
                _tuning.Visuals.DodgeButtonColor.b,
                HexagonViewDefaults.DodgeFaceAlpha);
            _dodgeLabel = CreateLabel(_dodge, "DODGE");
            _dodgeLabel.fontSize = 15;
            _dodgeLabel.fontStyle = FontStyle.Bold;
            _dodgeLabel.color = Color.white;
            var dodgeOutline = _dodgeLabel.gameObject.AddComponent<Outline>();
            dodgeOutline.effectColor = new Color(0.15f, 0.05f, 0.35f, 0.85f);
            dodgeOutline.effectDistance = new Vector2(HexagonViewDefaults.DodgeOutlineOffsetPx, -HexagonViewDefaults.DodgeOutlineOffsetPx);

            // Dodge her zaman rünlerin üstünde (görsel katman + dokunma okunurluğu).
            _dodge.SetAsLastSibling();

            BuildWeaponSwapButton(fallback, canvasGo.transform);
            BuildLockOnButton(fallback, canvasGo.transform);

            for (int dot = 1; dot <= n; dot++)
                CreateCooldownLabel(dot, canvasGo.transform);

            BuildDrawCaption(canvasGo.transform);

            if (overlayCam != null)
                SetLayerRecursively(canvasGo, FirstLayer(overlayCam.cullingMask));

            Layout();
            RefreshCooldownVisuals();
        }

        public void SetLoadout(RuneLoadout loadout)
        {
            if (loadout == null)
                return;
            _loadout = loadout;
            if (_dotLabels == null)
                return;
            for (int dot = 1; dot < _dotLabels.Length; dot++)
                ApplyDotIdentity(dot);
        }
    }
}
