using Dovus.Core.Actors;
using Dovus.Game.Actors;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4WorldHost
    {
        public static float ApplyDamage(ManifestationDirector director, Transform target, float amount)
        {
            if (amount <= 0f || target == null || director == null)
                return 0f;
            TargetableHost mark = target.GetComponentInParent<TargetableHost>();
            if (mark == null)
                return 0f;
            if (mark.ActorId == ActorDefaults.BossId
                && director.MechanicsBossVitals != null
                && !director.MechanicsBossVitals.IsDown)
            {
                director.MechanicsBossVitals.ApplyDamage(amount);
                director.MechanicsDamageHud?.ShowDamage(
                    amount, false, director.BossHitPoint(), null, victimIsBoss: true);
                director.NotifyBossStruck(false, allowHitstop: true);
                return amount;
            }

            if (mark.ActorId == ActorDefaults.AllyDummyId && director.MechanicsAlly != null)
            {
                director.MechanicsAlly.ApplyBossDamage(amount);
                return amount;
            }

            if (mark.ActorId == ActorDefaults.AllyDummy2Id
                && TryAllyDummy(director, mark, out AllyDummyController ally2))
            {
                ally2.ApplyBossDamage(amount);
                return amount;
            }

            var minion = target.GetComponentInParent<SliceLightMinionHost>();
            if (minion != null && !minion.IsDown)
            {
                minion.ApplyDamage(amount);
                director.MechanicsDamageHud?.ShowDamage(
                    amount, false, target.position, null, victimIsBoss: false);
                return amount;
            }

            return 0f;
        }

        public static void ApplyHeal(ManifestationDirector director, Transform target, float amount)
        {
            if (amount <= 0f || director == null)
                return;
            int heal = Mathf.RoundToInt(amount);
            if (target == director.MechanicsPlayer)
            {
                director.CachedPlayerVitals()?.ApplyHeal(heal);
                return;
            }

            TargetableHost mark = target != null ? target.GetComponentInParent<TargetableHost>() : null;
            if (mark == null)
                return;
            if (mark.ActorId == ActorDefaults.AllyDummyId && director.MechanicsAlly != null)
                director.MechanicsAlly.ApplyHeal(heal);
            else if (mark.ActorId == ActorDefaults.AllyDummy2Id
                     && TryAllyDummy(director, mark, out AllyDummyController ally2Heal))
                ally2Heal.ApplyHeal(heal);
        }

        static bool TryAllyDummy(
            ManifestationDirector director,
            TargetableHost mark,
            out AllyDummyController ally)
        {
            ally = mark != null ? mark.GetComponent<AllyDummyController>() : null;
            return ally != null;
        }
    }
}
