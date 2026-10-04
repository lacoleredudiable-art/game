using System;
using System.Collections.Generic;

namespace Dovus.Core.Boss
{
    /// <summary>Arena merkezinde üst üste binmeyen, ömürlü ağ alanları (Ağ Örme).</summary>
    public sealed class WebFieldSet
    {
        readonly int _maxCount;
        readonly float _radiusM;
        readonly double _lifeMs;
        readonly float _minCenterDistM;
        readonly List<Entry> _entries = new();

        struct Entry
        {
            public float X;
            public float Z;
            public double ExpireMs;
        }

        public WebFieldSet(int maxCount, float radiusM, double lifeMs, float minCenterDistM)
        {
            _maxCount = Math.Max(1, maxCount);
            _radiusM = Math.Max(BossDefaults.MinDistM, radiusM);
            _lifeMs = Math.Max(1.0, lifeMs);
            _minCenterDistM = Math.Max(0f, minCenterDistM);
        }

        public float RadiusM => _radiusM;
        public int Count => _entries.Count;

        public void Clear() => _entries.Clear();

        public void Prune(double nowMs)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].ExpireMs <= nowMs)
                    _entries.RemoveAt(i);
            }
        }

        public void Add(float x, float z, double nowMs)
        {
            PushFromCenter(ref x, ref z);
            Prune(nowMs);
            while (_entries.Count >= _maxCount)
                _entries.RemoveAt(0);
            _entries.Add(new Entry { X = x, Z = z, ExpireMs = nowMs + _lifeMs });
        }

        public bool Contains(float x, float z, double nowMs)
        {
            Prune(nowMs);
            float r = _radiusM;
            float r2 = r * r;
            for (int i = 0; i < _entries.Count; i++)
            {
                float dx = x - _entries[i].X;
                float dz = z - _entries[i].Z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }
            return false;
        }

        void PushFromCenter(ref float x, ref float z)
        {
            if (_minCenterDistM <= 0f)
                return;
            float dist = MathF.Sqrt(x * x + z * z);
            if (dist >= _minCenterDistM || dist < 0.0001f)
                return;
            float scale = _minCenterDistM / dist;
            x *= scale;
            z *= scale;
        }
    }
}
