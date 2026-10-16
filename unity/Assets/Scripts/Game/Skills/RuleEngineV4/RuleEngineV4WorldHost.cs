using Dovus.Core.Actors;
using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4WorldHost
    {
        public static float ApplyDamage(RuleEngineV4CombatContext combat, Transform victim, float amount)
        {
            if (amount <= 0 || victim == null || combat == null)
                return 0;
            TargetableHost mark = victim.GetComponentInParent<TargetableHost>();
            if (mark == null)
                return 0;
            if (mark.ActorId == ActorDefaults.BossId
                && combat.BossVitals != null
                && !combat.BossVitals.IsDown)
            {
                combat.BossVitals.ApplyDamage(amount);
                combat.DamageHud?.ShowDamage(
                    amount, false, combat.BossHitPoint?.Invoke(), null, victimIsBoss: true);
                RuleDrivenVfxSink.NotifyBossStrike(
                    combat.Player, victim, victim.position);
                combat.NotifyBossStruck?.Invoke(false, allowHitstop: true);
                return amount;
            }

            if (mark.ActorId == ActorDefaults.AllyDummyId && combat.Ally != null)
            {
                combat.Ally.ApplyBossDamage(amount);
                return amount;
            }

            if (mark.ActorId == ActorDefaults.AllyDummy2Id
                && TryAllyDummy(mark, out AllyDummyController ally2))
            {
                ally2.ApplyBossDamage(amount);
                return amount;
            }

            var minion = victim.GetComponentInParent<SliceLightMinionHost>();
            if (minion != null && !minion.IsDown)
            {
                minion.ApplyDamage(amount);
                combat.DamageHud?.ShowDamage(
                    amount, false, victim.position, null, victimIsBoss: false);
                return amount;
            }

            var cocoon = victim.GetComponentInParent<SliceArenaCocoonHost>();
            if (cocoon != null && !cocoon.IsBroken)
            {
                cocoon.ApplyDamage(amount);
                combat.DamageHud?.ShowDamage(
                    amount, false, victim.position, null, victimIsBoss: false);
                return amount;
            }

            return 0;
        }

        public static void ApplyPoise(RuleEngineV4CombatContext combat, Transform victim, float amount)
        {
            if (amount <= 0 || victim == null || combat == null)
                return;
            TargetableHost mark = victim.GetComponentInParent<TargetableHost>();
            if (mark != null && mark.ActorId == ActorDefaults.BossId)
                combat.BossDirector?.ApplyPoiseDamage(amount);
        }

        public static void ApplyHeal(RuleEngineV4CombatContext combat, Transform victim, float amount)
        {
            if (amount <= 0 || combat == null)
                return;
            int heal = Mathf.RoundToInt(amount);
            if (victim == combat.Player)
            {
                combat.PlayerVitals?.Invoke()?.ApplyHeal(heal);
                return;
            }

            TargetableHost mark = victim != null ? victim.GetComponentInParent<TargetableHost>() : null;
            if (mark == null)
                return;
            if (mark.ActorId == ActorDefaults.AllyDummyId && combat.Ally != null)
                combat.Ally.ApplyHeal(heal);
            else if (mark.ActorId == ActorDefaults.AllyDummy2Id
                     && TryAllyDummy(mark, out AllyDummyController ally2Heal))
                ally2Heal.ApplyHeal(heal);
        }

        static bool TryAllyDummy(TargetableHost mark, out AllyDummyController ally)
        {
            ally = mark != null ? mark.GetComponent<AllyDummyController>() : null;
            return ally != null;
        }
    }
}
