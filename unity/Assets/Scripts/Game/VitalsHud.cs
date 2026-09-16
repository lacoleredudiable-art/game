using Dovus.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Sade can göstergeleri (T9, §6/§11). Oyuncu / ally / boss barları.
    /// </summary>
    public sealed class VitalsHud : MonoBehaviour
    {
        PlayerVitals _vitals;
        BossVitals _bossVitals;
        AllyDummy _ally;
        PrototypeTuning _tuning;
        RectTransform _root;
        RectTransform _bossBg;
        RectTransform _playerBg;
        RectTransform _allyBg;
        Image _playerFill;
        Image _bossFill;
        Image _allyFill;
        Text _bossLabel;
        Text _playerLabel;
        Text _allyLabel;

        float _appliedWidthDp = -1f;
        float _appliedHeightDp = -1f;
        float _appliedSpacingDp = -1f;
        float _appliedMarginDp = -1f;
        Color _appliedBossColor;
        Color _appliedPlayerColor;
        bool _hasAlly;

        public void Configure(
            PlayerVitals vitals,
            BossVitals bossVitals,
            PrototypeTuning tuning,
            Transform canvasRoot,
            AllyDummy ally = null)
        {
            _vitals = vitals;
            _bossVitals = bossVitals;
            _ally = ally;
            _hasAlly = ally != null;
            _tuning = tuning;

            var go = new GameObject("VitalsHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.02f, 1f);
            _root.anchorMax = new Vector2(0.02f, 1f);
            _root.pivot = new Vector2(0f, 1f);

            _bossFill = CreateBar(go.transform, "Boss", out _bossBg);
            _bossLabel = CreateLabel(_bossBg, "BossHp");
            _playerFill = CreateBar(go.transform, "Player", out _playerBg);
            _playerLabel = CreateLabel(_playerBg, "PlayerHp");
            if (_hasAlly)
            {
                _allyFill = CreateBar(go.transform, "Ally", out _allyBg);
                _allyLabel = CreateLabel(_allyBg, "AllyHp");
                _allyFill.color = new Color(0.35f, 1f, 0.55f);
            }
            ApplyTuningLayout();
        }

        // 16 Eylül: HUD modernizasyonu (best-effort) — düz siyah dikdörtgen barlar yerine
        // yuvarlak köşeli + ince kenarlıklı bar. His katmanına dokunmuyor, sadece çizim.
        static Sprite _roundedSprite;

        static Image CreateBar(Transform parent, string name, out RectTransform bgRect)
        {
            Sprite rounded = RoundedRectSprite();

            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = rounded;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.04f, 0.05f, 0.07f, 0.62f);
            bgImg.raycastTarget = false;

            // İnce kenarlık — barı "kart" gibi ayırır, düz dikdörtgen hissini kırar.
            var borderGo = new GameObject(name + "Border");
            borderGo.transform.SetParent(bg.transform, false);
            var borderRect = borderGo.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = rounded;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(1f, 1f, 1f, 0.10f);
            borderImg.raycastTarget = false;

            var fillGo = new GameObject(name + "Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = rounded;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;
            fillGo.transform.SetAsLastSibling();
            borderGo.transform.SetAsLastSibling();
            return fillImg;
        }

        static Sprite RoundedRectSprite()
        {
            if (_roundedSprite != null)
                return _roundedSprite;

            const int size = 64;
            const float radius = 14f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply(false, true);

            int b = Mathf.RoundToInt(radius);
            _roundedSprite = Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            return _roundedSprite;
        }

        static Text CreateLabel(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 0f);
            rect.offsetMax = new Vector2(-6f, 0f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        void ApplyTuningLayout()
        {
            int rows = _hasAlly ? 3 : 2;
            bool sizeChanged =
                !Mathf.Approximately(_tuning.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.VitalsBarHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.VitalsBarSpacingDp, _appliedSpacingDp) ||
                !Mathf.Approximately(_tuning.VitalsMarginDp, _appliedMarginDp);

            if (sizeChanged)
            {
                _appliedWidthDp = _tuning.VitalsBarWidthDp;
                _appliedHeightDp = _tuning.VitalsBarHeightDp;
                _appliedSpacingDp = _tuning.VitalsBarSpacingDp;
                _appliedMarginDp = _tuning.VitalsMarginDp;

                float w = PentagonLayoutScreen.DpToPixels(_appliedWidthDp);
                float h = PentagonLayoutScreen.DpToPixels(_appliedHeightDp);
                float spacing = PentagonLayoutScreen.DpToPixels(_appliedSpacingDp);

                _root.anchoredPosition = new Vector2(0f, -PentagonLayoutScreen.DpToPixels(_appliedMarginDp));
                _root.sizeDelta = new Vector2(w, h * rows + spacing * (rows - 1));

                _bossBg.anchoredPosition = Vector2.zero;
                _bossBg.sizeDelta = new Vector2(w, h);
                _playerBg.anchoredPosition = new Vector2(0f, -(h + spacing));
                _playerBg.sizeDelta = new Vector2(w, h);
                if (_hasAlly && _allyBg != null)
                {
                    _allyBg.anchoredPosition = new Vector2(0f, -2f * (h + spacing));
                    _allyBg.sizeDelta = new Vector2(w, h);
                }
            }

            if (_appliedBossColor != _tuning.BossVitalsColor)
            {
                _appliedBossColor = _tuning.BossVitalsColor;
                _bossFill.color = _appliedBossColor;
            }

            if (_appliedPlayerColor != _tuning.PlayerColor)
            {
                _appliedPlayerColor = _tuning.PlayerColor;
                _playerFill.color = _appliedPlayerColor;
            }
        }

        void LateUpdate()
        {
            if (_tuning == null)
                return;

            ApplyTuningLayout();

            if (_vitals != null && _playerFill != null)
            {
                _playerFill.fillAmount = _vitals.MaxHp > 0
                    ? Mathf.Clamp01((float)_vitals.Hp / _vitals.MaxHp)
                    : 0f;
                if (_playerLabel != null)
                    _playerLabel.text = "Sen " + _vitals.Hp + " / " + _vitals.MaxHp;
            }

            if (_hasAlly && _ally != null && _allyFill != null)
            {
                _allyFill.fillAmount = Mathf.Clamp01(_ally.Ratio);
                if (_allyLabel != null)
                    _allyLabel.text = "Ally " + _ally.Hp + " / " + _ally.MaxHp;
            }

            if (_bossFill != null)
            {
                if (_bossVitals != null && _bossVitals.MaxHp > 0f)
                {
                    _bossFill.fillAmount = Mathf.Clamp01(_bossVitals.Hp / _bossVitals.MaxHp);
                    if (_bossLabel != null)
                        _bossLabel.text = Mathf.CeilToInt(_bossVitals.Hp) + " / " +
                                          Mathf.CeilToInt(_bossVitals.MaxHp);
                }
                else
                {
                    _bossFill.fillAmount = 1f;
                    if (_bossLabel != null)
                        _bossLabel.text = string.Empty;
                }
            }
        }
    }
}
