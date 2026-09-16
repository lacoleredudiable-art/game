using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
{
    /// <summary>
    /// Sağ boşlukta (hex/dodge/merkez dışı) sürükleyerek yaw. Stick ve widget parmaklarına
    /// dokunmaz. Editörde sağ-tık yedek.
    /// </summary>
    public sealed class CameraOrbitInput : MonoBehaviour
    {
        const float MouseDegreesPerPixel = 0.15f;

        FollowCamera _camera;
        MoveInput _moveInput;
        PentagonInput _pentagonInput;
        PrototypeTuning _tuning;
        int? _orbitFingerId;
        Vector2 _lastPos;
        float _yawDeg;
        bool _eventsHooked;

        public float YawDeg => _yawDeg;

        public void Bind(
            FollowCamera camera,
            MoveInput moveInput,
            PentagonInput pentagonInput,
            PrototypeTuning tuning = null)
        {
            _camera = camera;
            _moveInput = moveInput;
            _pentagonInput = pentagonInput;
            _tuning = tuning;
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
        }

        void Update()
        {
            HandleMouseOrbit();
            if (_camera != null)
                _camera.OrbitYawDeg = _yawDeg;
        }

        void HandleMouseOrbit()
        {
#if UNITY_EDITOR
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            _yawDeg -= delta.x * MouseDegreesPerPixel;
#endif
        }

        bool IsClaimedElsewhere(int fingerIndex) =>
            (_moveInput != null && _moveInput.ClaimedFingerId == fingerIndex)
            || (_pentagonInput != null && _pentagonInput.ClaimedFingerId == fingerIndex)
            || (_pentagonInput != null && _pentagonInput.ClaimedDodgeFingerId == fingerIndex);

        void OnFingerDown(Finger finger)
        {
            if (_orbitFingerId.HasValue || IsClaimedElsewhere(finger.index))
                return;

            // Sol yarı stick'e ait — orbit alma.
            Vector2 pos = finger.screenPosition;
            bool mirror = _tuning != null && _tuning.MirrorForLeftHand;
            if (!PentagonLayoutScreen.IsRightHalf(pos, mirror, Screen.width))
                return;

            _orbitFingerId = finger.index;
            _lastPos = pos;
        }

        void OnFingerMove(Finger finger)
        {
            if (!_orbitFingerId.HasValue || finger.index != _orbitFingerId.Value)
                return;

            Vector2 pos = finger.screenPosition;
            float deltaXDp = PixelsToDp(pos.x - _lastPos.x);
            _lastPos = pos;
            float sens = _tuning != null ? _tuning.OrbitDegreesPerDp : 0.35f;
            _yawDeg -= deltaXDp * sens;
        }

        void OnFingerUp(Finger finger)
        {
            if (_orbitFingerId.HasValue && finger.index == _orbitFingerId.Value)
                _orbitFingerId = null;
        }

        static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return px * (160f / dpi);
        }
    }
}
