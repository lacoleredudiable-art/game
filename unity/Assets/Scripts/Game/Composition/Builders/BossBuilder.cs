using Dovus.Core.Combat;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    public sealed class BossBuilder
    {
        public void Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var combat = ctx.Combat;
            float spawnMaxR = ctx.SpawnMaxR;

            float bossSpawnZ = 5f * Mathf.Max(1f, tuning.ArenaVisualScale * 0.55f);
            bossSpawnZ = Mathf.Clamp(bossSpawnZ, -spawnMaxR, spawnMaxR);
            ctx.Boss = VisualAttach.CreateCapsule(
                "Boss",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(0f, CompositionConstants.BossHeightM * 0.5f, bossSpawnZ),
                    spawnMaxR),
                CompositionConstants.BossRadiusM,
                CompositionConstants.BossHeightM,
                tuning.BossColor);
            var bossHitCollider = ctx.Boss.AddComponent<CapsuleCollider>();
            bossHitCollider.isTrigger = true;
            VisualAttach.Attach(
                ctx.Boss,
                VisualAttach.ResolveBossVisualPrefab(tuning, ctx.BossVisualPrefab),
                tuning.BossVisualHeightM,
                ctx.Boss.transform.position.y - CompositionConstants.BossHeightM * 0.5f,
                tuning.CharacterAnimSpeed,
                out var bossAnim);

            var bossVisual = ctx.Boss.AddComponent<BossVisual>();
            if (bossAnim != null)
                bossVisual.Bind(bossAnim, ctx.Boss.GetComponent<Renderer>());
            bossVisual.Configure(tuning);

            ctx.Boss.AddComponent<HitFlash>().Bind(combat.Feel);

            ctx.BossStatus = ctx.Boss.AddComponent<ActorStatus>();

            ctx.Boss.AddComponent<ActorGrounding>();
            ctx.Boss.AddComponent<MotionTemplateBody>();
            ctx.BossReactor = ctx.Boss.AddComponent<BossReactor>();
            ctx.BossReactor.Tuning = tuning;
            ctx.BossReactor.ConfigureFeel(combat.Feel);
            ctx.BossReactor.BodyRadiusM = CompositionConstants.BossRadiusM;
            ctx.BossReactor.CaptureHome();

            ctx.BossVitals = new BossVitals(ctx.Host.ScaledBossHp(combat.Boss.MaxHp));
            ctx.Ally.AddComponent<Targetable>().Configure(
                teamId: 0,
                displayName: "ALLY",
                available: () => ctx.AllyDummy.Hp > 0);
            ctx.Boss.AddComponent<Targetable>().Configure(
                teamId: 1,
                displayName: "BOSS",
                available: () => !ctx.BossVitals.IsDown);
            ctx.PlayerStatus.Bind(null, combat.Status, ctx.PlayerVitals, null, null);
            ctx.BossStatus.Bind(null, combat.Status, null, ctx.BossVitals, ctx.BossReactor);

            ctx.BossTelegraph = ctx.Boss.AddComponent<BossTelegraph>();
            ctx.BossTelegraph.Bind(tuning, combat.Boss, ctx.Boss.transform);
        }
    }
}
