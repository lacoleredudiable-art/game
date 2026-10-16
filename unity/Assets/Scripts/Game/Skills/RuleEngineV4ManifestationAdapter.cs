using Dovus.Game.Composition.Builders;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>ManifestationDirector → v4 combat bağlamı (yalnız eski motor tarafında).</summary>
    public static class RuleEngineV4ManifestationAdapter
    {
        public static RuleEngineV4CombatContext Bind(ManifestationDirector md)
        {
            if (md == null)
                return null;
            var combat = new RuleEngineV4CombatContext
            {
                CoroutineHost = md,
                Session = md.RuleEngineV4Session ?? new RuleEngineV4WorldSession(),
                Player = md.MechanicsPlayer,
                Visual = md._visual,
                Clock = md.MechanicsClock,
                Boss = md.MechanicsBoss,
                BossVitals = md.MechanicsBossVitals,
                BossDirector = md.MechanicsBossDirector,
                Ally = md.MechanicsAlly,
                DamageHud = md.MechanicsDamageHud,
                PlayerVitals = md.CachedPlayerVitals,
                Input = md._input,
                Targeting = md._targeting,
                SkillAim = md._skillAim,
                ClosingQueue = md._closingQueue,
                LiveTargetables = md._liveTargetables,
                Colors = md._colors,
                EquippedWeapon = md.MechanicsEquippedWeapon,
                AnimationBridge = md._animationBridge,
                EnsureAnimationReady = md.EnsureLaunchServices,
                ResolveSkillWords = md.ResolveSkillWords,
                NotifyCastDealt = dealt => md._castPort?.NotifyRuleEngineV4Dealt(dealt),
                NotifyBossStruck = md.NotifyBossStruck,
                BossHitPoint = md.BossHitPoint,
            };
            md.RuleEngineV4Session = combat.Session;
            return combat;
        }

        public static RuleEngineV4CombatContext Ensure(WorldContext ctx)
        {
            if (ctx?.RuleEngineV4Combat != null)
                return ctx.RuleEngineV4Combat;
            if (ctx?.ManifestationDirector == null)
                return null;
            ctx.RuleEngineV4Combat = Bind(ctx.ManifestationDirector);
            return ctx.RuleEngineV4Combat;
        }
    }
}
