using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Pasif yuva sayılarının saf hesabı. ManifestationDirector bunları uygular.
    /// </summary>
    public static class SlotPassiveCombat
    {
        /// <summary>O8 (kullanıcı kararı): ignore_armor = zırhı %100 deler ("Zırh yoksayar").</summary>
        public const float IgnoreArmorPierce = 1f;

        /// <summary>Pasif kopya bir kez, bu güçle. Skill sıfatının duplicate_damage_mult'u ayrıdır.</summary>
        public const float EchoPower = 0.5f;
        public const float EchoDelaySec = 0.3f;

        public static float ScaleOutgoingPoise(float basePoise, float skillPoiseMult, float slotPoiseMult)
        {
            float skill = skillPoiseMult > 0f ? skillPoiseMult : 1f;
            float slot = slotPoiseMult > 0f ? slotPoiseMult : 1f;
            return basePoise * skill * slot;
        }

        public static float CombineArmorPen(float existingPen, bool skillIgnoresArmor, float slotPen)
        {
            float pen = existingPen > 0f ? existingPen : 0f;
            if (skillIgnoresArmor && pen < IgnoreArmorPierce)
                pen = IgnoreArmorPierce;
            if (slotPen > pen)
                pen = slotPen;
            if (pen > 1f)
                pen = 1f;
            return pen;
        }
    }

    /// <summary>
    /// passive_slot_system silah uyumu şartı yazmıyorsa rün pasifi silahtan bağımsız açılır.
    /// </summary>
    public static class PassiveSlotPolicy
    {
        public static bool RequiresWeaponCompatibility(JsonValue root)
        {
            if (root.IsNull)
                return false;
            return MentionsWeaponGate(root["passive_slot_system"]);
        }

        public static bool ShouldArm(bool runeIsSlottedPassive, bool weaponPassiveEnabled, bool policyRequiresWeapon)
        {
            if (!runeIsSlottedPassive)
                return false;
            if (policyRequiresWeapon && !weaponPassiveEnabled)
                return false;
            return true;
        }

        static bool MentionsWeaponGate(JsonValue node)
        {
            if (node.IsNull)
                return false;
            if (node.Kind == JsonKind.String)
                return LooksLikeWeaponGate(node.AsString());
            if (node.Kind == JsonKind.Array)
            {
                IReadOnlyList<JsonValue> items = node.AsArray();
                for (int i = 0; i < items.Count; i++)
                {
                    if (MentionsWeaponGate(items[i]))
                        return true;
                }
                return false;
            }
            if (node.Kind != JsonKind.Object)
                return false;
            foreach (KeyValuePair<string, JsonValue> pair in node.AsObject())
            {
                if (LooksLikeWeaponGate(pair.Key) || MentionsWeaponGate(pair.Value))
                    return true;
            }
            return false;
        }

        static bool LooksLikeWeaponGate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (text.IndexOf("compatible_verbs", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("weapon compat", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("silah uyum", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
    }

    public readonly struct PassiveBounceCandidate
    {
        public PassiveBounceCandidate(int id, float distanceM)
        {
            Id = id;
            DistanceM = distanceM;
        }

        public int Id { get; }
        public float DistanceM { get; }
    }

    public readonly struct PassiveBounceHit
    {
        public PassiveBounceHit(int targetId, float damage)
        {
            TargetId = targetId;
            Damage = damage;
        }

        public int TargetId { get; }
        public float Damage { get; }
    }

    /// <summary>
    /// Sıçrama. Menzil global_rules.ally_skill_range_m ile aynı 6 m.
    /// Başka düşman yoksa ek vuruşlar kaynağa (boss) iner, adet bounce_targets.
    /// </summary>
    public static class PassiveBounce
    {
        public const float RangeM = 6f;

        public static List<PassiveBounceHit> Plan(
            float sourceDamage,
            int bounceCount,
            float damageMult,
            int sourceTargetId,
            IReadOnlyList<PassiveBounceCandidate> candidates)
        {
            var hits = new List<PassiveBounceHit>();
            if (bounceCount <= 0 || sourceDamage <= 0f || damageMult <= 0f)
                return hits;

            float damage = sourceDamage * damageMult;
            var nearest = new List<PassiveBounceCandidate>();
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    PassiveBounceCandidate candidate = candidates[i];
                    if (candidate.Id == sourceTargetId)
                        continue;
                    if (candidate.DistanceM < 0f || candidate.DistanceM > RangeM)
                        continue;
                    nearest.Add(candidate);
                }
            }

            nearest.Sort((a, b) =>
            {
                int byDistance = a.DistanceM.CompareTo(b.DistanceM);
                return byDistance != 0 ? byDistance : a.Id.CompareTo(b.Id);
            });

            if (nearest.Count == 0)
            {
                for (int i = 0; i < bounceCount; i++)
                    hits.Add(new PassiveBounceHit(sourceTargetId, damage));
                return hits;
            }

            int count = bounceCount < nearest.Count ? bounceCount : nearest.Count;
            for (int i = 0; i < count; i++)
                hits.Add(new PassiveBounceHit(nearest[i].Id, damage));
            return hits;
        }
    }

    public readonly struct PassiveFlowPlan
    {
        public PassiveFlowPlan(float durationSec, float tickSec, float tickDamage, int tickCount)
        {
            DurationSec = durationSec;
            TickSec = tickSec;
            TickDamage = tickDamage;
            TickCount = tickCount;
        }

        public float DurationSec { get; }
        public float TickSec { get; }
        public float TickDamage { get; }
        public int TickCount { get; }
        public float TotalDamage => TickDamage * TickCount;
    }

    /// <summary>
    /// Akış: vuruş channel_sec boyunca tik bırakır. Oran mechanic_grammar.params.flow_tick_fraction.
    /// </summary>
    public static class PassiveFlowMath
    {
        public const float DefaultTickFraction = 0.33f;

        public static bool TryPlan(
            float channelSec,
            float tickRateMult,
            float dealtDamage,
            float baseTickSec,
            float tickFraction,
            out PassiveFlowPlan plan)
        {
            plan = default;
            if (channelSec <= 0f || dealtDamage <= 0f)
                return false;
            float rate = tickRateMult > 0.01f ? tickRateMult : 1f;
            float tickSec = (baseTickSec > 0.01f ? baseTickSec : 1f) / rate;
            if (tickSec < 0.01f)
                tickSec = 0.01f;
            int count = 0;
            double t = tickSec;
            while (t < channelSec - 0.0001d && count < 64)
            {
                count++;
                t += tickSec;
            }
            if (count <= 0)
                return false;
            float fraction = tickFraction > 0f ? tickFraction : DefaultTickFraction;
            float tickDamage = dealtDamage * fraction;
            if (tickDamage <= 0f)
                return false;
            plan = new PassiveFlowPlan(channelSec, tickSec, tickDamage, count);
            return true;
        }
    }

    public sealed class PassiveFlowRunner
    {
        readonly List<LiveFlow> _items = new();

        public int ActiveCount => _items.Count;

        public void Start(PassiveFlowPlan plan, double worldMs)
        {
            if (plan.TickCount <= 0 || plan.TickDamage <= 0f)
                return;
            _items.Add(new LiveFlow
            {
                NextMs = worldMs + plan.TickSec * 1000.0,
                IntervalMs = plan.TickSec * 1000.0,
                Left = plan.TickCount,
                TickDamage = plan.TickDamage
            });
        }

        public float Collect(double worldMs)
        {
            float sum = 0f;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                LiveFlow item = _items[i];
                int guard = 0;
                while (item.Left > 0 && worldMs + 0.001d >= item.NextMs && guard < 64)
                {
                    sum += item.TickDamage;
                    item.Left--;
                    item.NextMs += item.IntervalMs;
                    guard++;
                }
                if (item.Left <= 0)
                    _items.RemoveAt(i);
                else
                    _items[i] = item;
            }
            return sum;
        }

        struct LiveFlow
        {
            public double NextMs;
            public double IntervalMs;
            public int Left;
            public float TickDamage;
        }
    }
}
