using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    /// <summary>Play doğrulama: hata shader sayımı, yeşil placeholder, kamera kadraj logları.</summary>
    public sealed class FeelPlayVerify : MonoBehaviour
    {
        FollowCamera _follow;
        Transform _player;
        float _nextScan;
        bool _loggedGreen;
        bool _loggedCamera;
        bool _loggedWindup;
        int _shaderErrorCount = -1;
        int _lockOnLogCount;
        float _lastLockOnSepM = -1f;

        public void Bind(FollowCamera follow, Transform player)
        {
            _follow = follow;
            _player = player;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextScan)
                return;
            _nextScan = Time.unscaledTime + 2.5f;

            if (_shaderErrorCount < 0)
            {
                _shaderErrorCount = CountErrorShaderRenderers();
                DebugConfig.DevLog($"[Feel2Verify] error-shader-renderers={_shaderErrorCount}");
            }

            if (!_loggedGreen)
            {
                LogGreenNearPlayer();
                _loggedGreen = true;
            }

            if (_follow != null && _player != null)
            {
                float dist = _follow.ResolvedDistanceM;
                float height = _follow.transform.position.y - _player.position.y;
                float pitch = _follow.OrbitPitchDeg;

                // ff-4: eski bug "framing>0.55 == lock-on" idi (menzil yakınlığı = her zaman
                // lock-on gibi okunuyordu). Artık gerçek FollowCamera.LockOnActive bayrağı okunur.
                if (!_loggedCamera && !_follow.LockOnActive)
                {
                    float headY = SampleBossHeadViewportY();
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera default dist={dist:F2}m height={height:F2}m pitch={pitch:F1}° bossHeadViewportY={headY:F2}");
                    _loggedCamera = true;
                }

                if (_follow.LockOnActive && _lockOnLogCount < 2
                    && (_lastLockOnSepM < 0f || Mathf.Abs(dist - _lastLockOnSepM) > 0.15f))
                {
                    _lastLockOnSepM = dist;
                    _lockOnLogCount++;
                    float sep = SampleBossSeparationM();
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera lock-on #{_lockOnLogCount} dist={dist:F2}m height={height:F2}m pitch={pitch:F1}° sep={sep:F2}m");
                }

                if (!_loggedWindup && _follow.WindupPullback01 > 0.3f)
                {
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera windup dist={dist:F2}m height={height:F2}m pullback={_follow.WindupPullback01:F2}");
                    _loggedWindup = true;
                }
            }
        }

        float SampleBossSeparationM()
        {
            if (_follow == null || _follow.BossTarget == null || _player == null)
                return 0f;
            Vector3 toBoss = _follow.BossTarget.position - _player.position;
            toBoss.y = 0f;
            return toBoss.magnitude;
        }

        /// <summary>Boss baş noktasının ekran viewport Y'si (0 alt, 1 üst) — &lt;0.95 kadrajda demektir.</summary>
        float SampleBossHeadViewportY()
        {
            if (_follow == null || _follow.BossTarget == null)
                return -1f;
            Camera cam = _follow.GetComponent<Camera>();
            if (cam == null)
                return -1f;
            Vector3 head = _follow.BossTarget.position + Vector3.up * _follow.Tuning.CameraBossAimHeightM;
            Vector3 vp = cam.WorldToViewportPoint(head);
            return vp.z > 0f ? vp.y : -1f;
        }

        public static int CountErrorShaderRenderers()
        {
            int count = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                Material m = r.sharedMaterial;
                if (m == null || m.shader == null)
                {
                    DebugConfig.DevLog($"[Feel2Verify] null-shader path={GetPath(r.transform)}");
                    count++;
                    continue;
                }

                string name = m.shader.name;
                if (name.Contains("InternalErrorShader") || name.Contains("Hidden/InternalErrorShader"))
                {
                    DebugConfig.DevLog($"[Feel2Verify] error-shader path={GetPath(r.transform)} mat={m.name}");
                    count++;
                }
            }

            return count;
        }

        static void LogGreenNearPlayer()
        {
            Transform player = Object.FindAnyObjectByType<KinematicMotor>()?.transform;
            if (player == null)
                return;
            Vector3 p = player.position;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                if (r is ParticleSystemRenderer or LineRenderer or TrailRenderer or SkinnedMeshRenderer)
                    continue;
                Color c = ReadColor(r);
                if (c.g < 0.7f || c.r > 0.55f || c.b > 0.55f)
                    continue;
                if (r.bounds.size.magnitude < 0.35f || r.bounds.size.magnitude > 2.5f)
                    continue;
                float d = Vector3.Distance(r.bounds.center, p);
                if (d > 3f)
                    continue;
                DebugConfig.DevLog(
                    $"[Feel2Verify] green-blob path={GetPath(r.transform)} size={r.bounds.size} dist={d:F2}m mat={r.sharedMaterial?.name}");
            }
        }

        static Color ReadColor(Renderer r)
        {
            if (r.sharedMaterial == null)
                return Color.black;
            if (r.sharedMaterial.HasProperty("_BaseColor"))
                return r.sharedMaterial.GetColor("_BaseColor");
            return r.sharedMaterial.color;
        }

        static string GetPath(Transform t)
        {
            if (t == null)
                return "";
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
