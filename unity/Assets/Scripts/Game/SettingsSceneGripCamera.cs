using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Ayar sahnesi: ele yakın kadraj, sürükleyerek orbit (basit).</summary>
    public sealed class SettingsSceneGripCamera : MonoBehaviour
    {
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

            if (Input.GetMouseButton(0))
            {
                _yawDeg += Input.GetAxis("Mouse X") * 2.5f;
                _pitchDeg = Mathf.Clamp(_pitchDeg - Input.GetAxis("Mouse Y") * 2f, -12f, 55f);
                _follow.OrbitYawDeg = _yawDeg;
                _follow.OrbitPitchDeg = _pitchDeg;
            }
        }

    }
}
