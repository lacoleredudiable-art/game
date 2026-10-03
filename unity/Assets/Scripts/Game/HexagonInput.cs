using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Dovus.Game
{
    /// <summary>
    /// Sağ yarı altıgen çizim girdisi + merkez tap (düz vuruş / erken kapanış) + altıgenin
    /// dışındaki dodge düğmesi (§2). Yalnızca Core motoruna bildirir.
    /// </summary>
    public sealed class HexagonInput : MonoBehaviour
    {
        PrototypeTuning _tuning = new();
        CombatTuning _combat = new();
        SentenceEngine _engine;
        DodgeState _dodge;
        DodgeChargeBank _charges;
        GameClock _clock;
        InkTrail _ink;
        SyllableFeedback _syllable;
        SentenceDebugHud _debugHud;

        int? _fingerId;
        bool _mouseHeld;
        FingerMode _mode;
        Vector2 _pressOrigin;
        Vector2 _lastPos;
        double _pressRealMs;
        int? _activeDot;
        double _dwellWorldMs;
        int _dwellReported;
        Vector2? _lastInkPx;
        bool _eventsHooked;
        // SentenceCompleted OnDotTouched içinde, AddSegment'ten ÖNCE ateşlenir — kapanış
        // segmenti çizildikten sonra Break edilmeli. Bayrak + FlushInkBreak bunu sıralar.
        bool _inkBreakPending;
        bool _sentenceHooked;

        // Denetim B ek — çizim tanıma: iki örnek arası parça taranır (StrokeDotTracker, CoreTests ölçer).
        readonly StrokeDotTracker _stroke = new StrokeDotTracker();
        readonly System.Collections.Generic.List<int> _strokeHits = new System.Collections.Generic.List<int>(6);
        readonly float[] _dotXs = new float[Dovus.Core.Grammar.HexagonLayout.DotCount];
        readonly float[] _dotYs = new float[Dovus.Core.Grammar.HexagonLayout.DotCount];
        int _strokeFedFrame = -1;
        int _strokeAccepted;
        DrawFeedback.DenialKind _strokeDenial;
        float _strokeLengthPx;
        Vector2 _strokePrevPx;
        readonly System.Collections.Generic.List<Vector2> _strokeAcceptedPx =
            new System.Collections.Generic.List<Vector2>(6);
        // Geri bildirim: çizimle kurulan cümle kapanınca şerit parlar + rün adları yazılır.
        bool _drawnSentence;
        bool _inkFlashPending;
        bool _centerStrikeArmed;

        // Çizim parmağından bağımsız ikinci yuva: cümle sürerken panik dodge (§2).
        // O1: kaçış basınca tetiklenir; yuva yalnız parmak kalkana kadar tutulur (kamera almasın).
        int? _dodgeFingerId;

        /// <summary>İkinci rün hâlâ basılıyken yükleme fazı bekler.</summary>
        public bool SkillFingerHeld => _mode == FingerMode.Drawing;

        enum FingerMode
        {
            None,
            CenterPending,
            SwapPending,
            Drawing
        }

        /// <summary>Q tuşu ve dokunmatik silah düğmesi: silah değiştir (K3: Küre'de de).</summary>
        public event System.Action WeaponSwapRequested;

        /// <summary>R tuşu ve silah düğmesinde uzun basma (Küre, JSON orb.hold_sec): küreyi yollar ya da çağırır.</summary>
        public event System.Action OrbCommandRequested;

        /// <summary>
        /// K3: silah düğmesinin uzun basma komutu eşiği (sn). 0 = yok → düğme basınca değiştirir.
        /// Küre kuşanılıyken JSON orb.hold_sec (0,4).
        /// </summary>
        public System.Func<float> SwapHoldCommandSec;

        bool _swapHoldFired;

        /// <summary>Dodge kabul edildi. Süren skill kesilir, kalıp konumu hemen bırakılır.</summary>
        public event System.Action SkillCancelledByDodge;

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        public CombatTuning Combat
        {
            get
            {
                _combat ??= new CombatTuning();
                return _combat;
            }
            set => _combat = value;
        }

        public SentenceEngine Engine => _engine;
        public DodgeState Dodge => _dodge;
        public DodgeChargeBank Charges => _charges;

        /// <summary>Motor bir dokunuşu kabul etti (0 = merkez düz vuruş). Yalnız UI juice için.</summary>
        public event System.Action<int> DotAccepted;

        /// <summary>Çizim geri bildirimi yazısı (metin, tanındı mı) — HexagonView altıgenin üstünde gösterir.</summary>
        public event System.Action<string, bool> DrawCaption;

        /// <summary>16 Eylül: CameraOrbitInput'un "bu parmak zaten çiziyor/dodge'a ait" kontrolü için.</summary>
        public int? ClaimedFingerId => _fingerId;
        public int? ClaimedDodgeFingerId => _dodgeFingerId;

        PlayerVitals _vitals;
        ActorStatus _status;
        PlayerResource _resource;
        PlayerCooldown _cooldown;
        ReactionReadout _readout;
        SkillMotor _skills;
        PlayerStateMachine _playerStates;
        System.Func<bool> _isCasting;
        System.Func<SkillResolution, bool> _skillTargetGate;

        /// <summary>Ölü oyuncu yazamaz ve dodge atamaz (T8.1).</summary>
        public void BindVitals(PlayerVitals vitals) => _vitals = vitals;

        public void BindStatus(ActorStatus status) => _status = status;

        /// <summary>state_machine.player_states — çizim/dodge kapısı.</summary>
        public void BindPlayerStates(PlayerStateMachine states, System.Func<bool> isCasting = null)
        {
            _playerStates = states;
            _isCasting = isCasting;
        }

        /// <summary>
        /// İkinci rün motora verilmeden hedef/menzil kontrolü. false ise cümle ilk ründe
        /// kalır; mana ve cooldown kapanışta harcanmadığı için reddedilen cast ücretsizdir.
        /// </summary>
        public void BindSkillTargetGate(System.Func<SkillResolution, bool> gate) =>
            _skillTargetGate = gate;

        /// <summary>Bağlama 3: EnforceResourceCost kapısı + yetersiz mana readout.</summary>
        public void BindResource(PlayerResource resource, ReactionReadout readout, SkillMotor skills = null)
        {
            _resource = resource;
            _readout = readout;
            _skills = skills;
        }

        /// <summary>Bağlama 4: EnforceCooldown kapısı + soğuma readout (dodge'a dokunmaz).</summary>
        public void BindCooldown(PlayerCooldown cooldown, ReactionReadout readout = null, SkillMotor skills = null)
        {
            _cooldown = cooldown;
            if (readout != null)
                _readout = readout;
            if (skills != null)
                _skills = skills;
        }

        bool InputLocked =>
            (_vitals != null && _vitals.IsDown)
            || (_status != null && _status.Board.BlocksCast);

        // T10: panel açıkken (ayar paneli modal) altıgen girdisi tamamen susar; EnhancedTouch
        // global olduğu için panelin arkasındaki oyun aynı dokunuşu almaya devam ederdi.
        bool PanelBlocking => TuningPanel.IsOpen || V611DebugPanel.IsOpen || BuildSelectScreen.IsOpen;

        public bool TrySetLoadout(RuneLoadout loadout)
        {
            EnsureRuntime();
            if (_engine == null || !_engine.TrySetLoadout(loadout))
                return false;
            _centerStrikeArmed = true;
            SyncPlayerStateFromWorld();
            return true;
        }

        /// <summary>
        /// v6 ladder smoke path: aynı SentenceEngine event zincirinden iki rün cast eder.
        /// ManifestationDirector normal cast gibi alır; ayrı hasar/skill yolu yoktur.
        /// </summary>
        /// <summary>Ayar sahnesi / debug: merkez düz vuruş (BasicStrikeDot), skill cast değil.</summary>
        public bool TryDebugBasicStrike()
        {
            EnsureRuntime();
            if (_engine == null || InputLocked)
                return false;

            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            SyncPlayerStateFromWorld();
            bool engineOk = _engine.State.Phase == SentencePhase.Idle
                || _engine.State.Phase == SentencePhase.Recovering
                || _engine.State.Phase == SentencePhase.Resolved
                || _engine.State.Phase == SentencePhase.Aborted;
            if (!BasicStrikeInput.AllowsCenterStrike(AllowsDrawNow, _centerStrikeArmed, engineOk))
                return false;

            int runeId = _tuning.BasicStrikeDot;
            if (!_engine.BeginBasicStrike(runeId, worldMs))
                return false;
            _engine.Commit();
            DotAccepted?.Invoke(0);
            FlushInkBreak();
            _syllable?.PlayForDot(runeId, 1);
            _debugHud?.NoteBasicStrike();
            return true;
        }

        public bool TryDebugCastSkill(int verbRuneId, int adjectiveRuneId)
        {
            EnsureRuntime();
            if (_engine == null || InputLocked)
                return false;

            int verbSlot = FindSlot(_engine.Loadout, verbRuneId);
            int adjectiveSlot = FindSlot(_engine.Loadout, adjectiveRuneId);
            if (verbSlot <= 0 || adjectiveSlot <= 0)
                return false;

            if (_engine.State.Phase == SentencePhase.Building)
                _engine.Abort();
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            _engine.OnDotTouched(verbSlot, worldMs);
            DotAccepted?.Invoke(verbSlot);
            _syllable?.PlayForDot(verbSlot, 1);
            SkillResolution prospective = _skills.Resolve(new[] { verbRuneId, adjectiveRuneId });
            if (_skillTargetGate != null && !_skillTargetGate(prospective))
            {
                _engine.Abort();
                FlushInkBreak();
                return false;
            }
            _engine.OnDotTouched(adjectiveSlot, worldMs);
            DotAccepted?.Invoke(adjectiveSlot);
            _syllable?.PlayForDot(adjectiveSlot, 2);
            FlushInkBreak();
            DebugConfig.DevLog($"[ElementSystem] smoke cast accepted: {verbRuneId}-{adjectiveRuneId}");
            return true;
        }

        static int FindSlot(RuneLoadout loadout, int runeId)
        {
            if (loadout == null)
                return 0;
            for (int slot = 1; slot <= RuneLoadout.SlotCount; slot++)
                if (loadout.RuneIdAtSlot(slot) == runeId)
                    return slot;
            return 0;
        }

        public void Bind(
            GameClock clock,
            InkTrail ink,
            SyllableFeedback syllable,
            SentenceDebugHud debugHud,
            SkillMotor skills = null,
            RuneLoadout loadout = null)
        {
            _clock = clock;
            _ink = ink;
            _syllable = syllable;
            _debugHud = debugHud;
            _skills = skills ?? _skills;
            _combat ??= new CombatTuning();
            if (_skills != null && _skills.MaxComboLength > 0)
                _combat.Sentence.MaxSentenceDots = _skills.MaxComboLength;
            if (_sentenceHooked && _engine != null)
            {
                _engine.SentenceCompleted -= OnSentenceCompleted;
                _sentenceHooked = false;
            }

            _engine = new SentenceEngine(
                _combat.Sentence,
                loadout ?? _skills?.DefaultLoadout ?? RuneLoadout.Sequential);
            _dodge = new DodgeState(_combat.Dodge);
            _charges = new DodgeChargeBank(_combat.Dodge);
            _engine.SentenceCompleted += OnSentenceCompleted;
            _sentenceHooked = true;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
            _eventsHooked = true;
        }

        void OnDisable()
        {
            if (_eventsHooked)
            {
                Touch.onFingerDown -= OnFingerDown;
                Touch.onFingerMove -= OnFingerMove;
                Touch.onFingerUp -= OnFingerUp;
                _eventsHooked = false;
            }

            EnhancedTouchSupport.Disable();
            _fingerId = null;
            _dodgeFingerId = null;
            EndPointer(cancelled: true);
        }

        void OnDestroy()
        {
            if (_sentenceHooked && _engine != null)
            {
                _engine.SentenceCompleted -= OnSentenceCompleted;
                _sentenceHooked = false;
            }
        }

        /// <summary>
        /// §5: cümle sınırı görülür — mürekkep şeridi kopar. AddSegment henüz çizilmediyse
        /// bayrak bırakılır; <see cref="FlushInkBreak"/> kapanış segmentinden sonra Break eder.
        /// </summary>
        void OnSentenceCompleted(CompletedSentence sentence)
        {
            _inkBreakPending = true;
            // Denetim B ek: çizimle kurulan cümle tanındı → şerit parlar, rün adları yazılır.
            bool drawn = _drawnSentence;
            _drawnSentence = false;
            if (!drawn || sentence == null || sentence.Phase == SentencePhase.Aborted
                || sentence.Words == null || sentence.Words.Count == 0)
                return;
            _inkFlashPending = true;
            DrawCaption?.Invoke(DrawFeedback.RuneChain(sentence.Words), true);
        }

        void FlushInkBreak()
        {
            if (!_inkBreakPending)
                return;

            _ink?.Break(_inkFlashPending);
            _inkFlashPending = false;
            _lastInkPx = null;
            _inkBreakPending = false;
        }

        void Update()
        {
            EnsureRuntime();
            TickCharges();
            SyncPlayerStateFromWorld();

            if (_engine != null && _clock != null)
                _engine.Tick(_clock.WorldDeltaMs);

            // Pencere dolunca Tick içinde kapanış → şeridi aynı karede kopar.
            FlushInkBreak();

            if (InputLocked || PanelBlocking)
            {
                if (_fingerId.HasValue || _mouseHeld)
                {
                    _fingerId = null;
                    _mouseHeld = false;
                    _dodgeFingerId = null;
                    EndPointer(cancelled: true);
                }
                return;
            }

            HandleKeyboardDodge();
            HandleMouse();
            TickStrokeSettle();
            TickSwapHold();
            TickDwell();
        }

        void SyncPlayerStateFromWorld()
        {
            if (_playerStates == null)
                return;

            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            bool isDead = _vitals != null && _vitals.IsDown;
            bool isStunned = false;
            bool isRooted = false;
            if (_status != null)
            {
                var board = _status.Board;
                isStunned = board.Has(StatusKind.Stun) || board.Has(StatusKind.Stasis) || board.Has(StatusKind.Fear);
                isRooted = board.Has(StatusKind.Root);
            }

            bool isDodging = _dodge != null && _dodge.IsActive(worldMs);
            bool isCasting = _isCasting != null && _isCasting();
            bool isDrawing = _engine != null && _engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _engine != null && _engine.State.Phase == SentencePhase.Recovering;

            _playerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
        }

        bool AllowsDrawNow => _playerStates == null || _playerStates.AllowsDraw;

        void EnsureRuntime()
        {
            _tuning ??= new PrototypeTuning();
            _combat ??= new CombatTuning();
            _clock ??= FindAnyObjectByType<GameClock>();

            if (_dodge == null)
                _dodge = new DodgeState(_combat.Dodge);
            if (_charges == null)
                _charges = new DodgeChargeBank(_combat.Dodge);

            if (_engine == null)
            {
                EnsureSkills();
                if (_skills != null && _skills.MaxComboLength > 0)
                    _combat.Sentence.MaxSentenceDots = _skills.MaxComboLength;
                _engine = new SentenceEngine(
                    _combat.Sentence,
                    _skills?.DefaultLoadout ?? RuneLoadout.Sequential);
                _engine.SentenceCompleted += OnSentenceCompleted;
                _sentenceHooked = true;
            }

            if (_ink == null)
                _ink = FindAnyObjectByType<InkTrail>();
            if (_syllable == null)
                _syllable = FindAnyObjectByType<SyllableFeedback>();
            if (_debugHud == null)
                _debugHud = FindAnyObjectByType<SentenceDebugHud>();
            if (_vitals == null)
                _vitals = FindAnyObjectByType<PlayerVitals>();
            if (_status == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _status = player.GetComponent<ActorStatus>();
            }
            if (_resource == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _resource = player.GetComponent<PlayerResource>();
            }
            if (_cooldown == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                    _cooldown = player.GetComponent<PlayerCooldown>();
            }
            if (_readout == null)
                _readout = FindAnyObjectByType<ReactionReadout>();
        }

        void HandleKeyboardDodge()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.qKey.wasPressedThisFrame)
                WeaponSwapRequested?.Invoke();
            if (kb.rKey.wasPressedThisFrame)
                OrbCommandRequested?.Invoke();
            if (kb.spaceKey.wasPressedThisFrame)
                TriggerDodge();
        }

        void HandleMouse()
        {
            // Mobilde dokunuş varken fare yolunu atla. Editörde Enhanced Touch bazen
            // hayalet touch bırakıp fareyi tamamen kilitleyebiliyor.
#if !UNITY_EDITOR
            if (Touch.activeTouches.Count > 0)
                return;
#endif

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsDrawHalf(pos) || TuningPanel.HitToggleButton(pos))
                    return;
                if (!HitDodgeButton(pos) && !HitSwapButton(pos) && !HitCenter(pos) && HitDot(pos) == null)
                    return;
                _mouseHeld = true;
                BeginPointer(pos);
            }
            else if (_mouseHeld && mouse.leftButton.isPressed)
            {
                MovePointer(pos);
            }
            else if (_mouseHeld && mouse.leftButton.wasReleasedThisFrame)
            {
                _mouseHeld = false;
                EndPointer(cancelled: false);
            }
        }

        void OnFingerDown(Finger finger)
        {
            if (InputLocked || PanelBlocking)
                return;

            Vector2 pos = finger.screenPosition;
            if (!IsDrawHalf(pos) || TuningPanel.HitToggleButton(pos))
                return;

            if (_fingerId.HasValue)
            {
                // Çizim parmağı meşgul: yalnızca dodge düğmesi ikinci parmağı kabul eder (§2).
                // O1: basınca kaçar; yuva parmak kalkana kadar tutulur.
                if (_dodgeFingerId.HasValue || !HitDodgeButton(pos))
                    return;

                _dodgeFingerId = finger.index;
                TriggerDodge();
                return;
            }

            // Sağ boşluk orbit'e bırak — yalnız widget üzerinde claim.
            if (!HitDodgeButton(pos) && !HitSwapButton(pos) && !HitCenter(pos) && HitDot(pos) == null)
                return;

            _fingerId = finger.index;
            BeginPointer(pos);
        }

        void OnFingerMove(Finger finger)
        {
            if (_dodgeFingerId.HasValue && finger.index == _dodgeFingerId.Value)
                return;

            if (!_fingerId.HasValue || finger.index != _fingerId.Value)
                return;

            MovePointer(finger.screenPosition);
        }

        void OnFingerUp(Finger finger)
        {
            if (_dodgeFingerId.HasValue && finger.index == _dodgeFingerId.Value)
            {
                _dodgeFingerId = null;
                return;
            }

            if (!_fingerId.HasValue || finger.index != _fingerId.Value)
                return;

            bool cancelled = finger.currentTouch.phase == TouchPhase.Canceled;
            _fingerId = null;
            EndPointer(cancelled);
        }

        void BeginPointer(Vector2 pos)
        {
            _pressOrigin = pos;
            _lastPos = pos;
            _pressRealMs = NowRealMs();
            _activeDot = null;
            _dwellWorldMs = 0;
            _dwellReported = 0;
            _lastInkPx = null;
            _swapHoldFired = false;

            // Hit sırası: dodge düğmesi → merkez → nokta (§2).
            // O1: kaçış basınca tetiklenir; düğmeden sürükleyip çizme yolu kaldırıldı.
            if (HitDodgeButton(pos))
            {
                _mode = FingerMode.None;
                TriggerDodge();
                return;
            }

            // K3/O1: uzun basma komutu yoksa basınca değiştirir; Küre'de bırakınca (eşik altı) değiştirir.
            if (HitSwapButton(pos))
            {
                if (TouchButtonGesture.SwapFiresOnPress(SwapHoldSec()))
                {
                    _mode = FingerMode.None;
                    WeaponSwapRequested?.Invoke();
                    return;
                }
                _mode = FingerMode.SwapPending;
                return;
            }

            if (HitCenter(pos))
            {
                _mode = FingerMode.CenterPending;
                return;
            }

            _mode = FingerMode.Drawing;
            BeginStroke(pos);
        }

        void MovePointer(Vector2 pos)
        {
            if (_mode == FingerMode.None)
                return;

            _lastPos = pos;
            if (_mode == FingerMode.SwapPending)
                return;

            // Merkezden eşiği aşan sürükleme çizimdir (§2).
            if (_mode == FingerMode.CenterPending)
            {
                float moveDp = PixelsToDp(Vector2.Distance(pos, _pressOrigin));
                if (moveDp > _combat.Dodge.TapMaxMoveDp)
                {
                    _mode = FingerMode.Drawing;
                    _lastInkPx = _pressOrigin;
                    BeginStroke(_pressOrigin);
                    FeedStroke(pos);
                }

                return;
            }

            FeedStroke(pos);
        }

        /// <summary>Denetim B ek: çizim başı — nokta konumları tazelenir, ham iz açılır.</summary>
        void BeginStroke(Vector2 pos)
        {
            _strokeAccepted = 0;
            _strokeDenial = DrawFeedback.DenialKind.None;
            _strokeLengthPx = 0f;
            _strokePrevPx = pos;
            _strokeAcceptedPx.Clear();
            int n = _dotXs.Length;
            for (int dot = 1; dot <= n; dot++)
            {
                Vector2 p = HexagonLayoutScreen.DotPx(dot, _tuning, Screen.width, Screen.height);
                _dotXs[dot - 1] = p.x;
                _dotYs[dot - 1] = p.y;
            }
            _ink?.RawBegin(pos);
            _stroke.Begin(pos.x, pos.y,
                HexagonLayoutScreen.DotHitRadiusPx(_tuning),
                HexagonLayoutScreen.DpToPixels(12f),
                HexagonLayoutScreen.DpToPixels(1.5f),
                _dotXs, _dotYs, _strokeHits);
            _strokeFedFrame = Time.frameCount;
            ApplyStrokeHits(pos);
        }

        /// <summary>
        /// Denetim B ek: önceki örnekten bu örneğe parça taranır; giriş sırasıyla gelen noktalar
        /// kaydedilir (hızlı çizgi noktayı atlamaz, uzak sıçrama aradakini sıyırınca eklemez).
        /// </summary>
        void FeedStroke(Vector2 pos)
        {
            if (_mode != FingerMode.Drawing)
                return;
            _strokeLengthPx += Vector2.Distance(_strokePrevPx, pos);
            _strokePrevPx = pos;
            _ink?.RawAppend(pos);
            _stroke.Move(pos.x, pos.y, _dotXs, _dotYs, _strokeHits);
            _strokeFedFrame = Time.frameCount;
            ApplyStrokeHits(pos);
        }

        /// <summary>
        /// Dokunuşta parmak durunca Move gelmez: bu karede örnek gelmediyse aynı yer sıfır adımla
        /// beslenir (halkada durma kaydı). Örnek gelen karede çalışmaz — yoksa her örnek "durdu" sayılır.
        /// </summary>
        void TickStrokeSettle()
        {
            if (_mode != FingerMode.Drawing || _strokeFedFrame == Time.frameCount)
                return;
            _stroke.Move(_lastPos.x, _lastPos.y, _dotXs, _dotYs, _strokeHits);
            ApplyStrokeHits(_lastPos);
        }

        void ApplyStrokeHits(Vector2 pos)
        {
            for (int i = 0; i < _strokeHits.Count && _mode == FingerMode.Drawing; i++)
                TryRegisterDot(_strokeHits[i]);

            // Dwell: yalnız parmak hâlâ o noktadaysa sürer; çıkınca kesilir, dönüş yeniden kayıt.
            int? under = HitDot(pos);
            if (!under.HasValue || under != _activeDot)
            {
                if (!under.HasValue)
                    _activeDot = null;
                _dwellWorldMs = 0;
                _dwellReported = 0;
            }
        }

        void EndPointer(bool cancelled)
        {
            // O1: süre sınırı yok — uzun basış sessizce düşmez.
            double heldSec = (NowRealMs() - _pressRealMs) / 1000.0;
            if (_mode == FingerMode.Drawing)
            {
                if (!cancelled)
                {
                    _stroke.End(_lastPos.x, _lastPos.y, _dotXs, _dotYs, _strokeHits);
                    for (int i = 0; i < _strokeHits.Count; i++)
                        TryRegisterDot(_strokeHits[i]);
                }
                float strokeLengthDp = PixelsToDp(_strokeLengthPx);
                DrawFeedback.StrokeOutcome outcome = DrawFeedback.OnStrokeEnd(
                    true, _strokeAccepted, _strokeDenial, cancelled, strokeLengthDp);
                if (outcome == DrawFeedback.StrokeOutcome.None
                    && _strokeAccepted > 0
                    && _strokeAcceptedPx.Count >= 1)
                    _ink?.RawEnd(false, _strokeAcceptedPx);
                else if (outcome == DrawFeedback.StrokeOutcome.TooShort
                    || outcome == DrawFeedback.StrokeOutcome.Unrecognized
                    || outcome == DrawFeedback.StrokeOutcome.Cooldown)
                    _ink?.RawEnd(true);
                else
                    _ink?.RawEnd(false);

                string caption = DrawFeedback.CaptionFor(outcome);
                if (!string.IsNullOrEmpty(caption))
                    DrawCaption?.Invoke(caption, false);

                if (!cancelled
                    && _strokeAccepted >= 1
                    && outcome == DrawFeedback.StrokeOutcome.None)
                {
                    long ms = _tuning != null ? _tuning.DotVibrationMs : 30L;
                    FeelHaptics.Pulse((int)ms);
                }
            }
            if (_mode == FingerMode.CenterPending)
            {
                float moveDp = PixelsToDp(Vector2.Distance(_lastPos, _pressOrigin));
                if (TouchButtonGesture.CenterFiresOnRelease(moveDp, _combat.Dodge.TapMaxMoveDp, cancelled))
                    TriggerCenter();
            }
            else if (_mode == FingerMode.SwapPending && !_swapHoldFired
                && TouchButtonGesture.SwapOnRelease(heldSec, SwapHoldSec(), cancelled))
            {
                WeaponSwapRequested?.Invoke();
            }

            _mode = FingerMode.None;
            _activeDot = null;
            _dwellWorldMs = 0;
            _dwellReported = 0;
            _lastInkPx = null;
            _mouseHeld = false;
            _swapHoldFired = false;
        }

        float SwapHoldSec() => SwapHoldCommandSec != null ? SwapHoldCommandSec() : 0f;

        /// <summary>K3: Küre'de silah düğmesini eşik kadar basılı tutmak küreyi yollar/çağırır (bir kez).</summary>
        void TickSwapHold()
        {
            if (_mode != FingerMode.SwapPending || _swapHoldFired)
                return;
            double heldSec = (NowRealMs() - _pressRealMs) / 1000.0;
            if (!TouchButtonGesture.HoldCommandDue(heldSec, SwapHoldSec()))
                return;
            _swapHoldFired = true;
            OrbCommandRequested?.Invoke();
        }

        /// <summary>
        /// Tarayıcının (StrokeDotTracker) verdiği nokta. Tekrarı tarayıcı süzer; burada yalnız
        /// motor kapıları. Her red bir yazı gösterir (kapalı rün dahil) — sessiz red yok.
        /// </summary>
        void TryRegisterDot(int dot)
        {
            int? hit = dot;
            if (_activeDot == hit.Value)
                return;

            if (_engine == null)
                return;

            if (!AllowsDrawNow)
            {
                _readout?.NoteDenied("çizilemez");
                _syllable?.PlayDenied();
                _strokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            if (!_tuning.IsDotOpen(hit.Value))
            {
                // Kapalı rün: motora/ses/mürekkep yok; aktif tut ki komşuya sızmasın.
                // Denetim B ek: artık sessiz değil — çizgi başına bir kez "kapalı rün".
                _activeDot = hit.Value;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                if (_strokeDenial == DrawFeedback.DenialKind.None)
                {
                    _readout?.NoteDenied(DrawFeedback.ClosedRune);
                    _syllable?.PlayDenied();
                    _strokeDenial = DrawFeedback.DenialKind.Other;
                }
                return;
            }

            // Bağlama 3: cümle BAŞLAMADAN önce mana — dodge / toparlanma kilidine dokunulmaz.
            if (!TryAllowSentenceStart(hit.Value))
            {
                _activeDot = hit.Value;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                if (WouldStartSentence() && !CanCooldownVerbDot(hit.Value))
                    _strokeDenial = DrawFeedback.DenialKind.Cooldown;
                else
                    _strokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            if (!TryAllowProspectiveTarget(hit.Value))
            {
                // Hedef kapısı (TryArmSkillTarget) kendi red yazısını gösterir.
                _activeDot = hit.Value;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                _syllable?.PlayDenied();
                _strokeDenial = DrawFeedback.DenialKind.Other;
                return;
            }

            Vector2 dotPx = HexagonLayoutScreen.DotPx(hit.Value, _tuning, Screen.width, Screen.height);
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;

            // SentenceCompleted OnDotTouched içinde ateşlenebilir; kapanış segmenti için
            // from'u önce sakla, Break'i segmentten sonra FlushInkBreak yapsın.
            Vector2? inkFrom = _lastInkPx;
            _drawnSentence = true;
            _strokeAccepted++;
            _strokeAcceptedPx.Add(dotPx);
            _engine.OnDotTouched(hit.Value, worldMs);
            DotAccepted?.Invoke(hit.Value);

            if (inkFrom.HasValue)
                _ink?.AddSegment(inkFrom.Value, dotPx);
            _syllable?.PlayForDot(hit.Value, _engine.State.Words.Count);

            _activeDot = hit.Value;
            _dwellWorldMs = 0;
            _dwellReported = 0;

            if (_inkBreakPending)
                FlushInkBreak();
            else
                _lastInkPx = _engine.State.Phase == SentencePhase.Building ? dotPx : null;
        }

        void TickDwell()
        {
            if (_mode != FingerMode.Drawing || !_activeDot.HasValue || _engine == null)
                return;

            if (_engine.State.Phase != SentencePhase.Building)
                return;

            if (!_tuning.IsDotOpen(_activeDot.Value))
                return;

            // Dünya zamanı: FreezeWindowForDwell DwellMs'i dünya biriminde iade ediyor (§3).
            _dwellWorldMs += _clock != null ? _clock.WorldDeltaMs : Time.deltaTime * 1000.0;
            int maxStacks = _combat.Sentence.DwellMaxStacks;
            while (_dwellReported < maxStacks &&
                   _dwellWorldMs >= _combat.Sentence.DwellMs * (_dwellReported + 1))
            {
                double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
                int stacksBefore = _engine.State.Words.Count > 0
                    ? _engine.State.Words[_engine.State.Words.Count - 1].IntensityStacks
                    : 0;
                _engine.OnDwell(worldMs);
                _dwellReported++;
                int stacksAfter = _engine.State.Words.Count > 0
                    ? _engine.State.Words[_engine.State.Words.Count - 1].IntensityStacks
                    : 0;
                if (stacksAfter > stacksBefore)
                    _syllable?.PlayForDot(_activeDot.Value, _engine.State.Words.Count);
            }
        }

        /// <summary>
        /// Merkez tap (§5). Cümle kuruluyorsa erken kapanış — o uzunluğun ödemesini alır.
        /// Değilse düz vuruş: tek noktalık cümle aynı karede açılıp kapanır. Merkez altıncı
        /// rün DEĞİL; hangi fiille vurduğu veridir (BasicStrikeDot).
        /// </summary>
        void TriggerCenter()
        {
            if (_engine == null)
                return;

            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            if (_engine.State.Phase == SentencePhase.Building)
            {
                _engine.Commit();
                FlushInkBreak();
                _debugHud?.NoteCommit();
                return;
            }

            SyncPlayerStateFromWorld();
            bool engineOk = _engine.State.Phase == SentencePhase.Idle
                || _engine.State.Phase == SentencePhase.Recovering
                || _engine.State.Phase == SentencePhase.Resolved
                || _engine.State.Phase == SentencePhase.Aborted;
            if (!BasicStrikeInput.AllowsCenterStrike(AllowsDrawNow, _centerStrikeArmed, engineOk))
                return;
            _centerStrikeArmed = false;

            // Idle ya da Recovering: kilidi keser (§5) ve tek noktalık cümleyi anında kapatır.
            // Düz vuruş skill değil — mana / CD / zincir kapısı yok. BasicStrikeDot rün
            // kimliğidir, ekran slotu değil; build 12,1,... iken de vuruş çalışır.
            int runeId = _tuning.BasicStrikeDot;
            if (!_engine.BeginBasicStrike(runeId, worldMs))
                return;
            _engine.Commit();
            DotAccepted?.Invoke(0);
            FlushInkBreak();
            _syllable?.PlayForDot(runeId, 1);
            _debugHud?.NoteBasicStrike();
        }

        /// <summary>
        /// Bağlama 3–4 / MCP: cümle başlatma kapısı. true = devam, false = reddedildi
        /// (readout + deny sesi zaten gösterildi). Dodge / recovery'ye dokunmaz.
        /// </summary>
        public bool TryAllowSentenceStart(int verbDot)
        {
            if (!WouldStartSentence())
                return true;
            if (!CanAffordVerbDot(verbDot))
            {
                NotifyInsufficientMana();
                return false;
            }
            if (!CanCooldownVerbDot(verbDot))
            {
                NotifyOnCooldown();
                return false;
            }
            return true;
        }

        /// <summary>
        /// OnDotTouched bu noktada yeni fiil başlatacak mı? (Idle / Recovering / Resolved /
        /// Aborted, ya da kapasite dolu Building → kapat+yeni fiil.) Building + sıfat ekleme değil.
        /// </summary>
        bool WouldStartSentence()
        {
            if (_engine == null)
                return false;

            SentencePhase phase = _engine.State.Phase;
            if (phase == SentencePhase.Idle
                || phase == SentencePhase.Recovering
                || phase == SentencePhase.Resolved
                || phase == SentencePhase.Aborted)
                return true;

            if (phase == SentencePhase.Building
                && _engine.State.Words.Count >= _combat.Sentence.MaxSentenceDots)
                return true;

            return false;
        }

        bool CanAffordVerbDot(int verbDot)
        {
            if (_combat == null || !_combat.EnforceResourceCost)
                return true;
            if (_resource == null)
                return true;

            float cost = LookupBaseResourceCost(verbDot);
            return _resource.CanAfford(cost);
        }

        bool CanCooldownVerbDot(int verbDot)
        {
            if (_combat == null || !_combat.EnforceCooldown)
                return true;
            if (_cooldown == null)
                return true;

            SkillResolution skill = LookupVerbSkill(verbDot);
            if (skill.IsEmpty || string.IsNullOrEmpty(skill.VerbId))
                return true;

            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            return _cooldown.CanStart(skill.VerbId, worldMs);
        }

        float LookupBaseResourceCost(int verbDot)
        {
            SkillResolution skill = LookupVerbSkill(verbDot);
            return skill.IsEmpty ? 0f : skill.BaseResourceCost;
        }

        SkillResolution LookupVerbSkill(int verbDot)
        {
            EnsureSkills();
            if (_skills == null)
                return SkillResolution.Empty;

            int runeId = _engine != null
                ? _engine.Loadout.RuneIdAtSlot(verbDot)
                : verbDot;
            return _skills.Resolve(new[] { runeId });
        }

        bool TryAllowProspectiveTarget(int nextDot)
        {
            if (_skillTargetGate == null || _engine == null
                || _engine.State.Phase != SentencePhase.Building
                || _engine.State.Words.Count != 1)
                return true;

            EnsureSkills();
            if (_skills == null)
                return true;
            int verbRuneId = (int)_engine.State.Words[0].Rune;
            int adjectiveRuneId = _engine.Loadout.RuneIdAtSlot(nextDot);
            SkillResolution skill = _skills.Resolve(new[] { verbRuneId, adjectiveRuneId });
            return skill.IsEmpty || _skillTargetGate(skill);
        }

        void EnsureSkills()
        {
            if (_skills != null)
                return;
            _skills = SkillMotorLoader.Load();
        }

        void NotifyInsufficientMana()
        {
            _readout?.NoteDenied("yetersiz mana");
            _syllable?.PlayDenied();
        }

        void NotifyOnCooldown()
        {
            _readout?.NoteDenied("soğumada");
            _syllable?.PlayDenied();
        }

        void TickCharges()
        {
            if (_charges == null)
                return;
            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            _charges.RechargeMult = _dodge != null ? _dodge.CooldownMult : 1f;
            _charges.Tick(worldMs);
        }

        void TriggerDodge()
        {
            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            if (_dodge == null)
                return;

            bool dead = _vitals != null && _vitals.IsDown;
            bool stun = false;
            bool freeze = false;
            bool knockdown = false;
            if (_status != null)
            {
                var board = _status.Board;
                stun = board.Has(StatusKind.Stun) || board.Has(StatusKind.Fear);
                freeze = board.Has(StatusKind.Stasis);
                knockdown = board.Has(StatusKind.Knockback);
            }

            // S3: state_machine can_dodge kapısı bağlı (yalnız JSON'da açıkça false olan durumlar;
            // casting istisnası PlayerStateMachine.AllowsDodgeGate'te).
            SyncPlayerStateFromWorld();
            bool stateAllows = _playerStates == null || _playerStates.AllowsDodgeGate;
            if (!stateAllows || !DodgeCancelRules.Allowed(dead, stun, freeze, knockdown))
            {
                _readout?.NoteDenied("dodge yok");
                _syllable?.PlayDenied();
                return;
            }

            // S2: tek kapı haklar (eski ölü cooldown yolu kaldırıldı).
            if (_charges != null)
            {
                _charges.RechargeMult = _dodge.CooldownMult;
                if (!_charges.TrySpend(worldMs))
                {
                    _readout?.NoteDenied("dodge yok");
                    _syllable?.PlayDenied();
                    return;
                }
            }

            bool wasBuilding = _engine != null && _engine.State.Phase == SentencePhase.Building;
            _engine?.Abort();
            FlushInkBreak();
            SkillCancelledByDodge?.Invoke();
            _dodge.Begin(worldMs);
            _debugHud?.NoteDodge(wasBuilding);
            if (_mode == FingerMode.Drawing)
                _ink?.RawEnd(false);
            _mode = FingerMode.None;
            _activeDot = null;
            _lastInkPx = null;
        }

        bool IsDrawHalf(Vector2 pos) =>
            HexagonLayoutScreen.IsRightHalf(pos, _tuning.MirrorForLeftHand, Screen.width);

        bool HitDodgeButton(Vector2 pos)
        {
            Vector2 c = HexagonLayoutScreen.DodgeButtonPx(_tuning, Screen.width, Screen.height);
            return Vector2.Distance(pos, c) <= HexagonLayoutScreen.DodgeButtonRadiusPx(_tuning);
        }

        bool HitSwapButton(Vector2 pos)
        {
            Vector2 c = HexagonLayoutScreen.WeaponSwapButtonPx(_tuning, Screen.width, Screen.height);
            return Vector2.Distance(pos, c) <= HexagonLayoutScreen.WeaponSwapButtonRadiusPx(_tuning);
        }

        bool HitCenter(Vector2 pos)
        {
            Vector2 c = HexagonLayoutScreen.CenterPx(_tuning, Screen.width, Screen.height);
            float centerR = HexagonLayoutScreen.CenterHitRadiusPx(_tuning);
            // Merkez, nokta hit'leriyle örtüşmesin diye noktalardan önce ayrı kontrol;
            // nokta hit yarıçapı merkeze taşarsa merkez öncelikli (düz vuruş / erken kapanış).
            return Vector2.Distance(pos, c) <= centerR;
        }

        int? HitDot(Vector2 pos)
        {
            // Merkezin veya dodge düğmesinin içindeyse çizim noktası sayma — tap/drag ayrımı
            // ve hit sırası (§2) bozulmasın.
            if (HitDodgeButton(pos) || HitSwapButton(pos) || HitCenter(pos))
                return null;

            float hitR = HexagonLayoutScreen.DotHitRadiusPx(_tuning);
            int? best = null;
            float bestDist = float.MaxValue;
            for (int dot = 1; dot <= Dovus.Core.Grammar.HexagonLayout.DotCount; dot++)
            {
                Vector2 p = HexagonLayoutScreen.DotPx(dot, _tuning, Screen.width, Screen.height);
                float d = Vector2.Distance(pos, p);
                if (d <= hitR && d < bestDist)
                {
                    bestDist = d;
                    best = dot;
                }
            }

            return best;
        }

        /// <summary>Hedef seçimi, altıgen/dodge/swap tap'lerini dünya tap'i saymasın.</summary>
        public bool IsCombatControlAt(Vector2 pos) =>
            IsDrawHalf(pos)
            && (HitDodgeButton(pos) || HitSwapButton(pos) || HitCenter(pos) || HitDot(pos).HasValue);

        /// <summary>Sol yarı sanal çubuktur; oradaki dokunuş hedef seçmez.</summary>
        public bool IsStickHalf(Vector2 pos) => !IsDrawHalf(pos);

        static double NowRealMs() => Time.realtimeSinceStartupAsDouble * 1000.0;

        static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return px * (160f / dpi);
        }
    }
}
