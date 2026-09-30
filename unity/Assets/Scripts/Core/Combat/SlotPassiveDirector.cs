using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// v6.1.1 passive_slot_system. Pasif rünün sıfat modlarını süreli tutar.
    /// Aynı pasif yeniden çizilince kalan süreye eklenir; farklı pasifler birlikte yaşar.
    /// Yeni açılan pasif, kendini açan cast'e uygulanmaz (ExcludedCastId). Sonraki
    /// cast ve canlı vuruşlar (castId 0) görür.
    /// </summary>
    public sealed class SlotPassiveDirector
    {
        readonly List<ActiveSlotPassive> _active = new();
        readonly List<EchoCharge> _echoes = new();
        int _nextCastId = 1;
        int _openCastId;

        public IReadOnlyList<ActiveSlotPassive> Active => _active;
        public int ActiveCount => _active.Count;
        public int OpenCastId => _openCastId;

        /// <summary>Bu skill cast'i boyunca yeni pasifler bu kimliği dışlar.</summary>
        public int OpenCast()
        {
            _openCastId = _nextCastId++;
            return _openCastId;
        }

        public void CloseCast() => _openCastId = 0;

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
                    runeId, name, current.SinceMs, baseUntil + addMs, modifiers, current.ExcludedCastId);
                NoteEcho(modifiers);
                return true;
            }

            _active.Add(new ActiveSlotPassive(
                runeId, name, worldMs, worldMs + addMs, modifiers, _openCastId));
            NoteEcho(modifiers);
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

        /// <summary>Canlı okuma (cast 0): HUD ve gelen hasar. Tetikleyen cast For(castId) kullanır.</summary>
        public float DamageMult => DamageMultFor(0);
        public float HitboxSizeMult => HitboxSizeMultFor(0);
        public float PoiseDamageMult => PoiseDamageMultFor(0);
        public float LifestealAdd => LifestealAddFor(0);
        public float ReflectRatioAdd => ReflectRatioAddFor(0);

        public float DamageMultFor(int castId)
        {
            float result = 1f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                JsonValue mods = _active[i].Modifiers;
                result *= PositiveOrOne(mods["damage_mult"].AsFloat(1f));
                float buff = mods["self_damage_buff"].AsFloat(0f);
                if (buff > 0f)
                    result *= 1f + buff;
            }
            return result;
        }

        public float HitboxSizeMultFor(int castId) => ProductFor("hitbox_scale_mult", castId);
        public float PoiseDamageMultFor(int castId) => ProductFor("poise_damage_mult", castId);
        public float LifestealAddFor(int castId) => SumFor("lifesteal", castId);
        public float ReflectRatioAddFor(int castId) => SumFor("reflect_ratio", castId);

        public float LifetimeAddSecFor(int castId) => SumFor("lifetime_add", castId);

        public float AccuracyLifetimeAddSecFor(int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                JsonValue mods = _active[i].Modifiers;
                if (mods["accuracy_debuff"].AsFloat(0f) <= 0f)
                    continue;
                result += Math.Max(0f, mods["lifetime_add"].AsFloat(0f));
            }
            return result;
        }

        public bool HasAccuracyDebuff(int castId) => AccuracyDebuffFor(castId) > 0f;

        /// <summary>Bulandırma accuracy_debuff. Kör ıskalama şansı budur (0.3 = %30).</summary>
        public float AccuracyDebuffFor(int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                result = Math.Max(result, _active[i].Modifiers["accuracy_debuff"].AsFloat(0f));
            }
            return result;
        }

        /// <summary>ignore_armor = zırh delme %50. Tam yok sayma değil.</summary>
        public float ArmorPenPercentFor(int castId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                if (_active[i].Modifiers["ignore_armor"].AsBool(false))
                    return SlotPassiveCombat.IgnoreArmorPierce;
            }
            return 0f;
        }

        public float RootSecondsFor(int castId) => MaxFor("apply_root_sec", castId);

        /// <summary>apply_slow 0.3 → hız 0.7. Gramerdeki 1 − apply_slow.</summary>
        public float SlowSpeedFor(int castId)
        {
            float slow = MaxFor("apply_slow", castId);
            if (slow <= 0f)
                return 1f;
            float speed = 1f - slow;
            if (speed < 0f)
                speed = 0f;
            return speed;
        }

        public int BounceCountFor(int castId) => (int)MaxFor("bounce_targets", castId);

        public float BounceDamageMultFor(int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                if (_active[i].Modifiers["bounce_targets"].AsFloat(0f) <= 0f)
                    continue;
                result = Math.Max(result, _active[i].Modifiers["bounce_damage_mult"].AsFloat(0f));
            }
            return result;
        }

        public float ChannelSecFor(int castId) => MaxFor("channel_sec", castId);

        public float TickRateMultFor(int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                result = Math.Max(result, _active[i].Modifiers["tick_rate_mult"].AsFloat(0f));
            }
            return result;
        }

        public int MaxTargetsFor(int castId) => (int)MaxFor("max_targets", castId);

        /// <summary>Pasif oyuncuyu köklemez. cast_mobility bu yoldan okunmaz.</summary>
        public bool BlocksPlayerMovement => false;

        public bool HasModifier(string key) => HasModifierFor(key, 0);

        public bool HasModifierFor(string key, int castId)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
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

        public float MaxModifier(string key) => MaxFor(key, 0);

        public string StringModifier(string key)
        {
            if (string.Equals(key, "cast_mobility", StringComparison.Ordinal))
                return string.Empty;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                string value = _active[i].Modifiers[key].AsString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        /// <summary>
        /// Kopyalama: aktivasyon başına bir yankı. Tetikleyen cast harcamaz.
        /// Gecikme JSON duplicate_delay_sec; güç %50 (pasif kopya tam güç değil).
        /// </summary>
        public bool TryConsumeEcho(int castId, out float delaySec, out float power)
        {
            delaySec = 0f;
            power = 0f;
            if (!DuplicateApplies(castId))
                return false;
            for (int i = 0; i < _echoes.Count; i++)
            {
                EchoCharge echo = _echoes[i];
                if (echo.Spent)
                    continue;
                if (castId != 0 && castId == echo.ExcludedCastId)
                    continue;
                echo.Spent = true;
                _echoes[i] = echo;
                delaySec = echo.DelaySec;
                power = echo.Power;
                return true;
            }
            return false;
        }

        bool DuplicateApplies(int castId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                if (_active[i].Modifiers["duplicate_cast"].AsBool(false))
                    return true;
            }
            return false;
        }

        void NoteEcho(JsonValue modifiers)
        {
            if (modifiers.IsNull || !modifiers["duplicate_cast"].AsBool(false))
                return;
            float delay = modifiers["duplicate_delay_sec"].AsFloat(SlotPassiveCombat.EchoDelaySec);
            if (delay < 0f)
                delay = 0f;
            _echoes.Add(new EchoCharge
            {
                ExcludedCastId = _openCastId,
                DelaySec = delay,
                Power = SlotPassiveCombat.EchoPower,
                Spent = false
            });
        }

        bool Applies(ActiveSlotPassive passive, int castId) =>
            castId == 0 || castId != passive.ExcludedCastId;

        float ProductFor(string key, int castId)
        {
            float result = 1f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                result *= PositiveOrOne(_active[i].Modifiers[key].AsFloat(1f));
            }
            return result;
        }

        float SumFor(string key, int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                result += Math.Max(0f, _active[i].Modifiers[key].AsFloat(0f));
            }
            return result;
        }

        float MaxFor(string key, int castId)
        {
            float result = 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!Applies(_active[i], castId))
                    continue;
                result = Math.Max(result, _active[i].Modifiers[key].AsFloat(0f));
            }
            return result;
        }

        static float PositiveOrOne(float value) => value > 0f ? value : 1f;

        struct EchoCharge
        {
            public int ExcludedCastId;
            public float DelaySec;
            public float Power;
            public bool Spent;
        }
    }

    public readonly struct ActiveSlotPassive
    {
        public ActiveSlotPassive(
            int runeId,
            string name,
            double sinceMs,
            double untilMs,
            JsonValue modifiers,
            int excludedCastId = 0)
        {
            RuneId = runeId;
            Name = name ?? string.Empty;
            SinceMs = sinceMs;
            UntilMs = untilMs;
            Modifiers = modifiers;
            ExcludedCastId = excludedCastId;
        }

        public int RuneId { get; }
        public string Name { get; }
        public double SinceMs { get; }
        public double UntilMs { get; }
        public JsonValue Modifiers { get; }
        public int ExcludedCastId { get; }
        public float RemainingSec(double worldMs) =>
            (float)(Math.Max(0.0, UntilMs - worldMs) / 1000.0);
    }
}
