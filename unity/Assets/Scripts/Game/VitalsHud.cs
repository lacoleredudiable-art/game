using Dovus.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Premium HUD: boss üst orta; oyuncu HP/mana sol üst glass panel.
    /// </summary>
    public sealed class VitalsHud : MonoBehaviour
    {
        PlayerVitals _vitals;
        PlayerResource _resource;
        BossVitals _bossVitals;
        AllyDummy _ally;
        PrototypeTuning _tuning;

        RectTransform _playerRoot;
        RectTransform _bossRoot;
        RectTransform _playerPanel;
        RectTransform _bossBg;
        RectTransform _playerBg;
        RectTransform _manaBg;
        RectTransform _allyBg;
        Image _playerFill;
        Image _manaFill;
        Image _bossFill;
        Image _allyFill;
        Image _playerSheen;
        Image _manaSheen;
        Image _bossSheen;
        Image _allySheen;
        Text _bossName;
        Text _bossLabel;
        Text _playerLabel;
        Text _manaLabel;
        Text _allyLabel;

        float _appliedWidthDp = -1f;
        float _appliedHeightDp = -1f;
        float _appliedBossHDp = -1f;
        float _appliedBossWDp = -1f;
        float _appliedSpacingDp = -1f;
        float _appliedMarginDp = -1f;
        Color _appliedBossColor;
        Color _appliedPlayerColor;
        Color _appliedManaColor;
        bool _hasAlly;

        /// <summary>Oyuncu sütunu satır sayısı (HP+mana[+ally]) — RecoveryLock için.</summary>
        public int BarCount => _hasAlly ? 3 : 2;

        /// <summary>Sol üst oyuncu panelinin alt kenarı (canvas px, üstten negatif Y).</summary>
        public float PlayerStackBottomCanvasY { get; private set; }

        /// <summary>Boss bar alt kenarı (canvas px).</summary>
        public float BossStackBottomCanvasY { get; private set; }

        public Transform CanvasParent => _playerRoot != null ? _playerRoot.parent : null;

        public void Configure(
            PlayerVitals vitals,
            BossVitals bossVitals,
            PrototypeTuning tuning,
            Transform canvasRoot,
            AllyDummy ally = null,
            PlayerResource resource = null)
        {
            _vitals = vitals;
            _resource = resource;
            _bossVitals = bossVitals;
            _ally = ally;
            _hasAlly = ally != null;
            _tuning = tuning;

            // —— Oyuncu (sol üst) ——
            var playerGo = new GameObject("VitalsPlayer");
            playerGo.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                playerGo.layer = canvasRoot.gameObject.layer;
            _playerRoot = playerGo.AddComponent<RectTransform>();
            _playerRoot.anchorMin = new Vector2(0f, 1f);
            _playerRoot.anchorMax = new Vector2(0f, 1f);
            _playerRoot.pivot = new Vector2(0f, 1f);

            _playerPanel = CreateGlassPanel(_playerRoot, "PlayerGlass");
            _playerFill = CreateBar(_playerPanel, "Player", out _playerBg, out _playerSheen);
            _playerLabel = CreateLabel(_playerBg, "PlayerHp");
            _manaFill = CreateBar(_playerPanel, "Mana", out _manaBg, out _manaSheen);
            _manaLabel = CreateLabel(_manaBg, "PlayerMana");
            if (_hasAlly)
            {
                _allyFill = CreateBar(_playerPanel, "Ally", out _allyBg, out _allySheen);
                _allyLabel = CreateLabel(_allyBg, "AllyHp");
                _allyFill.color = new Color(0.32f, 0.78f, 0.52f, 0.95f);
            }

            // —— Boss (üst orta) ——
            var bossGo = new GameObject("VitalsBoss");
            bossGo.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                bossGo.layer = canvasRoot.gameObject.layer;
            _bossRoot = bossGo.AddComponent<RectTransform>();
            _bossRoot.anchorMin = new Vector2(0.5f, 1f);
            _bossRoot.anchorMax = new Vector2(0.5f, 1f);
            _bossRoot.pivot = new Vector2(0.5f, 1f);

            _bossName = CreateBossName(_bossRoot);
            _bossFill = CreateBar(_bossRoot, "Boss", out _bossBg, out _bossSheen);
            _bossLabel = CreateLabel(_bossBg, "BossHp");
            _bossLabel.alignment = TextAnchor.MiddleCenter;

            ApplyTuningLayout();
        }

        static Sprite _roundedSprite;
        static Sprite _pillSprite;

        static RectTransform CreateGlassPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            var shadowRt = shadowGo.AddComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(2f, -4f);
            shadowRt.offsetMax = new Vector2(4f, -2f);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = RoundedRectSprite();
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0f, 0f, 0f, 0.35f);
            shadowImg.raycastTarget = false;

            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.04f, 0.06f, 0.09f, 0.62f);
            img.raycastTarget = false;

            var edgeGo = new GameObject("Edge");
            edgeGo.transform.SetParent(go.transform, false);
            var edgeRt = edgeGo.AddComponent<RectTransform>();
            edgeRt.anchorMin = Vector2.zero;
            edgeRt.anchorMax = Vector2.one;
            edgeRt.offsetMin = Vector2.zero;
            edgeRt.offsetMax = Vector2.zero;
            var edgeImg = edgeGo.AddComponent<Image>();
            edgeImg.sprite = RoundedRectSprite();
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = new Color(0.45f, 0.9f, 1f, 0.14f);
            edgeImg.raycastTarget = false;
            return rect;
        }

        static Image CreateBar(
            Transform parent,
            string name,
            out RectTransform bgRect,
            out Image sheen)
        {
            Sprite pill = PillSprite();

            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = pill;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            bgImg.raycastTarget = false;

            var fillGo = new GameObject(name + "Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = pill;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;

            var sheenGo = new GameObject(name + "Sheen");
            sheenGo.transform.SetParent(fillGo.transform, false);
            var sheenRt = sheenGo.AddComponent<RectTransform>();
            sheenRt.anchorMin = new Vector2(0f, 0.55f);
            sheenRt.anchorMax = new Vector2(1f, 1f);
            sheenRt.offsetMin = Vector2.zero;
            sheenRt.offsetMax = Vector2.zero;
            sheen = sheenGo.AddComponent<Image>();
            sheen.sprite = pill;
            sheen.type = Image.Type.Sliced;
            sheen.color = new Color(1f, 1f, 1f, 0.18f);
            sheen.raycastTarget = false;

            return fillImg;
        }

        static Sprite RoundedRectSprite()
        {
            if (_roundedSprite != null)
                return _roundedSprite;

            const int size = 64;
            const float radius = 14f;
            _roundedSprite = BuildRounded(size, radius);
            return _roundedSprite;
        }

        static Sprite PillSprite()
        {
            if (_pillSprite != null)
                return _pillSprite;

            // Tam yükseklik yarıçapı → stadium / modern bar.
            const int size = 64;
            _pillSprite = BuildRounded(size, size * 0.5f - 0.5f);
            return _pillSprite;
        }

        static Sprite BuildRounded(int size, float radius)
        {
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

            int b = Mathf.Max(1, Mathf.RoundToInt(radius));
            return Sprite.Create(
                tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Text CreateLabel(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 1f, 1f, 0.88f);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.text = string.Empty;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        static Text CreateBossName(Transform parent)
        {
            var go = new GameObject("BossName");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(400f, 18f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 12;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.78f, 0.74f, 0.7f, 0.75f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = "BOSS";
            return text;
        }

        void ApplyTuningLayout()
        {
            float topInset = PentagonLayoutScreen.SafeTopInsetPx();
            float leftInset = PentagonLayoutScreen.SafeLeftInsetPx();
            float margin = PentagonLayoutScreen.DpToPixels(_tuning.VitalsMarginDp);
            float w = PentagonLayoutScreen.DpToPixels(_tuning.VitalsBarWidthDp);
            float h = PentagonLayoutScreen.DpToPixels(_tuning.VitalsBarHeightDp);
            float bossW = PentagonLayoutScreen.DpToPixels(_tuning.VitalsBossBarWidthDp);
            float bossH = PentagonLayoutScreen.DpToPixels(_tuning.VitalsBossBarHeightDp);
            float spacing = PentagonLayoutScreen.DpToPixels(_tuning.VitalsBarSpacingDp);
            float pad = PentagonLayoutScreen.DpToPixels(8f);

            bool sizeChanged =
                !Mathf.Approximately(_tuning.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.VitalsBarHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.VitalsBossBarHeightDp, _appliedBossHDp) ||
                !Mathf.Approximately(_tuning.VitalsBossBarWidthDp, _appliedBossWDp) ||
                !Mathf.Approximately(_tuning.VitalsBarSpacingDp, _appliedSpacingDp) ||
                !Mathf.Approximately(_tuning.VitalsMarginDp, _appliedMarginDp);

            if (sizeChanged || true)
            {
                _appliedWidthDp = _tuning.VitalsBarWidthDp;
                _appliedHeightDp = _tuning.VitalsBarHeightDp;
                _appliedBossHDp = _tuning.VitalsBossBarHeightDp;
                _appliedBossWDp = _tuning.VitalsBossBarWidthDp;
                _appliedSpacingDp = _tuning.VitalsBarSpacingDp;
                _appliedMarginDp = _tuning.VitalsMarginDp;

                int rows = BarCount;
                float panelH = rows * h + (rows - 1) * spacing + pad * 2f;
                float panelW = w + pad * 2f;

                _playerRoot.anchoredPosition = new Vector2(leftInset + margin, -(topInset + margin));
                _playerPanel.anchoredPosition = Vector2.zero;
                _playerPanel.sizeDelta = new Vector2(panelW, panelH);

                _playerBg.anchoredPosition = new Vector2(pad, -pad);
                _playerBg.sizeDelta = new Vector2(w, h);
                _manaBg.anchoredPosition = new Vector2(pad, -(pad + h + spacing));
                _manaBg.sizeDelta = new Vector2(w, h);
                if (_hasAlly && _allyBg != null)
                {
                    _allyBg.anchoredPosition = new Vector2(pad, -(pad + 2f * (h + spacing)));
                    _allyBg.sizeDelta = new Vector2(w, h);
                }

                PlayerStackBottomCanvasY = -(topInset + margin + panelH);

                float nameH = PentagonLayoutScreen.DpToPixels(16f);
                _bossRoot.anchoredPosition = new Vector2(0f, -(topInset + margin * 0.5f));
                _bossName.rectTransform.anchoredPosition = Vector2.zero;
                _bossName.rectTransform.sizeDelta = new Vector2(bossW, nameH);
                _bossBg.anchoredPosition = new Vector2(-bossW * 0.5f, -nameH);
                _bossBg.sizeDelta = new Vector2(bossW, bossH);
                BossStackBottomCanvasY = -(topInset + margin * 0.5f + nameH + bossH);
            }

            if (_appliedBossColor != _tuning.BossVitalsColor)
            {
                _appliedBossColor = _tuning.BossVitalsColor;
                _bossFill.color = _appliedBossColor;
            }

            // Oyuncu HP: soft rose (kırmızı-turuncu boss tehdidine yaklaşmaz)
            Color hpColor = new Color(0.72f, 0.28f, 0.34f, 0.95f);
            if (_appliedPlayerColor != hpColor)
            {
                _appliedPlayerColor = hpColor;
                _playerFill.color = hpColor;
            }

            Color manaColor = new Color(0.30f, 0.62f, 0.88f, 0.95f);
            if (_appliedManaColor != manaColor)
            {
                _appliedManaColor = manaColor;
                if (_manaFill != null)
                    _manaFill.color = _appliedManaColor;
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
                    _playerLabel.text = _vitals.Hp + "  /  " + _vitals.MaxHp;
            }

            if (_manaFill != null)
            {
                if (_resource != null && _resource.MaxMana > 0f)
                {
                    _manaFill.fillAmount = Mathf.Clamp01(_resource.Mana / _resource.MaxMana);
                    if (_manaLabel != null)
                        _manaLabel.text = Mathf.RoundToInt(_resource.Mana) + "  /  "
                            + Mathf.RoundToInt(_resource.MaxMana);
                }
                else
                {
                    _manaFill.fillAmount = 0f;
                    if (_manaLabel != null)
                        _manaLabel.text = "—";
                }
            }

            if (_bossVitals != null && _bossFill != null)
            {
                _bossFill.fillAmount = _bossVitals.MaxHp > 0
                    ? Mathf.Clamp01(_bossVitals.Hp / _bossVitals.MaxHp)
                    : 0f;
                if (_bossLabel != null)
                    _bossLabel.text = Mathf.CeilToInt(_bossVitals.Hp) + "  /  "
                        + Mathf.CeilToInt(_bossVitals.MaxHp);
            }

            if (_hasAlly && _ally != null && _allyFill != null)
            {
                _allyFill.fillAmount = _ally.Ratio;
                if (_allyLabel != null)
                    _allyLabel.text = _ally.Hp + "  /  " + _ally.MaxHp;
            }
        }
    }
}
