using System;
using System.Collections.Generic;
using Dovus.Core.Layers;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// manipulation_layers.space_layer — InvisibleLink + Tear yaşam döngüsü (saf C#).
    /// Blink/stealth SkillMotionMotor'da kalır.
    /// </summary>
    public sealed class SpaceDirector : ISpaceDirector
    {
        readonly List<SpaceEffect> _effects = new();
        readonly Dictionary<int, float> _linkTickAccum = new();
        readonly Dictionary<int, HashSet<string>> _tearCrossed = new();
        readonly SpaceLayerTuning _tuning;
        int _nextId = 1;

        public SpaceDirector(int maxActiveLinks, SpaceLayerTuning? tuning = null)
        {
            MaxActiveLinks = Math.Max(1, maxActiveLinks);
            _tuning = tuning ?? new SpaceLayerTuning();
        }

        public IReadOnlyList<SpaceEffect> ActiveEffects => _effects;
        public int MaxActiveLinks { get; }
        public SpaceLayerTuning Tuning => _tuning;

        public bool TrySpawnLink(
            string effectId, float durationSec,
            float ownerX, float ownerY, float ownerZ,
            float targetX, float targetY, float targetZ,
            string ownerId, string targetId,
            float drainPerTick, float healPerTick, float maxRangeM,
            out SpaceEffect spawned)
        {
            spawned = default;
            if (durationSec <= 0f)
                return false;

            // Soft-cap: link + tear birlikte sayılır (JSON max_active_links).
            while (_effects.Count >= MaxActiveLinks)
                DropOldest();

            int id = _nextId++;
            spawned = new SpaceEffect
            {
                RuntimeId = id,
                Id = effectId ?? string.Empty,
                Kind = SpaceEffectKind.InvisibleLink,
                X = ownerX,
                Y = ownerY,
                Z = ownerZ,
                DurationSec = durationSec,
                DamageOnCross = 0f,
                DrainPerTick = drainPerTick > 0f ? drainPerTick : _tuning.LinkDrainPerTick,
                HealPerTick = healPerTick > 0f ? healPerTick : _tuning.LinkHealPerTick,
                MaxRangeM = maxRangeM > 0f ? maxRangeM : _tuning.LinkMaxRangeM,
                RemainingSec = durationSec,
                OwnerId = ownerId ?? string.Empty,
                TargetId = targetId ?? string.Empty
            };
            // Uçlar Tick'te güncellenir; spawn anında hedef konumu Y'de saklanmaz —
            // X/Z owner, ayrı uç Game sync'te.
            _effects.Add(spawned);
            _linkTickAccum[id] = 0f;
            // Hedef uç için geçici: struct'ta tek konum var; Tick owner+target parametreleriyle çalışır.
            return true;
        }

        public bool TrySpawnTear(
            string effectId, float durationSec,
            float x, float y, float z,
            float damageOnCross,
            out SpaceEffect spawned)
        {
            spawned = default;
            if (durationSec <= 0f)
                return false;

            while (_effects.Count >= MaxActiveLinks)
                DropOldest();

            int id = _nextId++;
            float dmg = damageOnCross > 0f ? damageOnCross : 30f;
            spawned = new SpaceEffect
            {
                RuntimeId = id,
                Id = effectId ?? string.Empty,
                Kind = SpaceEffectKind.Tear,
                X = x,
                Y = y,
                Z = z,
                DurationSec = durationSec,
                DamageOnCross = dmg,
                DrainPerTick = 0f,
                HealPerTick = 0f,
                MaxRangeM = 0f,
                RemainingSec = durationSec,
                OwnerId = string.Empty,
                TargetId = string.Empty
            };
            _effects.Add(spawned);
            _tearCrossed[id] = new HashSet<string>(StringComparer.Ordinal);
            return true;
        }

        public void Tick(
            float dtSec,
            float ownerX, float ownerY, float ownerZ,
            float targetX, float targetY, float targetZ,
            List<SpaceLinkTick> linkTicksOut)
        {
            if (dtSec <= 0f)
                return;

            float tickIv = _tuning.LinkTickIntervalSec > 0f ? _tuning.LinkTickIntervalSec : 0.5f;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                SpaceEffect e = _effects[i];
                if (e.Kind == SpaceEffectKind.InvisibleLink)
                {
                    float dx = targetX - ownerX;
                    float dy = targetY - ownerY;
                    float dz = targetZ - ownerZ;
                    float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (dist > e.MaxRangeM)
                    {
                        RemoveAt(i);
                        continue;
                    }

                    e.X = ownerX;
                    e.Y = ownerY;
                    e.Z = ownerZ;

                    if (!_linkTickAccum.TryGetValue(e.RuntimeId, out float accum))
                        accum = 0f;
                    accum += dtSec;
                    while (accum >= tickIv)
                    {
                        accum -= tickIv;
                        linkTicksOut?.Add(new SpaceLinkTick(
                            e.RuntimeId, e.OwnerId, e.TargetId, e.DrainPerTick, e.HealPerTick));
                    }
                    _linkTickAccum[e.RuntimeId] = accum;
                }

                e.RemainingSec -= dtSec;
                if (e.RemainingSec <= 0f)
                {
                    RemoveAt(i);
                    continue;
                }

                _effects[i] = e;
            }
        }

        public bool Remove(int runtimeId)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].RuntimeId != runtimeId)
                    continue;
                RemoveAt(i);
                return true;
            }
            return false;
        }

        public int BreakLinksOwnedBy(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
                return 0;
            int n = 0;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                SpaceEffect e = _effects[i];
                if (e.Kind != SpaceEffectKind.InvisibleLink)
                    continue;
                if (!string.Equals(e.OwnerId, ownerId, StringComparison.Ordinal))
                    continue;
                RemoveAt(i);
                n++;
            }
            return n;
        }

        public float TryCrossTear(int runtimeId, string actorId)
        {
            if (string.IsNullOrEmpty(actorId))
                return 0f;

            for (int i = 0; i < _effects.Count; i++)
            {
                SpaceEffect e = _effects[i];
                if (e.RuntimeId != runtimeId || e.Kind != SpaceEffectKind.Tear)
                    continue;

                if (!_tearCrossed.TryGetValue(runtimeId, out HashSet<string>? set) || set == null)
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    _tearCrossed[runtimeId] = set;
                }

                if (!set.Add(actorId))
                    return 0f;
                return e.DamageOnCross;
            }

            return 0f;
        }

        void DropOldest()
        {
            if (_effects.Count == 0)
                return;
            RemoveAt(0);
        }

        void RemoveAt(int index)
        {
            int id = _effects[index].RuntimeId;
            _effects.RemoveAt(index);
            _linkTickAccum.Remove(id);
            _tearCrossed.Remove(id);
        }
    }
}
