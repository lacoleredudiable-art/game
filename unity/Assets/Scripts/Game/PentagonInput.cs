using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
{
    /// <summary>
    /// Sağ yarı beşgen çizim girdisi + merkez tap-dodge. Yalnızca Core motoruna bildirir.
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
        double _dwellRealMs;
        int _dwellReported;
        Vector2? _lastInkPx;
        bool _eventsHooked;

        enum FingerMode
        {
            None,
            CenterPending,
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
            EndPointer(cancelled: true);
        }

        void Update()
        {
            if (_engine != null && _clock != null)
                _engine.Tick(_clock.WorldDeltaMs);

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
            if (_fingerId.HasValue)
                return;

            Vector2 pos = finger.screenPosition;
            if (!IsDrawHalf(pos))
                return;

            _fingerId = finger.index;
            BeginPointer(pos);
        }

        void OnFingerMove(Finger finger)
        {
            if (!_fingerId.HasValue || finger.index != _fingerId.Value)
                return;

            MovePointer(finger.screenPosition);
        }

        void OnFingerUp(Finger finger)
        {
            if (!_fingerId.HasValue || finger.index != _fingerId.Value)
                return;

            _fingerId = null;
            EndPointer(cancelled: false);
        }

        void BeginPointer(Vector2 pos)
        {
            _pressOrigin = pos;
            _lastPos = pos;
            _pressRealMs = NowRealMs();
            _activeDot = null;
            _dwellRealMs = 0;
            _dwellReported = 0;
            _lastInkPx = null;

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

            if (_mode == FingerMode.CenterPending)
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
                _dwellRealMs = 0;
                _dwellReported = 0;
                return;
            }

            TryRegisterDotAt(pos);
        }

        void EndPointer(bool cancelled)
        {
            if (_mode == FingerMode.CenterPending && !cancelled)
            {
                double heldMs = NowRealMs() - _pressRealMs;
                float moveDp = PixelsToDp(Vector2.Distance(_lastPos, _pressOrigin));
                if (heldMs <= _combat.Dodge.TapMaxMs && moveDp <= _combat.Dodge.TapMaxMoveDp)
                    TriggerDodge();
            }

            _mode = FingerMode.None;
            _activeDot = null;
            _dwellRealMs = 0;
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

            Vector2 dotPx = PentagonLayoutScreen.DotPx(hit.Value, _tuning, Screen.width, Screen.height);
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;

            _engine.OnDotTouched(hit.Value, worldMs);

            if (_lastInkPx.HasValue)
                _ink?.AddSegment(_lastInkPx.Value, dotPx);
            _syllable?.PlayForDot(hit.Value, _engine.State.Words.Count);

            _activeDot = hit.Value;
            _lastInkPx = dotPx;
            _dwellRealMs = 0;
            _dwellReported = 0;
        }

        void TickDwell()
        {
            if (_mode != FingerMode.Drawing || !_activeDot.HasValue || _engine == null)
                return;

            if (_engine.State.Phase != SentencePhase.Building)
                return;

            _dwellRealMs += _clock != null ? _clock.RealDeltaMs : Time.unscaledDeltaTime * 1000.0;
            int maxStacks = _combat.Sentence.DwellMaxStacks;
            while (_dwellReported < maxStacks &&
                   _dwellRealMs >= _combat.Sentence.DwellMs * (_dwellReported + 1))
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

        void TriggerDodge()
        {
            _engine?.Abort();
            int worldMs = _clock != null ? (int)_clock.Director.WorldTimeMs : 0;
            if (_dodge != null && !_dodge.IsOnCooldown(worldMs))
                _dodge.Begin(worldMs);
            _debugHud?.NoteDodge();
            _mode = FingerMode.None;
            _activeDot = null;
            _lastInkPx = null;
        }

        bool IsDrawHalf(Vector2 pos) =>
            PentagonLayoutScreen.IsRightHalf(pos, _tuning.MirrorForLeftHand, Screen.width);

        bool HitCenter(Vector2 pos)
        {
            Vector2 c = PentagonLayoutScreen.CenterPx(_tuning, Screen.width, Screen.height);
            float centerR = PentagonLayoutScreen.CenterHitRadiusPx(_tuning);
            // Merkez, nokta hit'leriyle örtüşmesin diye noktalardan önce ayrı kontrol;
            // nokta hit yarıçapı merkeze taşarsa merkez öncelikli (tap-dodge).
            return Vector2.Distance(pos, c) <= centerR;
        }

        int? HitDot(Vector2 pos)
        {
            // Merkezin içindeyse çizim noktası sayma — tap/drag ayrımı bozulmasın.
            if (HitCenter(pos))
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
