using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using UnityEngine;

namespace Dovus.Game
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
        }

        readonly List<DelayedLaunch> _delayedLaunches = new();
        VerbExecutionData _verbData;
        float _selfDamageBuff;
        double _selfDamageBuffUntilMs;

        public VerbExecutionData VerbData => _verbData;

        public void ConfigureVerbExecution(VerbExecutionData data) => _verbData = data;

        public void ConfigureMobilityCc(MobilityCcData data)
        {
            _mobilityCc = data;
            _playerStatus?.Board.ConfigureMobilityCc(data);
            _bossStatus?.Board.ConfigureMobilityCc(data);
        }

        void OnPlayerDamageTaken(float incomingDamage)
        {
            if (_mobilityCc == null || _pending.Count == 0)
                return;
            // JSON poise katmanını ekipmana bağlayan alan yok; prototip nötr "orta" kullanır.
            float threshold = _mobilityCc.PoiseThreshold("orta");
            if (!_mobilityCc.TryPoiseBreak(incomingDamage, threshold, out int stunMs))
                return;
            CancelPendingCast("poise");
            _playerStatus?.Board.Apply(StatusKind.Stun, stunMs, 1f);
        }

        void CancelPendingCast(string reason)
        {
            for (int i = 0; i < _pending.Count; i++)
                _pending[i].View?.Logic?.Abort();
            if (_pending.Count > 0)
                Debug.Log($"[Interrupt] startup cancelled by {reason}; count={_pending.Count}");
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
            JsonValue engine = skill.EngineModifiers;
            if (engine.IsNull)
                return;

            double now = _clock.Director.WorldTimeMs;
            float lifetimeAdd = Mathf.Max(0f, engine["lifetime_add"].AsFloat(0f));

            float buff = engine["buff_damage"].AsFloat(0f) + engine["self_damage_buff"].AsFloat(0f);
            float buffSec = engine["buff_duration_sec"].AsFloat(0f);
            if (buff > 0f && buffSec > 0f)
            {
                _selfDamageBuff = buff;
                _selfDamageBuffUntilMs = now + (buffSec + lifetimeAdd) * 1000.0;
            }

            float reflect = engine["reflect_ratio"].AsFloat(0f);
            float reflectSec = engine["reflect_duration_sec"].AsFloat(0f);
            if (reflect > 0f && reflectSec > 0f && _playerStatus != null)
                _playerStatus.GrantReflect(reflect, now + (reflectSec + lifetimeAdd) * 1000.0);
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
            JsonValue engine = skill.EngineModifiers;
            double now = _clock.Director.WorldTimeMs;
            LivingEffect logic = pending.View.Logic;
            SkillResolution skillCopy = skill;
            SkillMotionPlan motionCopy = motion;

            if (engine["duplicate_cast"].AsBool(false))
            {
                float delay = Mathf.Max(0f, engine["duplicate_delay_sec"].AsFloat(0f));
                float mult = engine.Has("duplicate_damage_mult")
                    ? engine["duplicate_damage_mult"].AsFloat(1f)
                    : 1f;
                Enqueue(now + delay * 1000.0, mult);
            }

            if (kind == SkillExecutorKind.Movement && engine["bounce_targets"].AsInt(0) > 0)
            {
                float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                float mult = engine.Has("bounce_damage_mult")
                    ? engine["bounce_damage_mult"].AsFloat(1f)
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
                Logic = logic
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
                TryLaunchSkillExecutor(d.Kind, d.Pending, d.Skill, d.Motion, d.EffectMult, d.Logic);
            }
        }

        /// <summary>mobility_cc.i_frame "minion_spawn_aninda" (11-10) — spawn anında kısa koruma.</summary>
        void ApplySpawnIFrame(in SkillResolution skill)
        {
            int ms = _verbData?.IFrameMsFor(skill.SkillId) ?? 0;
            if (ms > 0 && _playerStatus != null)
                _playerStatus.Board.Apply(StatusKind.Stasis, ms, 1f);
        }

        /// <summary>Minion vuruşu: ham hasar × sıfat damage_mult × oyuncu çıkış çarpanları.</summary>
        void ApplyMinionHit(in SkillResolution skill, float raw)
        {
            if (_bossVitals == null || _bossVitals.IsDown || raw <= 0f)
                return;
            float mult = skill.DamageMult > 0f ? skill.DamageMult : 1f;
            if (_playerStatus != null)
                mult *= _playerStatus.Board.OutgoingDamageMult;
            mult *= _passiveDirector?.DamageMult ?? 1f;
            mult *= _slotPassives?.DamageMult ?? 1f;
            mult *= SelfDamageBuffMult();
            if (_bossStatus != null)
                mult *= _bossStatus.Board.IncomingDamageMult;

            float damage = raw * mult;
            _damageHud?.ShowDamage(damage, false);
            _lastDamageDealtMs = _clock.Director.WorldTimeMs;
            float lifesteal = AdjectiveLifesteal(skill);
            lifesteal += _slotPassives?.LifestealAdd ?? 0f;
            if (lifesteal > 0f && _player != null)
            {
                var vitals = _player.GetComponent<PlayerVitals>();
                int heal = Mathf.RoundToInt(damage * lifesteal);
                if (vitals != null && heal > 0)
                    vitals.ApplyHeal(heal);
            }
            _bossVitals.ApplyDamage(damage);
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
