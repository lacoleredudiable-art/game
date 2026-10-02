using System;
using System.Globalization;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = MathF.PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public static readonly float Epsilon = float.Epsilon;

        public static float Sin(float f) => MathF.Sin(f);
        public static float Cos(float f) => MathF.Cos(f);
        public static float Tan(float f) => MathF.Tan(f);
        public static float Asin(float f) => MathF.Asin(f);
        public static float Acos(float f) => MathF.Acos(f);
        public static float Atan(float f) => MathF.Atan(f);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Sqrt(float f) => MathF.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int v) => Math.Abs(v);
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Min(params float[] values)
        {
            if (values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }
        public static int Min(params int[] values)
        {
            if (values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Max(params float[] values)
        {
            if (values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
        public static int Max(params int[] values)
        {
            if (values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
        public static float Pow(float f, float p) => MathF.Pow(f, p);
        public static float Exp(float p) => MathF.Exp(p);
        public static float Log(float f, float p) => MathF.Log(f, p);
        public static float Log(float f) => MathF.Log(f);
        public static float Log10(float f) => MathF.Log10(f);
        public static float Ceil(float f) => MathF.Ceiling(f);
        public static float Floor(float f) => MathF.Floor(f);
        public static float Round(float f) => MathF.Round(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat(b - a, 360f);
            if (delta > 180f) delta -= 360f;
            return a + delta * Clamp01(t);
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta) return target;
            return current + Sign(target - current) * maxDelta;
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float deltaAngle = DeltaAngle(current, target);
            if (-maxDelta < deltaAngle && deltaAngle < maxDelta) return target;
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }

        public static bool Approximately(float a, float b) =>
            Abs(b - a) < Max(0.000001f * Max(Abs(a), Abs(b)), Epsilon * 8f);

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;
            if (originalTo - current > 0.0f == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime) =>
            SmoothDamp(current, target, ref currentVelocity, smoothTime, Infinity, Time.deltaTime);

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed) =>
            SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime) =>
            SmoothDampAngle(current, target, ref currentVelocity, smoothTime, Infinity, Time.deltaTime);

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed) =>
            SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);

        public static float Repeat(float t, float length) => Clamp(t - Floor(t / length) * length, 0f, length);

        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - Abs(t - length);
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (a != b) return Clamp01((value - a) / (b - a));
            return 0f;
        }

        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }

        public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

        public static float PerlinNoise(float x, float y)
        {
            double v = Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453;
            return (float)(v - Math.Floor(v));
        }
    }

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

    public struct Matrix4x4
    {
        public float m00, m10, m20, m30;
        public float m01, m11, m21, m31;
        public float m02, m12, m22, m32;
        public float m03, m13, m23, m33;

        public static Matrix4x4 identity => new() { m00 = 1f, m11 = 1f, m22 = 1f, m33 = 1f };
        public static Matrix4x4 zero => new();

        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
        {
            float x = q.x * 2f, y = q.y * 2f, z = q.z * 2f;
            float xx = q.x * x, yy = q.y * y, zz = q.z * z;
            float xy = q.x * y, xz = q.x * z, yz = q.y * z;
            float wx = q.w * x, wy = q.w * y, wz = q.w * z;
            Matrix4x4 m;
            m.m00 = (1f - (yy + zz)) * s.x; m.m10 = (xy + wz) * s.x; m.m20 = (xz - wy) * s.x; m.m30 = 0f;
            m.m01 = (xy - wz) * s.y; m.m11 = (1f - (xx + zz)) * s.y; m.m21 = (yz + wx) * s.y; m.m31 = 0f;
            m.m02 = (xz + wy) * s.z; m.m12 = (yz - wx) * s.z; m.m22 = (1f - (xx + yy)) * s.z; m.m32 = 0f;
            m.m03 = pos.x; m.m13 = pos.y; m.m23 = pos.z; m.m33 = 1f;
            return m;
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 r;
            r.m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20 + a.m03 * b.m30;
            r.m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21 + a.m03 * b.m31;
            r.m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22 + a.m03 * b.m32;
            r.m03 = a.m00 * b.m03 + a.m01 * b.m13 + a.m02 * b.m23 + a.m03 * b.m33;
            r.m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20 + a.m13 * b.m30;
            r.m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21 + a.m13 * b.m31;
            r.m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22 + a.m13 * b.m32;
            r.m13 = a.m10 * b.m03 + a.m11 * b.m13 + a.m12 * b.m23 + a.m13 * b.m33;
            r.m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20 + a.m23 * b.m30;
            r.m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21 + a.m23 * b.m31;
            r.m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22 + a.m23 * b.m32;
            r.m23 = a.m20 * b.m03 + a.m21 * b.m13 + a.m22 * b.m23 + a.m23 * b.m33;
            r.m30 = a.m30 * b.m00 + a.m31 * b.m10 + a.m32 * b.m20 + a.m33 * b.m30;
            r.m31 = a.m30 * b.m01 + a.m31 * b.m11 + a.m32 * b.m21 + a.m33 * b.m31;
            r.m32 = a.m30 * b.m02 + a.m31 * b.m12 + a.m32 * b.m22 + a.m33 * b.m32;
            r.m33 = a.m30 * b.m03 + a.m31 * b.m13 + a.m32 * b.m23 + a.m33 * b.m33;
            return r;
        }

        public Vector3 MultiplyPoint3x4(Vector3 p) => new(
            m00 * p.x + m01 * p.y + m02 * p.z + m03,
            m10 * p.x + m11 * p.y + m12 * p.z + m13,
            m20 * p.x + m21 * p.y + m22 * p.z + m23);

        public Vector3 MultiplyPoint(Vector3 p)
        {
            float w = m30 * p.x + m31 * p.y + m32 * p.z + m33;
            Vector3 r = MultiplyPoint3x4(p);
            return w != 0f && w != 1f ? r / w : r;
        }

        public Vector3 MultiplyVector(Vector3 v) => new(
            m00 * v.x + m01 * v.y + m02 * v.z,
            m10 * v.x + m11 * v.y + m12 * v.z,
            m20 * v.x + m21 * v.y + m22 * v.z);

        public Vector4 GetColumn(int i) => i switch
        {
            0 => new Vector4(m00, m10, m20, m30),
            1 => new Vector4(m01, m11, m21, m31),
            2 => new Vector4(m02, m12, m22, m32),
            _ => new Vector4(m03, m13, m23, m33),
        };

        public Matrix4x4 inverse
        {
            get
            {
                float[] m =
                {
                    m00, m10, m20, m30, m01, m11, m21, m31, m02, m12, m22, m32, m03, m13, m23, m33,
                };
                float[] inv = new float[16];
                inv[0] = m[5] * m[10] * m[15] - m[5] * m[11] * m[14] - m[9] * m[6] * m[15] + m[9] * m[7] * m[14] + m[13] * m[6] * m[11] - m[13] * m[7] * m[10];
                inv[4] = -m[4] * m[10] * m[15] + m[4] * m[11] * m[14] + m[8] * m[6] * m[15] - m[8] * m[7] * m[14] - m[12] * m[6] * m[11] + m[12] * m[7] * m[10];
                inv[8] = m[4] * m[9] * m[15] - m[4] * m[11] * m[13] - m[8] * m[5] * m[15] + m[8] * m[7] * m[13] + m[12] * m[5] * m[11] - m[12] * m[7] * m[9];
                inv[12] = -m[4] * m[9] * m[14] + m[4] * m[10] * m[13] + m[8] * m[5] * m[14] - m[8] * m[6] * m[13] - m[12] * m[5] * m[10] + m[12] * m[6] * m[9];
                inv[1] = -m[1] * m[10] * m[15] + m[1] * m[11] * m[14] + m[9] * m[2] * m[15] - m[9] * m[3] * m[14] - m[13] * m[2] * m[11] + m[13] * m[3] * m[10];
                inv[5] = m[0] * m[10] * m[15] - m[0] * m[11] * m[14] - m[8] * m[2] * m[15] + m[8] * m[3] * m[14] + m[12] * m[2] * m[11] - m[12] * m[3] * m[10];
                inv[9] = -m[0] * m[9] * m[15] + m[0] * m[11] * m[13] + m[8] * m[1] * m[15] - m[8] * m[3] * m[13] - m[12] * m[1] * m[11] + m[12] * m[3] * m[9];
                inv[13] = m[0] * m[9] * m[14] - m[0] * m[10] * m[13] - m[8] * m[1] * m[14] + m[8] * m[2] * m[13] + m[12] * m[1] * m[10] - m[12] * m[2] * m[9];
                inv[2] = m[1] * m[6] * m[15] - m[1] * m[7] * m[14] - m[5] * m[2] * m[15] + m[5] * m[3] * m[14] + m[13] * m[2] * m[7] - m[13] * m[3] * m[6];
                inv[6] = -m[0] * m[6] * m[15] + m[0] * m[7] * m[14] + m[4] * m[2] * m[15] - m[4] * m[3] * m[14] - m[12] * m[2] * m[7] + m[12] * m[3] * m[6];
                inv[10] = m[0] * m[5] * m[15] - m[0] * m[7] * m[13] - m[4] * m[1] * m[15] + m[4] * m[3] * m[13] + m[12] * m[1] * m[7] - m[12] * m[3] * m[5];
                inv[14] = -m[0] * m[5] * m[14] + m[0] * m[6] * m[13] + m[4] * m[1] * m[14] - m[4] * m[2] * m[13] - m[12] * m[1] * m[6] + m[12] * m[2] * m[5];
                inv[3] = -m[1] * m[6] * m[11] + m[1] * m[7] * m[10] + m[5] * m[2] * m[11] - m[5] * m[3] * m[10] - m[9] * m[2] * m[7] + m[9] * m[3] * m[6];
                inv[7] = m[0] * m[6] * m[11] - m[0] * m[7] * m[10] - m[4] * m[2] * m[11] + m[4] * m[3] * m[10] + m[8] * m[2] * m[7] - m[8] * m[3] * m[6];
                inv[11] = -m[0] * m[5] * m[11] + m[0] * m[7] * m[9] + m[4] * m[1] * m[11] - m[4] * m[3] * m[9] - m[8] * m[1] * m[7] + m[8] * m[3] * m[5];
                inv[15] = m[0] * m[5] * m[10] - m[0] * m[6] * m[9] - m[4] * m[1] * m[10] + m[4] * m[2] * m[9] + m[8] * m[1] * m[6] - m[8] * m[2] * m[5];
                float det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];
                if (MathF.Abs(det) < 1e-20f) return zero;
                det = 1f / det;
                Matrix4x4 r;
                r.m00 = inv[0] * det; r.m10 = inv[1] * det; r.m20 = inv[2] * det; r.m30 = inv[3] * det;
                r.m01 = inv[4] * det; r.m11 = inv[5] * det; r.m21 = inv[6] * det; r.m31 = inv[7] * det;
                r.m02 = inv[8] * det; r.m12 = inv[9] * det; r.m22 = inv[10] * det; r.m32 = inv[11] * det;
                r.m03 = inv[12] * det; r.m13 = inv[13] * det; r.m23 = inv[14] * det; r.m33 = inv[15] * det;
                return r;
            }
        }
    }

    public struct Color : IEquatable<Color>
    {
        public float r, g, b, a;

        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }

        public static Color red => new(1f, 0f, 0f, 1f);
        public static Color green => new(0f, 1f, 0f, 1f);
        public static Color blue => new(0f, 0f, 1f, 1f);
        public static Color white => new(1f, 1f, 1f, 1f);
        public static Color black => new(0f, 0f, 0f, 1f);
        public static Color yellow => new(1f, 0.92156863f, 0.015686275f, 1f);
        public static Color cyan => new(0f, 1f, 1f, 1f);
        public static Color magenta => new(1f, 0f, 1f, 1f);
        public static Color gray => new(0.5f, 0.5f, 0.5f, 1f);
        public static Color grey => new(0.5f, 0.5f, 0.5f, 1f);
        public static Color clear => new(0f, 0f, 0f, 0f);

        public float grayscale => 0.299f * r + 0.587f * g + 0.114f * b;
        public float maxColorComponent => MathF.Max(MathF.Max(r, g), b);

        public float this[int i]
        {
            get => i switch { 0 => r, 1 => g, 2 => b, 3 => a, _ => throw new IndexOutOfRangeException() };
            set
            {
                switch (i)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    case 3: a = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }

        public static Color LerpUnclamped(Color a, Color b, float t) =>
            new(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);

        public static Color HSVToRGB(float h, float s, float v) => HSVToRGB(h, s, v, true);

        public static Color HSVToRGB(float h, float s, float v, bool hdr)
        {
            if (s == 0f) return new Color(v, v, v, 1f);
            h = (h % 1f + 1f) % 1f * 6f;
            int i = (int)MathF.Floor(h);
            float f = h - i;
            float p = v * (1f - s), q = v * (1f - s * f), t = v * (1f - s * (1f - f));
            return i switch
            {
                0 => new Color(v, t, p),
                1 => new Color(q, v, p),
                2 => new Color(p, v, t),
                3 => new Color(p, q, v),
                4 => new Color(t, p, v),
                _ => new Color(v, p, q),
            };
        }

        public static void RGBToHSV(Color rgb, out float h, out float s, out float v)
        {
            float max = MathF.Max(rgb.r, MathF.Max(rgb.g, rgb.b));
            float min = MathF.Min(rgb.r, MathF.Min(rgb.g, rgb.b));
            v = max;
            float d = max - min;
            s = max <= 0f ? 0f : d / max;
            if (d <= 0f) { h = 0f; return; }
            if (max == rgb.r) h = (rgb.g - rgb.b) / d % 6f;
            else if (max == rgb.g) h = (rgb.b - rgb.r) / d + 2f;
            else h = (rgb.r - rgb.g) / d + 4f;
            h /= 6f;
            if (h < 0f) h += 1f;
        }

        public static Color operator +(Color a, Color b) => new(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color operator -(Color a, Color b) => new(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);
        public static Color operator *(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator *(Color a, float b) => new(a.r * b, a.g * b, a.b * b, a.a * b);
        public static Color operator *(float b, Color a) => new(a.r * b, a.g * b, a.b * b, a.a * b);
        public static Color operator /(Color a, float b) => new(a.r / b, a.g / b, a.b / b, a.a / b);
        public static bool operator ==(Color lhs, Color rhs) =>
            MathF.Abs(lhs.r - rhs.r) < 1e-5f && MathF.Abs(lhs.g - rhs.g) < 1e-5f
            && MathF.Abs(lhs.b - rhs.b) < 1e-5f && MathF.Abs(lhs.a - rhs.a) < 1e-5f;
        public static bool operator !=(Color lhs, Color rhs) => !(lhs == rhs);
        public static implicit operator Vector4(Color c) => new(c.r, c.g, c.b, c.a);
        public static implicit operator Color(Vector4 v) => new(v.x, v.y, v.z, v.w);

        public override bool Equals(object other) => other is Color c && Equals(c);
        public bool Equals(Color o) => r == o.r && g == o.g && b == o.b && a == o.a;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "RGBA({0:F3}, {1:F3}, {2:F3}, {3:F3})", r, g, b, a);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new(
            (byte)Math.Round(Mathf.Clamp01(c.r) * 255f), (byte)Math.Round(Mathf.Clamp01(c.g) * 255f),
            (byte)Math.Round(Mathf.Clamp01(c.b) * 255f), (byte)Math.Round(Mathf.Clamp01(c.a) * 255f));
        public static implicit operator Color(Color32 c) => new(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string htmlString, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(htmlString)) return false;
            string s = htmlString.Trim();
            if (!s.StartsWith("#"))
            {
                switch (s.ToLowerInvariant())
                {
                    case "red": color = Color.red; return true;
                    case "white": color = Color.white; return true;
                    case "black": color = Color.black; return true;
                    case "green": color = Color.green; return true;
                    case "blue": color = Color.blue; return true;
                    case "yellow": color = Color.yellow; return true;
                    case "cyan": color = Color.cyan; return true;
                    case "magenta": color = Color.magenta; return true;
                    case "gray": case "grey": color = Color.gray; return true;
                    default: return false;
                }
            }
            s = s.Substring(1);
            if (s.Length == 3 || s.Length == 4)
            {
                var sb = new System.Text.StringBuilder();
                foreach (char ch in s) sb.Append(ch).Append(ch);
                s = sb.ToString();
            }
            if (s.Length != 6 && s.Length != 8) return false;
            if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v)) return false;
            if (s.Length == 6) v = (v << 8) | 0xFF;
            color = new Color(((v >> 24) & 0xFF) / 255f, ((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f);
            return true;
        }

        public static string ToHtmlStringRGB(Color c)
        {
            Color32 k = c;
            return $"{k.r:X2}{k.g:X2}{k.b:X2}";
        }

        public static string ToHtmlStringRGBA(Color c)
        {
            Color32 k = c;
            return $"{k.r:X2}{k.g:X2}{k.b:X2}{k.a:X2}";
        }
    }

    public struct Bounds
    {
        Vector3 _center;
        Vector3 _extents;

        public Bounds(Vector3 center, Vector3 size)
        {
            _center = center;
            _extents = size * 0.5f;
        }

        public Vector3 center { get => _center; set => _center = value; }
        public Vector3 size { get => _extents * 2f; set => _extents = value * 0.5f; }
        public Vector3 extents { get => _extents; set => _extents = value; }
        public Vector3 min { get => _center - _extents; set => SetMinMax(value, max); }
        public Vector3 max { get => _center + _extents; set => SetMinMax(min, value); }

        public void SetMinMax(Vector3 min, Vector3 max)
        {
            _extents = (max - min) * 0.5f;
            _center = min + _extents;
        }

        public void Encapsulate(Vector3 point) => SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point));

        public void Encapsulate(Bounds bounds)
        {
            Encapsulate(bounds.center - bounds.extents);
            Encapsulate(bounds.center + bounds.extents);
        }

        public void Expand(float amount) => _extents += new Vector3(amount, amount, amount) * 0.5f;
        public void Expand(Vector3 amount) => _extents += amount * 0.5f;

        public bool Contains(Vector3 p)
        {
            Vector3 mn = min, mx = max;
            return p.x >= mn.x && p.y >= mn.y && p.z >= mn.z && p.x <= mx.x && p.y <= mx.y && p.z <= mx.z;
        }

        public bool Intersects(Bounds b)
        {
            Vector3 amin = min, amax = max, bmin = b.min, bmax = b.max;
            return amin.x <= bmax.x && amax.x >= bmin.x && amin.y <= bmax.y && amax.y >= bmin.y
                   && amin.z <= bmax.z && amax.z >= bmin.z;
        }

        public Vector3 ClosestPoint(Vector3 p)
        {
            Vector3 mn = min, mx = max;
            return new Vector3(Mathf.Clamp(p.x, mn.x, mx.x), Mathf.Clamp(p.y, mn.y, mx.y), Mathf.Clamp(p.z, mn.z, mx.z));
        }

        public float SqrDistance(Vector3 p) => (ClosestPoint(p) - p).sqrMagnitude;

        public override string ToString() => $"Center: {_center}, Extents: {_extents}";
    }

    public struct Rect : IEquatable<Rect>
    {
        float _x, _y, _w, _h;

        public Rect(float x, float y, float width, float height) { _x = x; _y = y; _w = width; _h = height; }
        public Rect(Vector2 position, Vector2 size) { _x = position.x; _y = position.y; _w = size.x; _h = size.y; }

        public static Rect zero => new(0, 0, 0, 0);
        public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) =>
            new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        public float x { get => _x; set => _x = value; }
        public float y { get => _y; set => _y = value; }
        public float width { get => _w; set => _w = value; }
        public float height { get => _h; set => _h = value; }
        public Vector2 position { get => new(_x, _y); set { _x = value.x; _y = value.y; } }
        public Vector2 size { get => new(_w, _h); set { _w = value.x; _h = value.y; } }
        public Vector2 center { get => new(_x + _w / 2f, _y + _h / 2f); set { _x = value.x - _w / 2f; _y = value.y - _h / 2f; } }
        public Vector2 min => new(xMin, yMin);
        public Vector2 max => new(xMax, yMax);
        public float xMin { get => MathF.Min(_x, _x + _w); set { float oldxmax = xMax; _x = value; _w = oldxmax - _x; } }
        public float yMin { get => MathF.Min(_y, _y + _h); set { float oldymax = yMax; _y = value; _h = oldymax - _y; } }
        public float xMax { get => MathF.Max(_x, _x + _w); set => _w = value - _x; }
        public float yMax { get => MathF.Max(_y, _y + _h); set => _h = value - _y; }

        public bool Contains(Vector2 point) => point.x >= xMin && point.x < xMax && point.y >= yMin && point.y < yMax;
        public bool Contains(Vector3 point) => Contains(new Vector2(point.x, point.y));
        public bool Overlaps(Rect other) =>
            other.xMax > xMin && other.xMin < xMax && other.yMax > yMin && other.yMin < yMax;

        public static bool operator ==(Rect a, Rect b) => a._x == b._x && a._y == b._y && a._w == b._w && a._h == b._h;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public bool Equals(Rect o) => this == o;
        public override bool Equals(object o) => o is Rect r && this == r;
        public override int GetHashCode() => HashCode.Combine(_x, _y, _w, _h);
    }

    [Serializable]
    public class RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { this.left = left; this.right = right; this.top = top; this.bottom = bottom; }
        public int horizontal => left + right;
        public int vertical => top + bottom;
    }

    public struct Ray
    {
        Vector3 _origin;
        Vector3 _direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            _origin = origin;
            _direction = direction.normalized;
        }

        public Vector3 origin { get => _origin; set => _origin = value; }
        public Vector3 direction { get => _direction; set => _direction = value.normalized; }
        public Vector3 GetPoint(float distance) => _origin + _direction * distance;
    }

    public struct Plane
    {
        public Vector3 normal;
        public float distance;
        public Plane(Vector3 inNormal, Vector3 inPoint)
        {
            normal = inNormal.normalized;
            distance = -Vector3.Dot(normal, inPoint);
        }
        public bool Raycast(Ray ray, out float enter)
        {
            float vdot = Vector3.Dot(ray.direction, normal);
            float ndot = -Vector3.Dot(ray.origin, normal) - distance;
            if (Mathf.Approximately(vdot, 0f)) { enter = 0f; return false; }
            enter = ndot / vdot;
            return enter > 0f;
        }
    }
}
