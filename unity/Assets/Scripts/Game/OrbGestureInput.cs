using Dovus.Core.Equipment;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Dovus.Game
{
    /// <summary>
    /// Küre: yerde bir noktaya basılı tutup bırakınca küre oraya gider, çift dokunuş eline döner.
    /// Altıgen, dodge, silah değiştirme ve sol çubuk bu dokunuşu almaz.
    /// </summary>
    public sealed class OrbGestureInput : MonoBehaviour
    {
        const float MoveSlopDp = 24f;

        ManifestationDirector _director;
        HexagonInput _hexagon;
        Camera _camera;
        Transform _player;
        OrbGesture _gesture = new(0.4f, 0.3f);
        int? _fingerId;
        Vector2 _fingerStart;
        bool _fingerBlocked;
        bool _mouseHeld;
        Vector2 _mouseStart;
        bool _mouseBlocked;

        public void Bind(ManifestationDirector director, HexagonInput hexagon, Camera camera, Transform player)
        {
            _director = director;
            _hexagon = hexagon;
            _camera = camera;
            _player = player;
            RefreshGesture();
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
            _fingerId = null;
            _mouseHeld = false;
        }

        void Update()
        {
            HandleMouse();
        }

        void RefreshGesture()
        {
            WeaponCombatProfile profile = _director != null ? _director.EquippedProfile : null;
            float hold = profile != null && profile.OrbHoldSec > 0f ? profile.OrbHoldSec : 0.4f;
            float tap = profile != null && profile.OrbDoubleTapSec > 0f ? profile.OrbDoubleTapSec : 0.3f;
            _gesture = new OrbGesture(hold, tap);
        }

        bool OrbReady()
        {
            WeaponCombatProfile profile = _director != null ? _director.EquippedProfile : null;
            return profile != null && profile.OrbPlaceM > 0f;
        }

        void OnFingerDown(Finger finger)
        {
            if (_fingerId.HasValue || InputBlocked())
                return;
            _fingerId = finger.index;
            _fingerStart = finger.screenPosition;
            _fingerBlocked = _hexagon != null
                && (_hexagon.ClaimedFingerId == finger.index
                    || _hexagon.ClaimedDodgeFingerId == finger.index
                    || _hexagon.IsStickHalf(finger.screenPosition)
                    || _hexagon.IsCombatControlAt(finger.screenPosition));
            if (!_fingerBlocked && OrbReady())
            {
                RefreshGesture();
                _gesture.Press(NowMs());
            }
        }

        void OnFingerUp(Finger finger)
        {
            if (!_fingerId.HasValue || _fingerId.Value != finger.index)
                return;
            Vector2 end = finger.screenPosition;
            bool blocked = _fingerBlocked || InputBlocked();
            bool slipped = PixelsToDp(Vector2.Distance(_fingerStart, end)) > MoveSlopDp;
            _fingerId = null;
            _fingerBlocked = false;
            Finish(_fingerStart, blocked || slipped);
        }

        void HandleMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;
            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _mouseHeld = true;
                _mouseStart = pos;
                _mouseBlocked = InputBlocked()
                    || (_hexagon != null && (_hexagon.IsCombatControlAt(pos) || _hexagon.IsStickHalf(pos)));
                if (!_mouseBlocked && OrbReady())
                {
                    RefreshGesture();
                    _gesture.Press(NowMs());
                }
            }
            else if (_mouseHeld && mouse.leftButton.wasReleasedThisFrame)
            {
                _mouseHeld = false;
                bool slipped = PixelsToDp(Vector2.Distance(_mouseStart, pos)) > MoveSlopDp;
                Finish(_mouseStart, _mouseBlocked || InputBlocked() || slipped);
            }
        }

        void Finish(Vector2 pressScreen, bool ignore)
        {
            if (ignore || !OrbReady())
            {
                _gesture.Cancel();
                return;
            }

            OrbGestureResult result = _gesture.Release(NowMs());
            if (result == OrbGestureResult.Place && TryGround(pressScreen, out float x, out float z))
                _director.TryPlaceOrb(x, z);
            else if (result == OrbGestureResult.Recall)
                _director.TryRecallOrb();
        }

        bool TryGround(Vector2 screen, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || _player == null)
                return false;
            Ray ray = _camera.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
                return false;
            float t = (_player.position.y - ray.origin.y) / ray.direction.y;
            if (t < 0f)
                return false;
            Vector3 point = ray.origin + ray.direction * t;
            x = point.x;
            z = point.z;
            return true;
        }

        double NowMs()
        {
            if (_director != null)
                return _director.WorldTimeMs;
            return Time.realtimeSinceStartupAsDouble * 1000.0;
        }

        static bool InputBlocked() =>
            TuningPanel.IsOpen || V611DebugPanel.IsOpen || BuildSelectScreen.IsOpen
            || ElementRadialMenu.AnyOpen;

        static float PixelsToDp(float px)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return px * (160f / dpi);
        }
    }
}
