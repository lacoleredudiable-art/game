using System;
using System.Collections.Generic;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    /// <summary>
    /// Tek aktör üzerindeki durumlar. Unity bilmez — süre dünya ms ile akar.
    /// </summary>
    public sealed class StatusBoard
    {
        readonly Dictionary<StatusKind, StatusEntry> _active = new();

        public bool BlocksMovement =>
            Has(StatusKind.Stun) || Has(StatusKind.Root) || Has(StatusKind.Stasis)
            || Has(StatusKind.Fear);

        public bool BlocksCast =>
            Has(StatusKind.Stun) || Has(StatusKind.Silence) || Has(StatusKind.Stasis)
            || Has(StatusKind.Fear);

        public bool BlocksBossAttack =>
            Has(StatusKind.Stun) || Has(StatusKind.Stasis) || Has(StatusKind.Fear);

        public bool HasBlind => Has(StatusKind.Blind);
        public bool HasDisarm => Has(StatusKind.Disarm);

        public float MoveSpeedMult
        {
            get
            {
                if (BlocksMovement) return 0f;
                float m = 1f;
                if (Has(StatusKind.Slow) && _active.TryGetValue(StatusKind.Slow, out StatusEntry slow))
                    m *= slow.Magnitude;
                if (Has(StatusKind.Haste) && _active.TryGetValue(StatusKind.Haste, out StatusEntry haste))
                    m *= haste.Magnitude;
                return m;
            }
        }

        public float IncomingDamageMult
        {
            get
            {
                float m = 1f;
                if (Has(StatusKind.ArmorBreak) && _active.TryGetValue(StatusKind.ArmorBreak, out StatusEntry ab))
                    m *= ab.Magnitude;
                if (Has(StatusKind.DamageReduction) && _active.TryGetValue(StatusKind.DamageReduction, out StatusEntry dr))
                    m *= dr.Magnitude;
                return m;
            }
        }

        public float OutgoingDamageMult =>
            Has(StatusKind.Weaken) && _active.TryGetValue(StatusKind.Weaken, out StatusEntry w)
                ? w.Magnitude
                : 1f;

        /// <summary>
        /// 16 Eylül: GrievousWounds'un magnitude'u eskiden hep 1f'ti (kullanılmıyordu).
        /// Artık gelen heal'e çarpılır — "Kavurucu Yara" (grievous+burn) tepkisi bunu hedefler.
        /// </summary>
        public float HealEffectivenessMult =>
            Has(StatusKind.GrievousWounds) && _active.TryGetValue(StatusKind.GrievousWounds, out StatusEntry gw)
                ? gw.Magnitude
                : 1f;

        public float ShieldRemaining =>
            _active.TryGetValue(StatusKind.Shield, out StatusEntry s) ? s.Magnitude : 0f;

        /// <summary>Stasis = kısa i-frame (dodge dışı skill koruması).</summary>
        public bool IsInvulnerable => Has(StatusKind.Stasis);

        public int ActiveCount => _active.Count;

        public bool Has(StatusKind kind) =>
            kind != StatusKind.None && _active.ContainsKey(kind);

        public IReadOnlyCollection<StatusKind> ActiveKinds => _active.Keys;

        /// <summary>
        /// Süreleri ilerlet; burn/poison/regen tick hasarı/heal döner (pozitif = hasar,
        /// negatif = heal). 16 Eylül: burn/poison artık `tuning`'in sabit değerini değil,
        /// entry'nin KENDİ magnitude'unu okuyor — durum etkileşim tablosu (Apply) bunu
        /// değiştirebildiği için (örn. "Sürünen Alev": slow+burn → burn ×1.3) artık gerçek
        /// bir etkisi var; eskiden magnitude saklanıp hiç okunmuyordu.
        /// </summary>
        public float Tick(double worldDtMs, StatusTuning tuning)
        {
            if (worldDtMs <= 0 || _active.Count == 0)
                return 0f;

            float tickPayload = 0f;
            float dtSec = (float)(worldDtMs / 1000.0);
            float burnTickThisFrame = 0f;

            var expired = new List<StatusKind>();
            var refreshed = new List<KeyValuePair<StatusKind, StatusEntry>>();
            foreach (var kv in _active)
            {
                StatusEntry e = kv.Value;
                e.RemainingMs -= worldDtMs;
                if (kv.Key == StatusKind.Burn)
                {
                    burnTickThisFrame = e.Magnitude * dtSec;
                    tickPayload += burnTickThisFrame;
                }
                else if (kv.Key == StatusKind.Poison)
                    tickPayload += e.Magnitude * dtSec;
                else if (kv.Key == StatusKind.Regen)
                    tickPayload -= e.Magnitude * dtSec;

                if (e.RemainingMs <= 0)
                    expired.Add(kv.Key);
                else
                    refreshed.Add(new KeyValuePair<StatusKind, StatusEntry>(kv.Key, e));
            }

            // "Zehirli Ateş": burn + poison aynı anda → ekstra tick (docs/element-sistemi.json).
            if (burnTickThisFrame > 0f && _active.ContainsKey(StatusKind.Poison))
                tickPayload += tuning.BurnPoisonComboBonusPerSec * dtSec;

            for (int i = 0; i < refreshed.Count; i++)
                _active[refreshed[i].Key] = refreshed[i].Value;

            for (int i = 0; i < expired.Count; i++)
                _active.Remove(expired[i]);

            // "Yanan Kalkan": burn tick'i kalkanı da aşındırır. foreach bittiği için dict
            // artık güvenle mutasyona açık.
            if (burnTickThisFrame > 0f && _active.TryGetValue(StatusKind.Shield, out StatusEntry shieldAfter))
            {
                shieldAfter.Magnitude = Math.Max(0f, shieldAfter.Magnitude - burnTickThisFrame * tuning.ShieldBurnDrainRatio);
                if (shieldAfter.Magnitude <= 0.01f)
                    _active.Remove(StatusKind.Shield);
                else
                    _active[StatusKind.Shield] = shieldAfter;
            }

            return tickPayload;
        }

        public void Apply(StatusKind kind, double durationMs, float magnitude)
        {
            if (kind == StatusKind.None || durationMs <= 0)
                return;

            ApplyReactions(kind, ref durationMs, ref magnitude);

            if (_active.TryGetValue(kind, out StatusEntry existing))
            {
                // Yenile: daha uzun süre / daha güçlü magnitude kazanır.
                existing.RemainingMs = Math.Max(existing.RemainingMs, durationMs);
                existing.Magnitude = Math.Max(existing.Magnitude, magnitude);
                _active[kind] = existing;
                return;
            }

            _active[kind] = new StatusEntry(durationMs, magnitude);
        }

        /// <summary>
        /// 16 Eylül — durum etkileşim tablosu (docs/element-sistemi.json
        /// status_interaction_table). `kind` uygulanırken tahtada zaten bulunan başka bir
        /// status'la eşleşen bir kural varsa: gelen değerler (ref parametreler) ve/veya o
        /// mevcut status'un entry'si buna göre değişir.
        /// </summary>
        void ApplyReactions(StatusKind kind, ref double incomingDurationMs, ref float incomingMagnitude)
        {
            if (_active.Count == 0)
                return;

            var others = new List<StatusKind>(_active.Keys);
            for (int i = 0; i < others.Count; i++)
            {
                StatusKind other = others[i];
                if (other == kind)
                    continue;
                if (!StatusReactionTable.TryGetRule(kind, other, out StatusReactionRule rule, out bool incomingIsA))
                    continue;

                bool affectsIncoming = rule.Target == ReactionTarget.Both
                    || (rule.Target == ReactionTarget.A && incomingIsA)
                    || (rule.Target == ReactionTarget.B && !incomingIsA);
                bool affectsOther = rule.Target == ReactionTarget.Both
                    || (rule.Target == ReactionTarget.A && !incomingIsA)
                    || (rule.Target == ReactionTarget.B && incomingIsA);

                if (affectsIncoming)
                {
                    incomingMagnitude = rule.MagnitudeSet ?? incomingMagnitude * rule.MagnitudeMult;
                    incomingDurationMs = incomingDurationMs * rule.DurationMult + rule.DurationAddMs;
                }

                if (affectsOther && _active.TryGetValue(other, out StatusEntry otherEntry))
                {
                    otherEntry.Magnitude = rule.MagnitudeSet ?? otherEntry.Magnitude * rule.MagnitudeMult;
                    otherEntry.RemainingMs = otherEntry.RemainingMs * rule.DurationMult + rule.DurationAddMs;
                    _active[other] = otherEntry;
                }
            }
        }

        /// <summary>Kalkan hasar emer; stasis tüm hasarı yutar. Kalan hasarı döner.</summary>
        public float AbsorbDamage(float amount)
        {
            if (amount <= 0f)
                return amount;
            if (IsInvulnerable)
                return 0f;
            if (!Has(StatusKind.Shield))
                return amount;

            StatusEntry s = _active[StatusKind.Shield];
            float absorbed = Math.Min(s.Magnitude, amount);
            s.Magnitude -= absorbed;
            if (s.Magnitude <= 0.01f)
                _active.Remove(StatusKind.Shield);
            else
                _active[StatusKind.Shield] = s;
            return amount - absorbed;
        }

        public void CleanseHostile()
        {
            var remove = new List<StatusKind>();
            foreach (StatusKind k in _active.Keys)
            {
                if (StatusKindUtil.IsHardCc(k) || StatusKindUtil.IsSoftCc(k) || StatusKindUtil.IsDebuff(k))
                    remove.Add(k);
            }
            for (int i = 0; i < remove.Count; i++)
                _active.Remove(remove[i]);
        }

        public void Clear() => _active.Clear();

        struct StatusEntry
        {
            public StatusEntry(double remainingMs, float magnitude)
            {
                RemainingMs = remainingMs;
                Magnitude = magnitude;
            }

            public double RemainingMs;
            public float Magnitude;
        }
    }
}
