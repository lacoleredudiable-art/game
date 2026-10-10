using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>İşaretli sıfat için takım işareti (PR2: basit dünya noktası).</summary>
    public sealed class RuleEngineV4TeamMarkRegistry
    {
        readonly List<Entry> _marks = new();

        struct Entry
        {
            public Transform MarkedRoot;
            public float ExpireWorldMs;
        }

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

        public void Prune(double worldMs)
        {
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                if (_marks[i].MarkedRoot == null || worldMs >= _marks[i].ExpireWorldMs)
                    _marks.RemoveAt(i);
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
    }
}
