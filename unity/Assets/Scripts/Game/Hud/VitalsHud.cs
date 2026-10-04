using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
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
        HudTheme _theme;

        public void BindTheme(HudTheme theme) => _theme = theme;

        RectTransform _playerRoot;
        RectTransform _bossRoot;
        RectTransform _playerPanel;
        RectTransform _bossBg;
        RectTransform _poiseBg;
        RectTransform _playerBg;
        RectTransform _manaBg;
        RectTransform _allyBg;
        Image _playerFill;
        Image _manaFill;
        Image _bossFill;
        Image _poiseFill;
        Image _allyFill;
        Image _playerSheen;
        Image _manaSheen;
        Image _bossSheen;
        Image _allySheen;
        TextMeshProUGUI _bossName;
        TextMeshProUGUI _playerIdentity;
        Text _bossLabel;
        BarJuice _playerJuice;
        BarJuice _bossJuice;
        BossHudData _bossData;
        BossDirector _bossDirector;
        RectTransform _castRoot;
        CanvasGroup _castGroup;
        Image _castFill;
        TextMeshProUGUI _castLabel;
        BossAttackKind _castKind;
        bool _castShown;
        TextMeshProUGUI _banner;
        CanvasGroup _bannerGroup;
        float _bannerShownAt;
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
            _playerIdentity = HudTheme.CreateTmp(
                _playerPanel, "PlayerIdentity", _theme.PlayerIdentityDp,
                _theme.ControlCaptionColor, TextAlignmentOptions.Left);
            _playerIdentity.text = "AVCI  //  OYUNCU";
            _playerFill = CreateBar(_playerPanel, "Player", out _playerBg, out _playerSheen, out _playerJuice);
            _playerLabel = CreateLabel(_playerBg, "PlayerHp");
            _manaFill = CreateBar(_playerPanel, "Mana", out _manaBg, out _manaSheen);
            _manaLabel = CreateLabel(_manaBg, "PlayerMana");
            if (_hasAlly)
            {
                _allyFill = CreateBar(_playerPanel, "Ally", out _allyBg, out _allySheen);
                _allyLabel = CreateLabel(_allyBg, "AllyHp");
                _allyFill.color = _theme.AllyHpColor;
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

            _bossData = BossHudData.Load(tuning.Boss.ActiveBossResourcePath);
            _bossName = CreateBossName(_bossRoot, _bossData);
            _bossFill = CreateBar(_bossRoot, "Boss", out _bossBg, out _bossSheen, out _bossJuice);
            CreatePhaseNotches(_bossBg, _bossData);
            _bossLabel = CreateLabel(_bossBg, "BossHp");
            _bossLabel.alignment = TextAnchor.MiddleCenter;
            _poiseFill = CreateBar(_bossRoot, "Poise", out _poiseBg, out _);
            _poiseFill.color = new Color(0.95f, 0.72f, 0.25f, 1f);
            Text poiseLabel = CreateLabel(_poiseBg, "Poise");
            poiseLabel.alignment = TextAnchor.MiddleCenter;
            poiseLabel.text = "POISE";
            CreateCastBar(_bossRoot);
            CreatePhaseBanner(canvasRoot);

            ApplyTuningLayout();
        }

        /// <summary>Faz banner'ı ve cast barı için boss beyni.</summary>
        public void BindBoss(BossDirector boss)
        {
            if (_bossDirector != null)
                _bossDirector.BossPhaseChanged -= OnBossPhaseChanged;
            _bossDirector = boss;
            if (_bossDirector != null)
                _bossDirector.BossPhaseChanged += OnBossPhaseChanged;
        }

        void OnDestroy()
        {
            if (_bossDirector != null)
                _bossDirector.BossPhaseChanged -= OnBossPhaseChanged;
        }

        void OnBossPhaseChanged(int phase)
        {
            // Faz 1 = revive sonrası sıfırlama; banner yalnız yükselişte.
            if (phase <= 1 || _banner == null)
                return;
            string phaseName = _bossData.PhaseName(phase);
            _banner.text = string.IsNullOrEmpty(phaseName)
                ? "FAZ " + phase
                : "FAZ " + phase + "  <size=70%>" + _bossData.Upper(phaseName) + "</size>";
            _bannerShownAt = Time.unscaledTime;
            _bannerGroup.alpha = 1f;
            HudTheme th = _theme;
            UiJuice.PunchScale(_banner.transform, th.BannerPunchScale, th.JuiceSec * 2f);
            UiJuice.Shake(_bossRoot, HexagonLayoutScreen.DpToPixels(th.BossBarShakeDp), th.BossBarShakeSec);
        }

        void CreatePhaseNotches(RectTransform bossBg, BossHudData data)
        {
            foreach (var p in data.Phases)
            {
                if (p.UpperFrac <= 0.001f || p.UpperFrac >= 0.999f)
                    continue;
                var go = new GameObject("PhaseNotch" + p.Phase);
                go.transform.SetParent(bossBg, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(p.UpperFrac, 0f);
                rt.anchorMax = new Vector2(p.UpperFrac, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(2f), 0f);
                var img = go.AddComponent<Image>();
                img.color = _theme.PhaseNotchColor;
                img.raycastTarget = false;
            }
        }

        void CreateCastBar(RectTransform bossRoot)
        {
            HudTheme th = _theme;
            var go = new GameObject("BossCastBar");
            go.transform.SetParent(bossRoot, false);
            _castRoot = go.AddComponent<RectTransform>();
            _castRoot.anchorMin = new Vector2(0.5f, 1f);
            _castRoot.anchorMax = new Vector2(0.5f, 1f);
            _castRoot.pivot = new Vector2(0.5f, 1f);
            _castGroup = go.AddComponent<CanvasGroup>();
            _castGroup.alpha = 0f;
            _castGroup.blocksRaycasts = false;
            _castGroup.interactable = false;

            var bg = go.AddComponent<Image>();
            bg.sprite = PillSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            bg.raycastTarget = false;

            _castFill = CreateBarLayer(go.transform, "CastFill", PillSprite(), Image.Type.Filled);
            _castFill.color = th.CastBarColor;
            _castLabel = HudTheme.CreateTmp(go.transform, "CastLabel", th.CastLabelDp, Color.white);
            var lrt = _castLabel.rectTransform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, HexagonLayoutScreen.DpToPixels(1f));
            lrt.sizeDelta = new Vector2(0f, HexagonLayoutScreen.DpToPixels(th.CastLabelDp + 4f));
        }

        void CreatePhaseBanner(Transform canvasRoot)
        {
            HudTheme th = _theme;
            var go = new GameObject("PhaseBanner");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.68f);
            rt.anchorMax = new Vector2(0.5f, 0.68f);
            rt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(360f), HexagonLayoutScreen.DpToPixels(th.BannerDp * 1.6f));
            _bannerGroup = go.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;
            _banner = HudTheme.CreateTmp(go.transform, "BannerText", th.BannerDp, th.BannerColor);
            var brt = _banner.rectTransform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }

        void TickBossExtras()
        {
            HudTheme th = _theme;
            if (_bannerGroup != null && _bannerGroup.alpha > 0f)
            {
                float age = Time.unscaledTime - _bannerShownAt;
                _bannerGroup.alpha = age <= th.BannerHoldSec
                    ? 1f
                    : 1f - Mathf.Clamp01((age - th.BannerHoldSec) / Mathf.Max(0.01f, th.BannerFadeSec));
            }

            if (_castGroup == null)
                return;
            bool casting = _bossDirector != null && _bossDirector.IsWindingUp
                && _bossDirector.CurrentAttackKind.HasValue;
            if (casting)
            {
                var kind = _bossDirector.CurrentAttackKind.Value;
                if (!_castShown || _castKind != kind)
                {
                    _castKind = kind;
                    _castLabel.text = _bossData.AttackName(kind);
                    UiJuice.PunchScale(_castRoot, th.CastPopScale, th.JuiceSec);
                }
                _castShown = true;
                float progress = _bossDirector.WindupProgress01;
                _castFill.fillAmount = progress;
                float urgency = Mathf.InverseLerp(th.CastUrgencyStart01, 1f, progress);
                float pulse = UiJuice.Pulse01(th.CastUrgencyPulseHz) * th.CastUrgencyPulseStrength;
                _castFill.color = Color.Lerp(
                    th.CastBarColor,
                    th.CastUrgentColor,
                    Mathf.Clamp01(urgency + urgency * pulse));
                _castGroup.alpha = 1f;
            }
            else
            {
                _castShown = false;
                _castGroup.alpha = Mathf.MoveTowards(_castGroup.alpha, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, th.BannerFadeSec));
            }
        }

        static Sprite _roundedSprite;
        static Sprite _pillSprite;

        RectTransform CreateGlassPanel(Transform parent, string name)
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
            float shadowPx = HexagonLayoutScreen.DpToPixels(_theme.PanelShadowDp);
            shadowRt.offsetMin = new Vector2(0f, -shadowPx);
            shadowRt.offsetMax = new Vector2(shadowPx, 0f);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = RoundedRectSprite();
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0f, 0f, 0f, 0.35f);
            shadowImg.raycastTarget = false;

            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite();
            img.type = Image.Type.Sliced;
            img.color = _theme.PanelSoftColor;
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
            edgeImg.color = _theme.PanelEdgeColor;
            edgeImg.raycastTarget = false;
            return rect;
        }

        Image CreateBar(
            Transform parent,
            string name,
            out RectTransform bgRect,
            out Image sheen) => CreateBar(parent, name, out bgRect, out sheen, out _);

        Image CreateBar(
            Transform parent,
            string name,
            out RectTransform bgRect,
            out Image sheen,
            out BarJuice juice)
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

            Image ghost = CreateBarLayer(bg.transform, name + "Ghost", pill, Image.Type.Filled);

            var fillGo = new GameObject(name + "Fill");
            fillGo.transform.SetParent(bg.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            float barInset = HexagonLayoutScreen.DpToPixels(_theme.BarInsetDp);
            fillRect.offsetMin = new Vector2(barInset, barInset);
            fillRect.offsetMax = new Vector2(-barInset, -barInset);
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
            sheen.color = new Color(1f, 1f, 1f, 0.10f);
            sheen.raycastTarget = false;

            Image flash = CreateBarLayer(bg.transform, name + "Flash", pill, Image.Type.Sliced);
            flash.enabled = false;
            juice = new BarJuice(ghost, flash);
            return fillImg;
        }

        Image CreateBarLayer(Transform bg, string name, Sprite pill, Image.Type type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bg, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            float barInset = HexagonLayoutScreen.DpToPixels(_theme.BarInsetDp);
            rect.offsetMin = new Vector2(barInset, barInset);
            rect.offsetMax = new Vector2(-barInset, -barInset);
            var img = go.AddComponent<Image>();
            img.sprite = pill;
            img.type = type;
            if (type == Image.Type.Filled)
            {
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            img.raycastTarget = false;
            return img;
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

        Text CreateLabel(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            float labelInset = HexagonLayoutScreen.DpToPixels(_theme.BarLabelInsetDp);
            rect.offsetMin = new Vector2(labelInset, 0f);
            rect.offsetMax = new Vector2(-labelInset, 0f);
            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont;
            text.fontSize = Mathf.Max(11, Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(10.5f)));
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

        TextMeshProUGUI CreateBossName(Transform parent, BossHudData data)
        {
            HudTheme th = _theme;
            var text = HudTheme.CreateTmp(parent, "BossName", th.BossNameDp, new Color(0.92f, 0.88f, 0.82f, 0.95f));
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(400f, 18f);
            float subPct = th.BossNameDp > 0f ? th.BossSubtitleDp / th.BossNameDp * 100f : 70f;
            text.text = string.IsNullOrEmpty(data.Subtitle)
                ? data.Upper(data.Name)
                : data.Upper(data.Name) + "  <size=" + subPct.ToString("0") + "%><alpha=#AA>" + data.Subtitle + "</size>";
            return text;
        }

        void ApplyTuningLayout()
        {
            float topInset = HexagonLayoutScreen.SafeTopInsetPx();
            float leftInset = HexagonLayoutScreen.SafeLeftInsetPx();
            float margin = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsMarginDp);
            float w = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarWidthDp);
            float h = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarHeightDp);
            float bossW = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBossBarWidthDp);
            float bossH = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBossBarHeightDp);
            float spacing = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarSpacingDp);
            float pad = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsPanelPaddingDp);
            float headerH = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsHeaderHeightDp);
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            bossW = Mathf.Min(bossW, safe.width - margin * 2f);

            bool sizeChanged =
                !Mathf.Approximately(_tuning.Hud.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBarHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBossBarHeightDp, _appliedBossHDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBossBarWidthDp, _appliedBossWDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBarSpacingDp, _appliedSpacingDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsMarginDp, _appliedMarginDp);

            if (sizeChanged || true)
            {
                _appliedWidthDp = _tuning.Hud.VitalsBarWidthDp;
                _appliedHeightDp = _tuning.Hud.VitalsBarHeightDp;
                _appliedBossHDp = _tuning.Hud.VitalsBossBarHeightDp;
                _appliedBossWDp = _tuning.Hud.VitalsBossBarWidthDp;
                _appliedSpacingDp = _tuning.Hud.VitalsBarSpacingDp;
                _appliedMarginDp = _tuning.Hud.VitalsMarginDp;

                int rows = BarCount;
                float panelH = headerH + rows * h + (rows - 1) * spacing + pad * 2f;
                float panelW = w + pad * 2f;

                _playerRoot.anchoredPosition = new Vector2(leftInset + margin, -(topInset + margin));
                _playerPanel.anchoredPosition = Vector2.zero;
                _playerPanel.sizeDelta = new Vector2(panelW, panelH);

                if (_playerIdentity != null)
                {
                    RectTransform identity = _playerIdentity.rectTransform;
                    identity.anchorMin = identity.anchorMax = new Vector2(0f, 1f);
                    identity.pivot = new Vector2(0f, 1f);
                    identity.anchoredPosition = new Vector2(pad, -pad * 0.35f);
                    identity.sizeDelta = new Vector2(w, headerH);
                }
                float barsTop = pad + headerH;
                _playerBg.anchoredPosition = new Vector2(pad, -barsTop);
                _playerBg.sizeDelta = new Vector2(w, h);
                _manaBg.anchoredPosition = new Vector2(pad, -(barsTop + h + spacing));
                _manaBg.sizeDelta = new Vector2(w, h);
                if (_hasAlly && _allyBg != null)
                {
                    _allyBg.anchoredPosition = new Vector2(pad, -(barsTop + 2f * (h + spacing)));
                    _allyBg.sizeDelta = new Vector2(w, h);
                }

                PlayerStackBottomCanvasY = -(topInset + margin + panelH);

                HudTheme th = _theme;
                float nameH = HexagonLayoutScreen.DpToPixels(th.BossNameDp + 4f);
                _bossRoot.anchoredPosition = new Vector2(0f, -(topInset + margin * 0.5f));
                _bossName.rectTransform.anchoredPosition = Vector2.zero;
                _bossName.rectTransform.sizeDelta = new Vector2(bossW, nameH);
                _bossBg.anchoredPosition = new Vector2(-bossW * 0.5f, -nameH);
                _bossBg.sizeDelta = new Vector2(bossW, bossH);
                float poiseGap = HexagonLayoutScreen.DpToPixels(3f);
                float poiseH = Mathf.Max(6f, bossH * 0.42f);
                if (_poiseBg != null)
                {
                    _poiseBg.anchoredPosition = new Vector2(-bossW * 0.5f, -(nameH + bossH + poiseGap));
                    _poiseBg.sizeDelta = new Vector2(bossW, poiseH);
                }
                // Cast barı poise barının altında; etiketi barın üstünde durur.
                float castLabelH = HexagonLayoutScreen.DpToPixels(th.CastLabelDp + 4f);
                float castH = HexagonLayoutScreen.DpToPixels(th.CastBarHeightDp);
                float castTop = nameH + bossH + poiseGap + poiseH + castLabelH;
                if (_castRoot != null)
                {
                    _castRoot.anchoredPosition = new Vector2(0f, -castTop);
                    _castRoot.sizeDelta = new Vector2(bossW * th.CastBarWidthFrac, castH);
                }
                BossStackBottomCanvasY = -(topInset + margin * 0.5f + castTop + castH);
            }

            if (_appliedBossColor != _tuning.Hud.BossVitalsColor)
            {
                _appliedBossColor = _tuning.Hud.BossVitalsColor;
                _bossFill.color = _appliedBossColor;
            }

            // Oyuncu HP: mevcut soft rose tonu
            Color hpColor = _theme.PlayerHpColor;
            if (_appliedPlayerColor != hpColor)
            {
                _appliedPlayerColor = hpColor;
                _playerFill.color = hpColor;
            }

            Color manaColor = _theme.PlayerManaColor;
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
            HudTheme th = _theme;

            if (_vitals != null && _playerFill != null)
            {
                float ratio = _vitals.MaxHp > 0
                    ? Mathf.Clamp01((float)_vitals.Hp / _vitals.MaxHp)
                    : 0f;
                _playerFill.fillAmount = ratio;
                _playerJuice?.Tick(ratio, th);
                bool low = ratio > 0f && ratio <= th.LowHpFrac;
                _playerFill.color = low
                    ? Color.Lerp(_appliedPlayerColor, Color.white, UiJuice.Pulse01(th.LowHpPulseHz) * th.LowHpPulseStrength)
                    : _appliedPlayerColor;
                if (_playerLabel != null)
                    _playerLabel.text = "HP   " + _vitals.Hp + "  /  " + _vitals.MaxHp;
            }

            if (_manaFill != null)
            {
                if (_resource != null && _resource.MaxMana > 0f)
                {
                    _manaFill.fillAmount = Mathf.Clamp01(_resource.Mana / _resource.MaxMana);
                    if (_manaLabel != null)
                        _manaLabel.text = "MP   " + Mathf.RoundToInt(_resource.Mana) + "  /  "
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
                float bossRatio = _bossVitals.MaxHp > 0
                    ? Mathf.Clamp01(_bossVitals.Hp / _bossVitals.MaxHp)
                    : 0f;
                _bossFill.fillAmount = bossRatio;
                _bossJuice?.Tick(bossRatio, th);
                bool low = bossRatio > 0f && bossRatio <= th.BossLowHpFrac;
                _bossFill.color = low
                    ? Color.Lerp(
                        _appliedBossColor,
                        th.BossLowHpColor,
                        UiJuice.Pulse01(th.BossLowHpPulseHz) * th.BossLowHpPulseStrength)
                    : _appliedBossColor;
                if (_bossLabel != null)
                    _bossLabel.text = "HP   " + Mathf.CeilToInt(_bossVitals.Hp) + "  /  "
                        + Mathf.CeilToInt(_bossVitals.MaxHp);
                if (_poiseFill != null && _bossDirector != null)
                {
                    _poiseFill.fillAmount = Mathf.Clamp01(_bossDirector.PoiseRatio);
                    _poiseFill.color = _bossDirector.IsPoiseStaggered
                        ? new Color(0.95f, 0.32f, 0.18f, 1f)
                        : new Color(0.95f, 0.72f, 0.25f, 1f);
                }
            }

            if (_hasAlly && _ally != null && _allyFill != null)
            {
                _allyFill.fillAmount = _ally.Ratio;
                if (_allyLabel != null)
                    _allyLabel.text = "ALLY   " + _ally.Hp + "  /  " + _ally.MaxHp;
            }

            TickBossExtras();
        }
    }
}
