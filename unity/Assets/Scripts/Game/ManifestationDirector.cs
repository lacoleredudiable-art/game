using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Cümleyi dünyadaki yaşayan etkiye bağlar. Kapalı rün geri bildirimi T6.1'de — burada yok.
    /// T12: kapanış ödülü × ClosingDamagePerEffect → BossVitals; tür son rüne bağlı tepki.
    /// </summary>
    public sealed class ManifestationDirector : MonoBehaviour
    {
        GameClock _clock;
        SentenceEngine _engine;
        CombatTuning _combat;
        PrototypeTuning _colors;
        Transform _player;
        ActorPose _pose;
        ActorVisual _visual;
        BossReactor _boss;
        BossVitals _bossVitals;
        GroundScarField _scars;
        KinematicMotor _motor;
        DamageNumberHud _damageHud;
        BossDirector _bossDirector;
        SentenceDebugHud _debugHud;
        ReactionReadout _readout;
        FollowCamera _camera;

        readonly List<LivingEffectView> _active = new();
        readonly List<PendingClosing> _pending = new();
        readonly HashSet<LivingEffect> _closingStamped = new();

        // Cümlenin şu an sözcük aldığı etki — nokta sayısına göre değil, kimliğe göre izlenir
        // (aynı karede birden fazla nokta kaydı sayı polling'ini atlayabilir, bkz. T7.1).
        LivingEffectView _buildingView;
        int _lastWordCount;
        bool _hooked;
        bool _posedForRecovery;

        // §11 ölüm: çökme süresi bitince Revive (yavaş çekim yok).
        bool _deathPending;
        double _deathReviveAtMs;

        SkillMotor _skills;
        DamageCalculator _damageCalculator;
        ActorStatus _playerStatus;
        ActorStatus _bossStatus;
        SkillMotionDriver _motionDriver;
        StateBridgeBoard _stateBoard;
        StateBridgeView _bridgeView;
        AllyDummy _ally;

        // --- Ulti (active_modes) — 16 Eylül, güven kaygısına karşılık uçtan uca ---
        ActiveModeDirector _modeDirector;
        ActiveModeHud _modeHud;
        // --- Pasifler (Bağlama 5) — ulti gibi ama cooldown'suz, birden fazla aynı anda ---
        PassiveDirector _passiveDirector;
        PassiveHud _passiveHud;
        // --- Zincir (Bağlama 6) — son N cast elementi; Links geçmişe yazılmaz ---
        ChainDirector _chainDirector;
        ChainRules _chainRules;
        readonly Queue<int> _recentCastElements = new();
        float _pendingChainBonus = 1f;   // bir sonraki kapanış (links / finisher_mult)
        float _closingChainBonus = 1f;  // bu kapanışta ApplyClosing* çarpanı
        ChainStepResult _lastChainStep = ChainStepResult.None;
        string _lastFinisherAnnounced = string.Empty;
        PentagonView _pentagonView;
        PlayerResource _playerResource;
        PlayerCooldown _playerCooldown;
        double _lastDamageDealtMs = double.NegativeInfinity;
        double _lastMovedMs = double.NegativeInfinity;
        float _modeHpDrainAccum;

        SkillMotor Skills => _skills ??= SkillMotorLoader.LoadOrDefault();

        DamageCalculator EnsureDamageCalculator()
        {
            if (_damageCalculator != null)
                return _damageCalculator;

            const string resourcePath = "ElementSystem/element-sistemi";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    // Play'te crit ara sıra çıksın diye seed sabit değil.
                    _damageCalculator = DamageCalculator.FromElementSystemJson(
                        asset.text,
                        seed: unchecked((int)System.DateTime.UtcNow.Ticks));
                    return _damageCalculator;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"DamageCalculator JSON okunamadı: {e.Message}");
                }
            }

            // Resources yoksa: docs/element-sistemi.json crit_system varsayılanları
            // (base 0.05 / mult 2.0 / max 0.75) — sayı uydurma yok.
            _damageCalculator = new DamageCalculator(
                seed: unchecked((int)System.DateTime.UtcNow.Ticks),
                baseCritChance: 0.05f,
                critMultiplier: 2f,
                maxCritChance: 0.75f,
                adjectiveCritBonus: null);
            return _damageCalculator;
        }

        struct PendingClosing
        {
            public LivingEffectView View;
            public ClosingHit Closing;
            public double BangAtWorldMs;
            public List<SentenceWord> Words;
            public bool IsBasicStrike;
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

        public int ActiveCount => _active.Count;

        /// <summary>Bağlama 5 / MCP: Bind sonrası pasif durum makinesi (null = henüz bağlanmadı).</summary>
        public PassiveDirector PassiveDirector => _passiveDirector;

        /// <summary>Bağlama 6 / MCP: Bind sonrası zincir durum makinesi.</summary>
        public ChainDirector ChainDirector => _chainDirector;

        /// <summary>Bağlama 6 / MCP: son duyurulan Finisher metni (boş = henüz yok).</summary>
        public string LastFinisherAnnounced => _lastFinisherAnnounced;

        /// <summary>Bağlama 6 / MCP: bir sonraki kapanışa bekleyen Links/finisher çarpanı.</summary>
        public float PendingChainBonus => _pendingChainBonus;

        /// <summary>Editör/prob: Update beklemeden cümle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }

        public void Bind(
            GameClock clock,
            PentagonInput input,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            BossVitals bossVitals,
            GroundScarField scars,
            PrototypeTuning colors,
            DamageNumberHud damageHud = null,
            BossDirector bossDirector = null,
            ActorStatus playerStatus = null,
            ActorStatus bossStatus = null,
            SentenceDebugHud debugHud = null,
            ReactionReadout readout = null,
            FollowCamera camera = null,
            AllyDummy ally = null,
            ActiveModeHud modeHud = null,
            PentagonView pentagonView = null,
            PassiveHud passiveHud = null)
        {
            _clock = clock;
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _visual = player != null ? player.GetComponent<ActorVisual>() : null;
            _boss = boss;
            _bossVitals = bossVitals;
            _scars = scars;
            _damageHud = damageHud;
            _bossDirector = bossDirector;
            _motor = player.GetComponent<KinematicMotor>();
            _playerStatus = playerStatus;
            _bossStatus = bossStatus;
            _debugHud = debugHud;
            _readout = readout;
            _camera = camera;
            _ally = ally;
            _modeHud = modeHud;
            _passiveHud = passiveHud;
            _pentagonView = pentagonView;
            _playerResource = player != null ? player.GetComponent<PlayerResource>() : null;
            _playerCooldown = player != null ? player.GetComponent<PlayerCooldown>() : null;
            _skills = SkillMotorLoader.LoadOrDefault();
            _modeDirector = new ActiveModeDirector(_skills.ActiveModes);
            _passiveDirector = new PassiveDirector(_skills.Passives);
            _chainRules = LoadChainRulesOrDefault();
            _chainDirector = new ChainDirector(_skills.Chains, _chainRules);
            _recentCastElements.Clear();
            _pendingChainBonus = 1f;
            _closingChainBonus = 1f;
            _lastChainStep = ChainStepResult.None;
            _lastFinisherAnnounced = string.Empty;
            if (_playerStatus != null)
                _playerStatus.ModeDirector = _modeDirector;

            _motionDriver = player.GetComponent<SkillMotionDriver>();
            if (_motionDriver == null)
                _motionDriver = player.gameObject.AddComponent<SkillMotionDriver>();
            _motionDriver.Bind(clock, colors);

            _stateBoard = new StateBridgeBoard();
            _bridgeView = FindAnyObjectByType<StateBridgeView>();
            if (_bridgeView == null)
            {
                var bridgeGo = new GameObject("StateBridge");
                _bridgeView = bridgeGo.AddComponent<StateBridgeView>();
            }
            _bridgeView.Bind(_stateBoard);

            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
        }

        void OnDestroy()
        {
            if (_engine != null && _hooked)
                _engine.SentenceCompleted -= OnSentenceCompleted;
        }

        void Update()
        {
            if (_engine == null || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);

            // Kilit kesildi (§5): poz da kesilir. Kapanış patlaması kesilmez, kendi
            // zamanlamasıyla gelir (TickPendingClosings).
            if (_posedForRecovery && _engine.State.Phase != SentencePhase.Recovering)
            {
                _pose?.EndRecovery();
                _posedForRecovery = false;
            }

            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.01f)
                _lastMovedMs = worldMs;

            SyncFromSentence(worldMs);
            ApplyWindowCue();
            TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            TickPendingClosings(worldMs);
            TickBossDeath();
            TickStateBridge(worldMs);
            TickActiveMode(worldMs, dtSec);
            TickPassives(worldMs);
        }

        // --- Ulti (active_modes) ---

        ActiveModeContext BuildModeContext(double worldMs)
        {
            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            float hpRatio = vitals != null && vitals.MaxHp > 0 ? (float)vitals.Hp / vitals.MaxHp : 1f;
            float allyRatio = _ally != null ? _ally.Ratio : 1f;

            int debuffCount = 0;
            if (_playerStatus != null)
            {
                foreach (StatusKind kind in _playerStatus.Board.ActiveKinds)
                    if (StatusKindUtil.IsDebuff(kind)) debuffCount++;
            }

            return new ActiveModeContext
            {
                HpRatio = hpRatio,
                // 16 Eylül: ally'nin StatusBoard'u yok (bilinen açık, docs/durum.md) — team
                // debuff sayısı şimdilik yalnızca oyuncudan okunur.
                MinTeamHpRatio = Mathf.Min(hpRatio, allyRatio),
                SecondsSinceLastDamageDealt = (worldMs - _lastDamageDealtMs) / 1000.0,
                SecondsSinceLastMoved = (worldMs - _lastMovedMs) / 1000.0,
                TeamDebuffCount = debuffCount,
            };
        }

        void TickActiveMode(double worldMs, float dtSec)
        {
            if (_modeDirector == null)
                return;

            if (_modeDirector.Active == null)
            {
                _modeHpDrainAccum = 0f;
                return;
            }

            // Sürekli maliyet: HP/sn (Öfke Patlaması) — tam sayıya birikip öyle uygulanır,
            // yoksa 60 FPS'te her kare 0'a yuvarlanan hasar hiç işlemez.
            float hpPct = _modeDirector.HpPerSecPercentCost;
            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            if (hpPct > 0f && vitals != null && !vitals.IsDown)
            {
                _modeHpDrainAccum += vitals.MaxHp * (hpPct / 100f) * dtSec;
                int whole = Mathf.FloorToInt(_modeHpDrainAccum);
                if (whole > 0)
                {
                    _modeHpDrainAccum -= whole;
                    vitals.ApplyDamage(whole);
                }
            }

            ActiveModeContext ctx = BuildModeContext(worldMs);
            if (_modeDirector.Tick(worldMs, ctx))
            {
                _modeHud?.Hide();
                _modeHpDrainAccum = 0f;
            }
            else
            {
                _modeHud?.UpdateRemaining(_modeDirector.RemainingSec(worldMs));
            }
        }

        /// <summary>Dört-aynı-rün kapanışı geldiğinde (X-X-X-X) ulti tetiklenip tetiklenmediğine bakar.</summary>
        void TryActivateMode(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (_modeDirector == null || words == null || words.Count != 4 || _modeDirector.Active != null)
                return;

            int dot = (int)words[0].Rune;
            for (int i = 1; i < words.Count; i++)
                if ((int)words[i].Rune != dot)
                    return; // aynı elementin 4'lüsü değil — sıradan 4'lü cümle, ulti değil

            ActiveModeContext ctx = BuildModeContext(worldMs);
            ActiveModeNode? activated = _modeDirector.TryTrigger(dot, ctx, worldMs);
            if (activated == null)
                return;

            ActiveModeNode mode = activated.Value;
            Color tint = _colors != null ? _colors.ColorForRune((Rune)dot) : Color.white;
            _readout?.NoteSkill(mode.Name, mode.ReadAs, tint);
            _debugHud?.NoteSkillBang(mode.Name, mode.ReadAs);
            _modeHud?.ShowActivated(mode.Name, mode.ReadAs, tint);
            ApplyModeOneShotEffects(mode);
        }

        /// <summary>
        /// team_full_cleanse / team_invulnerability_sec / enemy_blind_sec / team_regen_per_sec —
        /// aktivasyon anında bir kez uygulanır (sürekli tick TickActiveMode'da değil, burada).
        /// </summary>
        void ApplyModeOneShotEffects(ActiveModeNode mode)
        {
            if (mode.GetEffectBool("team_full_cleanse") && _playerStatus != null)
                _playerStatus.Board.CleanseHostile();

            float invulnSec = mode.GetEffect("team_invulnerability_sec");
            if (invulnSec > 0f && _playerStatus != null)
                _playerStatus.Board.Apply(StatusKind.Stasis, invulnSec * 1000.0, 1f);

            float blindSec = mode.GetEffect("enemy_blind_sec");
            if (blindSec > 0f && _bossStatus != null)
                _bossStatus.Board.Apply(StatusKind.Blind, blindSec * 1000.0, 1f);

            float regenPerSec = mode.GetEffect("team_regen_per_sec");
            if (regenPerSec > 0f && mode.HasDuration && _playerStatus != null)
                _playerStatus.Board.Apply(StatusKind.Regen, mode.DurationSec * 1000.0, regenPerSec);
        }

        // --- Pasifler (Bağlama 5) ---

        void TickPassives(double worldMs)
        {
            if (_passiveDirector == null)
                return;

            _passiveDirector.Tick(worldMs);
            _passiveHud?.Sync(_passiveDirector.Active, worldMs);
        }

        /// <summary>
        /// Kapanıştaki rün dizisi bir pasifin trigger_combo'suyla eşleşirse açar.
        /// Ulti'den farkı: cooldown yok; birden fazla pasif aynı anda aktif olabilir.
        /// </summary>
        void TryTriggerPassive(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (_passiveDirector == null || words == null || words.Count == 0)
                return;

            var dots = new int[words.Count];
            for (int i = 0; i < words.Count; i++)
                dots[i] = (int)words[i].Rune;

            PassiveNode? triggered = _passiveDirector.TryTrigger(dots, worldMs);
            if (triggered == null)
                return;

            PassiveNode p = triggered.Value;
            Color tint = Color.cyan;
            if (_colors != null && p.TriggerCombo != null && p.TriggerCombo.Length > 0)
                tint = _colors.ColorForRune((Rune)p.TriggerCombo[0]);
            _readout?.NoteSkill(p.Id.Replace('_', ' '), p.Element, tint);
            _debugHud?.NoteSkillBang(p.Id, p.Element);
            _passiveHud?.Sync(_passiveDirector.Active, worldMs);
        }

        /// <summary>
        /// Bağlama 6: kapanışın fiil elementi (ilk rün) kuyruğa + ChainDirector.
        /// Links / finisher_mult geçmiş cast'e yazılmaz — yalnızca bir sonraki kapanışa
        /// (_pendingChainBonus). Bu kapanış önceki pending'i tüketir.
        /// </summary>
        float BeginChainClosing(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            float bonusForThis = _pendingChainBonus;
            _pendingChainBonus = 1f;
            _lastChainStep = ChainStepResult.None;

            if (_chainDirector == null || words == null || words.Count == 0)
                return bonusForThis;

            int element = (int)words[0].Rune;
            if (element < 1)
                return bonusForThis;

            _recentCastElements.Enqueue(element);
            int max = _chainRules.MaxChainLength > 0 ? _chainRules.MaxChainLength : 6;
            while (_recentCastElements.Count > max)
                _recentCastElements.Dequeue();

            _lastChainStep = _chainDirector.RegisterCast(element, worldMs);

            if (_lastChainStep.FinisherTriggered)
            {
                float fin = _chainRules.FinisherMult;
                _pendingChainBonus = fin > 0f ? fin : 1f;
            }
            else if (_lastChainStep.Matched)
            {
                float link = _lastChainStep.LinkBonus;
                _pendingChainBonus = link > 0f ? link : 1f;
            }

            return bonusForThis;
        }

        void AnnounceChainFinisherIfAny()
        {
            if (!_lastChainStep.FinisherTriggered)
                return;

            string finisher = _lastChainStep.Finisher;
            if (string.IsNullOrEmpty(finisher))
                return;

            _lastFinisherAnnounced = finisher;
            string element = _lastChainStep.Chain != null
                ? _lastChainStep.Chain.Value.Element
                : string.Empty;
            Color tint = Color.cyan;
            if (_colors != null && _lastChainStep.Chain != null)
            {
                // pattern ilk digit = çapa elementi (1..6)
                int dot = FirstPatternDigit(_lastChainStep.Chain.Value.Pattern);
                if (dot >= 1 && dot <= 6)
                    tint = _colors.ColorForRune((Rune)dot);
            }

            _readout?.NoteSkill(finisher, string.IsNullOrEmpty(element) ? "zincir" : element + " zincir", tint);
            _debugHud?.NoteSkillBang(finisher, "finisher");
        }

        static int FirstPatternDigit(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return 0;
            string[] tokens = pattern.Split('-');
            if (tokens.Length == 0)
                return 0;
            return int.TryParse(tokens[0].Trim(), out int d) ? d : 0;
        }

        static ChainRules LoadChainRulesOrDefault()
        {
            const string resourcePath = "ElementSystem/element-sistemi";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    return ChainRules.FromJsonRoot(MiniJson.Parse(asset.text));
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"ChainRules JSON okunamadı: {e.Message}");
                }
            }

            return ChainRules.DefaultFromSpec;
        }

        void TickStateBridge(double worldMs)
        {
            if (_stateBoard == null || _combat == null)
                return;

            var motion = _combat.SkillMotion;
            motion.ArenaHalfSizeM = _colors != null ? _colors.ArenaHalfSizeM : motion.ArenaHalfSizeM;
            _stateBoard.Tick(worldMs, motion);
            _bridgeView?.Sync();

            if (_player == null || _motionDriver == null || _motionDriver.IsDisplacing)
                return;
            if (_playerStatus != null && _playerStatus.Board.BlocksMovement)
                return;

            Vector3 p = _player.position;
            if (_stateBoard.TryTraverse(p.x, p.z, worldMs, motion, out float dx, out float dz))
            {
                _motionDriver.WarpInstant(dx, dz);
                _readout?.NoteSkill("Portal", "köprü geçişi", new Color(0.55f, 0.4f, 1f));
            }
        }

        void TickBossDeath()
        {
            if (!_deathPending || _clock == null)
                return;

            if (_clock.Director.WorldTimeMs < _deathReviveAtMs)
                return;

            _deathPending = false;
            _bossVitals?.Revive();
            _boss?.EndCollapse();
            _bossDirector?.NotifyBossRevived(_clock.Director.WorldTimeMs);
        }

        void SyncFromSentence(double worldMs)
        {
            var state = _engine.State;
            if (state.Phase != SentencePhase.Building)
                return;

            int count = state.Words.Count;
            if (count == 0)
                return;

            // Kimliğe göre karar: elde yaşayan (Building'e ait) etki yoksa spawn et; varsa
            // sadece SetWords çağır. Sayı polling'i (count==1) EnhancedTouch'ın bir karede
            // birden fazla nokta kaydettiği durumda 0→2 sıçrayıp spawn'ı hiç tetiklemeyebilir.
            if (_buildingView == null || _buildingView.Logic == null
                || _buildingView.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                _buildingView = SpawnEffect(state.Words, worldMs);
                PulseActor(state.Words[0].Rune, state.Words, worldMs);
                _lastWordCount = count;
                return;
            }

            _buildingView.Logic.SetWords(state.Words);
            ApplySkillTint(_buildingView, state.Words);
            if (count > _lastWordCount)
                PulseActor(state.Words[count - 1].Rune, state.Words, worldMs);

            _lastWordCount = count;
        }

        void PulseActor(Rune rune, IReadOnlyList<SentenceWord> words, double worldMs)
        {
            FaceBoss();
            _pose?.PulseRune(rune, worldMs);
            if (_visual == null)
                return;

            EffectSilhouette s = words != null && words.Count > 0
                ? SilhouetteBuilder.FromWords(words, _combat?.Manifestation)
                : default;
            _visual.PulseRune(rune, s);
        }

        void FaceBoss()
        {
            if (_player == null || _boss == null)
                return;
            Vector3 to = _boss.transform.position - _player.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
                return;
            _player.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void ApplyWindowCue()
        {
            if (_buildingView == null)
                return;

            var state = _engine.State;
            if (state.Phase != SentencePhase.Building || state.ArmedWindowMs <= 0.5)
            {
                _buildingView.SetWindowCue(1f);
                return;
            }

            _buildingView.SetWindowCue((float)(state.RemainingWindowMs / state.ArmedWindowMs));
        }

        LivingEffectView SpawnEffect(IReadOnlyList<SentenceWord> words, double worldMs, bool basicStrike = false)
        {
            _ = worldMs;
            Vector3 pos = _player.position;
            Vector3 facing = _player.forward;
            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.05f)
                facing = _motor.Velocity.normalized;
            if (_boss != null)
            {
                Vector3 toBoss = _boss.transform.position - pos;
                toBoss.y = 0f;
                if (toBoss.sqrMagnitude > 0.01f)
                    facing = toBoss.normalized;
            }

            ManifestationTuning man = _combat.Manifestation;
            if (basicStrike)
                man = man.WithBasicStrikeProfile();

            var logic = new LivingEffect(
                words[0].Rune,
                pos.x,
                pos.z,
                facing.x,
                facing.z,
                words,
                man);

            var go = new GameObject(basicStrike ? "LivingEffect_BasicStrike" : "LivingEffect_" + words[0].Rune);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<LivingEffectView>();
            view.Bind(logic, man, _colors, basicStrike);
            ApplySkillTint(view, words);
            _active.Add(view);
            return view;
        }

        void ApplySkillTint(LivingEffectView view, IReadOnlyList<SentenceWord> words)
        {
            if (view == null || words == null || words.Count == 0)
                return;

            SkillFeel.ElementPalette(words, _colors, out Color line, out Color blob);
            view.SetSkillTint(line, blob);
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            LivingEffectView view = _buildingView;
            _buildingView = null;
            _lastWordCount = 0;

            if (sentence.Phase == SentencePhase.Aborted || !sentence.Closing.HasValue)
            {
                if (view != null && view.Logic != null)
                    view.Logic.Abort();
                return;
            }

            // Düz vuruş (T6.2) OnDotTouched + Commit'i AYNI karede çağırır: Director'ın Update'i
            // araya girmediği için Building fazı hiç görülmez ve spawn yutulur. Ödenmiş kapanış
            // dünyada mutlaka yaşamak zorunda (§8/T2), o yüzden burada doğuyor. Elde yaşayan bir
            // etki ARAMIYORUZ — önceki cümlenin hâlâ patlayan etkisine bu kapanışı bağlamak
            // yanlış hedefe ödeme yapmak olur.
            // T14: Building hiç görülmeden spawn + tek kelime = düz vuruş YALNIZCA
            // merkezin BasicStrikeDot fiiliyse. Tek Su/Hava vb. skill cümlesi jab sayılmaz.
            bool spawnedForBasicStrike = false;
            if (view == null || view.Logic == null
                || view.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                int basicDot = _colors != null ? _colors.BasicStrikeDot : 1;
                spawnedForBasicStrike = sentence.Words.Count == 1
                    && (int)sentence.Words[0].Rune == basicDot;
                view = SpawnEffect(sentence.Words, _clock.Director.WorldTimeMs, spawnedForBasicStrike);
                if (spawnedForBasicStrike)
                {
                    FaceBoss();
                    _visual?.PulseBasicStrike();
                    _pose?.PulseRune(sentence.Words[0].Rune, _clock.Director.WorldTimeMs);
                }
                else
                {
                    PulseActor(sentence.Words[0].Rune, sentence.Words, _clock.Director.WorldTimeMs);
                }
            }

            // Kapanış kurulmadan önce son kelime listesi etkiye iletilir — dördüncü kelime
            // (cümlenin en pahalı sıfatı) burada gelmezse hedef silüete hiç işlemez, çünkü
            // SentenceEngine dördüncü noktada cümleyi dokunuş anında çözer ve SyncFromSentence
            // artık Building fazında değilken çalışmaz. LivingEffect.SetWords AwaitingClosing
            // fazında da kabul eder, sıra önemli değil.
            view.Logic.SetWords(sentence.Words);

            ClosingHit closing = sentence.Closing.Value;
            view.Logic.ArmClosing(closing);

            float recoverySec = _combat.Sentence.StepForDots(closing.DotCount).RecoverySec;
            double bangAt = _clock.Director.WorldTimeMs
                            + recoverySec * 1000.0
                            + _combat.Feel.PostHitSilenceMs;

            _pose?.BeginRecovery(recoverySec, _clock.Director.WorldTimeMs);
            _posedForRecovery = true;
            // Merkez düz vuruş: IsBasicStrike yalnızca BasicStrikeDot ile spawn edilen view.
            bool basic = view != null && view.IsBasicStrike;
            _pending.Add(new PendingClosing
            {
                View = view,
                Closing = closing,
                BangAtWorldMs = bangAt,
                Words = new List<SentenceWord>(sentence.Words),
                IsBasicStrike = basic
            });
        }

        /// <summary>Editör/prob: kapanış bang zamanını zorla işle (heal vb.).</summary>
        public void ForceTickClosings()
        {
            if (_clock == null)
                return;
            TickPendingClosings(_clock.Director.WorldTimeMs);
        }

        /// <summary>MCP: bekleyen kapanışları hemen ateşle (bang'i şimdiye çeker).</summary>
        public void ForceFirePendingClosings()
        {
            if (_clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingClosing p = _pending[i];
                p.BangAtWorldMs = now;
                _pending[i] = p;
            }
            TickPendingClosings(now);
        }

        void TickPendingClosings(double worldMs)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                if (p.View == null || p.View.Logic == null)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (p.View.Logic.Phase is LivingEffectPhase.Fading or LivingEffectPhase.Dead)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (worldMs < p.BangAtWorldMs)
                    continue;

                FireClosing(p);
                _pending.RemoveAt(i);
            }
        }

        void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View.Logic;
            logic.FireClosingBang();
            StampScar(p.View, p.Closing);
            _closingChainBonus = BeginChainClosing(p.Words, _clock.Director.WorldTimeMs);
            TryActivateMode(p.Words, _clock.Director.WorldTimeMs);
            TryTriggerPassive(p.Words, _clock.Director.WorldTimeMs);

            // Düz vuruş: jab — skill motoru / isim bang'i / kamera yumruğu yok (Ateş vb. yazmasın).
            // Heal vb. tek-rün skill asla IsBasicStrike olmamalı; yine de mend kaçmasın.
            if (p.IsBasicStrike || (p.View != null && p.View.IsBasicStrike))
            {
                SkillResolution basicSkill = ResolvePendingSkill(p);
                if (IsHealSkill(basicSkill))
                {
                    ApplyResourceCost(basicSkill);
                    ApplyClosingStatuses(p, basicSkill);
                    ShoutSkill(basicSkill, p.Words);
                    ApplyClosingHeal(p.Closing, basicSkill);
                    ApplyCooldown(basicSkill, p.Words, cosmeticIfDisabled: true);
                    AnnounceChainFinisherIfAny(); // ShoutSkill sonrası — Finisher readout kalsın
                    return;
                }

                ApplyResourceCost(basicSkill);
                ApplyBossClosingBasic(logic, p.Closing);
                ApplyClosingDamage(p.Closing, SkillResolution.Empty, isBasicStrike: true, slashCommitMult: 0f);
                // Kozmetik radial yoktu; EnforceCooldown=true iken tracker yine yazar.
                ApplyCooldown(basicSkill, p.Words, cosmeticIfDisabled: false);
                AnnounceChainFinisherIfAny();
                return;
            }

            SkillResolution skill = ResolvePendingSkill(p);
            ApplyResourceCost(skill);
            SkillMotionPlan motionPlan = ResolveSkillMotion(skill);
            ApplySkillMotion(motionPlan, skill);
            ApplyBossClosing(logic, p.Closing, skill);
            ApplyClosingDamage(p.Closing, skill, isBasicStrike: false, motionPlan.SlashCommitMult);
            ApplyClosingStatuses(p, skill);
            ShoutSkill(skill, p.Words);
            ApplyClosingHeal(p.Closing, skill); // readout ShoutSkill'den sonra (ally +N kalsın)
            ApplyCooldown(skill, p.Words, cosmeticIfDisabled: true);
            if (!motionPlan.IsEmpty)
                AnnotateMotion(skill, motionPlan);
            AnnounceChainFinisherIfAny(); // skill bang'ten sonra Finisher üstte kalsın
        }

        /// <summary>
        /// Bağlama 2: base_resource_cost düşer; yetersiz mana cast'i engellemez (0'a kilit).
        /// </summary>
        void ApplyResourceCost(SkillResolution skill)
        {
            if (_playerResource == null || skill.IsEmpty)
                return;
            float cost = skill.BaseResourceCost;
            if (cost <= 0f)
                return;
            _playerResource.Consume(cost);
        }

        /// <summary>
        /// EnforceCooldown=false: cosmeticIfDisabled ise Görev 12 kozmetik radial (birebir).
        /// true: CooldownTracker + radial gerçek kalan süre (basic dahil).
        /// </summary>
        void ApplyCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words, bool cosmeticIfDisabled)
        {
            if (skill.IsEmpty || words == null || words.Count == 0)
                return;

            bool enforce = _combat != null && _combat.EnforceCooldown;
            if (!enforce)
            {
                if (cosmeticIfDisabled)
                    PulseCosmeticCooldown(skill, words);
                return;
            }

            if (_playerCooldown == null || string.IsNullOrEmpty(skill.VerbId))
                return;

            float sec = skill.BaseCooldownSec;
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            if (!_playerCooldown.TryBeginCast(skill.VerbId, sec, worldMs))
                return;

            if (_pentagonView == null || sec <= 0f)
                return;

            _pentagonView.BeginTrackedCooldown(
                (int)words[0].Rune,
                skill.VerbId,
                sec,
                _playerCooldown,
                _clock);
        }

        /// <summary>
        /// ui_rules.cooldown_display — yalnızca görsel (EnforceCooldown=false).
        /// Fiil rünü (ilk kelime) etrafında base_cooldown_sec kadar radial dolum.
        /// </summary>
        void PulseCosmeticCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (_pentagonView == null || skill.IsEmpty || words == null || words.Count == 0)
                return;
            float sec = skill.BaseCooldownSec;
            if (sec <= 0f)
                return;
            _pentagonView.BeginCosmeticCooldown((int)words[0].Rune, sec);
        }

        SkillMotionPlan ResolveSkillMotion(SkillResolution skill)
        {
            if (skill.IsEmpty || _combat == null || _player == null)
                return SkillMotionPlan.None;

            var t = _combat.SkillMotion;
            t.ArenaHalfSizeM = _colors != null ? _colors.ArenaHalfSizeM : t.ArenaHalfSizeM;

            Vector3 face = _player.forward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f)
                face = Vector3.forward;

            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            bool bossAlive = _bossVitals == null || !_bossVitals.IsDown;

            var ctx = new SkillMotionContext(
                _player.position.x, _player.position.z,
                face.x, face.z,
                bossPos.x, bossPos.z,
                bossAlive,
                t.ArenaHalfSizeM);

            return SkillMotionMotor.Resolve(skill, ctx, t);
        }

        void ApplySkillMotion(in SkillMotionPlan plan, SkillResolution skill)
        {
            if (plan.IsEmpty || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            if (plan.Kind == SkillMotionKind.PlaceMark)
            {
                _stateBoard?.PlaceMark(plan.MarkType, plan.DestX, plan.DestZ, worldMs, _combat.SkillMotion);
                _bridgeView?.Sync();
                return;
            }

            _motionDriver?.Play(plan, worldMs);
        }

        void AnnotateMotion(SkillResolution skill, in SkillMotionPlan plan)
        {
            string tag = plan.Kind switch
            {
                SkillMotionKind.ZenitsuPass => "Zenitsu geçiş",
                SkillMotionKind.ShortBlink => "ışınlanma",
                SkillMotionKind.ForwardDash => "dash",
                SkillMotionKind.PlaceMark => "işaret",
                _ => null
            };
            if (tag == null) return;
            _debugHud?.NoteSkillBang(skill.DisplayName, tag);
        }

        SkillResolution ResolvePendingSkill(PendingClosing p)
        {
            if (_skills == null || p.Words == null || p.Words.Count == 0)
                return SkillResolution.Empty;
            return _skills.ResolveWords(p.Words);
        }

        void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (skill.IsEmpty)
                return;

            string mech = SkillFeel.MechanicShort(skill.Mechanics);
            SkillFeel.ElementPalette(words, _colors, out Color line, out _);
            _debugHud?.NoteSkillBang(skill.DisplayName, mech);
            string sub = skill.VerbName;
            if (!string.IsNullOrEmpty(mech))
                sub = string.IsNullOrEmpty(sub) ? mech : sub + "  ·  " + mech;
            _readout?.NoteSkill(skill.DisplayName, sub, line);
            SkillFeel.CameraKick(skill.VerbFamily, _camera, _colors);
        }

        void ApplyClosingStatuses(PendingClosing p, SkillResolution skill)
        {
            if (skill.IsEmpty)
                return;
            if (_playerStatus == null && _bossStatus == null)
                return;

            var result = StatusApplicator.ApplySkill(
                skill,
                _playerStatus != null ? _playerStatus.Board : null,
                _bossStatus != null ? _bossStatus.Board : null,
                _combat != null ? _combat.Status : new StatusTuning());

            if (result.Knockback && _bossStatus != null && _player != null)
                _bossStatus.ApplyKnockbackFrom(_player.position);

            // 16 Eylül: "skilleri attığımda bir etkileşim göremiyorum" raporu — durum
            // etkileşim tablosu (docs/element-sistemi.json status_interaction_table) mekanik
            // olarak zaten çalışıyordu, hiçbir görsel sinyali yoktu. Tetiklenen kural varsa
            // aynı tepki yazısı kanalını kullan (§10: kırmızı-turuncu yasak → AcidGreen).
            if (result.TriggeredReactions.Count > 0 && _readout != null)
            {
                StatusReactionRule rule = result.TriggeredReactions[0];
                _readout.NoteSkill(rule.Name, rule.ReadAs, _colors.AcidGreen);
                _debugHud?.NoteSkillBang(rule.Name, rule.ReadAs);
            }
        }

        /// <summary>
        /// Mend / heal / regen — daha boş olana basar (oran). Ally full ise oyuncu.
        /// Miktar: TotalEffect × ClosingDamagePerEffect (commit ile aynı birim).
        /// </summary>
        void ApplyClosingHeal(ClosingHit closing, SkillResolution skill)
        {
            if (skill.IsEmpty)
                return;
            if (!IsHealSkill(skill))
                return;

            float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
            // 16 Eylül: "Kavurucu Yara" (grievous_wounds+burn) — yanık hedefe gelen heal azalır.
            // Hedefin StatusBoard'u yoksa (ör. AllyDummy) çarpan 1f, davranış eskisiyle aynı.
            float healMult = _playerStatus != null ? _playerStatus.Board.HealEffectivenessMult : 1f;
            healMult *= _passiveDirector?.HealMult ?? 1f;
            healMult *= _closingChainBonus; // Bağlama 6: önceki link/finisher → bu kapanış
            int amount = Mathf.Max(1, Mathf.RoundToInt(closing.TotalEffect * per * healMult));
            if (amount <= 0)
                return;

            var playerVitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            bool allyNeeds = _ally != null && _ally.Hp < _ally.MaxHp;
            bool selfNeeds = playerVitals != null && !playerVitals.IsDown && playerVitals.Hp < playerVitals.MaxHp;
            if (!allyNeeds && !selfNeeds)
            {
                _readout?.NoteSkill(skill.DisplayName, "zaten full", new Color(0.7f, 0.9f, 0.75f));
                return;
            }

            bool healAlly = false;
            if (allyNeeds && selfNeeds)
            {
                float allyR = _ally.Ratio;
                float selfR = (float)playerVitals.Hp / playerVitals.MaxHp;
                // Eşitse kendine — "kendime heal" denemesi.
                healAlly = allyR < selfR;
            }
            else
                healAlly = allyNeeds;

            int healed;
            if (healAlly)
            {
                healed = _ally.ApplyHeal(amount);
                if (healed > 0)
                {
                    _damageHud?.ShowDamage(-healed);
                    _readout?.NoteSkill(skill.DisplayName, "ally +" + healed, new Color(0.4f, 1f, 0.65f));
                    _debugHud?.NoteSkillBang(skill.DisplayName, "ally +" + healed);
                }
                return;
            }

            healed = playerVitals.ApplyHeal(amount);
            if (healed > 0)
            {
                _modeDirector?.NotifyHealed(); // "healer iyileştirirse biter" (Kan Çılgınlığı)
                _damageHud?.ShowDamage(-healed);
                _readout?.NoteSkill(skill.DisplayName, "self +" + healed, new Color(0.4f, 1f, 0.65f));
                _debugHud?.NoteSkillBang(skill.DisplayName, "self +" + healed);
            }
        }

        static bool IsHealSkill(SkillResolution skill)
        {
            if (string.Equals(skill.VerbFamily, "mend", System.StringComparison.Ordinal))
                return true;
            string action = skill.Action ?? string.Empty;
            return action is "heal" or "regen" or "cleanse" or "area_cleanse" or "holy_shield";
        }

        /// <summary>
        /// Commit (§5 TotalEffect × ClosingDamagePerEffect) × skill fiil ölçeği.
        /// Heal/dash BaseDamage=0 → 0 can; status ayrı. Tür hasarı değiştirmez (§12).
        /// UseFormulaDamage=true → DamageCalculator (resistance/weakness nötr 0/1).
        /// </summary>
        void ApplyClosingDamage(ClosingHit closing, SkillResolution skill, bool isBasicStrike, float slashCommitMult)
        {
            if (_bossVitals == null || _bossVitals.IsDown)
                return;

            float outMult = 1f;
            if (_playerStatus != null)
                outMult = _playerStatus.Board.OutgoingDamageMult;
            outMult *= _modeDirector?.DamageMult ?? 1f; // ulti: Öfke Patlaması ×1.8, Kan Çılgınlığı ×2.0
            outMult *= _passiveDirector?.DamageMult ?? 1f; // pasif: alev_hiddeti ×1.15 × karanlik_sessizligi ×1.2 …
            outMult *= _closingChainBonus; // Bağlama 6: links/finisher_mult → sonraki (bu) kapanış

            bool isCrit = false;
            float damage;
            if (_combat != null && _combat.UseFormulaDamage &&
                !isBasicStrike && !skill.IsEmpty && skill.BaseDamage > 0f)
            {
                // length.damage_mult JSON'da 1.0 (anti-ladder); SkillResolution taşımıyor.
                DamageHit hit = EnsureDamageCalculator().Compute(
                    in skill,
                    lengthDamageMult: 1f,
                    resistance: 0f,
                    weaknessBonus: 1f);
                damage = hit.Amount * outMult;
                isCrit = hit.WasCrit;
            }
            else
            {
                damage = ClosingDamageMath.Compute(
                    closing.TotalEffect,
                    _combat != null ? _combat.ClosingDamagePerEffect : 1f,
                    skill,
                    isBasicStrike,
                    outMult);
            }

            // Teleport fiili BaseDamage=0; Zenitsu kesisi commit × SlashCommitMult.
            if (damage <= 0f && slashCommitMult > 0f && closing.TotalEffect > 0f)
            {
                float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
                damage = closing.TotalEffect * per * slashCommitMult * outMult;
            }

            if (damage <= 0f)
                return;

            // Armor break boss'ta incoming mult
            if (_bossStatus != null)
                damage *= _bossStatus.Board.IncomingDamageMult;

            _damageHud?.ShowDamage(damage, isCrit);
            _lastDamageDealtMs = _clock.Director.WorldTimeMs; // "dealt_damage_recently" (Öfke Patlaması)

            float lifesteal = (_modeDirector?.Lifesteal ?? 0f) + (_passiveDirector?.LifestealAdd ?? 0f);
            if (lifesteal > 0f)
            {
                var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
                int healAmt = Mathf.RoundToInt(damage * lifesteal);
                if (vitals != null && healAmt > 0)
                    vitals.ApplyHeal(healAmt); // Kan Çılgınlığı kendi hasarından beslenir — NotifyHealed BİLEREK çağrılmaz
            }

            bool killed = _bossVitals.ApplyDamage(damage);
            var bossVisual = _boss != null ? _boss.GetComponent<BossVisual>() : null;
            if (killed)
            {
                double worldMs = _clock.Director.WorldTimeMs;
                // Önce telegrafı kapat (Hide taban ölçeğe çeker), sonra çökme — sıra tersine
                // dönseydi Hide çökmeyi ezerdi.
                _bossDirector?.NotifyBossDown(worldMs);
                float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
                _boss?.BeginCollapse(collapseSec, worldMs);
                // Yavaş çekim kaldırıldı — revive çökme süresi kadar dünya saati sonra.
                _deathReviveAtMs = worldMs + collapseSec * 1000.0;
                _deathPending = true;
                return;
            }

            bossVisual?.PlayStagger();
        }

        // Kapanış izi (bu metot) ve seyahat izi (TickEffects, view.Scarred) iki ayrı bayrak:
        // biri seyahat çatlağının damgalanıp damgalanmadığını, diğeri kapanışın kendi izini
        // takip eder. Aynı bayrağı paylaşınca odaklı SARSINTI (`5-1`) seyahatte çatlak
        // bıraktığı için kapanış izini hiç bırakmıyordu (T7.1).
        void StampScar(LivingEffectView view, ClosingHit closing)
        {
            LivingEffect logic = view != null ? view.Logic : null;
            if (logic == null || _closingStamped.Contains(logic))
                return;

            Vector3 along = new Vector3(logic.DirX, 0f, logic.DirZ);
            Vector3 tip = new Vector3(logic.TipX, 0f, logic.TipZ);
            float scale = view.IsBasicStrike
                ? _combat.Manifestation.BasicStrikeScarScaleM * (0.7f + 0.15f * closing.DotCount)
                : _combat.Manifestation.ScarScaleM * (0.7f + 0.15f * closing.DotCount);

            ScarKind kind = view.IsBasicStrike
                ? ScarKind.Strike
                : closing.Type switch
                {
                    Rune.Aydinlik => ScarKind.Crack,
                    Rune.Ates => ScarKind.Needle,
                    Rune.Su => ScarKind.Swarm,
                    Rune.Toprak => ScarKind.Acid,
                    _ => ScarKind.Crack
                };

            // Hat boyunca çatlak: kökten uca birkaç damga
            if (kind == ScarKind.Crack && logic.Current.Focus > 0.5f)
            {
                Vector3 origin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
                for (int i = 1; i <= 3; i++)
                {
                    float u = i / 3f;
                    _scars.Stamp(Vector3.Lerp(origin, tip, u), scale * 0.85f, kind, along);
                }
            }
            else
            {
                _scars.Stamp(tip, scale, kind, along);
            }

            _closingStamped.Add(logic);
        }

        /// <summary>Düz vuruş jab — hafif tepki, skill ailesi / kamera yumruğu yok.</summary>
        void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            if (_boss == null || (_bossVitals != null && _bossVitals.IsDown))
                return;
            if (!IsClosingInRange(logic, closing))
                return;

            var man = _combat.Manifestation;
            _boss.React(
                new Vector3(logic.OriginX, 0f, logic.OriginZ),
                man.BossKnockbackM * 0.55f,
                0.04f,
                man.BossShakeSec * 0.4f,
                _clock.Director.WorldTimeMs);
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            if (_boss == null || (_bossVitals != null && _bossVitals.IsDown))
                return;

            // Kendine yönelik fiil (mend/guard/purge) boss gövdesini boğmaz — hafif titreşim yeter.
            if (!skill.IsEmpty && StatusApplicator.IsSelfTargeted(skill))
            {
                if (!IsClosingInRange(logic, closing))
                    return;
                _boss.React(
                    new Vector3(logic.OriginX, 0f, logic.OriginZ),
                    _combat.Manifestation.BossKnockbackM * 0.08f,
                    0.04f,
                    _combat.Manifestation.BossShakeSec * 0.35f,
                    _clock.Director.WorldTimeMs);
                return;
            }

            Vector3 from = new Vector3(logic.OriginX, 0f, logic.OriginZ);
            var man = _combat.Manifestation;
            float knock = man.BossKnockbackM;
            float lift = 0f;
            float shake = man.BossShakeSec;
            double worldMs = _clock.Director.WorldTimeMs;

            // Önce SkillMotor ailesi (iş), yoksa son rün (eski silüet tepkisi).
            string family = skill.IsEmpty ? string.Empty : skill.VerbFamily;
            if (!string.IsNullOrEmpty(family))
            {
                switch (family)
                {
                    case "strike":
                        knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                        lift = 0.05f;
                        shake = man.BossShakeSec * 0.55f;
                        break;
                    case "disrupt":
                        knock = man.BossKnockbackM * 0.12f;
                        lift = 0.08f;
                        shake = man.BossShakeSec * 1.6f;
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _boss.React(from, knock, lift, shake * 0.45f, worldMs);
                        _boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        _boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        return;
                    case "control":
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _boss.Pin(0.7f, worldMs);
                        return;
                    case "zone":
                        knock = man.BossKnockbackM * 0.55f;
                        lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                        shake = man.BossShakeSec * 0.9f;
                        break;
                    case "motion":
                        knock = man.BossKnockbackM * 0.9f;
                        lift = 0.12f;
                        shake = man.BossShakeSec * 0.7f;
                        break;
                    case "special":
                        knock = man.BossKnockbackM * 0.25f;
                        lift = 0.2f;
                        shake = man.BossShakeSec * 1.1f;
                        break;
                    default:
                        break;
                }

                if (!IsClosingInRange(logic, closing))
                    return;
                _boss.React(from, knock, lift, shake, worldMs);
                return;
            }

            // Tür = fiziksel tepki ekseni. Hasar miktarı burada yok (§5 + §12).
            switch (closing.Type)
            {
                case Rune.Aydinlik:
                    // Havalandırma — spec §5 açıkça yazar.
                    knock = man.BossKnockbackM * 0.55f;
                    lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                    shake = man.BossShakeSec * 0.9f;
                    break;
                case Rune.Ates:
                    // Tek yöne derin geri tepme (§4 daralt/odakla).
                    knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                    lift = 0.05f;
                    shake = man.BossShakeSec * 0.55f;
                    break;
                case Rune.Su:
                    // Yerinde çok noktalı sarsılma, yer değiştirme az (§4 çoğalt/yay).
                    knock = man.BossKnockbackM * 0.12f;
                    lift = 0.08f;
                    shake = man.BossShakeSec * 1.6f;
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.React(from, knock, lift, shake * 0.45f, worldMs);
                    _boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    _boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    return;
                case Rune.Hava:
                    // Sabitleme — §5.
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.Pin(0.55f, worldMs);
                    return;
                case Rune.Toprak:
                    // Birikinti izi StampScar'da; gövde hafif sarsılır.
                    knock = man.BossKnockbackM * 0.2f;
                    lift = 0f;
                    shake = man.BossShakeSec * 0.7f;
                    break;
            }

            if (!IsClosingInRange(logic, closing))
                return;

            _boss.React(from, knock, lift, shake, worldMs);
        }

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            Vector3 bossPos = _boss.transform.position;
            float dx = bossPos.x - logic.TipX;
            float dz = bossPos.z - logic.TipZ;
            float reach = _combat.Manifestation.ClosingBangRadiusM;
            if (closing.Type == Rune.Aydinlik)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.5f))
                {
                    float radial = Vector2.Distance(
                        new Vector2(bossPos.x, bossPos.z),
                        new Vector2(logic.OriginX, logic.OriginZ));
                    if (radial > logic.TipDistance + reach && radial > reach)
                        return false;
                }

                return true;
            }

            if (dx * dx + dz * dz > reach * reach)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.35f))
                    return false;
            }

            return true;
        }

        void TickEffects(float dtSec, double worldMs)
        {
            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            var man = _combat.Manifestation;
            bool bossDown = _bossVitals != null && _bossVitals.IsDown;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                LivingEffectView view = _active[i];
                if (view == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                LivingEffect logic = view.Logic;
                logic.Tick(dtSec);
                view.TickVisual(dtSec);

                if (!bossDown && logic.Phase == LivingEffectPhase.Traveling && _boss != null && !view.TravelHitDone)
                {
                    if (logic.OverlapsBoss(bossPos.x, bossPos.z, man.TravelHitRadiusM))
                    {
                        view.TravelHitDone = true;
                        _boss.React(
                            new Vector3(logic.OriginX, 0f, logic.OriginZ),
                            man.BossKnockbackM * 0.25f,
                            0.08f * logic.Current.Lift,
                            man.BossShakeSec * 0.45f,
                            worldMs);
                    }
                }

                // Seyahat izi: odaklı sarsıntı hattı yerde hafif çatlak bırakır (T4 erken kanıt)
                if (!view.Scarred && logic.Verb == Rune.Aydinlik && logic.Current.Focus > 0.7f
                    && logic.Travel > 2.5f)
                {
                    Vector3 mid = new Vector3(
                        logic.OriginX + logic.DirX * logic.TipDistance * 0.5f,
                        0f,
                        logic.OriginZ + logic.DirZ * logic.TipDistance * 0.5f);
                    _scars.Stamp(mid, man.ScarScaleM * 0.5f, ScarKind.Crack,
                        new Vector3(logic.DirX, 0f, logic.DirZ));
                    view.Scarred = true;
                }

                if (!logic.IsAlive)
                {
                    _closingStamped.Remove(logic);
                    Destroy(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
