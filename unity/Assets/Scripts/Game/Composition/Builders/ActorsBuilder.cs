using Dovus.App.Actors;
using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>
    /// Oyuncu + dost + boss zinciri. Nesne oluşturma ve AddComponent SIRASI eski
    /// GameBootstrapHost.BuildWorld ile birebir aynıdır (oyuncu/boss bileşenleri iç içe;
    /// Awake/OnEnable sırası ve örnek kimlikleri buna bağlı) — sırayı değiştirme.
    /// </summary>
    public sealed class ActorsBuilder
    {
        public void Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var combat = ctx.Combat;
            var clock = ctx.Clock;
            float spawnMaxR = ctx.SpawnMaxR;
            int playerHp = ctx.Host.ScaledPlayerHp(ctx.Assets);

            ctx.Player = VisualAttach.CreateCapsule(
                "Player",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(0f, CompositionConstants.PlayerHeightM * 0.5f, -2f),
                    spawnMaxR),
                CompositionConstants.PlayerRadiusM,
                CompositionConstants.PlayerHeightM,
                tuning.Visuals.PlayerColor);
            VisualAttach.Attach(
                ctx.Player,
                VisualAttach.ResolvePlayerVisualPrefab(ctx.PlayerVisualPrefab),
                tuning.Player.PlayerVisualHeightM,
                ctx.Player.transform.position.y - CompositionConstants.PlayerHeightM * 0.5f,
                tuning.Player.CharacterAnimSpeed,
                out var playerAnim);

            ctx.Ally = VisualAttach.CreateCapsule(
                "AllyDummy",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(ActorsBuilderDefaults.PlayerSpawnX, CompositionConstants.PlayerHeightM * 0.5f, ActorsBuilderDefaults.PlayerSpawnZ),
                    spawnMaxR),
                CompositionConstants.PlayerRadiusM * ActorsBuilderDefaults.PlayerColliderRadiusScale,
                CompositionConstants.PlayerHeightM,
                new Color(0.35f, 0.85f, 0.55f));
            VisualAttach.Attach(
                ctx.Ally,
                ctx.PlayerVisualPrefab,
                tuning.Player.PlayerVisualHeightM,
                ctx.Ally.transform.position.y - CompositionConstants.PlayerHeightM * 0.5f,
                tuning.Player.CharacterAnimSpeed,
                out var allyAnim);
            if (allyAnim != null)
            {
                var allyVisual = ctx.Ally.AddComponent<ActorView>();
                allyVisual.Bind(allyAnim, ctx.Ally.GetComponent<Renderer>());
                allyVisual.CrossFadeSec = tuning.Player.AnimCrossFadeSec;
                allyVisual.StrikeComboResetSec = tuning.Player.BasicStrikeComboResetSec;
                allyVisual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
                allyVisual.UpperBodyMinSpeed = tuning.Player.UpperBodyCastMinSpeed;
                allyVisual.SetWeapon("kilic");
            }
            var allyHitCollider = ctx.Ally.AddComponent<CapsuleCollider>();
            allyHitCollider.isTrigger = true;
            ctx.Ally.AddComponent<ActorGroundingController>();
            ctx.AllyDummyController = ctx.Ally.AddComponent<AllyDummyController>();
            var debugFlags = ctx.Runtime.DebugFlags;
            ctx.AllyDummyController.Bind(playerHp, startRatio: debugFlags.StartHpRatio);
            ctx.AllyDummyController.BindTeam(ctx.TeamAccess);

            float bossSpawnZ = ActorsBuilderDefaults.BossSpawnZBaseM * Mathf.Max(1f, tuning.Arena.ArenaVisualScale * ActorsBuilderDefaults.BossSpawnZArenaScale);
            bossSpawnZ = Mathf.Clamp(bossSpawnZ, -spawnMaxR, spawnMaxR);
            ctx.Boss = VisualAttach.CreateCapsule(
                "Boss",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(0f, CompositionConstants.BossHeightM * 0.5f, bossSpawnZ),
                    spawnMaxR),
                CompositionConstants.BossRadiusM,
                CompositionConstants.BossHeightM,
                tuning.Visuals.BossColor);
            // SkillExecutorController overlap/projectile yolu için gerçek fizik hedefi.
            var bossHitCollider = ctx.Boss.AddComponent<CapsuleCollider>();
            bossHitCollider.isTrigger = true;
            VisualAttach.Attach(
                ctx.Boss,
                VisualAttach.ResolveBossVisualPrefab(tuning, ctx.BossVisualPrefab),
                tuning.Player.BossVisualHeightM,
                ctx.Boss.transform.position.y - CompositionConstants.BossHeightM * 0.5f,
                tuning.Player.CharacterAnimSpeed,
                out var bossAnim);

            ctx.Player.AddComponent<MoveInputController>().Tuning = tuning;

            var motor = ctx.Player.AddComponent<KinematicMotorController>();
            motor.Tuning = tuning;
            motor.BodyRadiusM = CompositionConstants.PlayerRadiusM;
            motor.BindClock(clock);

            ctx.PlayerPose = ctx.Player.AddComponent<ActorPoseView>();
            ctx.PlayerPose.Tuning = tuning;
            ctx.PlayerPose.CaptureBase();

            var visual = ctx.Player.AddComponent<ActorView>();
            if (playerAnim != null)
                visual.Bind(playerAnim, ctx.Player.GetComponent<Renderer>());
            visual.CrossFadeSec = tuning.Player.AnimCrossFadeSec;
            visual.StrikeComboResetSec = tuning.Player.BasicStrikeComboResetSec;
            visual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
            visual.UpperBodyMinSpeed = tuning.Player.UpperBodyCastMinSpeed;

            var bossVisual = ctx.Boss.AddComponent<BossView>();
            if (bossAnim != null)
                bossVisual.Bind(bossAnim, ctx.Boss.GetComponent<Renderer>());
            bossVisual.Configure(tuning);

            ctx.Player.AddComponent<HitFlashView>().Bind(combat.Feel);
            ctx.Boss.AddComponent<HitFlashView>().Bind(combat.Feel);

            var vitals = ctx.Player.AddComponent<PlayerVitalsHost>();
            ctx.PlayerVitalsHost = vitals;
            vitals.Bind(combat.Boss, playerHp, startRatio: debugFlags.StartHpRatio);
            vitals.BindClock(clock);
            vitals.SetDevHp(debugFlags.DevHpActive);

            ctx.PlayerResourceHost = ctx.Player.AddComponent<PlayerResourceHost>();
            ctx.PlayerResourceHost.Bind();
            ctx.PlayerResourceHost.BindClock(clock);

            ctx.PlayerCooldownHost = ctx.Player.AddComponent<PlayerCooldownHost>();
            ctx.PlayerCooldownHost.Bind();

            ctx.PlayerStatus = ctx.Player.AddComponent<ActorStatusHost>();
            ctx.BossStatus = ctx.Boss.AddComponent<ActorStatusHost>();

            ctx.Afterimage = ctx.Player.AddComponent<AfterimageTrailView>();
            ctx.Afterimage.Bind(combat.Feel, tuning);

            ctx.DodgeMotionController = ctx.Player.AddComponent<DodgeMotionController>();
            ctx.Player.AddComponent<PlayerDodgeController>();
            ctx.Player.AddComponent<WeaponShortShieldHost>().Bind(clock);
            ctx.Player.AddComponent<MotionTemplateBodyHost>();
            ctx.Player.AddComponent<ActorGroundingController>();

            ctx.Boss.AddComponent<ActorGroundingController>();
            ctx.Boss.AddComponent<MotionTemplateBodyHost>();
            ctx.BossReactorController = ctx.Boss.AddComponent<BossReactorController>();
            ctx.BossReactorController.Tuning = tuning;
            ctx.BossReactorController.ConfigureFeel(combat.Feel);
            ctx.BossReactorController.BodyRadiusM = CompositionConstants.BossRadiusM;
            ctx.BossReactorController.CaptureHome();

            ctx.BossVitals = new BossVitals(ctx.Host.ScaledBossHp(ctx.Assets, combat.Boss.MaxHp));
            var allyTarget = ctx.Ally.AddComponent<TargetableHost>();
            allyTarget.BindLiveRegistry(ctx.Runtime.Targetables);
            allyTarget.Configure(
                teamId: 0,
                displayName: "ALLY",
                ActorDefaults.AllyDummyId,
                available: () => ctx.AllyDummyController.Hp > 0);
            var bossTarget = ctx.Boss.AddComponent<TargetableHost>();
            bossTarget.BindLiveRegistry(ctx.Runtime.Targetables);
            bossTarget.Configure(
                teamId: 1,
                displayName: "BOSS",
                ActorDefaults.BossId,
                available: () => !ctx.BossVitals.IsDown);
            ctx.PlayerStatus.Bind(null, combat.Status, ctx.PlayerVitalsHost, null, null);
            ctx.BossStatus.Bind(null, combat.Status, null, ctx.BossVitals, ctx.BossReactorController);

            ctx.BossTelegraphView = ctx.Boss.AddComponent<BossTelegraphView>();
            ctx.BossTelegraphView.Bind(tuning, combat.Boss, ctx.Boss.transform);

            ctx.Player.GetComponent<ActorGroundingController>()?.BindClock(clock);
            ctx.Ally.GetComponent<ActorGroundingController>()?.BindClock(clock);
            ctx.Boss.GetComponent<ActorGroundingController>()?.BindClock(clock);

            ctx.ActorRegistry = new ActorRegistry();
            ctx.ActorViewRegistry = new ActorViewRegistry();
            ctx.ActorRegistry.Register(new PlayerActor(ActorDefaults.PlayerId, ActorTeam.Friendly, vitals.Health));
            ctx.ActorRegistry.Register(new AllyActor(ActorDefaults.AllyDummyId, ActorTeam.Friendly));
            ctx.ActorRegistry.Register(new BossActor(ActorDefaults.BossId, ActorTeam.Hostile, ctx.BossVitals));
            ctx.ActorViewRegistry.Register(ActorDefaults.PlayerId, ctx.Player.transform);
            ctx.ActorViewRegistry.Register(ActorDefaults.AllyDummyId, ctx.Ally.transform);
            ctx.ActorViewRegistry.Register(ActorDefaults.BossId, ctx.Boss.transform);
            ctx.TeamComboHost.Modifiers.BindActorRegistry(ctx.ActorRegistry);
        }
    }
}
