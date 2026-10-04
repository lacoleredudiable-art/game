using Dovus.Core.Combat;
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
            int playerHp = ctx.Host.ScaledPlayerHp();

            ctx.Player = VisualAttach.CreateCapsule(
                "Player",
                VisualAttach.ClampSpawnXZ(
                    new Vector3(0f, CompositionConstants.PlayerHeightM * 0.5f, -2f),
                    spawnMaxR),
                CompositionConstants.PlayerRadiusM,
                CompositionConstants.PlayerHeightM,
                tuning.PlayerColor);
            VisualAttach.Attach(
                ctx.Player,
                VisualAttach.ResolvePlayerVisualPrefab(ctx.PlayerVisualPrefab),
                tuning.PlayerVisualHeightM,
                ctx.Player.transform.position.y - CompositionConstants.PlayerHeightM * 0.5f,
                tuning.CharacterAnimSpeed,
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
                tuning.PlayerVisualHeightM,
                ctx.Ally.transform.position.y - CompositionConstants.PlayerHeightM * 0.5f,
                tuning.CharacterAnimSpeed,
                out var allyAnim);
            if (allyAnim != null)
            {
                var allyVisual = ctx.Ally.AddComponent<ActorVisual>();
                allyVisual.Bind(allyAnim, ctx.Ally.GetComponent<Renderer>());
                allyVisual.CrossFadeSec = tuning.AnimCrossFadeSec;
                allyVisual.StrikeComboResetSec = tuning.BasicStrikeComboResetSec;
                allyVisual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
                allyVisual.UpperBodyMinSpeed = tuning.UpperBodyCastMinSpeed;
                allyVisual.SetWeapon("kilic");
            }
            var allyHitCollider = ctx.Ally.AddComponent<CapsuleCollider>();
            allyHitCollider.isTrigger = true;
            ctx.Ally.AddComponent<ActorGrounding>();
            ctx.AllyDummy = ctx.Ally.AddComponent<AllyDummy>();
            ctx.AllyDummy.Bind(playerHp, startRatio: DebugConfig.StartHpRatio);

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
            // SkillExecutor overlap/projectile yolu için gerçek fizik hedefi.
            var bossHitCollider = ctx.Boss.AddComponent<CapsuleCollider>();
            bossHitCollider.isTrigger = true;
            VisualAttach.Attach(
                ctx.Boss,
                VisualAttach.ResolveBossVisualPrefab(tuning, ctx.BossVisualPrefab),
                tuning.BossVisualHeightM,
                ctx.Boss.transform.position.y - CompositionConstants.BossHeightM * 0.5f,
                tuning.CharacterAnimSpeed,
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
            visual.CrossFadeSec = tuning.AnimCrossFadeSec;
            visual.StrikeComboResetSec = tuning.BasicStrikeComboResetSec;
            visual.BasicStrikeAnimSpeed = combat.Feel.BasicStrikeAnimSpeed;
            visual.UpperBodyMinSpeed = tuning.UpperBodyCastMinSpeed;

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
