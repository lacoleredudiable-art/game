using Dovus.Core.Actors;
using Dovus.Game.Actors;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4WorldHost
    {
        public static float ApplyDamage(ManifestationDirector director, Transform victim, float amount)
        {
            if (amount <= 0 || victim == null || director == null)
                return 0;
            TargetableHost mark = victim.GetComponentInParent<TargetableHost>();
            if (mark == null)
                return 0;
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

            var minion = victim.GetComponentInParent<SliceLightMinionHost>();
            if (minion != null && !minion.IsDown)
            {
                minion.ApplyDamage(amount);
                director.MechanicsDamageHud?.ShowDamage(
                    amount, false, victim.position, null, victimIsBoss: false);
                return amount;
            }

            return 0;
        }

        public static void ApplyPoise(ManifestationDirector director, Transform victim, float amount)
        {
            if (amount <= 0 || victim == null || director == null)
                return;
            TargetableHost mark = victim.GetComponentInParent<TargetableHost>();
            if (mark != null && mark.ActorId == ActorDefaults.BossId)
                director.MechanicsBossDirector?.ApplyPoiseDamage(amount);
        }

        public static void ApplyHeal(ManifestationDirector director, Transform victim, float amount)
        {
            if (amount <= 0 || director == null)
                return;
            int heal = Mathf.RoundToInt(amount);
            if (victim == director.MechanicsPlayer)
            {
                director.CachedPlayerVitals()?.ApplyHeal(heal);
                return;
            }

            TargetableHost mark = victim != null ? victim.GetComponentInParent<TargetableHost>() : null;
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
