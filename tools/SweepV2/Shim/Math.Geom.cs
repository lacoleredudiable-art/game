using System;
using System.Globalization;

namespace UnityEngine
{
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

}
