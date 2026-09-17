using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Dovus.Game
{
    /// <summary>
    /// Sağ yarı beşgen çizim girdisi + merkez tap (düz vuruş / erken kapanış) + beşgenin
    /// dışındaki dodge düğmesi (§2). Yalnızca Core motoruna bildirir.
    /// </summary>
    public sealed class PentagonInput : MonoBehaviour
    {
        PrototypeTuning _tuning = new();
        CombatTuning _combat = new();
        SentenceEngine _engine;
        DodgeState _dodge;
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

        // Çizim parmağından bağımsız ikinci yuva: cümle sürerken panik dodge (§2).
        int? _dodgeFingerId;
        Vector2 _dodgePressOrigin;
        double _dodgePressRealMs;
        bool _dodgeTapAlive;

        enum FingerMode
        {
            None,
            CenterPending,
            DodgePending,
            Drawing
        }

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

        /// <summary>16 Eylül: CameraOrbitInput'un "bu parmak zaten çiziyor/dodge'a ait" kontrolü için.</summary>
        public int? ClaimedFingerId => _fingerId;
        public int? ClaimedDodgeFingerId => _dodgeFingerId;

        PlayerVitals _vitals;
        ActorStatus _status;
        PlayerResource _resource;
        PlayerCooldown _cooldown;
        ReactionReadout _readout;
        SkillMotor _skills;

        /// <summary>Ölü oyuncu yazamaz ve dodge atamaz (T8.1).</summary>
        public void BindVitals(PlayerVitals vitals) => _vitals = vitals;

        public void BindStatus(ActorStatus status) => _status = status;

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

        // T10: panel açıkken (ayar paneli modal) beşgen girdisi tamamen susar; EnhancedTouch
        // global olduğu için panelin arkasındaki oyun aynı dokunuşu almaya devam ederdi.
        bool PanelBlocking => TuningPanel.IsOpen;

        public void Bind(
            GameClock clock,
            InkTrail ink,
            SyllableFeedback syllable,
            SentenceDebugHud debugHud)
        {
            _clock = clock;
            _ink = ink;
            _syllable = syllable;
            _debugHud = debugHud;
            _combat ??= new CombatTuning();
            if (_sentenceHooked && _engine != null)
            {
                _engine.SentenceCompleted -= OnSentenceCompleted;
                _sentenceHooked = false;
            }

            _engine = new SentenceEngine(_combat.Sentence);
            _dodge = new DodgeState(_combat.Dodge);
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
            _dodgeTapAlive = false;
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
        void OnSentenceCompleted(CompletedSentence _)
        {
            _inkBreakPending = true;
        }

        void FlushInkBreak()
        {
            if (!_inkBreakPending)
                return;

            _ink?.Break();
            _lastInkPx = null;
            _inkBreakPending = false;
        }

        void Update()
        {
            EnsureRuntime();

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
                    _dodgeTapAlive = false;
                    EndPointer(cancelled: true);
                }
                return;
            }

            HandleKeyboardDodge();
            HandleMouse();
            TickDwell();
        }

        void EnsureRuntime()
        {
            _tuning ??= new PrototypeTuning();
            _combat ??= new CombatTuning();
            _clock ??= FindAnyObjectByType<GameClock>();

            if (_dodge == null)
                _dodge = new DodgeState(_combat.Dodge);

            if (_engine == null)
            {
                _engine = new SentenceEngine(_combat.Sentence);
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
            if (kb == null || !kb.spaceKey.wasPressedThisFrame)
                return;

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
                if (!HitDodgeButton(pos) && !HitCenter(pos) && HitDot(pos) == null)
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
                if (_dodgeFingerId.HasValue || !HitDodgeButton(pos))
                    return;

                _dodgeFingerId = finger.index;
                _dodgePressOrigin = pos;
                _dodgePressRealMs = NowRealMs();
                _dodgeTapAlive = true;
                return;
            }

            // Sağ boşluk orbit'e bırak — yalnız widget üzerinde claim.
            if (!HitDodgeButton(pos) && !HitCenter(pos) && HitDot(pos) == null)
                return;

            _fingerId = finger.index;
            BeginPointer(pos);
        }

        void OnFingerMove(Finger finger)
        {
            if (_dodgeFingerId.HasValue && finger.index == _dodgeFingerId.Value)
            {
                // İkinci parmak: eşiği aşan sürükleme dodge'u iptal eder (çizim yuvası dolu).
                float moveDp = PixelsToDp(Vector2.Distance(finger.screenPosition, _dodgePressOrigin));
                if (moveDp > _combat.Dodge.TapMaxMoveDp)
                    _dodgeTapAlive = false;
                return;
            }

            if (!_fingerId.HasValue || finger.index != _fingerId.Value)
                return;

            MovePointer(finger.screenPosition);
        }

        void OnFingerUp(Finger finger)
        {
            if (_dodgeFingerId.HasValue && finger.index == _dodgeFingerId.Value)
            {
                bool dodgeCancelled = finger.currentTouch.phase == TouchPhase.Canceled;
                double heldMs = NowRealMs() - _dodgePressRealMs;
                _dodgeFingerId = null;
                if (!dodgeCancelled && _dodgeTapAlive && heldMs <= _combat.Dodge.TapMaxMs)
                    TriggerDodge();
                _dodgeTapAlive = false;
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

            // Hit sırası: dodge düğmesi → merkez → nokta (§2).
            if (HitDodgeButton(pos))
            {
                _mode = FingerMode.DodgePending;
                return;
            }

            if (HitCenter(pos))
            {
                _mode = FingerMode.CenterPending;
                return;
            }

            _mode = FingerMode.Drawing;
            TryRegisterDotAt(pos);
        }

        void MovePointer(Vector2 pos)
        {
            if (_mode == FingerMode.None)
                return;

            _lastPos = pos;

            // Merkezden ve dodge düğmesinden eşiği aşan sürükleme çizimdir (§2).
            if (_mode == FingerMode.CenterPending || _mode == FingerMode.DodgePending)
            {
                float moveDp = PixelsToDp(Vector2.Distance(pos, _pressOrigin));
                if (moveDp > _combat.Dodge.TapMaxMoveDp)
                {
                    _mode = FingerMode.Drawing;
                    _lastInkPx = _pressOrigin;
                    TryRegisterDotAt(pos);
                }

                return;
            }

            int? hit = HitDot(pos);
            if (!hit.HasValue)
            {
                // Noktadan çıkınca dwell kesilir; geri dönüş tekrar kayıt için serbest.
                _activeDot = null;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                return;
            }

            TryRegisterDotAt(pos);
        }

        void EndPointer(bool cancelled)
        {
            bool pendingTap = _mode == FingerMode.CenterPending || _mode == FingerMode.DodgePending;
            if (pendingTap && !cancelled)
            {
                double heldMs = NowRealMs() - _pressRealMs;
                float moveDp = PixelsToDp(Vector2.Distance(_lastPos, _pressOrigin));
                if (heldMs <= _combat.Dodge.TapMaxMs && moveDp <= _combat.Dodge.TapMaxMoveDp)
                {
                    if (_mode == FingerMode.CenterPending)
                        TriggerCenter();
                    else
                        TriggerDodge();
                }
            }

            _mode = FingerMode.None;
            _activeDot = null;
            _dwellWorldMs = 0;
            _dwellReported = 0;
            _lastInkPx = null;
            _mouseHeld = false;
        }

        void TryRegisterDotAt(Vector2 pos)
        {
            int? hit = HitDot(pos);
            if (!hit.HasValue)
                return;

            if (_activeDot == hit.Value)
                return;

            if (_engine == null)
                return;

            if (!_tuning.IsDotOpen(hit.Value))
            {
                // Kapalı rün: motora/ses/mürekkep yok; aktif tut ki komşuya sızmasın.
                _activeDot = hit.Value;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                return;
            }

            // Bağlama 3: cümle BAŞLAMADAN önce mana — dodge / toparlanma kilidine dokunulmaz.
            if (!TryAllowSentenceStart(hit.Value))
            {
                _activeDot = hit.Value;
                _dwellWorldMs = 0;
                _dwellReported = 0;
                return;
            }

            Vector2 dotPx = PentagonLayoutScreen.DotPx(hit.Value, _tuning, Screen.width, Screen.height);
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;

            // SentenceCompleted OnDotTouched içinde ateşlenebilir; kapanış segmenti için
            // from'u önce sakla, Break'i segmentten sonra FlushInkBreak yapsın.
            Vector2? inkFrom = _lastInkPx;
            _engine.OnDotTouched(hit.Value, worldMs);

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

            // Idle ya da Recovering: kilidi keser (§5) ve tek noktalık cümleyi anında kapatır.
            // Düz vuruş skill değil — mana / CD / zincir kapısı yok (BasicStrikeDot yalnızca
            // gramer fiili; Ateş×N sayılmaz).
            int dot = _tuning.BasicStrikeDot;
            _engine.OnDotTouched(dot, worldMs);
            _engine.Commit();
            FlushInkBreak();
            _syllable?.PlayForDot(dot, 1);
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

            return _skills.Resolve(new[] { verbDot });
        }

        void EnsureSkills()
        {
            if (_skills != null)
                return;
            _skills = SkillMotorLoader.LoadOrDefault();
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

        void TriggerDodge()
        {
            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            if (_dodge == null || _dodge.IsOnCooldown(worldMs))
                return;

            // Building: yatırım batar. Recovering: yalnızca kilit kesilir, ödenmiş kapanış durur.
            bool wasBuilding = _engine != null && _engine.State.Phase == SentencePhase.Building;
            _engine?.Abort();
            FlushInkBreak();
            _dodge.Begin(worldMs);
            _debugHud?.NoteDodge(wasBuilding);
            _mode = FingerMode.None;
            _activeDot = null;
            _lastInkPx = null;
        }

        bool IsDrawHalf(Vector2 pos) =>
            PentagonLayoutScreen.IsRightHalf(pos, _tuning.MirrorForLeftHand, Screen.width);

        bool HitDodgeButton(Vector2 pos)
        {
            Vector2 c = PentagonLayoutScreen.DodgeButtonPx(_tuning, Screen.width, Screen.height);
            return Vector2.Distance(pos, c) <= PentagonLayoutScreen.DodgeButtonRadiusPx(_tuning);
        }

        bool HitCenter(Vector2 pos)
        {
            Vector2 c = PentagonLayoutScreen.CenterPx(_tuning, Screen.width, Screen.height);
            float centerR = PentagonLayoutScreen.CenterHitRadiusPx(_tuning);
            // Merkez, nokta hit'leriyle örtüşmesin diye noktalardan önce ayrı kontrol;
            // nokta hit yarıçapı merkeze taşarsa merkez öncelikli (düz vuruş / erken kapanış).
            return Vector2.Distance(pos, c) <= centerR;
        }

        int? HitDot(Vector2 pos)
        {
            // Merkezin veya dodge düğmesinin içindeyse çizim noktası sayma — tap/drag ayrımı
            // ve hit sırası (§2) bozulmasın.
            if (HitDodgeButton(pos) || HitCenter(pos))
                return null;

            float hitR = PentagonLayoutScreen.DotHitRadiusPx(_tuning);
            int? best = null;
            float bestDist = float.MaxValue;
            for (int dot = 1; dot <= Dovus.Core.Grammar.PentagonLayout.DotCount; dot++)
            {
                Vector2 p = PentagonLayoutScreen.DotPx(dot, _tuning, Screen.width, Screen.height);
                float d = Vector2.Distance(pos, p);
                if (d <= hitR && d < bestDist)
                {
                    bestDist = d;
                    best = dot;
                }
            }

            return best;
        }

        static double NowRealMs() => Time.realtimeSinceStartupAsDouble * 1000.0;

        static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return px * (160f / dpi);
        }
    }
}
