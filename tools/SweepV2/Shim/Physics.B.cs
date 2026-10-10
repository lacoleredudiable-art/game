using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Physics
    {
        public const int IgnoreRaycastLayer = 4;
        public const int DefaultRaycastLayers = -5;
        public const int AllLayers = -1;

        public static bool queriesHitTriggers { get; set; } = true;
        public static bool autoSyncTransforms { get; set; }
        public static Vector3 gravity { get; set; } = new(0f, -9.81f, 0f);

        static readonly List<Collider> s_colliders = new();
        static readonly HashSet<(int, int)> s_triggerPairs = new();

        internal static void Register(Collider c)
        {
            if (!c.Fitted)
            {
                c.Fitted = true;
                Mesh mesh = c.gameObject.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null) c.Fit(mesh.bounds);
            }
            s_colliders.Add(c);
        }

        internal static void Unregister(Collider c) => s_colliders.Remove(c);

        public static void SyncTransforms() { }
        public static void IgnoreCollision(Collider a, Collider b, bool ignore = true) { }
        public static void IgnoreLayerCollision(int a, int b, bool ignore = true) { }

        static bool Accept(Collider c, int mask, QueryTriggerInteraction q)
        {
            if (!c.Live) return false;
            if ((mask & (1 << c.gameObject.layer)) == 0) return false;
            if (c.isTrigger)
            {
                if (q == QueryTriggerInteraction.Ignore) return false;
                if (q == QueryTriggerInteraction.UseGlobal && !queriesHitTriggers) return false;
            }
            return true;
        }

        // -------------------------------------------------------------- overlap

        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results,
            int layerMask = AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            int n = 0;
            var probe = new Bounds(position, Vector3.one * (2f * radius));
            foreach (Collider c in s_colliders.ToArray())
            {
                if (n >= results.Length) break;
                if (!Accept(c, layerMask, queryTriggerInteraction)) continue;
                if (!c.WorldBounds().Intersects(probe)) continue;
                if (c.PointDistance(position) <= radius) results[n++] = c;
            }
            return n;
        }

        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask = AllLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var buf = new Collider[256];
            int n = OverlapSphereNonAlloc(position, radius, buf, layerMask, q);
            Array.Resize(ref buf, n);
            return buf;
        }

        public static bool CheckSphere(Vector3 position, float radius, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            OverlapSphereNonAlloc(position, radius, new Collider[1], layerMask, q) > 0;

        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results,
            int layerMask = AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            int n = 0;
            var probe = new Bounds(point0, Vector3.zero);
            probe.Encapsulate(point1);
            probe.Expand(2f * radius);
            foreach (Collider c in s_colliders.ToArray())
            {
                if (n >= results.Length) break;
                if (!Accept(c, layerMask, queryTriggerInteraction)) continue;
                if (!c.WorldBounds().Intersects(probe)) continue;
                if (c.SegmentDistance(point0, point1) <= radius) results[n++] = c;
            }
            return n;
        }

        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask = AllLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var buf = new Collider[256];
            int n = OverlapCapsuleNonAlloc(point0, point1, radius, buf, layerMask, q);
            Array.Resize(ref buf, n);
            return buf;
        }

        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            OverlapCapsuleNonAlloc(start, end, radius, new Collider[1], layerMask, q) > 0;

        // -------------------------------------------------------------- casts

        internal static bool CastOne(Collider c, Vector3 p0, Vector3 p1, float radius, Vector3 dir, float maxDistance,
            out RaycastHit hit, bool reportInitialOverlap)
        {
            hit = default;
            dir = dir.normalized;
            if (dir.sqrMagnitude < 1e-12f) return false;
            bool point = (p1 - p0).sqrMagnitude < 1e-12f;
            float Dist(float t)
            {
                Vector3 off = dir * t;
                return point ? c.PointDistance(p0 + off) : c.SegmentDistance(p0 + off, p1 + off);
            }

            float f0 = Dist(0f) - radius;
            if (f0 <= 0f)
            {
                if (!reportInitialOverlap) return false;
                hit.m_Collider = c;
                hit.distance = 0f;
                hit.point = Vector3.zero;
                hit.normal = -dir;
                return true;
            }
            float max = float.IsInfinity(maxDistance) ? 1000f : maxDistance;
            float tMin = Geo.ArgMinConvex(t => Dist(t), 0f, max);
            if (Dist(tMin) - radius > 0f) return false;
            float lo = 0f, hi = tMin;
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Dist(mid) - radius > 0f) lo = mid;
                else hi = mid;
            }
            float tHit = lo;
            Vector3 center = point ? p0 + dir * tHit : ClosestOnSegmentTo(c, p0 + dir * tHit, p1 + dir * tHit);
            Vector3 onShape = c.ClosestOnShape(center);
            Vector3 n = center - onShape;
            hit.m_Collider = c;
            hit.distance = tHit;
            hit.normal = n.sqrMagnitude > 1e-12f ? n.normalized : -dir;
            hit.point = onShape;
            return true;
        }

        static Vector3 ClosestOnSegmentTo(Collider c, Vector3 a, Vector3 b)
        {
            float t = Geo.ArgMinConvex(s => c.PointDistance(Vector3.LerpUnclamped(a, b, s)), 0f, 1f);
            return Vector3.LerpUnclamped(a, b, t);
        }

        static int CastAll(Vector3 p0, Vector3 p1, float radius, Vector3 dir, float maxDistance, RaycastHit[] results,
            int mask, QueryTriggerInteraction q, bool reportInitialOverlap)
        {
            int n = 0;
            Vector3 d = dir.normalized;
            float max = float.IsInfinity(maxDistance) ? 1000f : maxDistance;
            var swept = new Bounds(p0, Vector3.zero);
            swept.Encapsulate(p1);
            swept.Encapsulate(p0 + d * max);
            swept.Encapsulate(p1 + d * max);
            swept.Expand(2f * radius + 0.01f);
            foreach (Collider c in s_colliders.ToArray())
            {
                if (n >= results.Length) break;
                if (!Accept(c, mask, q)) continue;
                if (!c.WorldBounds().Intersects(swept)) continue;
                if (CastOne(c, p0, p1, radius, d, max, out RaycastHit h, reportInitialOverlap)) results[n++] = h;
            }
            return n;
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results,
            float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) =>
            CastAll(origin, origin, radius, direction, maxDistance, results, layerMask, queryTriggerInteraction, true);

        /// <summary>Headless: kutu yerine yarıçap ≈ max(halfExtents) küre cast.</summary>
        public static int BoxCastNonAlloc(
            Vector3 center,
            Vector3 halfExtents,
            Vector3 direction,
            RaycastHit[] results,
            Quaternion orientation,
            float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float radius = Mathf.Max(halfExtents.x, Mathf.Max(halfExtents.y, halfExtents.z));
            return SphereCastNonAlloc(
                center, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            SphereCastNonAlloc(ray.origin, radius, ray.direction, results, maxDistance, layerMask, q);

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var buf = new RaycastHit[256];
            int n = SphereCastNonAlloc(origin, radius, direction, buf, maxDistance, layerMask, q);
            Array.Resize(ref buf, n);
            return buf;
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo,
            float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Nearest(CastAllList(origin, origin, radius, direction, maxDistance, layerMask, q, false), out hitInfo);

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction,
            RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) =>
            CastAll(point1, point2, radius, direction, maxDistance, results, layerMask, queryTriggerInteraction, true);

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction,
            float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var buf = new RaycastHit[256];
            int n = CapsuleCastNonAlloc(point1, point2, radius, direction, buf, maxDistance, layerMask, q);
            Array.Resize(ref buf, n);
            return buf;
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo,
            float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Nearest(CastAllList(point1, point2, radius, direction, maxDistance, layerMask, q, false), out hitInfo);

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            CastAllList(origin, origin, 0f, direction, maxDistance, layerMask, q, false).ToArray();

        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, q);

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            CastAll(ray.origin, ray.origin, 0f, ray.direction, maxDistance, results, layerMask, q, false);

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Raycast(origin, direction, out _, maxDistance, layerMask, q);

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Nearest(CastAllList(origin, origin, 0f, direction, maxDistance, layerMask, q, false), out hitInfo);

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, q);

        public static bool Raycast(Ray ray, float maxDistance = Mathf.Infinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Raycast(ray.origin, ray.direction, out _, maxDistance, layerMask, q);

        static List<RaycastHit> CastAllList(Vector3 p0, Vector3 p1, float r, Vector3 dir, float max, int mask,
            QueryTriggerInteraction q, bool initial)
        {
            var buf = new RaycastHit[256];
            int n = CastAll(p0, p1, r, dir, max, buf, mask, q, initial);
            var list = new List<RaycastHit>(n);
            for (int i = 0; i < n; i++) list.Add(buf[i]);
            return list;
        }

        static bool Nearest(List<RaycastHit> hits, out RaycastHit best)
        {
            best = default;
            if (hits.Count == 0) return false;
            best = hits[0];
            foreach (RaycastHit h in hits)
                if (h.distance < best.distance) best = h;
            return true;
        }

        // -------------------------------------------------------------- tetik olayları

        /// <summary>Tetik + Rigidbody çiftleri için OnTriggerEnter/Exit (Unity kuralı: en az birinde Rigidbody).</summary>
        internal static void StepTriggers()
        {
            List<Collider> listeners = null;
            foreach (Collider c in s_colliders)
            {
                if (!c.Live) continue;
                foreach (Component comp in c.gameObject.Components)
                {
                    if (comp is MonoBehaviour mb && (World.HasMessage(mb.GetType(), "OnTriggerEnter")
                                                     || World.HasMessage(mb.GetType(), "OnTriggerExit")))
                    {
                        (listeners ??= new List<Collider>()).Add(c);
                        break;
                    }
                }
            }
            if (listeners == null)
            {
                s_triggerPairs.Clear();
                return;
            }
            var now = new HashSet<(int, int)>();
            foreach (Collider a in listeners)
            {
                foreach (Collider b in s_colliders.ToArray())
                {
                    if (ReferenceEquals(a, b) || !b.Live) continue;
                    if (!a.isTrigger && !b.isTrigger) continue;
                    if (a.attachedRigidbody == null && b.attachedRigidbody == null) continue;
                    if (!a.WorldBounds().Intersects(b.WorldBounds())) continue;
                    if (!Overlaps(a, b)) continue;
                    var key = (a.GetInstanceID(), b.GetInstanceID());
                    now.Add(key);
                    if (!s_triggerPairs.Contains(key)) Send(a.gameObject, "OnTriggerEnter", b);
                }
            }
            foreach ((int, int) old in s_triggerPairs)
            {
                if (now.Contains(old)) continue;
                Collider a = s_colliders.Find(c => c.GetInstanceID() == old.Item1);
                Collider b = s_colliders.Find(c => c.GetInstanceID() == old.Item2);
                if (a != null && b != null) Send(a.gameObject, "OnTriggerExit", b);
            }
            s_triggerPairs.Clear();
            s_triggerPairs.UnionWith(now);
        }

        static bool Overlaps(Collider a, Collider b)
        {
            switch (a)
            {
                case SphereCollider s:
                    s.World(out Vector3 c, out float r);
                    return b.PointDistance(c) <= r;
                case CapsuleCollider cap:
                    cap.World(out Vector3 p0, out Vector3 p1, out float cr);
                    return b.SegmentDistance(p0, p1) <= cr;
                default:
                    if (b is SphereCollider || b is CapsuleCollider) return Overlaps(b, a);
                    return true;
            }
        }

        static void Send(GameObject go, string message, Collider other)
        {
            foreach (Component comp in go.Components.ToArrayList())
                if (comp is MonoBehaviour mb && mb.isActiveAndEnabled) World.InvokeMessage(mb, message, other);
        }

        static List<Component> ToArrayList(this IReadOnlyList<Component> list) => new(list);
    }

    public struct LayerMask
    {
        int _value;
        public static implicit operator int(LayerMask mask) => mask._value;
        public static implicit operator LayerMask(int value) => new LayerMask { _value = value };

        public static int GetMask(params string[] layerNames)
        {
            int mask = 0;
            foreach (string name in layerNames)
            {
                int layer = NameToLayer(name);
                if (layer >= 0)
                    mask |= 1 << layer;
            }

            return mask;
        }

        public static int NameToLayer(string layerName)
        {
            if (string.Equals(layerName, "Default", StringComparison.Ordinal))
                return 0;
            if (string.Equals(layerName, "CameraBlocker", StringComparison.Ordinal))
                return 31;
            return -1;
        }
    }
}
