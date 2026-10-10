using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Dilim oturumu: sabit yapı sınırı, azalma, koruma.</summary>
    public sealed class RuleEngineV4WorldSession
    {
        public readonly RuleEngineV4StructureLimits Structures = new();
        readonly Dictionary<int, int> _diminishByTarget = new();
        double _guardUntilMs;
        Transform _guardCaster;

        public bool GuardActive(double worldMs, Transform caster) =>
            _guardCaster == caster && worldMs < _guardUntilMs;

        public void ArmGuard(Transform caster, float durationSec, double worldMs)
        {
            _guardCaster = caster;
            _guardUntilMs = worldMs + durationSec * 1000.0;
        }

        public int DiminishStack(Transform target)
        {
            int key = target != null ? target.GetInstanceID() : 0;
            _diminishByTarget.TryGetValue(key, out int stack);
            _diminishByTarget[key] = stack + 1;
            return stack;
        }

        public float ScaleNonDamage(Transform target, float value) =>
            RuleEngineV4DiminishStack.ScaleNonDamage(DiminishStack(target), value);
    }
}
