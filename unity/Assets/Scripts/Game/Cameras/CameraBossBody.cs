using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Kameranın gördüğü boss gövdesi: kökün altından tepeye dikey silindir. Kilit mesafesi gövde kenarından
    /// ölçülür (O-5.8); kamera gövdenin içine giremez ve içinden geçmez (O-5.15).
    /// </summary>
    public sealed class CameraBossBody
    {
        readonly Transform _root;
        readonly float _radiusM;
        readonly float _topAboveRootM;

        public CameraBossBody(Transform root, float radiusM, float topAboveRootM)
        {
            _root = root;
            _radiusM = radiusM;
            _topAboveRootM = topAboveRootM;
        }

        public bool Is(Transform t) => _root != null && t == _root;

        /// <summary>Yatay düzlemde noktanın gövde kenarına uzaklığı; içerideyse 0.</summary>
        public float EdgeDistanceM(Vector3 p)
        {
            Vector3 c = _root.position;
            return Mathf.Max(0f, new Vector2(p.x - c.x, p.z - c.z).magnitude - _radiusM);
        }

        /// <summary>
        /// Işının (başlangıç + yön × t, t ≤ maxAlong) gövdeye (yarıçap + inflate) giriş t'si; girmiyorsa sonsuz.
        /// Başlangıç şişirilmiş gövdenin içindeyse ve ışın gövdeye doğruysa 0.
        /// </summary>
        public float EntryAlong(Vector3 origin, Vector3 dir, float maxAlong, float inflateM)
        {
            if (_root == null || _radiusM <= 0f)
                return float.PositiveInfinity;
            Vector3 c = _root.position;
            float r = _radiusM + inflateM;
            float ox = origin.x - c.x;
            float oz = origin.z - c.z;
            float cc = ox * ox + oz * oz - r * r;
            float a = dir.x * dir.x + dir.z * dir.z;
            float b = ox * dir.x + oz * dir.z;
            if (cc <= 0f)
                return b < 0f ? 0f : float.PositiveInfinity;
            float disc = b * b - a * cc;
            if (a <= Mathf.Epsilon || b >= 0f || disc < 0f)
                return float.PositiveInfinity;
            float t = (-b - Mathf.Sqrt(disc)) / a;
            if (t > maxAlong || origin.y + dir.y * t > c.y + _topAboveRootM + inflateM)
                return float.PositiveInfinity;
            return t;
        }

        /// <summary>
        /// Kamera küresi (yarıçap <paramref name="sphereM"/>) gövdeye giriyorsa güvenli konumu verir: bakış noktasından
        /// kameraya giden ışında giriş noktasının <paramref name="marginM"/> önü; bakış noktası da gövdedeyse yatayda
        /// kenara itilir.
        /// </summary>
        public bool TryKeepOut(Vector3 pivot, Vector3 camPos, float sphereM, float marginM, out Vector3 safe)
        {
            safe = camPos;
            if (_root == null || _radiusM <= 0f)
                return false;
            Vector3 c = _root.position;
            float r = _radiusM + sphereM;
            if (FlatSqr(camPos - c) >= r * r || camPos.y > c.y + _topAboveRootM + sphereM)
                return false;

            Vector3 delta = camPos - pivot;
            float len = delta.magnitude;
            float entry = len > Mathf.Epsilon && FlatSqr(pivot - c) > r * r
                ? EntryAlong(pivot, delta / len, len, sphereM)
                : float.PositiveInfinity;
            if (float.IsPositiveInfinity(entry))
            {
                Vector3 flat = new Vector3(camPos.x - c.x, 0f, camPos.z - c.z);
                Vector3 outward = flat.sqrMagnitude > Mathf.Epsilon ? flat.normalized : Vector3.back;
                safe = new Vector3(c.x + outward.x * r, camPos.y, c.z + outward.z * r);
                return true;
            }

            safe = pivot + delta / len * Mathf.Clamp(entry - marginM, 0f, entry);
            return true;
        }

        static float FlatSqr(Vector3 v) => v.x * v.x + v.z * v.z;
    }
}
