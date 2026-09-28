using System;
using System.Collections.Generic;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// Dünya zamanıyla tutulan küçük durum geçmişi. Unity tiplerini bilmez; geri sarılacak
    /// adaptör konum/cast durumunu T içine koyar.
    /// </summary>
    public sealed class TimedHistory<T>
    {
        readonly struct Sample
        {
            public Sample(double worldMs, T value)
            {
                WorldMs = worldMs;
                Value = value;
            }

            public double WorldMs { get; }
            public T Value { get; }
        }

        readonly List<Sample> _samples = new List<Sample>();
        readonly double _retentionMs;
        readonly double _sampleIntervalMs;

        public TimedHistory(double retentionMs, double sampleIntervalMs)
        {
            if (retentionMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(retentionMs));
            if (sampleIntervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleIntervalMs));
            _retentionMs = retentionMs;
            _sampleIntervalMs = sampleIntervalMs;
        }

        public int Count => _samples.Count;

        public void Record(double worldMs, T value)
        {
            if (_samples.Count > 0 && worldMs < _samples[_samples.Count - 1].WorldMs)
                throw new ArgumentOutOfRangeException(nameof(worldMs), "Dünya zamanı geriye gidemez.");

            if (_samples.Count == 0 || worldMs - _samples[_samples.Count - 1].WorldMs >= _sampleIntervalMs)
                _samples.Add(new Sample(worldMs, value));

            double oldest = worldMs - _retentionMs;
            int remove = 0;
            while (remove + 1 < _samples.Count && _samples[remove + 1].WorldMs < oldest)
                remove++;
            if (remove > 0)
                _samples.RemoveRange(0, remove);
        }

        /// <summary>Hedef zamandaki veya ondan önceki en yakın örnek; yoksa en eski örnek.</summary>
        public bool TryGetAtOrBefore(double targetWorldMs, out T value)
        {
            if (_samples.Count == 0)
            {
                value = default;
                return false;
            }

            for (int i = _samples.Count - 1; i >= 0; i--)
            {
                if (_samples[i].WorldMs <= targetWorldMs)
                {
                    value = _samples[i].Value;
                    return true;
                }
            }

            value = _samples[0].Value;
            return true;
        }

        public void Clear() => _samples.Clear();
    }
}
