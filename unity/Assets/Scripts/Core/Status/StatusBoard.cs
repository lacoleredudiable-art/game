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
    /// Tek aktör üzerindeki durumlar. Unity bilmez — süre dünya ms ile akar.
    /// </summary>
    public sealed partial class StatusBoard
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

        /// <summary>Gizlilik — boss nişan alamaz. Hasar yutulmaz; yer/AoE değer.</summary>
        public bool IsStealthed => Has(StatusKind.Stealth);

        /// <summary>Kör büyüklüğü ıskalama şansı. 0.3 = %30. 1 = her vuruş ıskalar.</summary>
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
            DecayTempo(StatusKind.Slow, _slowSources, worldDtMs);
            DecayTempo(StatusKind.Haste, _hasteSources, worldDtMs);

            if (_active.Count == 0)
                return 0f;

            float tickPayload = 0f;
            float dtSec = (float)(worldDtMs / Dovus.Core.Shared.Units.SecToMs);
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
                if (shieldAfter.Magnitude <= StatusDefaults.MinRadiusM)
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
                // Aynı etki yeniden gelince süre yenilenir: max(kalan, yeni). Eklenmez.
                // same_cc "süre_uzar" ve status_no_stacking ikinci kopyayı yasaklıyor;
                // tempo bağı her tikte kart süresini üst üste bindirmesin.
                // Pasif yuva ayrıdır (same_passive) ve burada değişmez.
                existing.RemainingMs = Math.Max(existing.RemainingMs, durationMs);
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

        /// <summary>Pipeline kalkan payını hesapladı; havuzdan düşülür.</summary>
        public void ConsumeShield(float absorbed)
        {
            if (absorbed <= 0f || !Has(StatusKind.Shield))
                return;
            StatusEntry s = _active[StatusKind.Shield];
            s.Magnitude -= absorbed;
            if (s.Magnitude <= StatusDefaults.MinRadiusM)
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
        /// cleanse_count: en fazla maxCount kötü durum siler — önce sert CC, sonra yumuşak CC,
        /// sonra debuff; aynı grupta en uzun kalan önce. int.MaxValue = hepsi. Dönüş: silinen sayı.
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

    }
}
