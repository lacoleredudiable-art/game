using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum ForceMode { Force = 0, Acceleration = 5, Impulse = 1, VelocityChange = 2 }

    [Flags]
    public enum RigidbodyConstraints
    {
        None = 0, FreezePositionX = 2, FreezePositionY = 4, FreezePositionZ = 8, FreezeRotationX = 16,
        FreezeRotationY = 32, FreezeRotationZ = 64, FreezePosition = 14, FreezeRotation = 112, FreezeAll = 126,
    }

    public struct RaycastHit
    {
        internal Collider m_Collider;
        public Vector3 point { get; set; }
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Collider collider => m_Collider;
        public Transform transform => m_Collider != null ? m_Collider.transform : null;
        public Rigidbody rigidbody => m_Collider != null ? m_Collider.attachedRigidbody : null;
    }

    public class PhysicsMaterial : Object { }

    public class Rigidbody : Component
    {
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; } = true;
        public Vector3 velocity { get; set; }
        public Vector3 linearVelocity { get => velocity; set => velocity = value; }
        public Vector3 angularVelocity { get; set; }
        public float mass { get; set; } = 1f;
        public float drag { get; set; }
        public float angularDrag { get; set; }
        public bool detectCollisions { get; set; } = true;
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public RigidbodyInterpolation interpolation { get; set; }
        public RigidbodyConstraints constraints { get; set; }
        public Vector3 position { get => transform.position; set => transform.position = value; }
        public Quaternion rotation { get => transform.rotation; set => transform.rotation = value; }
        public void MovePosition(Vector3 p) => transform.position = p;
        public void MoveRotation(Quaternion q) => transform.rotation = q;
        public void AddForce(Vector3 f, ForceMode mode = ForceMode.Force) { }
        public void Sleep() { }
        public void WakeUp() { }
    }

    public abstract class Collider : Component
    {
        bool _enabled = true;
        internal bool Fitted;

        public bool enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public bool isTrigger { get; set; }
        public float contactOffset { get; set; } = 0.01f;
        public PhysicsMaterial sharedMaterial { get; set; }
        public PhysicsMaterial material { get; set; }

        public Rigidbody attachedRigidbody
        {
            get
            {
                for (Transform t = transform; t != null; t = t._parent)
                {
                    Rigidbody rb = t.gameObject.GetComponent<Rigidbody>();
                    if (rb != null) return rb;
                }
                return null;
            }
        }

        internal bool Live => _enabled && !Destroyed && _go != null && !_go.IsAsset && _go.activeInHierarchy;

        internal abstract void Fit(Bounds meshBounds);
        internal abstract Bounds WorldBounds();
        internal abstract Vector3 ClosestOnShape(Vector3 p);

        internal virtual float PointDistance(Vector3 p) => (ClosestOnShape(p) - p).magnitude;

        internal virtual float SegmentDistance(Vector3 a, Vector3 b) =>
            Geo.MinConvex(t => PointDistance(Vector3.LerpUnclamped(a, b, t)), 0f, 1f);

        public Bounds bounds => WorldBounds();

        public Vector3 ClosestPoint(Vector3 position) => ClosestOnShape(position);

        public Vector3 ClosestPointOnBounds(Vector3 position) => WorldBounds().ClosestPoint(position);

        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            hitInfo = default;
            return Physics.CastOne(this, ray.origin, ray.origin, 0f, ray.direction, maxDistance, out hitInfo, false);
        }
    }

    public class SphereCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; } = 0.5f;

        internal override void Fit(Bounds b)
        {
            center = b.center;
            radius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        }

        internal void World(out Vector3 c, out float r)
        {
            c = transform.TransformPoint(center);
            Vector3 s = transform.lossyScale;
            r = radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        }

        internal override Bounds WorldBounds()
        {
            World(out Vector3 c, out float r);
            return new Bounds(c, Vector3.one * (2f * r));
        }

        internal override Vector3 ClosestOnShape(Vector3 p)
        {
            World(out Vector3 c, out float r);
            Vector3 d = p - c;
            float m = d.magnitude;
            return m <= r ? p : c + d / m * r;
        }

        internal override float PointDistance(Vector3 p)
        {
            World(out Vector3 c, out float r);
            return Mathf.Max(0f, (p - c).magnitude - r);
        }

        internal override float SegmentDistance(Vector3 a, Vector3 b)
        {
            World(out Vector3 c, out float r);
            return Mathf.Max(0f, (Geo.ClosestOnSegment(c, a, b) - c).magnitude - r);
        }
    }

    public class CapsuleCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; } = 0.5f;
        public float height { get; set; } = 2f;
        public int direction { get; set; } = 1;

        internal override void Fit(Bounds b)
        {
            center = b.center;
            Vector3 e = b.extents;
            direction = e.y >= e.x && e.y >= e.z ? 1 : e.x >= e.z ? 0 : 2;
            float along = direction == 0 ? e.x : direction == 1 ? e.y : e.z;
            float r0 = direction == 0 ? Mathf.Max(e.y, e.z) : direction == 1 ? Mathf.Max(e.x, e.z) : Mathf.Max(e.x, e.y);
            radius = r0;
            height = along * 2f;
        }

        internal void World(out Vector3 a, out Vector3 b, out float r)
        {
            Vector3 s = transform.lossyScale;
            Vector3 axisLocal = direction == 0 ? Vector3.right : direction == 1 ? Vector3.up : Vector3.forward;
            float axisScale = Mathf.Abs(direction == 0 ? s.x : direction == 1 ? s.y : s.z);
            float radScale = direction == 0 ? Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z))
                : direction == 1 ? Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z))
                : Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
            r = radius * radScale;
            float half = Mathf.Max(0f, height * axisScale * 0.5f - r);
            Vector3 c = transform.TransformPoint(center);
            Vector3 axis = (transform.rotation * axisLocal).normalized;
            a = c - axis * half;
            b = c + axis * half;
        }

        internal override Bounds WorldBounds()
        {
            World(out Vector3 a, out Vector3 b, out float r);
            var bb = new Bounds(a, Vector3.zero);
            bb.Encapsulate(b);
            bb.Expand(2f * r);
            return bb;
        }

        internal override Vector3 ClosestOnShape(Vector3 p)
        {
            World(out Vector3 a, out Vector3 b, out float r);
            Vector3 q = Geo.ClosestOnSegment(p, a, b);
            Vector3 d = p - q;
            float m = d.magnitude;
            return m <= r ? p : q + d / m * r;
        }

        internal override float PointDistance(Vector3 p)
        {
            World(out Vector3 a, out Vector3 b, out float r);
            return Mathf.Max(0f, (p - Geo.ClosestOnSegment(p, a, b)).magnitude - r);
        }

        internal override float SegmentDistance(Vector3 p0, Vector3 p1)
        {
            World(out Vector3 a, out Vector3 b, out float r);
            return Mathf.Max(0f, Geo.SegmentSegment(p0, p1, a, b) - r);
        }
    }

    public class BoxCollider : Collider
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; } = Vector3.one;

        internal override void Fit(Bounds b)
        {
            center = b.center;
            size = b.size;
        }

        internal void World(out Vector3 c, out Vector3 ax, out Vector3 ay, out Vector3 az, out Vector3 h)
        {
            c = transform.TransformPoint(center);
            Quaternion q = transform.rotation;
            ax = q * Vector3.right;
            ay = q * Vector3.up;
            az = q * Vector3.forward;
            Vector3 s = transform.lossyScale;
            h = new Vector3(Mathf.Abs(size.x * s.x), Mathf.Abs(size.y * s.y), Mathf.Abs(size.z * s.z)) * 0.5f;
        }

        internal override Bounds WorldBounds()
        {
            World(out Vector3 c, out Vector3 ax, out Vector3 ay, out Vector3 az, out Vector3 h);
            Vector3 e = new(
                Mathf.Abs(ax.x) * h.x + Mathf.Abs(ay.x) * h.y + Mathf.Abs(az.x) * h.z,
                Mathf.Abs(ax.y) * h.x + Mathf.Abs(ay.y) * h.y + Mathf.Abs(az.y) * h.z,
                Mathf.Abs(ax.z) * h.x + Mathf.Abs(ay.z) * h.y + Mathf.Abs(az.z) * h.z);
            return new Bounds(c, e * 2f);
        }

        internal override Vector3 ClosestOnShape(Vector3 p)
        {
            World(out Vector3 c, out Vector3 ax, out Vector3 ay, out Vector3 az, out Vector3 h);
            Vector3 d = p - c;
            float x = Mathf.Clamp(Vector3.Dot(d, ax), -h.x, h.x);
            float y = Mathf.Clamp(Vector3.Dot(d, ay), -h.y, h.y);
            float z = Mathf.Clamp(Vector3.Dot(d, az), -h.z, h.z);
            return c + ax * x + ay * y + az * z;
        }
    }

    public class MeshCollider : BoxCollider
    {
        public Mesh sharedMesh { get; set; }
        public bool convex { get; set; }

        internal override void Fit(Bounds b)
        {
            center = b.center;
            Vector3 s = b.size;
            size = new Vector3(Mathf.Max(s.x, 0.001f), Mathf.Max(s.y, 0.001f), Mathf.Max(s.z, 0.001f));
        }
    }

    public class CharacterController : CapsuleCollider { }

    internal static class Geo
    {
        const float Inv = 0.61803398875f;

        /// <summary>Konveks tek değişkenli fonksiyonun [a,b] aralığındaki en küçüğü (altın oran).</summary>
        internal static float MinConvex(Func<float, float> f, float a, float b) => f(ArgMinConvex(f, a, b));

        internal static float ArgMinConvex(Func<float, float> f, float a, float b)
        {
            float c = b - (b - a) * Inv;
            float d = a + (b - a) * Inv;
            float fc = f(c), fd = f(d);
            for (int i = 0; i < 40 && b - a > 1e-5f; i++)
            {
                if (fc < fd)
                {
                    b = d; d = c; fd = fc;
                    c = b - (b - a) * Inv; fc = f(c);
                }
                else
                {
                    a = c; c = d; fc = fd;
                    d = a + (b - a) * Inv; fd = f(d);
                }
            }
            float m = (a + b) * 0.5f;
            float fa = f(a), fb = f(b), fm = f(m);
            if (fa <= fm && fa <= fb) return a;
            return fb < fm ? b : m;
        }

        internal static Vector3 ClosestOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-12f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return a + ab * t;
        }

        internal static float SegmentSegment(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s, t;
            const float eps = 1e-10f;
            if (a <= eps && e <= eps) return (p1 - p2).magnitude;
            if (a <= eps)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= eps)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom != 0f ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                }
            }
            return ((p1 + d1 * s) - (p2 + d2 * t)).magnitude;
        }
    }

}
