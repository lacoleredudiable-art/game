using System;
using System.Collections.Generic;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    /// <summary>
    /// Tek akt├Âr ├╝zerindeki durumlar. Unity bilmez ÔÇö s├╝re d├╝nya ms ile akar.
    /// </summary>
    public sealed class StatusBoard
    {
        readonly Dictionary<StatusKind, StatusEntry> _active = new();
        readonly Dictionary<string, double> _rootSources = new(StringComparer.Ordinal);
        readonly Dictionary<string, TempoSource> _slowSources = new(StringComparer.Ordinal);
        readonly Dictionary<string, TempoSource> _hasteSources = new(StringComparer.Ordinal);
        MobilityCcData? _mobilityCc;
        double _rootImmunityMs = StatusDefaults.RootImmunityMs;
        double _rootImmunityRemainingMs;
        double _attackLockImmunityRemainingMs;
        bool _attackLockImmunity;

        public void ConfigureMobilityCc(MobilityCcData data)
        {
            _mobilityCc = data;
            if (data != null && data.RootImmunityMs >= 0)
                _rootImmunityMs = data.RootImmunityMs;
        }

        public bool IsRootImmune => _rootImmunityRemainingMs > 0;

        /// <summary>
        /// Boss tahtas─▒nda a├ğ─▒k. Sersemlik bitince k─▒sa ba─ş─▒┼ş─▒kl─▒k ba┼şlar;
        /// ayn─▒ CC s├╝reyi ├╝st ├╝ste ekleyip d├Âv├╝┼ş├╝ kilitleyemez.
        /// </summary>
        public void EnableAttackLockImmunity() => _attackLockImmunity = true;

        public bool IsAttackLockImmune => _attackLockImmunityRemainingMs > 0;

        /// <summary>Yava┼şlatma ve h─▒z ├ğarpan─▒. K├Âk/sersemlik bunu s─▒f─▒rlamaz; hareket ayr─▒ kal─▒r.</summary>
        public float ActionSpeedMult
        {
            get
            {
                float m = 1f;
                if (HasEffective(StatusKind.Slow) && _active.TryGetValue(StatusKind.Slow, out StatusEntry slow) && slow.Magnitude > 0f)
                    m *= slow.Magnitude;
                if (Has(StatusKind.Haste) && _active.TryGetValue(StatusKind.Haste, out StatusEntry haste) && haste.Magnitude > 0f)
                    m *= haste.Magnitude;
                return m;
            }
        }

        public bool BlocksMovement =>
            HasEffective(StatusKind.Stun) || HasEffective(StatusKind.Root) || Has(StatusKind.Stasis)
            || HasEffective(StatusKind.Fear);

        public bool BlocksCast =>
            HasEffective(StatusKind.Stun) || HasEffective(StatusKind.Silence) || Has(StatusKind.Stasis)
            || HasEffective(StatusKind.Fear);

        public bool BlocksBossAttack =>
            HasEffective(StatusKind.Stun) || Has(StatusKind.Stasis) || HasEffective(StatusKind.Fear);

        public bool HasBlind => HasEffective(StatusKind.Blind);
        public bool HasDisarm => HasEffective(StatusKind.Disarm);

        public float MoveSpeedMult
        {
            get
            {
                if (BlocksMovement) return 0f;
                float m = 1f;
                if (HasEffective(StatusKind.Slow) && _active.TryGetValue(StatusKind.Slow, out StatusEntry slow))
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
        /// 16 Eyl├╝l: GrievousWounds'un magnitude'u eskiden hep 1f'ti (kullan─▒lm─▒yordu).
        /// Art─▒k gelen heal'e ├ğarp─▒l─▒r ÔÇö "Kavurucu Yara" (grievous+burn) tepkisi bunu hedefler.
        /// </summary>
        public float HealEffectivenessMult =>
            Has(StatusKind.GrievousWounds) && _active.TryGetValue(StatusKind.GrievousWounds, out StatusEntry gw)
                ? gw.Magnitude
                : 1f;

        public float ShieldRemaining =>
            _active.TryGetValue(StatusKind.Shield, out StatusEntry s) ? s.Magnitude : 0f;

        /// <summary>Stasis = k─▒sa i-frame (dodge d─▒┼ş─▒ skill korumas─▒).</summary>
        public bool IsInvulnerable => Has(StatusKind.Stasis);

        /// <summary>Gizlilik ÔÇö boss ni┼şan alamaz. Hasar yutulmaz; yer/AoE de─şer.</summary>
        public bool IsStealthed => Has(StatusKind.Stealth);

        /// <summary>K├Âr b├╝y├╝kl├╝─ş├╝ ─▒skalama ┼şans─▒. 0.3 = %30. 1 = her vuru┼ş ─▒skalar.</summary>
        public float BlindMissChance
        {
            get
            {
                if (!HasBlind || !_active.TryGetValue(StatusKind.Blind, out StatusEntry blind))
                    return 0f;
                float magnitude = blind.Magnitude;
                if (magnitude <= 0f)
                    return 0f;
                if (magnitude >= 1f)
                    return 1f;
                return magnitude;
            }
        }

        public int ActiveCount => _active.Count;

        public bool Has(StatusKind kind) =>
            kind != StatusKind.None && _active.ContainsKey(kind);

        /// <summary>CC priority_table'da ba┼şka bir aktif CC taraf─▒ndan gizlenmiyorsa true.</summary>
        public bool HasEffective(StatusKind kind) =>
            Has(kind) && (_mobilityCc == null || _mobilityCc.IsCcVisible(kind, _active.Keys));

        public IReadOnlyCollection<StatusKind> ActiveKinds => _active.Keys;

        /// <summary>
        /// S├╝releri ilerlet; burn/poison/regen tick hasar─▒/heal d├Âner (pozitif = hasar,
        /// negatif = heal). 16 Eyl├╝l: burn/poison art─▒k `tuning`'in sabit de─şerini de─şil,
        /// entry'nin KEND─░ magnitude'unu okuyor ÔÇö durum etkile┼şim tablosu (Apply) bunu
        /// de─şi┼ştirebildi─şi i├ğin (├Ârn. "S├╝r├╝nen Alev": slow+burn ÔåÆ burn ├ù1.3) art─▒k ger├ğek
        /// bir etkisi var; eskiden magnitude saklan─▒p hi├ğ okunmuyordu.
        /// </summary>
        public float Tick(double worldDtMs, StatusTuning tuning)
        {
            if (worldDtMs <= 0)
                return 0f;

            if (_rootImmunityRemainingMs > 0)
                _rootImmunityRemainingMs = Math.Max(0, _rootImmunityRemainingMs - worldDtMs);
            if (_attackLockImmunityRemainingMs > 0)
                _attackLockImmunityRemainingMs = Math.Max(0, _attackLockImmunityRemainingMs - worldDtMs);
            bool lockBefore = AttackLockPresent();
            DecayRoots(worldDtMs);
            DecayTempo(StatusKind.Slow, _slowSources, worldDtMs);
            DecayTempo(StatusKind.Haste, _hasteSources, worldDtMs);

            if (_active.Count == 0)
                return 0f;

            float tickPayload = 0f;
            float dtSec = (float)(worldDtMs / 1000.0);
            float burnTickThisFrame = 0f;

            var expired = new List<StatusKind>();
            var refreshed = new List<KeyValuePair<StatusKind, StatusEntry>>();
            foreach (var kv in _active)
            {
                if (kv.Key == StatusKind.Root || kv.Key == StatusKind.Slow || kv.Key == StatusKind.Haste)
                    continue;
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

            // "Zehirli Ate┼ş": burn + poison ayn─▒ anda ÔåÆ ekstra tick (docs/element-sistemi.json).
            if (burnTickThisFrame > 0f && _active.ContainsKey(StatusKind.Poison))
                tickPayload += tuning.BurnPoisonComboBonusPerSec * dtSec;

            for (int i = 0; i < refreshed.Count; i++)
                _active[refreshed[i].Key] = refreshed[i].Value;

            for (int i = 0; i < expired.Count; i++)
                _active.Remove(expired[i]);

            // "Yanan Kalkan": burn tick'i kalkan─▒ da a┼ş─▒nd─▒r─▒r. foreach bitti─şi i├ğin dict
            // art─▒k g├╝venle mutasyona a├ğ─▒k.
            if (burnTickThisFrame > 0f && _active.TryGetValue(StatusKind.Shield, out StatusEntry shieldAfter))
            {
                shieldAfter.Magnitude = Math.Max(0f, shieldAfter.Magnitude - burnTickThisFrame * tuning.ShieldBurnDrainRatio);
                if (shieldAfter.Magnitude <= 0.01f)
                    _active.Remove(StatusKind.Shield);
                else
                    _active[StatusKind.Shield] = shieldAfter;
            }

            FinishAttackLockIfEnded(lockBefore);
            return tickPayload;
        }

        public void Apply(StatusKind kind, double durationMs, float magnitude, string? sourceId = null)
        {
            if (kind == StatusKind.None || durationMs <= 0)
                return;

            if (kind == StatusKind.Root)
            {
                ApplyRoot(sourceId, durationMs, magnitude);
                return;
            }

            if (kind == StatusKind.Slow)
            {
                ApplyTempo(_slowSources, StatusKind.Slow, sourceId, "slow", durationMs, magnitude);
                return;
            }

            if (kind == StatusKind.Haste)
            {
                ApplyTempo(_hasteSources, StatusKind.Haste, sourceId, "haste", durationMs, magnitude);
                return;
            }

            if (IsAttackLockKind(kind) && _attackLockImmunity && _attackLockImmunityRemainingMs > 0)
                return;

            if (_active.TryGetValue(kind, out StatusEntry existing))
            {
                // Ayn─▒ etki yeniden gelince s├╝re yenilenir: max(kalan, yeni). Eklenmez.
                // same_cc "s├╝re_uzar" ve status_no_stacking ikinci kopyay─▒ yasakl─▒yor;
                // tempo ba─ş─▒ her tikte kart s├╝resini ├╝st ├╝ste bindirmesin.
                // Pasif yuva ayr─▒d─▒r (same_passive) ve burada de─şi┼şmez.
                existing.RemainingMs = Math.Max(existing.RemainingMs, durationMs);
                existing.Magnitude = Math.Max(existing.Magnitude, magnitude);
                existing.TotalDurationMs = Math.Max(existing.TotalDurationMs, existing.RemainingMs);
                _active[kind] = existing;
                return;
            }

            _active[kind] = new StatusEntry(durationMs, magnitude, durationMs);
        }

        /// <summary>HUD: kalan s├╝re + magnitude + halka oran─▒ i├ğin toplam s├╝re.</summary>
        public bool TryGet(
            StatusKind kind,
            out double remainingMs,
            out float magnitude,
            out double totalDurationMs)
        {
            if (kind != StatusKind.None && _active.TryGetValue(kind, out StatusEntry e))
            {
                remainingMs = e.RemainingMs;
                magnitude = e.Magnitude;
                totalDurationMs = e.TotalDurationMs > 0 ? e.TotalDurationMs : e.RemainingMs;
                return true;
            }

            remainingMs = 0;
            magnitude = 0f;
            totalDurationMs = 0;
            return false;
        }

        /// <summary>Pipeline kalkan pay─▒n─▒ hesaplad─▒; havuzdan d├╝┼ş├╝l├╝r.</summary>
        public void ConsumeShield(float absorbed)
        {
            if (absorbed <= 0f || !Has(StatusKind.Shield))
                return;
            StatusEntry s = _active[StatusKind.Shield];
            s.Magnitude -= absorbed;
            if (s.Magnitude <= 0.01f)
                _active.Remove(StatusKind.Shield);
            else
                _active[StatusKind.Shield] = s;
        }

        public void CleanseHostile()
        {
            bool hadLock = AttackLockPresent();
            bool hadRoot = Has(StatusKind.Root) || _rootSources.Count > 0;
            var remove = new List<StatusKind>();
            foreach (StatusKind k in _active.Keys)
            {
                if (StatusKindUtil.IsHardCc(k) || StatusKindUtil.IsSoftCc(k) || StatusKindUtil.IsDebuff(k))
                    remove.Add(k);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                if (remove[i] == StatusKind.Slow)
                    _slowSources.Clear();
                _active.Remove(remove[i]);
            }
            if (hadRoot)
                EndRoot();
            FinishAttackLockIfEnded(hadLock);
        }

        /// <summary>
        /// cleanse_count: en fazla maxCount k├Ât├╝ durum siler ÔÇö ├Ânce sert CC, sonra yumu┼şak CC,
        /// sonra debuff; ayn─▒ grupta en uzun kalan ├Ânce. int.MaxValue = hepsi. D├Ân├╝┼ş: silinen say─▒.
        /// </summary>
        public int CleanseHostile(int maxCount)
        {
            if (maxCount <= 0)
                return 0;
            var candidates = new List<(int Group, double Remaining, StatusKind Kind)>();
            foreach (KeyValuePair<StatusKind, StatusEntry> pair in _active)
            {
                StatusKind k = pair.Key;
                int group = StatusKindUtil.IsHardCc(k) ? 0 : StatusKindUtil.IsSoftCc(k) ? 1 : StatusKindUtil.IsDebuff(k) ? 2 : -1;
                if (group >= 0)
                    candidates.Add((group, pair.Value.RemainingMs, k));
            }
            if (candidates.Count == 0)
                return 0;
            if (maxCount >= candidates.Count)
            {
                CleanseHostile();
                return candidates.Count;
            }
            candidates.Sort((a, b) => a.Group != b.Group ? a.Group.CompareTo(b.Group) : b.Remaining.CompareTo(a.Remaining));
            var remove = new List<StatusKind>();
            for (int i = 0; i < maxCount; i++)
                remove.Add(candidates[i].Kind);
            RemoveKinds(remove);
            return remove.Count;
        }

        /// <summary>
        /// Belirtilen t├╝rleri siler (reality_layer partial_erase / full_erase).
        /// CleanseHostile'a dokunmaz ÔÇö yaln─▒zca listedekileri kald─▒r─▒r.
        /// </summary>
        public void RemoveKinds(IReadOnlyList<StatusKind> kinds)
        {
            if (kinds == null || kinds.Count == 0)
                return;
            bool hadLock = AttackLockPresent();
            bool dropRoot = false;
            for (int i = 0; i < kinds.Count; i++)
            {
                StatusKind k = kinds[i];
                if (k == StatusKind.None)
                    continue;
                if (k == StatusKind.Root && (Has(StatusKind.Root) || _rootSources.Count > 0))
                    dropRoot = true;
                if (k == StatusKind.Slow)
                    _slowSources.Clear();
                if (k == StatusKind.Haste)
                    _hasteSources.Clear();
                _active.Remove(k);
            }
            if (dropRoot)
                EndRoot();
            FinishAttackLockIfEnded(hadLock);
        }

        public void Clear()
        {
            _active.Clear();
            _rootSources.Clear();
            _slowSources.Clear();
            _hasteSources.Clear();
            _rootImmunityRemainingMs = 0;
            _attackLockImmunityRemainingMs = 0;
        }

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
        /// Yava┼şlatma ve h─▒z: ayn─▒ kaynak s├╝reyi yeniler (max), farkl─▒ kaynaklar toplanmaz.
        /// G├╝├ğte en g├╝├ğl├╝ olan kal─▒r (yava┼şta k├╝├ğ├╝k ├ğarpan, h─▒zda b├╝y├╝k ├ğarpan). Ba─ş─▒┼ş─▒kl─▒k yok.
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
            /// <summary>Apply an─▒ndaki s├╝re ÔÇö HUD radial fill i├ğin.</summary>
            public double TotalDurationMs;
        }
    }
}