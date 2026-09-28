using Dovus.Core.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Savaş üstü okunurluk katmanı: düşük can vinyeti, hasar yönü göstergesi, ekran dışı boss
    /// oku ve zafer/yenilgi banner'ı. Oyun durumunu yalnız okur (can düşüşü = isabet; tek hasar
    /// kaynağı boss). Kullanılmayan görseller kapalı tutulur (overdraw, bkz. CombatFeel T8.1).
    /// </summary>
    public sealed class CombatOverlayHud : MonoBehaviour
    {
        PlayerVitals _player;
        BossVitals _bossVitals;
        Transform _playerTf;
        Transform _bossTf;
        Camera _cam;

        Image _lowHp;
        RectTransform _dirRoot;
        Image _dir;
        RectTransform _arrowRoot;
        Image _arrow;
        CanvasGroup _outcomeGroup;
        TextMeshProUGUI _outcomeTitle;
        TextMeshProUGUI _outcomeSub;

        int _lastHp = -1;
        float _dirShownAt = -10f;
        bool _playerWasDown;
        bool _bossWasDown;
        float _fightStartUnscaled;
        float _outcomeShownAt = -10f;
        bool _outcomeIsDefeat;

        public bool OffscreenArrowVisible => _arrow != null && _arrow.enabled;
        public bool LowHpVisible => _lowHp != null && _lowHp.enabled;
        public bool DamageDirectionVisible => _dir != null && _dir.enabled;
        public string OutcomeText => _outcomeTitle != null && _outcomeGroup.alpha > 0f ? _outcomeTitle.text : string.Empty;

        public void Configure(
            PlayerVitals player, BossVitals bossVitals, Transform playerTf, Transform bossTf, Camera overlayCam)
        {
            _player = player;
            _bossVitals = bossVitals;
            _playerTf = playerTf;
            _bossTf = bossTf;
            _cam = Camera.main;
            _fightStartUnscaled = Time.unscaledTime;
            Build(overlayCam);
        }

        void Build(Camera overlayCam)
        {
            HudTheme th = HudTheme.Current;
            var go = new GameObject("CombatOverlayCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = overlayCam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = overlayCam;
            // FeelCanvas (200, 0.8) üstünde: sonuç banner'ı isabet partiküllerinin altında kalmasın.
            canvas.planeDistance = 0.7f;
            canvas.sortingOrder = 210;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            _lowHp = CreateImage(go.transform, "LowHpVignette", EdgeMaskSprite());
            Stretch(_lowHp.rectTransform);

            float dirPx = HexagonLayoutScreen.DpToPixels(th.DamageDirectionDp);
            _dirRoot = new GameObject("DamageDirection").AddComponent<RectTransform>();
            _dirRoot.SetParent(go.transform, false);
            _dirRoot.sizeDelta = Vector2.zero;
            _dir = CreateImage(_dirRoot, "Wedge", WedgeSprite());
            _dir.rectTransform.sizeDelta = new Vector2(dirPx, dirPx * 0.45f);

            float arrowPx = HexagonLayoutScreen.DpToPixels(th.OffscreenArrowDp);
            _arrowRoot = new GameObject("BossArrow").AddComponent<RectTransform>();
            _arrowRoot.SetParent(go.transform, false);
            _arrowRoot.anchorMin = Vector2.zero;
            _arrowRoot.anchorMax = Vector2.zero;
            _arrowRoot.sizeDelta = new Vector2(arrowPx, arrowPx);
            _arrow = CreateImage(_arrowRoot, "Arrow", ArrowSprite());
            Stretch(_arrow.rectTransform);
            _arrow.color = th.OffscreenArrowColor;

            var outcome = new GameObject("Outcome");
            outcome.transform.SetParent(go.transform, false);
            var ort = outcome.AddComponent<RectTransform>();
            ort.anchorMin = new Vector2(0.5f, 0.55f);
            ort.anchorMax = new Vector2(0.5f, 0.55f);
            ort.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(420f), HexagonLayoutScreen.DpToPixels(th.BannerDp * 3f));
            _outcomeGroup = outcome.AddComponent<CanvasGroup>();
            _outcomeGroup.alpha = 0f;
            _outcomeGroup.blocksRaycasts = false;
            _outcomeGroup.interactable = false;
            _outcomeTitle = HudTheme.CreateTmp(outcome.transform, "Title", th.BannerDp * 1.4f, th.VictoryColor);
            var trt = _outcomeTitle.rectTransform;
            trt.anchorMin = new Vector2(0f, 0.45f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            _outcomeSub = HudTheme.CreateTmp(outcome.transform, "Sub", th.BannerDp * 0.5f, Color.white);
            var srt = _outcomeSub.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.45f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            if (overlayCam != null)
                SetLayerRecursively(go, FirstLayer(overlayCam.cullingMask));
        }

        void LateUpdate()
        {
            if (_cam == null)
                _cam = Camera.main;
            HudTheme th = HudTheme.Current;
            TickLowHp(th);
            TickDamageDirection(th);
            TickOffscreenArrow(th);
            TickOutcome(th);
        }

        void TickLowHp(HudTheme th)
        {
            if (_player == null || _player.MaxHp <= 0 || _player.IsDown)
            {
                _lowHp.enabled = false;
                return;
            }
            float ratio = (float)_player.Hp / _player.MaxHp;
            if (ratio > th.LowHpFrac)
            {
                _lowHp.enabled = false;
                return;
            }
            float severity = 1f - Mathf.Clamp01(ratio / Mathf.Max(0.01f, th.LowHpFrac));
            Color c = th.LowHpVignetteColor;
            c.a = th.LowHpVignetteMaxAlpha * Mathf.Lerp(0.55f, 1f, severity)
                * Mathf.Lerp(0.6f, 1f, UiJuice.Pulse01(th.LowHpPulseHz));
            _lowHp.color = c;
            _lowHp.enabled = true;
        }

        void TickDamageDirection(HudTheme th)
        {
            if (_player != null)
            {
                if (_lastHp >= 0 && _player.Hp < _lastHp)
                    _dirShownAt = Time.unscaledTime;
                _lastHp = _player.Hp;
            }

            float age = Time.unscaledTime - _dirShownAt;
            if (age > th.DamageDirectionSec || _cam == null || _playerTf == null || _bossTf == null)
            {
                _dir.enabled = false;
                return;
            }

            // Kameraya göre ekran-uzayı yön: yukarı = kameranın baktığı yer.
            Vector3 to = _bossTf.position - _playerTf.position;
            Vector3 fwd = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_cam.transform.right, Vector3.up).normalized;
            Vector2 dir = new Vector2(Vector3.Dot(to, right), Vector3.Dot(to, fwd));
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector2.up;
            dir.Normalize();
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.36f;
            _dirRoot.anchoredPosition = Vector2.zero;
            _dirRoot.localRotation = Quaternion.Euler(0f, 0f, ang);
            _dir.rectTransform.anchoredPosition = new Vector2(0f, radius);
            Color c = th.DamageDirectionColor;
            c.a *= 1f - Mathf.Clamp01(age / th.DamageDirectionSec);
            _dir.color = c;
            _dir.enabled = true;
        }

        void TickOffscreenArrow(HudTheme th)
        {
            if (_cam == null || _bossTf == null || (_bossVitals != null && _bossVitals.IsDown))
            {
                _arrow.enabled = false;
                return;
            }
            Vector3 vp = _cam.WorldToViewportPoint(_bossTf.position + Vector3.up * 1.5f);
            bool behind = vp.z < 0f;
            bool onScreen = !behind && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
            if (onScreen)
            {
                _arrow.enabled = false;
                return;
            }

            Vector2 c = new Vector2(0.5f, 0.5f);
            Vector2 d = new Vector2(vp.x, vp.y) - c;
            if (behind)
                d = -d;
            if (d.sqrMagnitude < 1e-6f)
                d = Vector2.down;
            Vector2 px = new Vector2(d.x * Screen.width, d.y * Screen.height);
            float margin = HexagonLayoutScreen.DpToPixels(th.OffscreenMarginDp);
            float halfW = Screen.width * 0.5f - margin;
            float halfH = Screen.height * 0.5f - margin;
            float scale = Mathf.Min(halfW / Mathf.Max(1e-3f, Mathf.Abs(px.x)), halfH / Mathf.Max(1e-3f, Mathf.Abs(px.y)));
            Vector2 edge = px * scale + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _arrowRoot.anchoredPosition = edge;
            _arrowRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(px.y, px.x) * Mathf.Rad2Deg - 90f);
            _arrow.enabled = true;
        }

        void TickOutcome(HudTheme th)
        {
            bool bossDown = _bossVitals != null && _bossVitals.IsDown;
            bool playerDown = _player != null && _player.IsDown;

            if (bossDown && !_bossWasDown)
            {
                float t = Time.unscaledTime - _fightStartUnscaled;
                ShowOutcome("ZAFER", th.VictoryColor,
                    "Süre " + Mathf.FloorToInt(t / 60f).ToString("00") + ":" + Mathf.FloorToInt(t % 60f).ToString("00"),
                    defeat: false);
            }
            if (!bossDown && _bossWasDown)
                _fightStartUnscaled = Time.unscaledTime;
            if (playerDown && !_playerWasDown)
                ShowOutcome("YENİLDİN", th.DefeatColor, string.Empty, defeat: true);

            _bossWasDown = bossDown;
            _playerWasDown = playerDown;

            if (_outcomeGroup.alpha <= 0f)
                return;
            if (_outcomeIsDefeat && playerDown)
            {
                _outcomeSub.text = "Dönüş " + _player.RespawnInSec.ToString("0.0") + " sn";
                _outcomeGroup.alpha = 1f;
                return;
            }
            float age = Time.unscaledTime - _outcomeShownAt;
            _outcomeGroup.alpha = age <= th.BannerHoldSec
                ? 1f
                : 1f - Mathf.Clamp01((age - th.BannerHoldSec) / Mathf.Max(0.01f, th.BannerFadeSec));
        }

        void ShowOutcome(string title, Color color, string sub, bool defeat)
        {
            _outcomeTitle.text = title;
            _outcomeTitle.color = color;
            _outcomeSub.text = sub;
            _outcomeShownAt = Time.unscaledTime;
            _outcomeIsDefeat = defeat;
            _outcomeGroup.alpha = 1f;
            UiJuice.PunchScale(_outcomeTitle.transform, HudTheme.Current.BannerPunchScale, HudTheme.Current.JuiceSec * 2f);
        }

        static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Sprite _edge;
        static Sprite _wedge;
        static Sprite _arrowSprite;

        static Sprite EdgeMaskSprite()
        {
            if (_edge != null)
                return _edge;
            _edge = BuildSprite(64, (x, y) =>
            {
                float r = Mathf.Clamp01(new Vector2(x - 0.5f, y - 0.5f).magnitude * 2f / 1.4142f);
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
            });
            return _edge;
        }

        /// <summary>Hasar yönü: altı düz, üstü yuvarlak yay — merkezden dışarı bakan hilal.</summary>
        static Sprite WedgeSprite()
        {
            if (_wedge != null)
                return _wedge;
            _wedge = BuildSprite(64, (x, y) =>
            {
                float dx = (x - 0.5f) * 2f;
                float arc = 1f - dx * dx;
                float band = Mathf.InverseLerp(arc - 0.55f, arc - 0.25f, y * 1.1f)
                             * (1f - Mathf.InverseLerp(arc - 0.05f, arc + 0.05f, y * 1.1f));
                return Mathf.Clamp01(band) * (1f - Mathf.Abs(dx) * 0.6f);
            });
            return _wedge;
        }

        static Sprite ArrowSprite()
        {
            if (_arrowSprite != null)
                return _arrowSprite;
            _arrowSprite = BuildSprite(64, (x, y) =>
            {
                float half = (1f - y) * 0.5f;
                float edge = Mathf.Abs(x - 0.5f);
                return edge < half && y > 0.1f ? Mathf.Clamp01((half - edge) * 24f) : 0f;
            });
            return _arrowSprite;
        }

        static Sprite BuildSprite(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha((x + 0.5f) / size, (y + 0.5f) / size)));
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        static int FirstLayer(int mask)
        {
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0)
                    return i;
            return 0;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
