using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>İşaretli sıfat için takım işareti: canlı gövde ya da MarkOnly yüzeyde sabit nokta.</summary>
    public sealed class RuleEngineV4TeamMarkRegistry
    {
        readonly List<Entry> _marks = new();
        readonly List<SurfaceEntry> _surfaceMarks = new();

        struct Entry
        {
            public Transform MarkedRoot;
            public float ExpireWorldMs;
        }

        struct SurfaceEntry
        {
            public RuleEngineV4SurfaceMark Mark;
            public float ExpireWorldMs;
        }

        public int SurfaceMarkCount => _surfaceMarks.Count;

        public void Place(Transform marked, float lifeSec, double worldMs)
        {
            if (marked == null || lifeSec <= 0)
                return;
            float expire = (float)worldMs + lifeSec * 1000f;
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].MarkedRoot == marked)
                {
                    _marks[i] = new Entry { MarkedRoot = marked, ExpireWorldMs = expire };
                    return;
                }
            }
            _marks.Add(new Entry { MarkedRoot = marked, ExpireWorldMs = expire });
        }

        public RuleEngineV4SurfaceMark PlaceOnSurface(in RuleEngineV4SurfaceAim aim, float lifeSec, double worldMs)
        {
            if (!aim.IsValid || lifeSec <= 0)
                return null;
            var go = new GameObject("RuleEngineV4SurfaceMark");
            go.transform.position = aim.Point;
            var body = go.AddComponent<RuleEngineV4PhysicsBodyHost>();
            body.WeightTier = RuleEngineV4WeightTier.Anchored;
            body.BodyRadiusM = 0f;
            var mark = go.AddComponent<RuleEngineV4SurfaceMark>();
            mark.Configure(aim.Surface, aim.Normal);
            _surfaceMarks.Add(new SurfaceEntry { Mark = mark, ExpireWorldMs = (float)(worldMs + lifeSec * Units.SecToMs) });
            return mark;
        }

        public void Prune(double worldMs)
        {
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                if (_marks[i].MarkedRoot == null || worldMs >= _marks[i].ExpireWorldMs)
                    _marks.RemoveAt(i);
            }
            for (int i = _surfaceMarks.Count - 1; i >= 0; i--)
            {
                SurfaceEntry e = _surfaceMarks[i];
                if (e.Mark != null && worldMs < e.ExpireWorldMs)
                    continue;
                if (e.Mark != null)
                    Object.Destroy(e.Mark.gameObject);
                _surfaceMarks.RemoveAt(i);
            }
        }

        public bool HasMarkOn(Transform marked, double worldMs)
        {
            Prune(worldMs);
            if (marked == null)
                return false;
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].MarkedRoot == marked || marked.IsChildOf(_marks[i].MarkedRoot))
                    return true;
            }
            return false;
        }

        /// <summary>Yatay mesafeyle en yakın yüzey işareti (menzil içinde).</summary>
        public bool TryNearestSurfaceMark(Vector3 from, float rangeM, double worldMs, out Transform anchor, out float distanceM)
        {
            Prune(worldMs);
            anchor = null;
            distanceM = float.MaxValue;
            for (int i = 0; i < _surfaceMarks.Count; i++)
            {
                Vector3 flat = _surfaceMarks[i].Mark.transform.position - from;
                flat.y = 0;
                float d = flat.magnitude;
                if (d > rangeM || d >= distanceM)
                    continue;
                distanceM = d;
                anchor = _surfaceMarks[i].Mark.transform;
            }
            return anchor != null;
        }
    }
}
