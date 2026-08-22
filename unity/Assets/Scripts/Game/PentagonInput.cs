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

        PlayerVitals _vitals;

        /// <summary>Ölü oyuncu yazamaz ve dodge atamaz (T8.1).</summary>
        public void BindVitals(PlayerVitals vitals) => _vitals = vitals;

        bool InputLocked => _vitals != null && _vitals.IsDown;

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
            _engine = new SentenceEngine(_combat.Sentence);
            _dodge = new DodgeState(_combat.Dodge);
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

        void Update()
        {
            if (_engine != null && _clock != null)
                _engine.Tick(_clock.WorldDeltaMs);

            if (InputLocked)
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

        void HandleKeyboardDodge()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.spaceKey.wasPressedThisFrame)
                return;

            TriggerDodge();
        }

        void HandleMouse()
        {
            if (Touch.activeTouches.Count > 0)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsDrawHalf(pos))
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
            if (InputLocked)
                return;

            Vector2 pos = finger.screenPosition;
            if (!IsDrawHalf(pos))
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

            Vector2 dotPx = PentagonLayoutScreen.DotPx(hit.Value, _tuning, Screen.width, Screen.height);
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;

            _engine.OnDotTouched(hit.Value, worldMs);

            if (_lastInkPx.HasValue)
                _ink?.AddSegment(_lastInkPx.Value, dotPx);
            _syllable?.PlayForDot(hit.Value, _engine.State.Words.Count);

            _activeDot = hit.Value;
            _lastInkPx = dotPx;
            _dwellWorldMs = 0;
            _dwellReported = 0;
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
                _debugHud?.NoteCommit();
                return;
            }

            // Idle ya da Recovering: kilidi keser (§5) ve tek noktalık cümleyi anında kapatır.
            int dot = _tuning.BasicStrikeDot;
            _engine.OnDotTouched(dot, worldMs);
            _engine.Commit();
            _syllable?.PlayForDot(dot, 1);
            _debugHud?.NoteBasicStrike();
        }

        void TriggerDodge()
        {
            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            if (_dodge == null || _dodge.IsOnCooldown(worldMs))
                return;

            // Building: yatırım batar. Recovering: yalnızca kilit kesilir, ödenmiş kapanış durur.
            bool wasBuilding = _engine != null && _engine.State.Phase == SentencePhase.Building;
            _engine?.Abort();
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
            for (int dot = 1; dot <= 5; dot++)
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
