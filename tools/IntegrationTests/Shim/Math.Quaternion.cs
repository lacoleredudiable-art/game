using System;
using System.Globalization;

namespace UnityEngine
{
    public struct Quaternion : IEquatable<Quaternion>
    {
        public float x, y, z, w;
        public const float kEpsilon = 0.000001f;

        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static Quaternion identity => new(0f, 0f, 0f, 1f);

        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) => new(
            lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x,
            lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            float x = rotation.x * 2f, y = rotation.y * 2f, z = rotation.z * 2f;
            float xx = rotation.x * x, yy = rotation.y * y, zz = rotation.z * z;
            float xy = rotation.x * y, xz = rotation.x * z, yz = rotation.y * z;
            float wx = rotation.w * x, wy = rotation.w * y, wz = rotation.w * z;
            Vector3 res;
            res.x = (1f - (yy + zz)) * point.x + (xy - wz) * point.y + (xz + wy) * point.z;
            res.y = (xy + wz) * point.x + (1f - (xx + zz)) * point.y + (yz - wx) * point.z;
            res.z = (xz - wy) * point.x + (yz + wx) * point.y + (1f - (xx + yy)) * point.z;
            return res;
        }

        static bool IsEqualUsingDot(float dot) => dot > 1.0f - kEpsilon;

        public static bool operator ==(Quaternion lhs, Quaternion rhs) => IsEqualUsingDot(Dot(lhs, rhs));
        public static bool operator !=(Quaternion lhs, Quaternion rhs) => !(lhs == rhs);

        public static float Dot(Quaternion a, Quaternion b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = MathF.Min(MathF.Abs(Dot(a, b)), 1.0f);
            return IsEqualUsingDot(dot) ? 0.0f : MathF.Acos(dot) * 2.0f * Mathf.Rad2Deg;
        }

        public static Quaternion Normalize(Quaternion q)
        {
            float mag = MathF.Sqrt(Dot(q, q));
            if (mag < Mathf.Epsilon) return identity;
            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }

        public void Normalize() => this = Normalize(this);
        public Quaternion normalized => Normalize(this);

        public static Quaternion Inverse(Quaternion q)
        {
            float n = Dot(q, q);
            if (n < Mathf.Epsilon) return identity;
            return new Quaternion(-q.x / n, -q.y / n, -q.z / n, q.w / n);
        }

        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            axis = axis.normalized;
            float half = angle * Mathf.Deg2Rad * 0.5f;
            float s = MathF.Sin(half);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, MathF.Cos(half));
        }

        public static Quaternion Euler(float x, float y, float z) => FromEulerRad(new Vector3(x, y, z) * Mathf.Deg2Rad);
        public static Quaternion Euler(Vector3 euler) => FromEulerRad(euler * Mathf.Deg2Rad);

        static Quaternion FromEulerRad(Vector3 e)
        {
            // Unity: Z, sonra X, sonra Y (q = qy * qx * qz)
            Quaternion qx = new(MathF.Sin(e.x * 0.5f), 0f, 0f, MathF.Cos(e.x * 0.5f));
            Quaternion qy = new(0f, MathF.Sin(e.y * 0.5f), 0f, MathF.Cos(e.y * 0.5f));
            Quaternion qz = new(0f, 0f, MathF.Sin(e.z * 0.5f), MathF.Cos(e.z * 0.5f));
            return qy * qx * qz;
        }

        public Vector3 eulerAngles
        {
            get
            {
                Quaternion q = this;
                float sinX = 2f * (q.w * q.x - q.y * q.z);
                sinX = Mathf.Clamp(sinX, -1f, 1f);
                float ex, ey, ez;
                if (MathF.Abs(sinX) > 0.9999f)
                {
                    ex = MathF.PI * 0.5f * MathF.Sign(sinX);
                    ey = MathF.Atan2(-2f * (q.x * q.z - q.w * q.y), 1f - 2f * (q.y * q.y + q.z * q.z));
                    ez = 0f;
                }
                else
                {
                    ex = MathF.Asin(sinX);
                    ey = MathF.Atan2(2f * (q.x * q.z + q.w * q.y), 1f - 2f * (q.x * q.x + q.y * q.y));
                    ez = MathF.Atan2(2f * (q.x * q.y + q.w * q.z), 1f - 2f * (q.x * q.x + q.z * q.z));
                }
                return new Vector3(Wrap(ex * Mathf.Rad2Deg), Wrap(ey * Mathf.Rad2Deg), Wrap(ez * Mathf.Rad2Deg));
            }
            set => this = Euler(value);
        }

        static float Wrap(float deg)
        {
            deg %= 360f;
            if (deg < 0f) deg += 360f;
            return deg;
        }

        public static Quaternion LookRotation(Vector3 forward) => LookRotation(forward, Vector3.up);

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            if (forward.sqrMagnitude < 1e-12f) return identity;
            Vector3 f = forward.normalized;
            Vector3 r = Vector3.Cross(upwards, f);
            if (r.sqrMagnitude < 1e-12f)
            {
                // yukarıyla paralel: FromToRotation ile
                return FromToRotation(Vector3.forward, f);
            }
            r = r.normalized;
            Vector3 u = Vector3.Cross(f, r);
            float m00 = r.x, m01 = u.x, m02 = f.x;
            float m10 = r.y, m11 = u.y, m12 = f.y;
            float m20 = r.z, m21 = u.z, m22 = f.z;
            float trace = m00 + m11 + m22;
            Quaternion q;
            if (trace > 0f)
            {
                float s = MathF.Sqrt(trace + 1f) * 2f;
                q.w = 0.25f * s;
                q.x = (m21 - m12) / s;
                q.y = (m02 - m20) / s;
                q.z = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = MathF.Sqrt(1f + m00 - m11 - m22) * 2f;
                q.w = (m21 - m12) / s;
                q.x = 0.25f * s;
                q.y = (m01 + m10) / s;
                q.z = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = MathF.Sqrt(1f + m11 - m00 - m22) * 2f;
                q.w = (m02 - m20) / s;
                q.x = (m01 + m10) / s;
                q.y = 0.25f * s;
                q.z = (m12 + m21) / s;
            }
            else
            {
                float s = MathF.Sqrt(1f + m22 - m00 - m11) * 2f;
                q.w = (m10 - m01) / s;
                q.x = (m02 + m20) / s;
                q.y = (m12 + m21) / s;
                q.z = 0.25f * s;
            }
            return Normalize(q);
        }

        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
        {
            Vector3 a = fromDirection.normalized, b = toDirection.normalized;
            float dot = Vector3.Dot(a, b);
            if (dot > 0.999999f) return identity;
            if (dot < -0.999999f)
            {
                Vector3 axis = Vector3.Cross(Vector3.right, a);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(Vector3.up, a);
                return AngleAxis(180f, axis.normalized);
            }
            Vector3 c = Vector3.Cross(a, b);
            var q = new Quaternion(c.x, c.y, c.z, 1f + dot);
            return Normalize(q);
        }

        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            float dot = Dot(a, b);
            if (dot < 0f)
            {
                b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
                dot = -dot;
            }
            if (dot > 0.9995f)
            {
                return Normalize(new Quaternion(
                    a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t));
            }
            float theta0 = MathF.Acos(dot);
            float theta = theta0 * t;
            float sinTheta = MathF.Sin(theta);
            float sinTheta0 = MathF.Sin(theta0);
            float s0 = MathF.Cos(theta) - dot * sinTheta / sinTheta0;
            float s1 = sinTheta / sinTheta0;
            return new Quaternion(a.x * s0 + b.x * s1, a.y * s0 + b.y * s1, a.z * s0 + b.z * s1, a.w * s0 + b.w * s1);
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => SlerpUnclamped(a, b, Mathf.Clamp01(t));

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            if (Dot(a, b) < 0f) b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
            return Normalize(new Quaternion(
                a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t));
        }

        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta)
        {
            float angle = Angle(from, to);
            if (angle == 0.0f) return to;
            return SlerpUnclamped(from, to, MathF.Min(1.0f, maxDegreesDelta / angle));
        }

        public override bool Equals(object other) => other is Quaternion q && Equals(q);
        public bool Equals(Quaternion o) => x == o.x && y == o.y && z == o.z && w == o.w;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:F5}, {1:F5}, {2:F5}, {3:F5})", x, y, z, w);
    }

}
