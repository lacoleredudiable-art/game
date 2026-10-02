using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Play doğrulama: hata shader sayımı, yeşil placeholder, kamera kadraj logları.</summary>
    public sealed class FeelPlayVerify : MonoBehaviour
    {
        FollowCamera _follow;
        Transform _player;
        float _nextScan;
        bool _loggedGreen;
        bool _loggedCamera;
        bool _loggedLockOn;
        bool _loggedWindup;
        int _shaderErrorCount = -1;

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
                float framing = SampleFramingWeight();
                float dist = _follow.ResolvedDistanceM;
                float height = _follow.transform.position.y - _player.position.y;
                float pitch = _follow.OrbitPitchDeg;

                if (!_loggedCamera && framing < 0.25f)
                {
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera default dist={dist:F2}m height={height:F2}m pitch={pitch:F1}°");
                    _loggedCamera = true;
                }

                if (!_loggedLockOn && framing > 0.55f)
                {
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera lock-on dist={dist:F2}m height={height:F2}m pitch={pitch:F1}° framing={framing:F2}");
                    _loggedLockOn = true;
                }

                if (!_loggedWindup && _follow.WindupPullback01 > 0.45f)
                {
                    DebugConfig.DevLog(
                        $"[Feel2Verify] camera windup dist={dist:F2}m height={height:F2}m pullback={_follow.WindupPullback01:F2}");
                    _loggedWindup = true;
                }
            }
        }

        float SampleFramingWeight()
        {
            if (_follow == null || _follow.BossTarget == null || _player == null)
                return 0f;
            Vector3 toBoss = _follow.BossTarget.position - _player.position;
            toBoss.y = 0f;
            var tuning = _follow.Tuning;
            float range = Mathf.Max(0.01f, tuning.CameraSoftLockRangeM);
            float distanceWeight = 1f - Mathf.SmoothStep(0.72f, 1f, toBoss.magnitude / range);
            return Mathf.Clamp01(distanceWeight);
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
