using Dovus.App.Casting;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.DevTools;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// v6 fiil executor'larının JSON verisi (hitbox_vfx.fiil_hitbox, mobility_cc.i_frame),
    /// cast anındaki kendine etkiler (reflect / damage buff) ve Kopyalama/Sıçrama tekrarları.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        static readonly Collider[] ReachHits = new Collider[24];

        struct DelayedLaunch
        {
            public double DueMs;
            public SkillExecutorKind Kind;
            public PendingClosing Pending;
            public SkillResolution Skill;
            public SkillMotionPlan Motion;
            public float EffectMult;
            public LivingEffect Logic;
            public int SlotCastId;
        }

        readonly List<DelayedLaunch> _delayedLaunches = new();
        VerbExecutionData _verbData;
        float _selfDamageBuff;
        double _selfDamageBuffUntilMs;
        float _emHealRatio;
        double _emHealUntilMs;

        public VerbExecutionData VerbData => _verbData;

        public void ConfigureVerbExecution(VerbExecutionData data) => _verbData = data;

        public void ConfigureMobilityCc(MobilityCcData data)
        {
            _mobilityCc = data;
            _playerStatus?.Board.ConfigureMobilityCc(data);
            _bossStatus?.Board.ConfigureMobilityCc(data);
        }

        public void ConfigureSkillNumbers(SkillNumberCatalog numbers) => _skillNumbers = numbers;

        void OnPlayerDamageTaken(float incomingDamage)
        {
            if (_emHealRatio > 0f && incomingDamage > 0.5f && _clock != null
                && _clock.Director.WorldTimeMs < _emHealUntilMs && _player != null)
            {
                PlayerVitals vitals = CachedPlayerVitals();
                int heal = Mathf.RoundToInt(incomingDamage * _emHealRatio);
                if (vitals != null && heal > 0)
                    vitals.ApplyHeal(heal);
            }

            NoteShieldBlockIfGuarding();
            bool crit = _playerStatus != null && _playerStatus.LastHitWasCrit;
            Vector3 at = _player != null ? _player.position + Vector3.up * 1.6f : Vector3.zero;
            _damageHud?.ShowDamage(incomingDamage, crit, at, victimIsPlayer: true);
            ReflectFromWorldVolumes(incomingDamage);
            if (_mobilityCc == null || _pending.Count == 0)
                return;
            // Poise, ölçeklenmiş can hasarıyla değil eski (küçük) vuruş sayısıyla kırılır.
            float poiseDamage = _playerStatus != null && _playerStatus.LastPoise > 0f
                ? _playerStatus.LastPoise
                : incomingDamage / CombatScale.DamageAndHp;
            float threshold = _mobilityCc.PoiseThreshold("orta");
            if (!_mobilityCc.TryPoiseBreak(poiseDamage, threshold, out int stunMs))
                return;
            CancelPendingCast("poise");
            _playerStatus?.Board.Apply(StatusKind.Stun, stunMs, 1f);
        }

        void CancelPendingCast(string reason)
        {
            for (int i = 0; i < _pending.Count; i++)
                _pending[i].View?.Logic?.Abort();
            if (_pending.Count > 0)
                DebugConfig.DevLog($"[Interrupt] startup cancelled by {reason}; count={_pending.Count}");
            _pending.Clear();
            _playerStatus?.ClearCastMobility();
        }

        bool TryVerbHitbox(in SkillResolution skill, out VerbHitboxSpec spec)
        {
            spec = default;
            return _verbData != null && _verbData.TryGetHitbox(skill, out spec) && !spec.IsEmpty;
        }

        float SelfDamageBuffMult()
        {
            if (_clock == null || _clock.Director.WorldTimeMs >= _selfDamageBuffUntilMs)
                return 1f;
            return 1f + _selfDamageBuff;
        }

        /// <summary>
        /// Güçlendirme buff_damage + Yükseltme self_damage_buff (buff_duration_sec) ve
        /// Yansıma/Aynalama reflect_ratio (reflect_duration_sec). lifetime_add süreye eklenir.
        /// </summary>
        void ApplySelfCastEffects(in SkillResolution skill)
        {
            if (skill.IsEmpty || _clock == null)
                return;
            var engine = skill.Engine;
            if (engine.IsNull)
                return;

            double now = _clock.Director.WorldTimeMs;
            float lifetimeAdd = Mathf.Max(0f, engine.LifetimeAdd(0f));

            float friendly = WeaponFriendlyScale();
            float buff = engine.BuffDamage(0f) + engine.SelfDamageBuff(0f);
            buff = WeaponPassiveRules.ScaleFriendlyMagnitude(buff, friendly);
            float buffSec = engine.BuffDurationSec(0f);
            MechanicPlan mechanicPlan = MechanicPlanFor(skill);
            CaptureBuffOverflow(mechanicPlan, now);
            // 8-9 hasar_buff koruyucu tetiktedir; kalıp/cast anında bir daha yazılmaz.
            if (buff > 0f && buffSec > 0f && GuardTriggerDelivery.AllowImmediate(mechanicPlan, "hasar_buff"))
            {
                _selfDamageBuff = buff;
                _selfDamageBuffUntilMs = now + (buffSec + lifetimeAdd) * 1000.0;
            }

            float reflect = engine.ReflectRatio(0f);
            reflect = WeaponPassiveRules.ScaleFriendlyMagnitude(reflect, friendly);
            float reflectSec = engine.ReflectDurationSec(0f);
            if (reflect > 0f && reflectSec > 0f && _playerStatus != null && HasSelfReflect(mechanicPlan))
                _playerStatus.GrantReflect(reflect, now + (reflectSec + lifetimeAdd) * 1000.0);

            MechanicEffect absorb = mechanicPlan?.Effects.Find(e => e.Stat == "em");
            if (absorb != null && absorb.Amount > 0)
            {
                _emHealRatio = (float)absorb.Amount;
                _emHealUntilMs = now + Mathf.Max(0.2f, reflectSec + lifetimeAdd) * 1000.0;
            }
            ApplyJsonSelfCast(mechanicPlan, reflect, reflectSec + lifetimeAdd, now);
        }

        /// <summary>
        /// Kopyalama duplicate_cast → duplicate_delay_sec sonra aynı executor (duplicate_damage_mult).
        /// Hareket + Sıçrama bounce_targets → dash bitince ikinci adım (bounce_damage_mult;
        /// hitbox_vfx.sifat_override.3 chain_count=2, skill 3-3 "Çift dash").
        /// </summary>
        void ScheduleFollowUpLaunches(
            SkillExecutorKind kind,
            PendingClosing pending,
            in SkillResolution skill,
            in SkillMotionPlan motion)
        {
            if (_clock == null || skill.IsEmpty || pending.View == null)
                return;
            var engine = skill.Engine;
            double now = _clock.Director.WorldTimeMs;
            LivingEffect logic = pending.View.Logic;
            SkillResolution skillCopy = skill;
            SkillMotionPlan motionCopy = motion;

            if (engine.DuplicateCast(false))
            {
                float delay = Mathf.Max(0f, engine.DuplicateDelaySec(0f));
                float mult = engine.HasDuplicateDamageMult
                    ? engine.DuplicateDamageMult(1f)
                    : 1f;
                Enqueue(now + delay * 1000.0, mult);
            }

            if (kind == SkillExecutorKind.Movement && engine.BounceTargets(0) > 0)
            {
                float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                float mult = engine.HasBounceDamageMult
                    ? engine.BounceDamageMult(1f)
                    : 1f;
                Enqueue(now + dashSec * 1000.0, mult);
            }

            void Enqueue(double dueMs, float mult) => _delayedLaunches.Add(new DelayedLaunch
            {
                DueMs = dueMs,
                Kind = kind,
                Pending = pending,
                Skill = skillCopy,
                Motion = motionCopy,
                EffectMult = mult,
                Logic = logic,
                SlotCastId = _slotQueryCastId
            });
        }

        void TickDelayedLaunches(double worldMs)
        {
            for (int i = _delayedLaunches.Count - 1; i >= 0; i--)
            {
                DelayedLaunch d = _delayedLaunches[i];
                if (worldMs < d.DueMs)
                    continue;
                _delayedLaunches.RemoveAt(i);
                int prevCast = _slotQueryCastId;
                _slotQueryCastId = d.SlotCastId;
                try
                {
                    TryLaunchSkillExecutor(d.Kind, d.Pending, d.Skill, d.Motion, d.EffectMult, d.Logic, d.SlotCastId);
                }
                finally
                {
                    _slotQueryCastId = prevCast;
                }
            }
        }

        /// <summary>
        /// mobility_cc.i_frame "minion_spawn_aninda" (11-10) — spawn anında kısa koruma.
        /// F1: eskiden Stasis'ti ve oyuncuyu ~0,3 sn donduruyordu (BlocksMovement + BlocksCast);
        /// artık skill hareketleriyle aynı donmayan dokunulmazlık penceresi. Düşmana etkisi yok.
        /// </summary>
        void ApplySpawnIFrame(in SkillResolution skill)
        {
            int ms = _verbData?.IFrameMsFor(skill.SkillId) ?? 0;
            if (ms <= 0 || _player == null)
                return;
            _player.GetComponent<PlayerDodgeRig>()?.OpenSkillIframe(ms);
        }

        /// <summary>Minion vuruşu: ham hasar boru hattından (zırh, kritik, ölçek bir kez).</summary>
        float ApplyMinionHit(in SkillResolution skill, float raw)
        {
            if (_bossVitals == null || _bossVitals.IsDown || raw <= 0f)
                return 0f;
            raw = DamagePipeline.TuneOutgoingPower(
                false, raw, 0f, _combat != null ? _combat.SkillPreArmorScale : 1f);
            float mult = skill.DamageMult > 0f ? skill.DamageMult : 1f;
            if (_playerStatus != null)
                mult *= _playerStatus.Board.OutgoingDamageMult;
            mult *= _slotPassives?.DamageMultFor(_slotQueryCastId) ?? 1f;
            mult *= SelfDamageBuffMult();

            EnsureBossArmor();
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            float armor = 0f;
            float taken = 1f;
            float shield = 0f;
            if (_bossStatus != null)
            {
                armor = _bossStatus.Armor.Effective(now);
                taken = _bossStatus.Board.IncomingDamageMult;
                shield = _bossStatus.Board.ShieldRemaining;
            }
            bool ignoreArmor = !skill.IsEmpty && !skill.Engine.IsNull
                && skill.Engine.IgnoreArmor(false);
            float slotPen = _slotPassives?.ArmorPenPercentFor(_slotQueryCastId) ?? 0f;
            float penPct = SlotPassiveCombat.CombineArmorPen(0f, ignoreArmor, slotPen);
            var dealt = DamagePipeline.Resolve(new DamageQuery
            {
                SkillPower = raw,
                Multiplier = mult,
                CanCrit = !skill.IsEmpty && skill.BaseDamage > 0f,
                CritChance = Crits.ChanceWith(ClosingHealRules.ExtraCritChanceAdd(skill)),
                CritMultiplier = Crits.Multiplier,
                CritRoll01 = _combatRng.NextRoll01(),
                Armor = armor,
                ArmorPenPercent = penPct,
                DamageTakenFactor = taken,
                Shield = shield,
                ApplyVariance = true,
                VarianceRoll01 = _combatRng.NextRoll01(),
                Poise = skill.IsEmpty ? 0f : skill.BasePoise,
                ScaleMagnitudes = true
            });
            if (dealt.ShieldAbsorbed > 0f && _bossStatus != null)
                _bossStatus.Board.ConsumeShield(dealt.ShieldAbsorbed);
            float damage = dealt.Amount;
            // S8: minyon kritikleri de gösterilir.
            _damageHud?.ShowDamage(damage, dealt.WasCrit, BossHitPoint(), DamageTint(), victimIsBoss: true);
            float lifesteal = ClosingHealRules.AdjectiveLifesteal(skill);
            lifesteal += _slotPassives?.LifestealAddFor(_slotQueryCastId) ?? 0f;
            if (lifesteal > 0f && _player != null)
            {
                var vitals = CachedPlayerVitals();
                int heal = Mathf.RoundToInt(damage * lifesteal);
                if (vitals != null && heal > 0)
                    vitals.ApplyHeal(heal);
            }
            _bossVitals.ApplyDamage(damage);
            NotifyBossStruck(false, allowHitstop: false);
            return damage;
        }

        /// <summary>Kendine/dost alan boss'a da değiyor mu (düşmanca sıfat durumları için).</summary>
        bool BossWithin(Vector3 center, float radiusM)
        {
            if (_boss == null)
                return false;
            int count = Physics.OverlapSphereNonAlloc(
                center, radiusM, ReachHits, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Transform boss = _boss.transform;
            for (int i = 0; i < count; i++)
            {
                Transform hit = ReachHits[i] != null ? ReachHits[i].transform : null;
                if (hit != null && (hit == boss || hit.IsChildOf(boss)))
                    return true;
            }
            return false;
        }
    }
}
