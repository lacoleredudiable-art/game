using Dovus.App.Actors;
using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Shared;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>kural_motoru_v4 dilim: ek düşmanlar + ikinci AllyDummy.</summary>
    public static class RuleEngineV4SliceBuilder
    {
        public static void BuildIfEnabled(WorldContext ctx)
        {
            if (ctx.Tuning == null || !ctx.Tuning.RuleEngineV4.SliceScene)
                return;

            float r = Mathf.Max(RuleEngineV4SliceBuilderDefaults.MinRadiusFloorM, ctx.Tuning.RuleEngineV4.SliceMinionRadiusM);
            float h = Mathf.Max(RuleEngineV4SliceBuilderDefaults.MinHeightFloorM, ctx.Tuning.RuleEngineV4.SliceMinionHeightM);
            int hp = Mathf.Max(1, ctx.Tuning.RuleEngineV4.SliceMinionMaxHp);
            float spawnMaxR = ctx.SpawnMaxR;
            int playerHp = ctx.Host.ScaledPlayerHp(ctx.Assets);

            SpawnMinion(
                ctx,
                ActorDefaults.SliceMinion1Id,
                "SliceMinion1",
                new Vector3(RuleEngineV4SliceBuilderDefaults.Minion1X, h * 0.5f, RuleEngineV4SliceBuilderDefaults.Minion1Z),
                r,
                h,
                hp,
                spawnMaxR);
            SpawnMinion(
                ctx,
                ActorDefaults.SliceMinion2Id,
                "SliceMinion2",
                new Vector3(RuleEngineV4SliceBuilderDefaults.Minion2X, h * 0.5f, RuleEngineV4SliceBuilderDefaults.Minion2Z),
                r,
                h,
                hp,
                spawnMaxR);
            SpawnMinion(
                ctx,
                ActorDefaults.SliceMinion3Id,
                "SliceMinion3",
                new Vector3(0f, h * 0.5f, RuleEngineV4SliceBuilderDefaults.Minion3Z),
                r,
                h,
                hp,
                spawnMaxR);

            ctx.Ally2 = VisualAttach.CreateCapsule(
                "AllyDummy2",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(
                        RuleEngineV4SliceBuilderDefaults.Ally2SpawnX,
                        CompositionConstants.PlayerHeightM * 0.5f,
                        ActorsBuilderDefaults.PlayerSpawnZ),
                    spawnMaxR),
                CompositionConstants.PlayerRadiusM * ActorsBuilderDefaults.PlayerColliderRadiusScale,
                CompositionConstants.PlayerHeightM,
                new Color(0.25f, 0.75f, 0.45f));
            var ally2Hit = ctx.Ally2.AddComponent<CapsuleCollider>();
            ally2Hit.isTrigger = true;
            ctx.Ally2.AddComponent<ActorGroundingController>();
            ctx.AllyDummy2Controller = ctx.Ally2.AddComponent<AllyDummyController>();
            ctx.AllyDummy2Controller.Bind(playerHp, startRatio: ctx.Runtime.DebugFlags.StartHpRatio);
            ctx.AllyDummy2Controller.BindTeam(ctx.TeamAccess);

            var ally2Target = ctx.Ally2.AddComponent<TargetableHost>();
            ally2Target.BindLiveRegistry(ctx.Runtime.Targetables);
            ally2Target.Configure(0, "ALLY2", ActorDefaults.AllyDummy2Id, () => ctx.AllyDummy2Controller.Hp > 0);

            ctx.ActorRegistry.Register(new AllyActor(ActorDefaults.AllyDummy2Id, ActorTeam.Friendly));
            ctx.ActorViewRegistry.Register(ActorDefaults.AllyDummy2Id, ctx.Ally2.transform);
        }

        static void SpawnMinion(
            WorldContext ctx,
            ActorId id,
            string name,
            Vector3 localPos,
            float radiusM,
            float heightM,
            int hp,
            float spawnMaxR)
        {
            var go = VisualAttach.CreateCapsule(
                name,
                VisualAttach.ClampSpawnXZ(localPos, spawnMaxR),
                radiusM,
                heightM,
                new Color(0.85f, 0.35f, 0.3f));
            var col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = radiusM;
            col.height = heightM;
            var minion = go.AddComponent<SliceLightMinionHost>();
            minion.Configure(id, hp);
            var body = go.AddComponent<RuleEngineV4PhysicsBodyHost>();
            body.WeightTier = RuleEngineV4WeightTier.Light;
            body.BodyRadiusM = radiusM;
            go.AddComponent<ActorGroundingController>();

            var target = go.AddComponent<TargetableHost>();
            target.BindLiveRegistry(ctx.Runtime.Targetables);
            target.Configure(1, name, id, () => !minion.IsDown);

            ctx.ActorViewRegistry.Register(id, go.transform);
            ctx.SliceMinions.Add(minion);
        }
    }
}
