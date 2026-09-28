using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// v6.1.1 passive_slot_system. Pasif rünün sıfat modlarını süreli tutar.
    /// Aynı pasif yeniden çizilince kalan süreye eklenir; farklı pasifler birlikte yaşar.
    /// </summary>
    public sealed class SlotPassiveDirector
    {
        readonly List<ActiveSlotPassive> _active = new();

        public IReadOnlyList<ActiveSlotPassive> Active => _active;
        public int ActiveCount => _active.Count;

        public bool Activate(int runeId, string name, float durationSec, JsonValue modifiers, double worldMs)
        {
            if (runeId <= 0 || durationSec <= 0f)
                return false;

            double addMs = durationSec * 1000.0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].RuneId != runeId)
                    continue;
                ActiveSlotPassive current = _active[i];
                double baseUntil = Math.Max(worldMs, current.UntilMs);
                _active[i] = new ActiveSlotPassive(
                    runeId, name, current.SinceMs, baseUntil + addMs, modifiers);
                return true;
            }

            _active.Add(new ActiveSlotPassive(
                runeId, name, worldMs, worldMs + addMs, modifiers));
            return true;
        }

        public bool Tick(double worldMs)
        {
            bool removed = false;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (worldMs < _active[i].UntilMs)
                    continue;
                _active.RemoveAt(i);
                removed = true;
            }
            return removed;
        }

        public float DamageMult
        {
            get
            {
                float result = 1f;
                for (int i = 0; i < _active.Count; i++)
                {
                    JsonValue mods = _active[i].Modifiers;
                    result *= PositiveOrOne(mods["damage_mult"].AsFloat(1f));
                    float buff = mods["self_damage_buff"].AsFloat(0f);
                    if (buff > 0f)
                        result *= 1f + buff;
                }
                return result;
            }
        }

        public float HitboxSizeMult => Product("hitbox_scale_mult");
        public float PoiseDamageMult => Product("poise_damage_mult");
        public float LifestealAdd => Sum("lifesteal");
        public float ReflectRatioAdd => Sum("reflect_ratio");

        public bool HasModifier(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            for (int i = 0; i < _active.Count; i++)
            {
                JsonValue value = _active[i].Modifiers[key];
                if (value.Kind == JsonKind.Bool && value.AsBool(false))
                    return true;
                if (value.Kind == JsonKind.Number && value.AsFloat(0f) != 0f)
                    return true;
                if (value.Kind == JsonKind.String && !string.IsNullOrEmpty(value.AsString()))
                    return true;
            }
            return false;
        }

        public float MaxModifier(string key)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
                result = Math.Max(result, _active[i].Modifiers[key].AsFloat(0f));
            return result;
        }

        public string StringModifier(string key)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                string value = _active[i].Modifiers[key].AsString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        float Product(string key)
        {
            float result = 1f;
            for (int i = 0; i < _active.Count; i++)
                result *= PositiveOrOne(_active[i].Modifiers[key].AsFloat(1f));
            return result;
        }

        float Sum(string key)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
                result += Math.Max(0f, _active[i].Modifiers[key].AsFloat(0f));
            return result;
        }

        static float PositiveOrOne(float value) => value > 0f ? value : 1f;
    }

    public readonly struct ActiveSlotPassive
    {
        public ActiveSlotPassive(
            int runeId,
            string name,
            double sinceMs,
            double untilMs,
            JsonValue modifiers)
        {
            RuneId = runeId;
            Name = name ?? string.Empty;
            SinceMs = sinceMs;
            UntilMs = untilMs;
            Modifiers = modifiers;
        }

        public int RuneId { get; }
        public string Name { get; }
        public double SinceMs { get; }
        public double UntilMs { get; }
        public JsonValue Modifiers { get; }
        public float RemainingSec(double worldMs) =>
            (float)(Math.Max(0.0, UntilMs - worldMs) / 1000.0);
    }
}
