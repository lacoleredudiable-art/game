using System;
using System.Collections.Generic;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json manipulation_layers.time_layer — cast gecikme/yankı/
    /// kalıcılık/ölüm erteleme zamanlaması. Core/Time/TimeDirector (dünya saati) ile
    /// karıştırma: bu sınıf yalnızca "ne zaman tetiklenir" hesabı; patlama/ölüm uygulaması
    /// Faz 6 bağlama turunun işi.
    /// </summary>
    public sealed class TimeEffectDirector
    {
        /// <summary>JSON time_layer.max_active_fields.</summary>
        public const int DefaultMaxActiveFields = 2;

        readonly List<TimeEffectField> _fields = new();
        int _nextId = 1;

        public TimeEffectDirector(int maxActiveFields = DefaultMaxActiveFields)
        {
            MaxActiveFields = Math.Max(1, maxActiveFields);
        }

        public IReadOnlyList<TimeEffectField> ActiveFields => _fields;
        public int MaxActiveFields { get; }

        /// <summary>
        /// delayed_detonation (Karabasan): cast worldMs'te planlanır, delay_sec sonra tetik.
        /// pendingDamage bang hasarı (CollectDue → ComputedDetonationDamage).
        /// delaySec ≤ 0 ise false.
        /// </summary>
        public bool TryScheduleDelayedDetonation(
            string effectId, string element, float delaySec, double worldMs,
            out TimeEffectField field, float pendingDamage = 0f)
        {
            return TrySchedule(
                effectId, element, TimeEffectTypes.DelayedDetonation,
                delaySec, worldMs, damageRatio: 0f, sourceDamage: pendingDamage, out field);
        }

        /// <summary>
        /// echo (Alev): sourceDamage × damage_ratio, delay_sec sonra yankı.
        /// delaySec ≤ 0 veya damageRatio &lt; 0 ise false.
        /// </summary>
        public bool TryScheduleEcho(
            string effectId, string element, float delaySec, float damageRatio,
            float sourceDamage, double worldMs, out TimeEffectField field)
        {
            if (damageRatio < 0f)
            {
                field = default;
                return false;
            }

            return TrySchedule(
                effectId, element, TimeEffectTypes.Echo,
                delaySec, worldMs, damageRatio, sourceDamage, out field);
        }

        /// <summary>
        /// death_delay (Cehennem): ölüm beyanı anı + delay_sec = gerçek tetik zamanı.
        /// PlayerVitals/BossVitals'a bağlanmaz — yalnızca TriggerAtMs.
        /// </summary>
        public bool TryScheduleDeathDelay(
            string effectId, string element, float delaySec, double deathDeclaredAtMs,
            out TimeEffectField field)
        {
            return TrySchedule(
                effectId, element, TimeEffectTypes.DeathDelay,
                delaySec, deathDeclaredAtMs, damageRatio: 0f, sourceDamage: 0f, out field);
        }

        /// <summary>
        /// extend_lifetime (Lav): RemainingSec × multiplier. Anlık; alan yuvası tutmaz.
        /// multiplier ≤ 0 veya remainingSec &lt; 0 ise 0.
        /// </summary>
        public static float ExtendRemainingSec(float remainingSec, float multiplier)
        {
            if (remainingSec < 0f || multiplier <= 0f)
                return 0f;
            return remainingSec * multiplier;
        }

        /// <summary>Yankı hasarı = kaynak × damage_ratio (JSON alev_yanki: 0.6).</summary>
        public static float EchoDamage(float sourceDamage, float damageRatio) =>
            sourceDamage * Math.Max(0f, damageRatio);

        /// <summary>worldMs ≥ TriggerAtMs olan alanları dst'ye yazar ve listeden siler.</summary>
        public int CollectDue(double worldMs, List<TimeEffectField> dst)
        {
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));

            int n = 0;
            for (int i = 0; i < _fields.Count;)
            {
                if (worldMs < _fields[i].TriggerAtMs)
                {
                    i++;
                    continue;
                }

                dst.Add(_fields[i]);
                _fields.RemoveAt(i);
                n++;
            }

            return n;
        }

        public float RemainingSec(int id, double worldMs)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Id != id)
                    continue;
                return Math.Max(0f, (float)((_fields[i].TriggerAtMs - worldMs) / 1000.0));
            }

            return 0f;
        }

        public bool Remove(int id)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Id != id)
                    continue;
                _fields.RemoveAt(i);
                return true;
            }

            return false;
        }

        bool TrySchedule(
            string effectId, string element, string type,
            float delaySec, double worldMs, float damageRatio, float sourceDamage,
            out TimeEffectField field)
        {
            field = default;
            if (delaySec <= 0f)
                return false;

            while (_fields.Count >= MaxActiveFields)
                _fields.RemoveAt(0);

            double triggerAt = worldMs + Math.Round(delaySec * 1000.0);
            field = new TimeEffectField(
                _nextId++,
                effectId ?? string.Empty,
                element ?? string.Empty,
                type,
                triggerAt,
                damageRatio,
                sourceDamage);
            _fields.Add(field);
            return true;
        }
    }

    /// <summary>JSON time_layer.effects[].type sabitleri.</summary>
    public static class TimeEffectTypes
    {
        public const string DelayedDetonation = "delayed_detonation";
        public const string Echo = "echo";
        public const string ExtendLifetime = "extend_lifetime";
        public const string DeathDelay = "death_delay";
    }

    /// <summary>Zamanlanmış bir time_layer alanı (patlama/yankı/ölüm erteleme).</summary>
    public readonly struct TimeEffectField
    {
        public TimeEffectField(
            int id, string effectId, string element, string type,
            double triggerAtMs, float damageRatio, float sourceDamage)
        {
            Id = id;
            EffectId = effectId ?? string.Empty;
            Element = element ?? string.Empty;
            Type = type ?? string.Empty;
            TriggerAtMs = triggerAtMs;
            DamageRatio = damageRatio;
            SourceDamage = sourceDamage;
        }

        public int Id { get; }
        public string EffectId { get; }
        public string Element { get; }
        public string Type { get; }
        public double TriggerAtMs { get; }
        public float DamageRatio { get; }
        public float SourceDamage { get; }

        /// <summary>echo için SourceDamage × DamageRatio; diğer tiplerde 0.</summary>
        public float ComputedEchoDamage =>
            string.Equals(Type, TimeEffectTypes.Echo, StringComparison.Ordinal)
                ? TimeEffectDirector.EchoDamage(SourceDamage, DamageRatio)
                : 0f;

        /// <summary>delayed_detonation için bekleyen bang hasarı; diğer tiplerde 0.</summary>
        public float ComputedDetonationDamage =>
            string.Equals(Type, TimeEffectTypes.DelayedDetonation, StringComparison.Ordinal)
                ? Math.Max(0f, SourceDamage)
                : 0f;
    }
}
