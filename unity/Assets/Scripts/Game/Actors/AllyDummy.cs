using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Team;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// TakÄ±m arkadaÅŸÄ± dummy â€” heal denemesi iÃ§in. BaÅŸlangÄ±Ã§ can oranÄ± varsayÄ±lan %50.
    /// DÃ¼nya Ã¼stÃ¼ bar + HUD (VitalsHud) birlikte okunur.
    /// Boss onu da hedef alÄ±r (<see cref="HostileTargets"/>): vuruÅŸun ally_damage_mult kadarÄ±nÄ± alÄ±r,
    /// can 0'da dÃ¼ÅŸer ve ally_revive_sec (dÃ¼nya saati) sonra ally_revive_ratio canla kalkar.
    /// </summary>
    public sealed class AllyDummy : MonoBehaviour
    {
        // O11: PortalBorderTeamHost her kare FindObjectsOfType<AllyDummy> yapÄ±yordu â†’ etkin kayÄ±t.
        static readonly System.Collections.Generic.List<AllyDummy> s_live = new System.Collections.Generic.List<AllyDummy>();
        public static System.Collections.Generic.IReadOnlyList<AllyDummy> Live => s_live;

        void OnEnable()
        {
            if (!s_live.Contains(this))
                s_live.Add(this);
        }

        void Start()
        {
            // GÃ¶rsel child varsa yeÅŸil kapsÃ¼l placeholder'Ä± asla gÃ¶sterme (feel-pack doÄŸrulama).
            if (transform.Find("Visual") != null)
            {
                var rend = GetComponent<Renderer>();
                if (rend != null)
                    rend.enabled = false;
            }
        }

        void OnDisable() => s_live.Remove(this);

        int _hp;
        int _maxHp;
        Text _label;
        Image _fill;
        Transform _billboard;
        Transform _cam;
        StatusBoard _statusBoard;
        GameClock _clock;
        StatusTuning _statusTuning = new();
        TargetingConfig _life = new();
        double _downAtMs = -1;

        public int Hp => _hp;
        public bool IsDown => _hp <= 0;

        /// <summary>KalkÄ±ÅŸa kalan sÃ¼re (sn); dÃ¼ÅŸmemiÅŸse 0.</summary>
        public float SecondsUntilRevive =>
            _downAtMs < 0 || _clock == null
                ? 0f
                : Mathf.Max(0f, (float)((_downAtMs + _life.AllyReviveSec * 1000.0 - _clock.Director.WorldTimeMs) / 1000.0));
        public int MaxHp => _maxHp;
        public float Ratio => _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        public StatusBoard Board => _statusBoard;

        public void EnsureStatusBoard()
        {
            _statusBoard ??= new StatusBoard();
        }

        public void BindStatusClock(GameClock clock, StatusTuning tuning)
        {
            _clock = clock;
            _statusTuning = tuning ?? new StatusTuning();
            EnsureStatusBoard();
        }

        /// <summary>karadul.json targeting: hasar Ã§arpanÄ± ve kalkÄ±ÅŸ sÃ¼resi/oranÄ±.</summary>
        public void ConfigureLife(TargetingConfig life) => _life = life ?? new TargetingConfig();

        public void Bind(int maxHp, float startRatio = 0.5f)
        {
            _downAtMs = -1;
            _maxHp = Mathf.Max(1, maxHp);
            _hp = Mathf.Clamp(Mathf.RoundToInt(_maxHp * Mathf.Clamp01(startRatio)), 1, _maxHp);
            EnsureBillboard();
            RefreshLabel();
        }

        public int ApplyHeal(int amount)
        {
            if (amount <= 0 || _hp >= _maxHp || IsDown)
                return 0;
            int before = _hp;
            _hp = Mathf.Min(_maxHp, _hp + amount);
            RefreshLabel();
            return _hp - before;
        }

        public bool ApplyDamage(int amount)
        {
            TeamActor actor = GetComponent<TeamActor>();
            if (actor != null && PortalBorderTeamHooks.TryMiss(actor.Id))
                return false;
            if (actor != null)
                amount = Mathf.RoundToInt(amount * PortalBorderTeamHooks.DamageTakenMult(actor.Id));
            if (amount <= 0 || _hp <= 0)
                return false;
            _hp = Mathf.Max(0, _hp - amount);
            if (_hp <= 0 && _downAtMs < 0)
                _downAtMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            RefreshLabel();
            return _hp <= 0;
        }

        /// <summary>
        /// Boss vuruÅŸu: ham hasar Ã— ally_damage_mult, oyuncu yoluyla aynÄ± boru (Ã¶lÃ§ek, alÄ±nan hasar
        /// Ã§arpanÄ±, kalkan emer, stasis yutar). Dost dodge atamaz. DÃ¶ner: dÃ¼ÅŸtÃ¼ mÃ¼.
        /// </summary>
        public bool ApplyBossDamage(float raw)
        {
            float scaledRaw = AllyLifeRules.AllyRawDamage(raw, _life);
            if (scaledRaw <= 0f || IsDown)
                return false;
            EnsureStatusBoard();
            var outcome = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = scaledRaw,
                AttackPower = 1f,
                Multiplier = 1f,
                CanCrit = false,
                DamageTakenFactor = _statusBoard.IncomingDamageMult,
                Shield = _statusBoard.ShieldRemaining,
                Invulnerable = BossStatusMath.DamageInvulnerable(_statusBoard.IsInvulnerable),
                Poise = scaledRaw,
                ScaleMagnitudes = true
            });
            if (outcome.ShieldAbsorbed > 0f)
                _statusBoard.ConsumeShield(Mathf.Min(_statusBoard.ShieldRemaining, outcome.ShieldAbsorbed));
            if (outcome.Amount <= 0f)
                return false;
            return ApplyDamage(Mathf.CeilToInt(outcome.Amount));
        }

        void TickRevive()
        {
            if (_clock == null || !IsDown)
                return;
            double now = _clock.Director.WorldTimeMs;
            if (_downAtMs < 0)
                _downAtMs = now;
            if (!AllyLifeRules.ReviveDue(_downAtMs, now, _life))
                return;
            _downAtMs = -1;
            _statusBoard?.Clear();
            _hp = AllyLifeRules.ReviveHp(_maxHp, _life);
            RefreshLabel();
            DebugConfig.DevLog($"[Ally] kalktÄ±: {_hp}/{_maxHp}");
        }

        void EnsureBillboard()
        {
            if (_billboard != null)
                return;

            var root = new GameObject("AllyHpBillboard");
            root.transform.SetParent(transform, false);
            // KapsÃ¼l merkezi + gÃ¶rsel offset â€” kafanÄ±n Ã¼stÃ¼.
            root.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            _billboard = root.transform;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 56f);
            root.transform.localScale = Vector3.one * 0.012f;

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);
            bg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.04f, 0.2f);
            fillRt.anchorMax = new Vector2(0.96f, 0.55f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _fill = fillGo.AddComponent<Image>();
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.color = new Color(0.35f, 1f, 0.55f, 1f);
            _fill.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.45f);
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(4f, 0f);
            tr.offsetMax = new Vector2(-4f, -2f);
            _label = textGo.AddComponent<Text>();
            _label.font = HudTheme.LegacyFont;
            if (_label.font == null)
                _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.fontSize = 28;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        void RefreshLabel()
        {
            if (_label != null)
                _label.text = IsDown ? "ALLY DÃœÅTÃœ" : "ALLY " + _hp + "/" + _maxHp;
            if (_fill != null)
                _fill.fillAmount = Ratio;
        }

        void LateUpdate()
        {
            if (_billboard == null)
                return;
            if (_cam == null && Camera.main != null)
            {
                _cam = Camera.main.transform;
                var canvas = _billboard.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.worldCamera = Camera.main;
            }
            if (_cam != null)
                _billboard.rotation = Quaternion.LookRotation(
                    _billboard.position - _cam.position, Vector3.up);
        }

        void Update()
        {
            TickRevive();
            if (_statusBoard == null || _clock == null || IsDown)
                return;
            float payload = _statusBoard.Tick(_clock.WorldDeltaMs, _statusTuning);
            if (payload > 0f)
            {
                var hit = DamagePipeline.Resolve(new DamageQuery
                {
                    SkillPower = payload,
                    CanCrit = false,
                    ScaleMagnitudes = true,
                    Poise = payload
                });
                ApplyDamage(Mathf.CeilToInt(hit.Amount));
            }
            else if (payload < 0f)
            {
                var heal = DamagePipeline.Resolve(new DamageQuery
                {
                    Heal = true,
                    HealPower = -payload,
                    ScaleMagnitudes = true
                });
                ApplyHeal(Mathf.CeilToInt(heal.Amount));
            }
        }
    }
}
