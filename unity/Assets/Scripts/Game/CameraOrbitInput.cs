using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
{
    /// <summary>
    /// 16 Eylül: "kamerayı 360 derece döndüremiyorum, sabit" bug raporu. Sol yarı (MoveInput)
    /// ve sağ yarı (PentagonInput çizim + dodge) her zaman kendi parmaklarını talep ediyor;
    /// bu yüzden orbit girdisi onların DIŞINDA kalan bir parmağa bağlanır (üçüncü parmak veya
    /// tek elle oynarken masaüstünde sağ-tık sürükleme). İki dokunuşu aynı anda desteklemeyen
    /// telefonlarda hareket/çizim sırasında kamera döndürülemez — bilinen sınır, docs/durum.md.
    /// </summary>
    public sealed class CameraOrbitInput : MonoBehaviour
    {
        const float DegreesPerDp = 0.35f;
        const float MouseDegreesPerPixel = 0.15f;

        FollowCamera _camera;
        MoveInput _moveInput;
        PentagonInput _pentagonInput;
        int? _orbitFingerId;
        Vector2 _lastPos;
        float _yawDeg;
        bool _eventsHooked;

        public void Bind(FollowCamera camera, MoveInput moveInput, PentagonInput pentagonInput)
        {
            _camera = camera;
            _moveInput = moveInput;
            _pentagonInput = pentagonInput;
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

            _orbitFingerId = finger.index;
            _lastPos = finger.screenPosition;
        }

        void OnFingerMove(Finger finger)
        {
            if (!_orbitFingerId.HasValue || finger.index != _orbitFingerId.Value)
                return;

            Vector2 pos = finger.screenPosition;
            float deltaXDp = PixelsToDp(pos.x - _lastPos.x);
            _lastPos = pos;
            _yawDeg -= deltaXDp * DegreesPerDp;
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
