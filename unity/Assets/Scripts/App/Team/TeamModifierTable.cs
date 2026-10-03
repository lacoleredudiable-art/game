using System;
using System.Collections.Generic;

namespace Dovus.App.Team
{
    public sealed class TeamModifierTable
    {
        static readonly ActorModifiers DefaultModifiers = ActorModifiers.Default;

        readonly Dictionary<int, ActorModifiers> _modifiers = new();
        readonly Dictionary<int, float> _miss = new();
        readonly Dictionary<int, float> _taken = new();

        public float BossIncomingMult { get; set; } = 1f;
        public float BossStrikeScale { get; set; } = 1f;
        public Func<float> Roll { get; set; }

        public ActorModifiers For(int actorId) =>
            _modifiers.TryGetValue(actorId, out ActorModifiers mods) ? mods : DefaultModifiers;

        public void Set(int actorId, ActorModifiers modifiers) => _modifiers[actorId] = modifiers;

        public void Reset()
        {
            _modifiers.Clear();
            _miss.Clear();
            _taken.Clear();
            BossIncomingMult = 1f;
            BossStrikeScale = 1f;
        }

        public void SetMiss(int actorId, float chance)
        {
            if (chance <= 0f)
                _miss.Remove(actorId);
            else
                _miss[actorId] = chance;
        }

        public void SetTaken(int actorId, float mult)
        {
            if (mult <= 0f)
                mult = 1f;
            if (Math.Abs(mult - 1f) < 0.0001f)
                _taken.Remove(actorId);
            else
                _taken[actorId] = mult;
        }

        public float DamageTakenMult(int actorId) =>
            _taken.TryGetValue(actorId, out float mult) ? mult : 1f;

        public bool TryMiss(int actorId)
        {
            if (!_miss.TryGetValue(actorId, out float chance) || chance <= 0f)
                return false;
            float roll = Roll != null ? Roll() : 1f;
            return roll < chance;
        }
    }
}
