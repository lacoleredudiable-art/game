using System;
using System.Collections.Generic;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    public sealed partial class StatusBoard
    {
        bool AttackLockPresent() =>
            Has(StatusKind.Stun) || Has(StatusKind.Fear) || Has(StatusKind.Stasis);

        static bool IsAttackLockKind(StatusKind kind) =>
            kind is StatusKind.Stun or StatusKind.Fear or StatusKind.Stasis;

        void FinishAttackLockIfEnded(bool hadLock)
        {
            if (!_attackLockImmunity || !hadLock || AttackLockPresent() || _rootImmunityMs <= 0)
                return;
            _attackLockImmunityRemainingMs = _rootImmunityMs;
        }

        void ApplyRoot(string? sourceId, double durationMs, float magnitude)
        {
            if (_rootImmunityRemainingMs > 0)
                return;

            if (durationMs <= 0)
                return;

            string source = string.IsNullOrEmpty(sourceId) ? "root" : sourceId;
            _rootSources[source] = durationMs;
            PublishRoot(magnitude > 0f ? magnitude : 1f);
        }

        void DecayRoots(double worldDtMs)
        {
            if (_rootSources.Count == 0)
                return;

            var keys = new List<string>(_rootSources.Keys);
            var dead = new List<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                double rem = _rootSources[keys[i]] - worldDtMs;
                if (rem <= 0)
                    dead.Add(keys[i]);
                else
                    _rootSources[keys[i]] = rem;
            }

            for (int i = 0; i < dead.Count; i++)
                _rootSources.Remove(dead[i]);

            if (_rootSources.Count == 0)
            {
                _active.Remove(StatusKind.Root);
                BeginRootImmunity();
                return;
            }

            float mag = 1f;
            if (_active.TryGetValue(StatusKind.Root, out StatusEntry existing))
                mag = existing.Magnitude;
            PublishRoot(mag);
        }

        void PublishRoot(float magnitude)
        {
            double longest = 0;
            foreach (KeyValuePair<string, double> kv in _rootSources)
            {
                if (kv.Value > longest)
                    longest = kv.Value;
            }

            if (longest <= 0)
            {
                _active.Remove(StatusKind.Root);
                return;
            }

            if (_active.TryGetValue(StatusKind.Root, out StatusEntry existing))
            {
                existing.RemainingMs = longest;
                existing.Magnitude = Math.Max(existing.Magnitude, magnitude);
                existing.TotalDurationMs = Math.Max(existing.TotalDurationMs, longest);
                _active[StatusKind.Root] = existing;
                return;
            }

            _active[StatusKind.Root] = new StatusEntry(longest, magnitude, longest);
        }

        void EndRoot()
        {
            _rootSources.Clear();
            _active.Remove(StatusKind.Root);
            BeginRootImmunity();
        }

        void BeginRootImmunity()
        {
            if (_rootImmunityMs > 0)
                _rootImmunityRemainingMs = _rootImmunityMs;
        }

        /// <summary>
        /// Yavaşlatma ve hız: aynı kaynak süreyi yeniler (max), farklı kaynaklar toplanmaz.
        /// Güçte en güçlü olan kalır (yavaşta küçük çarpan, hızda büyük çarpan). Bağışıklık yok.
        /// </summary>
        void ApplyTempo(
            Dictionary<string, TempoSource> sources,
            StatusKind kind,
            string? sourceId,
            string fallbackSource,
            double durationMs,
            float magnitude)
        {
            if (durationMs <= 0)
                return;

            string source = string.IsNullOrEmpty(sourceId) ? fallbackSource : sourceId;
            if (sources.TryGetValue(source, out TempoSource existing))
            {
                existing.RemainingMs = Math.Max(existing.RemainingMs, durationMs);
                existing.Magnitude = magnitude;
                sources[source] = existing;
            }
            else
                sources[source] = new TempoSource(durationMs, magnitude);

            PublishTempo(kind, sources);
        }

        void DecayTempo(StatusKind kind, Dictionary<string, TempoSource> sources, double worldDtMs)
        {
            if (sources.Count == 0)
                return;

            var keys = new List<string>(sources.Keys);
            var dead = new List<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                TempoSource source = sources[keys[i]];
                source.RemainingMs -= worldDtMs;
                if (source.RemainingMs <= 0)
                    dead.Add(keys[i]);
                else
                    sources[keys[i]] = source;
            }

            for (int i = 0; i < dead.Count; i++)
                sources.Remove(dead[i]);

            PublishTempo(kind, sources);
        }

        void PublishTempo(StatusKind kind, Dictionary<string, TempoSource> sources)
        {
            double longest = 0;
            float strongest = 0f;
            bool any = false;
            foreach (KeyValuePair<string, TempoSource> kv in sources)
            {
                if (kv.Value.RemainingMs <= 0)
                    continue;
                if (!any)
                {
                    any = true;
                    longest = kv.Value.RemainingMs;
                    strongest = kv.Value.Magnitude;
                    continue;
                }

                if (kv.Value.RemainingMs > longest)
                    longest = kv.Value.RemainingMs;
                strongest = kind == StatusKind.Slow
                    ? StrongerSlow(strongest, kv.Value.Magnitude)
                    : Math.Max(strongest, kv.Value.Magnitude);
            }

            if (!any || longest <= 0)
            {
                _active.Remove(kind);
                return;
            }

            if (_active.TryGetValue(kind, out StatusEntry existing))
            {
                existing.RemainingMs = longest;
                existing.Magnitude = strongest;
                existing.TotalDurationMs = Math.Max(existing.TotalDurationMs, longest);
                _active[kind] = existing;
                return;
            }

            _active[kind] = new StatusEntry(longest, strongest, longest);
        }

        static float StrongerSlow(float current, float candidate)
        {
            if (candidate <= 0f)
                return current;
            if (current <= 0f)
                return candidate;
            return Math.Min(current, candidate);
        }

        struct TempoSource
        {
            public TempoSource(double remainingMs, float magnitude)
            {
                RemainingMs = remainingMs;
                Magnitude = magnitude;
            }

            public double RemainingMs;
            public float Magnitude;
        }

        struct StatusEntry
        {
            public StatusEntry(double remainingMs, float magnitude, double totalDurationMs)
            {
                RemainingMs = remainingMs;
                Magnitude = magnitude;
                TotalDurationMs = totalDurationMs > 0 ? totalDurationMs : remainingMs;
            }

            public double RemainingMs;
            public float Magnitude;
            /// <summary>Apply anındaki süre — HUD radial fill için.</summary>
            public double TotalDurationMs;
        }
    }
}
