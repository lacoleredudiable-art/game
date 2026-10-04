using System;
using System.Globalization;

namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x;
        public float y;
        public const float kEpsilon = 0.00001f;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public float this[int index]
        {
            get => index == 0 ? x : index == 1 ? y : throw new IndexOutOfRangeException();
            set { if (index == 0) x = value; else if (index == 1) y = value; else throw new IndexOutOfRangeException(); }
        }

        public static Vector2 zero => new(0f, 0f);
        public static Vector2 one => new(1f, 1f);
        public static Vector2 up => new(0f, 1f);
        public static Vector2 down => new(0f, -1f);
        public static Vector2 left => new(-1f, 0f);
        public static Vector2 right => new(1f, 0f);

        public float magnitude => MathF.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;

        public Vector2 normalized
        {
            get
            {
                var v = new Vector2(x, y);
                v.Normalize();
                return v;
            }
        }

        public void Normalize()
        {
            float mag = magnitude;
            if (mag > kEpsilon) this = this / mag;
            else this = zero;
        }

        public void Set(float newX, float newY) { x = newX; y = newY; }

        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) =>
            new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            Vector2 d = target - current;
            float sq = d.sqrMagnitude;
            if (sq == 0f || (maxDistanceDelta >= 0f && sq <= maxDistanceDelta * maxDistanceDelta)) return target;
            float dist = MathF.Sqrt(sq);
            return current + d / dist * maxDistanceDelta;
        }
        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
        {
            float sq = vector.sqrMagnitude;
            if (sq > maxLength * maxLength)
            {
                float mag = MathF.Sqrt(sq);
                return vector / mag * maxLength;
            }
            return vector;
        }
        public static float Angle(Vector2 from, Vector2 to)
        {
            float denominator = MathF.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < 1e-15f) return 0f;
            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f);
            return MathF.Acos(dot) * Mathf.Rad2Deg;
        }
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            float unsigned = Angle(from, to);
            float sign = MathF.Sign(from.x * to.y - from.y * to.x);
            return unsigned * sign;
        }
        public static Vector2 Scale(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);
        public static Vector2 Min(Vector2 a, Vector2 b) => new(MathF.Min(a.x, b.x), MathF.Min(a.y, b.y));
        public static Vector2 Max(Vector2 a, Vector2 b) => new(MathF.Max(a.x, b.x), MathF.Max(a.y, b.y));
        public static Vector2 Perpendicular(Vector2 d) => new(-d.y, d.x);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);
        public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.x / b.x, a.y / b.y);
        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new(a.x / d, a.y / d);
        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            float dx = lhs.x - rhs.x, dy = lhs.y - rhs.y;
            return dx * dx + dy * dy < kEpsilon * kEpsilon;
        }
        public static bool operator !=(Vector2 lhs, Vector2 rhs) => !(lhs == rhs);
        public static implicit operator Vector2(Vector3 v) => new(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new(v.x, v.y, 0f);

        public override bool Equals(object other) => other is Vector2 v && Equals(v);
        public bool Equals(Vector2 other) => x == other.x && y == other.y;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2})", x, y);
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x;
        public float y;
        public float z;
        public const float kEpsilon = 0.00001f;
        public const float kEpsilonNormalSqrt = 1e-15f;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0f; }

        public float this[int index]
        {
            get => index switch { 0 => x, 1 => y, 2 => z, _ => throw new IndexOutOfRangeException() };
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Vector3 zero => new(0f, 0f, 0f);
        public static Vector3 one => new(1f, 1f, 1f);
        public static Vector3 up => new(0f, 1f, 0f);
        public static Vector3 down => new(0f, -1f, 0f);
        public static Vector3 left => new(-1f, 0f, 0f);
        public static Vector3 right => new(1f, 0f, 0f);
        public static Vector3 forward => new(0f, 0f, 1f);
        public static Vector3 back => new(0f, 0f, -1f);
        public static Vector3 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static Vector3 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;

        public Vector3 normalized => Normalize(this);

        public void Normalize()
        {
            float mag = magnitude;
            if (mag > kEpsilon) this = this / mag;
            else this = zero;
        }

        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }

        public static Vector3 Normalize(Vector3 value)
        {
            float mag = value.magnitude;
            return mag > kEpsilon ? value / mag : zero;
        }

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) =>
            new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Magnitude(Vector3 v) => v.magnitude;
        public static float SqrMagnitude(Vector3 v) => v.sqrMagnitude;
        public static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
        public void Scale(Vector3 s) { x *= s.x; y *= s.y; z *= s.z; }
        public static Vector3 Min(Vector3 a, Vector3 b) => new(MathF.Min(a.x, b.x), MathF.Min(a.y, b.y), MathF.Min(a.z, b.z));
        public static Vector3 Max(Vector3 a, Vector3 b) => new(MathF.Max(a.x, b.x), MathF.Max(a.y, b.y), MathF.Max(a.z, b.z));

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) =>
            new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            float dx = target.x - current.x, dy = target.y - current.y, dz = target.z - current.z;
            float sq = dx * dx + dy * dy + dz * dz;
            if (sq == 0f || (maxDistanceDelta >= 0f && sq <= maxDistanceDelta * maxDistanceDelta)) return target;
            float dist = MathF.Sqrt(sq);
            return new Vector3(current.x + dx / dist * maxDistanceDelta, current.y + dy / dist * maxDistanceDelta,
                current.z + dz / dist * maxDistanceDelta);
        }

        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
        {
            float sq = vector.sqrMagnitude;
            if (sq > maxLength * maxLength)
            {
                float mag = MathF.Sqrt(sq);
                return vector / mag * maxLength;
            }
            return vector;
        }

        public static float Angle(Vector3 from, Vector3 to)
        {
            float denominator = MathF.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < kEpsilonNormalSqrt) return 0f;
            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f);
            return MathF.Acos(dot) * Mathf.Rad2Deg;
        }

        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Angle(from, to);
            float cx = from.y * to.z - from.z * to.y;
            float cy = from.z * to.x - from.x * to.z;
            float cz = from.x * to.y - from.y * to.x;
            float sign = MathF.Sign(axis.x * cx + axis.y * cy + axis.z * cz);
            return unsignedAngle * sign;
        }

        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            float sqrMag = Dot(onNormal, onNormal);
            if (sqrMag < Mathf.Epsilon) return zero;
            float dot = Dot(vector, onNormal);
            return onNormal * dot / sqrMag;
        }

        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            float sqrMag = Dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon) return vector;
            float dot = Dot(vector, planeNormal);
            return vector - planeNormal * dot / sqrMag;
        }

        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            float factor = -2f * Dot(inNormal, inDirection);
            return factor * inNormal + inDirection;
        }

        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            float ma = a.magnitude, mb = b.magnitude;
            if (ma < kEpsilon || mb < kEpsilon) return Lerp(a, b, t);
            Vector3 na = a / ma, nb = b / mb;
            float dot = Mathf.Clamp(Dot(na, nb), -1f, 1f);
            float theta = MathF.Acos(dot) * t;
            Vector3 rel = Normalize(nb - na * dot);
            Vector3 dir = na * MathF.Cos(theta) + rel * MathF.Sin(theta);
            return dir * Mathf.Lerp(ma, mb, t);
        }

        public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta)
        {
            float angle = Angle(current, target) * Mathf.Deg2Rad;
            if (angle < 1e-6f) return MoveTowards(current, target, maxMagnitudeDelta);
            float t = Mathf.Min(1f, maxRadiansDelta / angle);
            Vector3 dir = Slerp(current.normalized, target.normalized, t);
            float mag = Mathf.MoveTowards(current.magnitude, target.magnitude, maxMagnitudeDelta);
            return dir.normalized * mag;
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float changeX = current.x - target.x;
            float changeY = current.y - target.y;
            float changeZ = current.z - target.z;
            Vector3 originalTo = target;
            float maxChange = maxSpeed * smoothTime;
            float maxChangeSq = maxChange * maxChange;
            float sqrmag = changeX * changeX + changeY * changeY + changeZ * changeZ;
            if (sqrmag > maxChangeSq)
            {
                float mag = MathF.Sqrt(sqrmag);
                changeX = changeX / mag * maxChange;
                changeY = changeY / mag * maxChange;
                changeZ = changeZ / mag * maxChange;
            }
            target.x = current.x - changeX;
            target.y = current.y - changeY;
            target.z = current.z - changeZ;
            float tempX = (currentVelocity.x + omega * changeX) * deltaTime;
            float tempY = (currentVelocity.y + omega * changeY) * deltaTime;
            float tempZ = (currentVelocity.z + omega * changeZ) * deltaTime;
            currentVelocity.x = (currentVelocity.x - omega * tempX) * exp;
            currentVelocity.y = (currentVelocity.y - omega * tempY) * exp;
            currentVelocity.z = (currentVelocity.z - omega * tempZ) * exp;
            float outputX = target.x + (changeX + tempX) * exp;
            float outputY = target.y + (changeY + tempY) * exp;
            float outputZ = target.z + (changeZ + tempZ) * exp;
            float origMinusCurrentX = originalTo.x - current.x;
            float origMinusCurrentY = originalTo.y - current.y;
            float origMinusCurrentZ = originalTo.z - current.z;
            float outMinusOrigX = outputX - originalTo.x;
            float outMinusOrigY = outputY - originalTo.y;
            float outMinusOrigZ = outputZ - originalTo.z;
            if (origMinusCurrentX * outMinusOrigX + origMinusCurrentY * outMinusOrigY + origMinusCurrentZ * outMinusOrigZ > 0)
            {
                outputX = originalTo.x;
                outputY = originalTo.y;
                outputZ = originalTo.z;
                currentVelocity.x = (outputX - originalTo.x) / deltaTime;
                currentVelocity.y = (outputY - originalTo.y) / deltaTime;
                currentVelocity.z = (outputZ - originalTo.z) / deltaTime;
            }
            return new Vector3(outputX, outputY, outputZ);
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime) =>
            SmoothDamp(current, target, ref currentVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed) =>
            SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            float dx = lhs.x - rhs.x, dy = lhs.y - rhs.y, dz = lhs.z - rhs.z;
            return dx * dx + dy * dy + dz * dz < kEpsilon * kEpsilon;
        }
        public static bool operator !=(Vector3 lhs, Vector3 rhs) => !(lhs == rhs);

        public override bool Equals(object other) => other is Vector3 v && Equals(v);
        public bool Equals(Vector3 other) => x == other.x && y == other.y && z == other.z;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}, {2:F2})", x, y, z);
        public string ToString(string format) => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})",
            x.ToString(format, CultureInfo.InvariantCulture), y.ToString(format, CultureInfo.InvariantCulture),
            z.ToString(format, CultureInfo.InvariantCulture));
    }

    public struct Vector4 : IEquatable<Vector4>
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; w = 0f; }
        public static Vector4 zero => new(0, 0, 0, 0);
        public static Vector4 one => new(1, 1, 1, 1);
        public float this[int i]
        {
            get => i switch { 0 => x, 1 => y, 2 => z, 3 => w, _ => throw new IndexOutOfRangeException() };
            set
            {
                switch (i)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
        public static implicit operator Vector4(Vector3 v) => new(v.x, v.y, v.z, 0f);
        public static implicit operator Vector3(Vector4 v) => new(v.x, v.y, v.z);
        public static Vector4 operator *(Vector4 a, float d) => new(a.x * d, a.y * d, a.z * d, a.w * d);
        public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        public bool Equals(Vector4 o) => x == o.x && y == o.y && z == o.z && w == o.w;
        public override bool Equals(object o) => o is Vector4 v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public struct Vector3Int
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

}
