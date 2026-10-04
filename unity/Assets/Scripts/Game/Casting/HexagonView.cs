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
    /// EnforceCooldown=false → kozmetik (yerel sayaç). true → PlayerCooldown / CooldownTracker.
    /// </summary>
    public sealed partial class HexagonView : MonoBehaviour
    {
        PrototypeTuning _tuning;
        SkillMotor _skills;
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
        PlayerCooldown _cdSource;
        GameClock _cdClock;
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
            PrototypeTuning tuning,
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
            _canvas.planeDistance = 1.2f;
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
                0.92f);
            _dodgeLabel = CreateLabel(_dodge, "DODGE");
            _dodgeLabel.fontSize = 15;
            _dodgeLabel.fontStyle = FontStyle.Bold;
            _dodgeLabel.color = Color.white;
            var dodgeOutline = _dodgeLabel.gameObject.AddComponent<Outline>();
            dodgeOutline.effectColor = new Color(0.15f, 0.05f, 0.35f, 0.85f);
            dodgeOutline.effectDistance = new Vector2(1.2f, -1.2f);

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
        public void BeginTrackedCooldown(int dot, string comboKey, float durationSec, PlayerCooldown source, GameClock clock)
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
            HudTheme th = HudTheme.Current;
            UiJuice.PunchScale(target, th.PressScale, th.JuiceSec);
            if (dot > 0 && _dotHighlightUntil != null && dot < _dotHighlightUntil.Length)
            {
                _dotHighlightUntil[dot] = Time.unscaledTime + th.RuneHighlightSec;
                UiJuice.PunchScale(target, th.RuneHighlightScale, th.JuiceSec * 1.35f);
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
                    HudTheme th = HudTheme.Current;
                    UiJuice.PunchScale(_dots[i], th.ReadyPopScale, th.JuiceSec * 1.5f);
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
                fill.color = HudTheme.Current.CooldownOverlayColor;
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
                return 22f;
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
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            _cdLabels[dot] = label;
        }

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
                    _dotImages[dot].color = IsDotUnavailable(dot) ? col * HudTheme.Current.DisabledTint : col;
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
            Color c = HudTheme.Current.RuneAccent(RuneIdAt(dot));
            c.a = _tuning.IsDotOpen(dot) ? 0.82f : 0.25f;
            return c;
        }

        Color RuneFallbackColor(int dot)
        {
            Color c = (dot & 1) == 0 ? _tuning.Visuals.InkPurple : _tuning.Visuals.InkCyan;
            c.a = _tuning.IsDotOpen(dot) ? 0.92f : 0.28f;
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
                float a = _tuning.IsDotOpen(dot) ? 1f : 0.28f;
                Color tint = HudTheme.Current.RuneFaceTint;
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
                float pulse = UiJuice.Pulse01(8f);
                Color bright = Color.Lerp(baseColor, Color.white, 0.55f + pulse * 0.25f);
                bright.a = 0.92f;
                _dotRims[dot].color = bright;
            }
            else
            {
                _dotRims[dot].color = baseColor;
            }
        }

        void BuildCombatTrayBackdrop(Transform parent)
        {
            HudTheme th = HudTheme.Current;
            var go = new GameObject("CombatRuneTray");
            go.transform.SetParent(parent, false);
            _tray = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.sprite = CreateRoundedRectSprite(_tuning.Input.CombatTrayCornerRadiusDp);
            image.type = Image.Type.Sliced;
            image.color = th.PanelSoftColor;
            image.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -HexagonLayoutScreen.DpToPixels(th.PanelShadowDp));
            var outline = go.AddComponent<Outline>();
            outline.effectColor = th.PanelEdgeColor;
            float outlinePx = HexagonLayoutScreen.DpToPixels(th.PanelOutlineDp);
            outline.effectDistance = new Vector2(outlinePx, -outlinePx);

            var edgeGo = new GameObject("AccentEdge");
            edgeGo.transform.SetParent(go.transform, false);
            _trayEdge = edgeGo.AddComponent<RectTransform>();
            _trayEdge.anchorMin = new Vector2(0f, 1f);
            _trayEdge.anchorMax = new Vector2(1f, 1f);
            _trayEdge.pivot = new Vector2(0.5f, 1f);
            _trayEdge.sizeDelta = new Vector2(0f, HexagonLayoutScreen.DpToPixels(th.TrayAccentHeightDp));
            var edge = edgeGo.AddComponent<Image>();
            edge.color = th.SkillNeutralColor;
            edge.raycastTarget = false;

            _trayTitle = CreateLabel(_tray, "RÜN ZİNCİRİ  ·  ÇİZ / BIRAK");
            _trayTitle.fontStyle = FontStyle.Bold;
            _trayTitle.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(th.RuneTrayTitleDp));
            _trayTitle.alignment = TextAnchor.UpperLeft;
            _trayTitle.color = th.ControlCaptionColor;
            _trayTitle.rectTransform.offsetMin = new Vector2(HexagonLayoutScreen.DpToPixels(th.TrayTitleInsetDp), 0f);
            _trayTitle.rectTransform.offsetMax = new Vector2(0f, -HexagonLayoutScreen.DpToPixels(th.TrayTitleTopDp));

            _trayLinks = new RectTransform[Dovus.Core.Input.HexagonLayout.DotCount];
            for (int i = 0; i < _trayLinks.Length; i++)
            {
                var linkGo = new GameObject("HexLink" + (i + 1));
                linkGo.transform.SetParent(parent, false);
                _trayLinks[i] = linkGo.AddComponent<RectTransform>();
                var link = linkGo.AddComponent<Image>();
                Color c = th.PanelEdgeColor;
                c.a *= 0.72f;
                link.color = c;
                link.raycastTarget = false;
            }
        }

        void LayoutCombatTray(Vector2 center, float dotRadius)
        {
            if (_tray == null)
                return;
            float padding = HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayPaddingDp);
            float header = HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayHeaderHeightDp);
            float radius = HexagonLayoutScreen.RadiusPx(_tuning);
            float width = (radius + dotRadius + padding) * 2f;
            float height = width + header;
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            float x = Mathf.Clamp(center.x, safe.xMin + width * 0.5f, safe.xMax - width * 0.5f);
            float y = Mathf.Clamp(center.y + header * 0.5f, safe.yMin + height * 0.5f, safe.yMax - height * 0.5f);
            _tray.anchorMin = Vector2.zero;
            _tray.anchorMax = Vector2.zero;
            _tray.pivot = new Vector2(0.5f, 0.5f);
            _tray.sizeDelta = new Vector2(width, height);
            _tray.anchoredPosition = new Vector2(x, y);
            _tray.SetAsFirstSibling();

            if (_trayLinks == null)
                return;
            for (int i = 0; i < _trayLinks.Length; i++)
            {
                Vector2 a = HexagonLayoutScreen.DotPx(i + 1, _tuning, Screen.width, Screen.height);
                Vector2 b = HexagonLayoutScreen.DotPx((i + 1) % _trayLinks.Length + 1, _tuning, Screen.width, Screen.height);
                LayoutLink(_trayLinks[i], a, b, HexagonLayoutScreen.DpToPixels(_tuning.Input.CombatTrayLinkWidthDp));
            }
        }

        static Sprite _roundedRectSprite;

        static Sprite CreateRoundedRectSprite(float cornerRadiusDp)
        {
            if (_roundedRectSprite != null)
                return _roundedRectSprite;
            const int size = 64;
            float radius = Mathf.Clamp(cornerRadiusDp, 4f, size * 0.45f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist + 0.5f)));
            }
            texture.Apply(false, true);
            int border = Mathf.RoundToInt(radius);
            _roundedRectSprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _roundedRectSprite;
        }

        static void LayoutLink(RectTransform rect, Vector2 from, Vector2 to, float width)
        {
            Vector2 delta = to - from;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(1f, width));
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        static void Place(RectTransform rt, Vector2 screenPx, float diameterPx, int screenW, int screenH)
        {
            // Overlay canvas: anchor bottom-left in pixel space via anchoredPosition with stretch off
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameterPx, diameterPx);
            rt.anchoredPosition = screenPx;
        }

        /// <summary>
        /// Gölge + rim (daire) + yüz — dodge/rünlerde düz diskten ayrışır.
        /// </summary>
        static RectTransform CreateLayeredDisc(
            string name,
            Sprite faceSprite,
            Sprite discSprite,
            Color color,
            Transform parent,
            out Image image,
            Color rimColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            var shadowRt = shadowGo.AddComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(3f, -5f);
            shadowRt.offsetMax = new Vector2(3f, -5f);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = discSprite;
            shadowImg.color = new Color(0f, 0f, 0f, 0.45f);
            shadowImg.raycastTarget = false;
            shadowImg.preserveAspect = true;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(go.transform, false);
            var rimRt = rimGo.AddComponent<RectTransform>();
            rimRt.anchorMin = Vector2.zero;
            rimRt.anchorMax = Vector2.one;
            rimRt.offsetMin = new Vector2(-3f, -3f);
            rimRt.offsetMax = new Vector2(3f, 3f);
            var rimImg = rimGo.AddComponent<Image>();
            rimImg.sprite = discSprite;
            rimImg.color = rimColor.a > 0.01f
                ? rimColor
                : new Color(1f, 1f, 1f, 0.28f);
            rimImg.raycastTarget = false;
            rimImg.preserveAspect = true;

            var faceGo = new GameObject("Face");
            faceGo.transform.SetParent(go.transform, false);
            var faceRt = faceGo.AddComponent<RectTransform>();
            faceRt.anchorMin = Vector2.zero;
            faceRt.anchorMax = Vector2.one;
            faceRt.offsetMin = new Vector2(2f, 2f);
            faceRt.offsetMax = new Vector2(-2f, -2f);
            image = faceGo.AddComponent<Image>();
            image.sprite = faceSprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rt;
        }

        static Text CreateLabel(RectTransform parent, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = HudTheme.LegacyFont;
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            t.raycastTarget = false;
            return t;
        }

        static int FirstLayer(int mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = (size - 1) * 0.5f;
            Vector2 c = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
