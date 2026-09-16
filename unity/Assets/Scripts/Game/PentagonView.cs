using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Ekrana sabit altıgen noktaları + merkez (Canvas Overlay).
    /// ui_rules.cooldown_display: her rün etrafında radial dolum + kalan sn.
    /// EnforceCooldown=false → kozmetik (yerel sayaç). true → PlayerCooldown / CooldownTracker.
    /// </summary>
    public sealed class PentagonView : MonoBehaviour
    {
        PrototypeTuning _tuning;
        RectTransform[] _dots;
        Image[] _dotImages;
        Sprite[] _dotIcons;
        RectTransform[] _cdRings;
        Image[] _cdFills;
        Text[] _cdLabels;
        float[] _cdRemainingSec;
        float[] _cdDurationSec;
        string[] _cdVerbIds;
        bool[] _cdTracked;
        PlayerCooldown _cdSource;
        GameClock _cdClock;
        RectTransform _center;
        RectTransform _dodge;
        Canvas _canvas;

        public Canvas Canvas => _canvas;
        public Transform CanvasRoot => _canvas != null ? _canvas.transform : null;

        public void Build(PrototypeTuning tuning, Camera overlayCam)
        {
            _tuning = tuning;

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
            int n = Dovus.Core.Grammar.PentagonLayout.DotCount;
            _dots = new RectTransform[n + 1];
            _dotImages = new Image[n + 1];
            _dotIcons = new Sprite[n + 1];
            _cdRings = new RectTransform[n + 1];
            _cdFills = new Image[n + 1];
            _cdLabels = new Text[n + 1];
            _cdRemainingSec = new float[n + 1];
            _cdDurationSec = new float[n + 1];
            _cdVerbIds = new string[n + 1];
            _cdTracked = new bool[n + 1];
            for (int dot = 1; dot <= n; dot++)
            {
                _dotIcons[dot] = TryCreateIconSprite(dot);
                Sprite icon = _dotIcons[dot] != null ? _dotIcons[dot] : fallback;
                Color col = _dotIcons[dot] != null ? DotColor(dot) : RuneFallbackColor(dot);
                _dots[dot] = CreateDisc($"Dot{dot}", icon, col, canvasGo.transform, out _dotImages[dot]);
                if (_dotIcons[dot] == null)
                {
                    var label = CreateLabel(_dots[dot], DotGlyph(dot));
                    label.fontSize = 22;
                }
            }

            // Radial örtü ikon ÜSTÜNDE (klasik pie); sn sayısı en üstte.
            for (int dot = 1; dot <= n; dot++)
                CreateCooldownOverlay(dot, ringSprite, canvasGo.transform, underDots: false);

            _center = CreateDisc("Center", fallback, _tuning.PentagonCenterColor, canvasGo.transform, out _);
            var centerLabel = CreateLabel(_center, "⚔");
            centerLabel.fontSize = 36;

            _dodge = CreateDisc("DodgeButton", fallback, _tuning.DodgeButtonColor, canvasGo.transform, out Image dodgeImg);
            // Frosted rim — oyuncu cyan kenar
            dodgeImg.color = new Color(
                _tuning.DodgeButtonColor.r,
                _tuning.DodgeButtonColor.g,
                _tuning.DodgeButtonColor.b,
                0.88f);
            var dodgeLabel = CreateLabel(_dodge, "DODGE");
            dodgeLabel.fontSize = 18;
            dodgeLabel.color = Color.white;

            for (int dot = 1; dot <= n; dot++)
                CreateCooldownLabel(dot, canvasGo.transform);

            if (overlayCam != null)
                SetLayerRecursively(canvasGo, FirstLayer(overlayCam.cullingMask));

            Layout();
            RefreshCooldownVisuals();
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
            _cdVerbIds[dot] = null;
            _cdDurationSec[dot] = durationSec;
            _cdRemainingSec[dot] = durationSec;
            Layout();
            RefreshCooldownVisuals();
        }

        /// <summary>
        /// Bağlama 4: gerçek CooldownTracker kalanı — radial fillAmount = rem/duration.
        /// </summary>
        public void BeginTrackedCooldown(int dot, string verbId, float durationSec, PlayerCooldown source, GameClock clock)
        {
            if (_cdRemainingSec == null || dot < 1 || dot >= _cdRemainingSec.Length)
                return;
            if (string.IsNullOrEmpty(verbId) || durationSec <= 0f || source == null)
                return;

            _cdSource = source;
            _cdClock = clock;
            _cdTracked[dot] = true;
            _cdVerbIds[dot] = verbId;
            _cdDurationSec[dot] = durationSec;
            double worldMs = clock != null ? clock.Director.WorldTimeMs : 0;
            _cdRemainingSec[dot] = source.VerbRemainingSec(verbId, worldMs);
            Layout();
            RefreshCooldownVisuals();
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;

            Layout();
            TickCooldowns();
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
                if (_cdTracked != null && _cdTracked[i] && _cdSource != null && !string.IsNullOrEmpty(_cdVerbIds[i]))
                {
                    float rem = _cdSource.VerbRemainingSec(_cdVerbIds[i], worldMs);
                    if (!Mathf.Approximately(rem, _cdRemainingSec[i]))
                        any = true;
                    _cdRemainingSec[i] = rem;
                    if (rem <= 0f)
                    {
                        _cdTracked[i] = false;
                        _cdVerbIds[i] = null;
                    }
                    continue;
                }

                if (_cdRemainingSec[i] <= 0f)
                    continue;
                _cdRemainingSec[i] = Mathf.Max(0f, _cdRemainingSec[i] - dt);
                any = true;
            }

            if (any || (_cdFills != null && AnyCooldownVisible()))
                RefreshCooldownVisuals();
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

            Color accent = _tuning.InkCyan;
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
                fill.color = new Color(0.05f, 0.12f, 0.18f, 0.72f);
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
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            float dotR = PentagonLayoutScreen.DotHitRadiusPx(_tuning);
            float centerR = PentagonLayoutScreen.CenterHitRadiusPx(_tuning);
            int n = Dovus.Core.Grammar.PentagonLayout.DotCount;

            for (int dot = 1; dot <= n; dot++)
            {
                Vector2 px = PentagonLayoutScreen.DotPx(dot, _tuning, w, h);
                float mul = _dotIcons != null && _dotIcons[dot] != null
                    ? Mathf.Max(0.5f, _tuning.IconDisplayScale)
                    : 1f;
                float diam = dotR * 2f * mul;
                Place(_dots[dot], px, diam, w, h);
                if (_dotImages[dot] != null)
                    _dotImages[dot].color = _dotIcons != null && _dotIcons[dot] != null
                        ? DotColor(dot)
                        : RuneFallbackColor(dot);

                if (_cdRings != null && _cdRings[dot] != null)
                    Place(_cdRings[dot], px, diam * 1.05f, w, h);
                if (_cdLabels != null && _cdLabels[dot] != null)
                {
                    var lrt = _cdLabels[dot].rectTransform;
                    Place(lrt, px, diam * 0.9f, w, h);
                }
            }

            Vector2 c = PentagonLayoutScreen.CenterPx(_tuning, w, h);
            Place(_center, c, centerR * 2f, w, h);

            Vector2 d = PentagonLayoutScreen.DodgeButtonPx(_tuning, w, h);
            Place(_dodge, d, PentagonLayoutScreen.DodgeButtonRadiusPx(_tuning) * 2f, w, h);
        }

        Color RuneFallbackColor(int dot)
        {
            Color c = dot switch
            {
                1 => _tuning.ElementFire,
                2 => _tuning.ElementWater,
                3 => _tuning.ElementAir,
                4 => _tuning.ElementEarth,
                5 => _tuning.ElementLight,
                6 => _tuning.ElementDark,
                _ => _tuning.PentagonDotColor
            };
            c.a = _tuning.IsDotOpen(dot) ? 0.92f : 0.28f;
            return c;
        }

        static string DotGlyph(int dot) => dot switch
        {
            1 => "AT",
            2 => "SU",
            3 => "HV", // Hava — yıldırım değil
            4 => "TP",
            5 => "AY",
            6 => "KR",
            _ => "?"
        };

        Color DotColor(int dot)
        {
            if (_dotIcons != null && _dotIcons[dot] != null)
            {
                float a = _tuning.IsDotOpen(dot) ? 1f : 0.28f;
                return new Color(1f, 1f, 1f, a);
            }

            Color c = _tuning.PentagonDotColor;
            if (_tuning.IsDotOpen(dot))
                return c;
            return new Color(c.r, c.g, c.b, c.a * 0.28f);
        }

        static Sprite TryCreateIconSprite(int dot)
        {
            // element-sistemi.json çekirdek: Ateş Su Hava Toprak Aydınlık Karanlık
            string name = dot switch
            {
                1 => "Concept/icon-fire",
                2 => "Concept/icon-water",
                3 => "Concept/icon-air", // Hava — lightning ikonu YASAK (yıldırım hissi)
                4 => "Concept/icon-earth",
                5 => "Concept/icon-light",
                6 => "Concept/icon-dark",
                _ => null
            };
            if (string.IsNullOrEmpty(name))
                return null;

            var tex = Resources.Load<Texture2D>(name);
            if (tex == null)
                return null;

            // Concept PNG'lerde siyah kare zemin var — yakındaki siyahı alfa yap.
            Texture2D punched = PunchNearBlackToAlpha(tex);
            return Sprite.Create(
                punched,
                new Rect(0f, 0f, punched.width, punched.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        /// <summary>Siyah/koyu kare zemini şeffafa çevirir (ikonlar yuvarlak diskte okunur kalsın).</summary>
        static Texture2D PunchNearBlackToAlpha(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px;
            try
            {
                px = src.GetPixels32();
            }
            catch
            {
                // Read/Write kapalı asset — olduğu gibi kullan.
                return src;
            }

            const byte thresh = 28;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                if (c.r <= thresh && c.g <= thresh && c.b <= thresh)
                    px[i] = new Color32(0, 0, 0, 0);
            }

            dst.SetPixels32(px);
            dst.Apply(false, true);
            return dst;
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

        static RectTransform CreateDisc(
            string name,
            Sprite sprite,
            Color color,
            Transform parent,
            out Image image)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            image = go.AddComponent<Image>();
            image.sprite = sprite;
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
