using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Kameranın gördüğü boss gövdesi: kökün altından tepeye dikey silindir. Kilit mesafesi gövde kenarından
    /// ölçülür (O-5.8); kamera gövdenin içine giremez (O-5.15) ve gövde arada kalınca öne gelmez (O-5.11).
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

        public Transform Root => _root;
        public float RadiusM => _radiusM;
        public float TopAboveRootM => _topAboveRootM;

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

        /// <summary>O-5.11: gövde, karakterden kameraya giden doğru parçasını kesiyor mu.</summary>
        public bool Occludes(Vector3 character, Vector3 camPos)
        {
            Vector3 delta = camPos - character;
            float len = delta.magnitude;
            return len > Mathf.Epsilon && EntryAlong(character, delta / len, len, 0f) < len;
        }

        /// <summary>
        /// O-5.15 kamera küresi (yarıçap <paramref name="sphereM"/>) gövdeye giriyorsa güvenli konumu verir. O-5.11:
        /// kamera öne (karaktere) çekilmez; bakış noktasından kameraya giden ışında gövdenin çıkışının
        /// <paramref name="marginM"/> arkasına itilir (oyuncu boss dibindeyken kamera göğsüne itilmez, Y3).
        /// Işın gövdeden çıkamıyorsa (bakış noktası gövdede ya da ışın dikey) yatayda kenara itilir.
        /// </summary>
        public bool TryKeepOut(Vector3 pivot, Vector3 camPos, float sphereM, float marginM, out Vector3 safe)
        {
            safe = camPos;
            if (_root == null || _radiusM <= 0f)
                return false;
            Vector3 c = _root.position;
            float r = _radiusM + sphereM;
            float topY = c.y + _topAboveRootM + sphereM;
            if (FlatSqr(camPos - c) >= r * r || camPos.y > topY)
                return false;

            Vector3 delta = camPos - pivot;
            float len = delta.magnitude;
            float exit = len > Mathf.Epsilon && FlatSqr(pivot - c) > r * r
                ? ExitAlong(pivot, delta / len, r, topY)
                : float.PositiveInfinity;
            if (float.IsPositiveInfinity(exit))
            {
                Vector3 flat = new Vector3(camPos.x - c.x, 0f, camPos.z - c.z);
                Vector3 outward = flat.sqrMagnitude > Mathf.Epsilon ? flat.normalized : Vector3.back;
                safe = new Vector3(c.x + outward.x * r, camPos.y, c.z + outward.z * r);
                return true;
            }

            safe = pivot + delta / len * (exit + marginM);
            return true;
        }

        /// <summary>Işının yarıçapı <paramref name="r"/>, tepesi <paramref name="topY"/> silindirden çıkış t'si.</summary>
        float ExitAlong(Vector3 origin, Vector3 dir, float r, float topY)
        {
            Vector3 c = _root.position;
            float ox = origin.x - c.x;
            float oz = origin.z - c.z;
            float a = dir.x * dir.x + dir.z * dir.z;
            float b = ox * dir.x + oz * dir.z;
            float cc = ox * ox + oz * oz - r * r;
            float disc = b * b - a * cc;
            float side = a > Mathf.Epsilon && disc >= 0f ? (-b + Mathf.Sqrt(disc)) / a : float.PositiveInfinity;
            float top = dir.y > Mathf.Epsilon ? (topY - origin.y) / dir.y : float.PositiveInfinity;
            return Mathf.Min(side, top);
        }

        static float FlatSqr(Vector3 v) => v.x * v.x + v.z * v.z;
    }
}
