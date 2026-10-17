using Dovus.Game.Skills.RuleEngineV4;

namespace Dovus.Game.Skills
{
    /// <summary>RuleEngineV4SentenceHost → v4 combat bağlamı (yalnız eski motor tarafında).</summary>
    public static class RuleEngineV4ManifestationAdapter
    {
        /// <summary>İlk kurulum: CastHost yalnız MdCastPort → DirectorBridge ctor'unda atanır.</summary>
        public static RuleEngineV4CombatContext Bind(RuleEngineV4SentenceHost md) =>
            md == null ? null : CreateCombatContext(md);

        /// <summary>Mevcut bağlamı günceller (silah swap, yeni Bind yok).</summary>
        public static void Refresh(RuleEngineV4CombatContext combat, RuleEngineV4SentenceHost md)
        {
            if (combat == null || md == null)
                return;
            combat.CoroutineHost = md;
            combat.Player = md.MechanicsPlayer;
            combat.Visual = md._visual;
            combat.Clock = md.MechanicsClock;
            combat.Boss = md.MechanicsBoss;
            combat.BossVitals = md.MechanicsBossVitals;
            combat.BossDirector = md.MechanicsBossDirector;
            combat.Ally = md.MechanicsAlly;
            combat.DamageHud = md.MechanicsDamageHud;
            combat.PlayerVitals = md.CachedPlayerVitals;
            combat.Input = md._input;
            combat.Targeting = md._targeting;
            combat.SkillAim = md._skillAim;
            combat.ClosingQueue = md._closingQueue;
            combat.LiveTargetables = md._liveTargetables;
            combat.Colors = md._colors;
            combat.EquippedWeapon = md.MechanicsEquippedWeapon;
            combat.AnimationBridge = md._animationBridge;
            combat.EnsureAnimationReady = md.EnsureLaunchServices;
            combat.ResolveSkillWords = md.ResolveSkillWords;
            combat.NotifyCastDealt = dealt => md._castPort?.NotifyRuleEngineV4Dealt(dealt);
            combat.NotifyBossStruck = md.NotifyBossStruck;
            combat.BossHitPoint = md.BossHitPoint;
        }

        static RuleEngineV4CombatContext CreateCombatContext(RuleEngineV4SentenceHost md)
        {
            var combat = new RuleEngineV4CombatContext
            {
                Session = md.RuleEngineV4Session ?? new RuleEngineV4WorldSession(),
            };
            md.RuleEngineV4Session = combat.Session;
            Refresh(combat, md);
            return combat;
        }
    }
}
