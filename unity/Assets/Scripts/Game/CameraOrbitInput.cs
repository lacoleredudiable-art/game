using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
{
    /// <summary>
    /// Sağ boşlukta (hex/dodge/merkez/element dışı) sürükleyerek yaw + pitch. Stick ve widget
    /// parmaklarına dokunmaz. Editörde sağ-tık yedek.
    /// </summary>
    public sealed class CameraOrbitInput : MonoBehaviour
    {
        const float MouseDegreesPerPixel = 0.15f;

        FollowCamera _camera;
        MoveInput _moveInput;
        HexagonInput _hexagonInput;
        PrototypeTuning _tuning;
        int? _orbitFingerId;
        Vector2 _lastPos;
        float _yawDeg;
        float _pitchDeg;
        bool _eventsHooked;

        public float YawDeg => _yawDeg;
        public float PitchDeg => _pitchDeg;

        public void Bind(
            FollowCamera camera,
            MoveInput moveInput,
            HexagonInput hexagonInput,
            PrototypeTuning tuning = null)
        {
            _camera = camera;
            _moveInput = moveInput;
            _hexagonInput = hexagonInput;
            _tuning = tuning;
            if (_tuning != null)
                _pitchDeg = _tuning.CameraDefaultPitchDeg;
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
            HandleLockOnKey();
            if (_camera != null)
            {
                _camera.OrbitYawDeg = _yawDeg;
                _camera.OrbitPitchDeg = _pitchDeg;
            }
        }

        /// <summary>ff-4: Tab ile gerçek lock-on açma/kapama (editör/masaüstü klavyesi).</summary>
        void HandleLockOnKey()
        {
            if (_camera == null)
                return;
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
                _camera.LockOnActive = !_camera.LockOnActive;
        }

        void HandleMouseOrbit()
        {
#if UNITY_EDITOR
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            _yawDeg -= delta.x * MouseDegreesPerPixel;
            AddPitch(delta.y * MouseDegreesPerPixel);
#endif
        }

        void AddPitch(float fingerUpDeg)
        {
            bool invert = _tuning != null && _tuning.OrbitInvertPitch;
            float min = _tuning != null ? _tuning.CameraPitchMinDeg : -8f;
            float max = _tuning != null ? _tuning.CameraPitchMaxDeg : 35f;
            _pitchDeg = Mathf.Clamp(_pitchDeg + (invert ? fingerUpDeg : -fingerUpDeg), min, max);
        }

        bool IsClaimedElsewhere(int fingerIndex) =>
            (_moveInput != null && _moveInput.ClaimedFingerId == fingerIndex)
            || (_hexagonInput != null && _hexagonInput.ClaimedFingerId == fingerIndex)
            || (_hexagonInput != null && _hexagonInput.ClaimedDodgeFingerId == fingerIndex);

        void OnFingerDown(Finger finger)
        {
            if (_orbitFingerId.HasValue || IsClaimedElsewhere(finger.index) || BuildSelectScreen.IsOpen)
                return;

            // Sol yarı stick'e ait — orbit alma.
            Vector2 pos = finger.screenPosition;
            bool mirror = _tuning != null && _tuning.MirrorForLeftHand;
            if (!HexagonLayoutScreen.IsRightHalf(pos, mirror, Screen.width))
                return;
            if (ElementRadialMenu.AnyOpen || ElementRadialMenu.HitHoldChip(pos))
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
            float deltaYDp = PixelsToDp(pos.y - _lastPos.y);
            _lastPos = pos;
            float sens = _tuning != null ? _tuning.OrbitDegreesPerDp : 0.35f;
            _yawDeg -= deltaXDp * sens;
            AddPitch(deltaYDp * sens);
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
