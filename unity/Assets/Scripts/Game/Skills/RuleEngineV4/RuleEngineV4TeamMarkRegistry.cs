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
            public Transform Target;
            public float ExpireWorldMs;
        }

        public void Place(Transform target, float lifeSec, double worldMs)
        {
            if (target == null || lifeSec <= 0f)
                return;
            float expire = (float)worldMs + lifeSec * 1000f;
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].Target == target)
                {
                    _marks[i] = new Entry { Target = target, ExpireWorldMs = expire };
                    return;
                }
            }
            _marks.Add(new Entry { Target = target, ExpireWorldMs = expire });
        }

        public void Prune(double worldMs)
        {
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                if (_marks[i].Target == null || worldMs >= _marks[i].ExpireWorldMs)
                    _marks.RemoveAt(i);
            }
        }

        public bool HasMarkOn(Transform target, double worldMs)
        {
            Prune(worldMs);
            if (target == null)
                return false;
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].Target == target || target.IsChildOf(_marks[i].Target))
                    return true;
            }
            return false;
        }
    }
}
