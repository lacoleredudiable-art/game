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
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    /// <summary>
    /// Oyuncu + dost + boss zinciri. Nesne oluşturma ve AddComponent SIRASI eski
    /// PrototypeBootstrap.BuildWorld ile birebir aynıdır (oyuncu/boss bileşenleri iç içe;
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
                    new Vector3(-3.2f, CompositionConstants.PlayerHeightM * 0.5f, -1.2f),
                    spawnMaxR),
                CompositionConstants.PlayerRadiusM * 0.95f,
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
                var allyVisual = ctx.Ally.AddComponent<ActorVisual>();
                allyVisual.Bind(allyAnim, ctx.Ally.GetComponent<Renderer>());
                allyVisual.CrossFadeSec = tuning.Player.AnimCrossFadeSec;
                allyVisual.StrikeComboResetSec = tuning.Player.BasicStrikeComboResetSec;
                allyVisual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
                allyVisual.UpperBodyMinSpeed = tuning.Player.UpperBodyCastMinSpeed;
                allyVisual.SetWeapon("kilic");
            }
            var allyHitCollider = ctx.Ally.AddComponent<CapsuleCollider>();
            allyHitCollider.isTrigger = true;
            ctx.Ally.AddComponent<ActorGrounding>();
            ctx.AllyDummy = ctx.Ally.AddComponent<AllyDummy>();
            ctx.AllyDummy.Bind(playerHp, startRatio: DebugConfig.StartHpRatio);
            ctx.AllyDummy.BindTeam(ctx.TeamAccess);

            float bossSpawnZ = 5f * Mathf.Max(1f, tuning.Arena.ArenaVisualScale * 0.55f);
            bossSpawnZ = Mathf.Clamp(bossSpawnZ, -spawnMaxR, spawnMaxR);
            ctx.Boss = VisualAttach.CreateCapsule(
                "Boss",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(0f, CompositionConstants.BossHeightM * 0.5f, bossSpawnZ),
                    spawnMaxR),
                CompositionConstants.BossRadiusM,
                CompositionConstants.BossHeightM,
                tuning.Visuals.BossColor);
            // SkillExecutor overlap/projectile yolu için gerçek fizik hedefi.
            var bossHitCollider = ctx.Boss.AddComponent<CapsuleCollider>();
            bossHitCollider.isTrigger = true;
            VisualAttach.Attach(
                ctx.Boss,
                VisualAttach.ResolveBossVisualPrefab(tuning, ctx.BossVisualPrefab),
                tuning.Player.BossVisualHeightM,
                ctx.Boss.transform.position.y - CompositionConstants.BossHeightM * 0.5f,
                tuning.Player.CharacterAnimSpeed,
                out var bossAnim);

            ctx.Player.AddComponent<MoveInput>().Tuning = tuning;

            var motor = ctx.Player.AddComponent<KinematicMotor>();
            motor.Tuning = tuning;
            motor.BodyRadiusM = CompositionConstants.PlayerRadiusM;

            ctx.PlayerPose = ctx.Player.AddComponent<ActorPose>();
            ctx.PlayerPose.Tuning = tuning;
            ctx.PlayerPose.CaptureBase();

            var visual = ctx.Player.AddComponent<ActorVisual>();
            if (playerAnim != null)
                visual.Bind(playerAnim, ctx.Player.GetComponent<Renderer>());
            visual.CrossFadeSec = tuning.Player.AnimCrossFadeSec;
            visual.StrikeComboResetSec = tuning.Player.BasicStrikeComboResetSec;
            visual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
            visual.UpperBodyMinSpeed = tuning.Player.UpperBodyCastMinSpeed;

            var bossVisual = ctx.Boss.AddComponent<BossVisual>();
            if (bossAnim != null)
                bossVisual.Bind(bossAnim, ctx.Boss.GetComponent<Renderer>());
            bossVisual.Configure(tuning);

            ctx.Player.AddComponent<HitFlash>().Bind(combat.Feel);
            ctx.Boss.AddComponent<HitFlash>().Bind(combat.Feel);

            var vitals = ctx.Player.AddComponent<PlayerVitals>();
            ctx.PlayerVitals = vitals;
            vitals.Bind(combat.Boss, playerHp, startRatio: DebugConfig.StartHpRatio);
            vitals.BindClock(clock);
            vitals.SetDevHp(DebugConfig.DevHpActive);

            ctx.PlayerResource = ctx.Player.AddComponent<PlayerResource>();
            ctx.PlayerResource.Bind();
            ctx.PlayerResource.BindClock(clock);

            ctx.PlayerCooldown = ctx.Player.AddComponent<PlayerCooldown>();
            ctx.PlayerCooldown.Bind();

            ctx.PlayerStatus = ctx.Player.AddComponent<ActorStatus>();
            ctx.BossStatus = ctx.Boss.AddComponent<ActorStatus>();

            ctx.Afterimage = ctx.Player.AddComponent<AfterimageTrail>();
            ctx.Afterimage.Bind(combat.Feel, tuning);

            ctx.DodgeMotion = ctx.Player.AddComponent<DodgeMotion>();
            ctx.Player.AddComponent<PlayerDodgeRig>();
            ctx.Player.AddComponent<WeaponShortShieldHost>().Bind(clock);
            ctx.Player.AddComponent<MotionTemplateBody>();
            ctx.Player.AddComponent<ActorGrounding>();

            ctx.Boss.AddComponent<ActorGrounding>();
            ctx.Boss.AddComponent<MotionTemplateBody>();
            ctx.BossReactor = ctx.Boss.AddComponent<BossReactor>();
            ctx.BossReactor.Tuning = tuning;
            ctx.BossReactor.ConfigureFeel(combat.Feel);
            ctx.BossReactor.BodyRadiusM = CompositionConstants.BossRadiusM;
            ctx.BossReactor.CaptureHome();

            ctx.BossVitals = new BossVitals(ctx.Host.ScaledBossHp(ctx.Assets, combat.Boss.MaxHp));
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
