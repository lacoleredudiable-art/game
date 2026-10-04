using Dovus.Game.Cameras;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Sol yarıda dinamik sanal çubuk + masaüstü WASD. Çok parmak: sol hareket, sağ serbest.
    /// </summary>
    public sealed class MoveInput : MonoBehaviour
    {
        [SerializeField] PrototypeTuning _tuning = new();

        int? _stickFingerId;
        Vector2 _stickOrigin;
        Vector2 _moveDirection;
        Vector2 _knobOffsetPx;

        Vector2? _scriptedDirection;

        public Vector2 MoveDirection => _scriptedDirection ?? _moveDirection;

        /// <summary>Editor smoke testleri için çubuğu koddan tutar; null bırakınca gerçek girdiye döner.</summary>
        public void SetScriptedDirection(Vector2? direction) => _scriptedDirection = direction;

        /// <summary>16 Eylül: JoystickView için — çubuk şu an ekranda basılı mı.</summary>
        public bool IsActive => _stickFingerId.HasValue;

        /// <summary>16 Eylül: CameraOrbitInput'un "bu parmak zaten çubuğa ait" kontrolü için.</summary>
        public int? ClaimedFingerId => _stickFingerId;

        /// <summary>Dinamik çubuğun doğduğu ekran noktası (px) — sadece IsActive'ken geçerli.</summary>
        public Vector2 OriginPx => _stickOrigin;

        /// <summary>Merkezden kabarcığa (knob) kırpılmış piksel ofseti — görsel için.</summary>
        public Vector2 KnobOffsetPx => _knobOffsetPx;

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            // T10: panel açıkken hareket girdisi de susar (bkz. HexagonInput.PanelBlocking).
            if (TuningPanel.IsOpen || BuildSelectScreen.IsOpen || ElementRadialMenu.AnyOpen)
            {
                if (_stickFingerId.HasValue)
                {
                    _stickFingerId = null;
                }
                _moveDirection = Vector2.zero;
                return;
            }

            if (_stickFingerId.HasValue)
                return;

            Vector2 keyboard = ReadKeyboard();
            _moveDirection = keyboard.sqrMagnitude > 0.0001f ? keyboard : Vector2.zero;
        }

        static Vector2 ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return Vector2.zero;

            float x = 0f;
            float y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        void OnFingerDown(Finger finger)
        {
            if (_stickFingerId.HasValue || TuningPanel.IsOpen || BuildSelectScreen.IsOpen
                || ElementRadialMenu.HitHoldChip(finger.screenPosition))
                return;

            Vector2 pos = finger.screenPosition;
            // Çubuk yarısı = çizim yarısının tersi (aynı IsRightHalf yardımcısı; MirrorForLeftHand).
            if (HexagonLayoutScreen.IsRightHalf(pos, _tuning.Input.MirrorForLeftHand, Screen.width))
                return;

            _stickFingerId = finger.index;
            _stickOrigin = pos;
            UpdateStick(pos);
        }

        void OnFingerMove(Finger finger)
        {
            if (!_stickFingerId.HasValue || finger.index != _stickFingerId.Value)
                return;

            UpdateStick(finger.screenPosition);
        }

        void OnFingerUp(Finger finger)
        {
            if (!_stickFingerId.HasValue || finger.index != _stickFingerId.Value)
                return;

            _stickFingerId = null;
            _moveDirection = Vector2.zero;
        }

        void UpdateStick(Vector2 current)
        {
            float maxRadiusPx = HexagonLayoutScreen.PhysicalDpToPixels(_tuning.Input.JoystickMaxRadiusDp);
            Vector2 delta = current - _stickOrigin;
            Vector2 clamped = delta.magnitude > maxRadiusPx
                ? delta.normalized * maxRadiusPx
                : delta;
            // Görsel kabarcık deadzone'da da parmağı takip eder (his) — hareket eşiği ayrı.
            _knobOffsetPx = clamped;

            if (delta.magnitude < maxRadiusPx * _tuning.Input.JoystickDeadZone)
            {
                _moveDirection = Vector2.zero;
                return;
            }

            _moveDirection = clamped / maxRadiusPx;
        }
    }
}
