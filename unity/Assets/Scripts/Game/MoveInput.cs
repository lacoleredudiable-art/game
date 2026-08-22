using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
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

        public Vector2 MoveDirection => _moveDirection;

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
            // T10: panel açıkken hareket girdisi de susar (bkz. PentagonInput.PanelBlocking).
            if (TuningPanel.IsOpen)
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
            if (_stickFingerId.HasValue || TuningPanel.IsOpen)
                return;

            Vector2 pos = finger.screenPosition;
            // Çubuk yarısı = çizim yarısının tersi (aynı IsRightHalf yardımcısı; MirrorForLeftHand).
            if (PentagonLayoutScreen.IsRightHalf(pos, _tuning.MirrorForLeftHand, Screen.width))
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
            float maxRadiusPx = DpToPixels(_tuning.JoystickMaxRadiusDp);
            Vector2 delta = current - _stickOrigin;
            if (delta.magnitude < maxRadiusPx * _tuning.JoystickDeadZone)
            {
                _moveDirection = Vector2.zero;
                return;
            }

            Vector2 clamped = delta.magnitude > maxRadiusPx
                ? delta.normalized * maxRadiusPx
                : delta;

            _moveDirection = clamped / maxRadiusPx;
        }

        static float DpToPixels(float dp)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return dp * (dpi / 160f);
        }
    }
}
