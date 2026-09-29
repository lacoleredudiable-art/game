using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Tuning;

namespace Dovus.Core.Status
{
    /// <summary>
    /// Tek aktör üzerindeki durumlar. Unity bilmez — süre dünya ms ile akar.
    /// </summary>
    public sealed class StatusBoard
    {
        readonly Dictionary<StatusKind, StatusEntry> _active = new();
        readonly Dictionary<string, double> _rootSources = new(StringComparer.Ordinal);
        MobilityCcData? _mobilityCc;
        double _rootImmunityMs = SkillNumberFallbacks.RootImmunityMs;
        double _rootImmunityRemainingMs;
        double _attackLockImmunityRemainingMs;
        bool _attackLockImmunity;

        public void ConfigureMobilityCc(MobilityCcData data)
        {
            _mobilityCc = data;
            if (data != null && data.RootImmunityMs >= 0)
                _rootImmunityMs = data.RootImmunityMs;
        }

        public double RootImmunityRemainingMs => _rootImmunityRemainingMs;
        public bool IsRootImmune => _rootImmunityRemainingMs > 0;

        /// <summary>
        /// Boss tahtasında açık. Sersemlik bitince kısa bağışıklık başlar;
        /// aynı CC süreyi üst üste ekleyip dövüşü kilitleyemez.
        /// </summary>
        public void EnableAttackLockImmunity() => _attackLockImmunity = true;

        public bool IsAttackLockImmune => _attackLockImmunityRemainingMs > 0;

        /// <summary>Yavaşlatma ve hız çarpanı. Kök/sersemlik bunu sıfırlamaz; hareket ayrı kalır.</summary>
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

        /// <summary>
        /// 16 Eylül: "skilleri attığımda bir etkileşim göremiyorum" raporu — mekanik zaten
        /// çalışıyordu (sayılar değişiyordu), ama hiçbir görsel/ses sinyali yoktu. Bu event
        /// bir reaksiyon tetiklendiğinde (isim+açıklama ile) ateşlenir; Game katmanı
        /// (ManifestationDirector → ReactionReadout) bunu ekrana yazar.
        /// </summary>
        public event Action<StatusReactionRule>? ReactionTriggered;

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

        /// <summary>Gizlilik — boss hedef almaz, hasar yutulur.</summary>
        public bool IsStealthed => Has(StatusKind.Stealth);

        public int ActiveCount => _active.Count;

        public bool Has(StatusKind kind) =>
            kind != StatusKind.None && _active.ContainsKey(kind);

        /// <summary>CC priority_table'da başka bir aktif CC tarafından gizlenmiyorsa true.</summary>
        public bool HasEffective(StatusKind kind) =>
            Has(kind) && (_mobilityCc == null || _mobilityCc.IsCcVisible(kind, _active.Keys));

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
            if (worldDtMs <= 0)
                return 0f;

            if (_rootImmunityRemainingMs > 0)
                _rootImmunityRemainingMs = Math.Max(0, _rootImmunityRemainingMs - worldDtMs);
            if (_attackLockImmunityRemainingMs > 0)
                _attackLockImmunityRemainingMs = Math.Max(0, _attackLockImmunityRemainingMs - worldDtMs);
            bool lockBefore = AttackLockPresent();
            DecayRoots(worldDtMs);

            if (_active.Count == 0)
                return 0f;

            float tickPayload = 0f;
            float dtSec = (float)(worldDtMs / 1000.0);
            float burnTickThisFrame = 0f;

            var expired = new List<StatusKind>();
            var refreshed = new List<KeyValuePair<StatusKind, StatusEntry>>();
            foreach (var kv in _active)
            {
                if (kv.Key == StatusKind.Root)
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

            if (IsAttackLockKind(kind) && _attackLockImmunity && _attackLockImmunityRemainingMs > 0)
                return;

            ApplyReactions(kind, ref durationMs, ref magnitude);

            if (_active.TryGetValue(kind, out StatusEntry existing))
            {
                // mobility_cc.same_cc=süre_uzar: kalan süreye yeni süre eklenir.
                // Boss kilit bağışıklığında sersemlik uzamaz; süre yenilenir, sonra pencere açılır.
                bool extend = _mobilityCc != null && _mobilityCc.ExtendSameCc
                    && (StatusKindUtil.IsHardCc(kind) || StatusKindUtil.IsSoftCc(kind))
                    && !(_attackLockImmunity && IsAttackLockKind(kind));
                existing.RemainingMs = extend
                        ? existing.RemainingMs + durationMs
                        : Math.Max(existing.RemainingMs, durationMs);
                existing.Magnitude = Math.Max(existing.Magnitude, magnitude);
                existing.TotalDurationMs = Math.Max(existing.TotalDurationMs, existing.RemainingMs);
                _active[kind] = existing;
                return;
            }

            _active[kind] = new StatusEntry(durationMs, magnitude, durationMs);
        }

        /// <summary>HUD: kalan süre + magnitude + halka oranı için toplam süre.</summary>
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

                ReactionTriggered?.Invoke(rule);
            }
        }

        /// <summary>Kalkan hasar emer; stasis/stealth tüm hasarı yutar. Kalan hasarı döner.</summary>
        public float AbsorbDamage(float amount)
        {
            if (amount <= 0f)
                return amount;
            if (IsInvulnerable || IsStealthed)
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
            bool hadLock = AttackLockPresent();
            bool hadRoot = Has(StatusKind.Root) || _rootSources.Count > 0;
            var remove = new List<StatusKind>();
            foreach (StatusKind k in _active.Keys)
            {
                if (StatusKindUtil.IsHardCc(k) || StatusKindUtil.IsSoftCc(k) || StatusKindUtil.IsDebuff(k))
                    remove.Add(k);
            }
            for (int i = 0; i < remove.Count; i++)
                _active.Remove(remove[i]);
            if (hadRoot)
                EndRoot();
            FinishAttackLockIfEnded(hadLock);
        }

        /// <summary>
        /// Belirtilen türleri siler (reality_layer partial_erase / full_erase).
        /// CleanseHostile'a dokunmaz — yalnızca listedekileri kaldırır.
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

            ApplyReactions(StatusKind.Root, ref durationMs, ref magnitude);
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
