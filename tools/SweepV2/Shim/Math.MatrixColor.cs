using System;
using System.Globalization;

namespace UnityEngine
{
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

}
