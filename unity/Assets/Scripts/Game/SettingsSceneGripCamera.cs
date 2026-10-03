using UnityEngine;
using UnityEngine.InputSystem;

namespace Dovus.Game
{
    /// <summary>Ayar sahnesi: ele yakın kadraj, sürükleyerek orbit (basit).</summary>
    public sealed class SettingsSceneGripCamera : MonoBehaviour
    {
        const float MouseDegreesPerPixel = 0.15f;

        FollowCamera _follow;
        float _yawDeg;
        float _pitchDeg = 18f;

        public void Bind(FollowCamera follow, Animator animator, PrototypeTuning tuning)
        {
            _follow = follow;
            if (tuning != null)
            {
                tuning.CameraDistanceM = 1.15f;
                tuning.CameraCollisionMinDistanceM = 0.85f;
                tuning.CameraLockOnMinDistanceM = 1f;
                tuning.CameraLockOnMaxDistanceM = 2.2f;
                tuning.CameraShoulderOffset = new Vector3(0.35f, 0.55f, -0.2f);
            }

            if (_follow != null)
            {
                _follow.OrbitYawDeg = _yawDeg;
                _follow.OrbitPitchDeg = _pitchDeg;
                _follow.LockOnActive = false;
            }
        }

        void LateUpdate()
        {
            if (_follow == null)
                return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yawDeg -= delta.x * MouseDegreesPerPixel;
                _pitchDeg = Mathf.Clamp(_pitchDeg - delta.y * MouseDegreesPerPixel, -12f, 55f);
                _follow.OrbitYawDeg = _yawDeg;
                _follow.OrbitPitchDeg = _pitchDeg;
            }
        }
    }
}
