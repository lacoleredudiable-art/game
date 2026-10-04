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
    public sealed partial class VitalsHud : MonoBehaviour
    {
        PlayerVitalsHost _vitals;
        PlayerResourceHost _resource;
        BossVitals _bossVitals;
        AllyDummyController _ally;
        GameTuning _tuning;
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
            PlayerVitalsHost vitals,
            BossVitals bossVitals,
            GameTuning tuning,
            Transform canvasRoot,
            AllyDummyController ally = null,
            PlayerResourceHost resource = null)
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
