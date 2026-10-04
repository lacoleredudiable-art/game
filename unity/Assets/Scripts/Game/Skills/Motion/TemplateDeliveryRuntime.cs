using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Motion
{
    public sealed class TemplateDeliveryRuntime
    {
        struct ArmedBeat
        {
            public double DueMs;
            public DeliveryBeatKind Kind;
            public float Power;
        }

        struct DeliveredCast
        {
            public bool Valid;
            public bool Spawned;
            public SkillResolution Skill;
            public PendingClosing Pending;
            public SkillMotionPlan Motion;
            public int SlotCastId;
        }

        readonly ITemplateDeliveryRuntimeHost _host;
        readonly List<ArmedBeat> _beats = new();
        TemplateDeliveryOrder _order;
        double _startMs;
        int _slotCastId;
        float _stashedShare;
        bool _stashed;
        bool _detonated;
        bool _hostileNoted;
        SkillResolution _skill;
        PendingClosing _pending;
        SkillMotionPlan _motion;
        DeliveredCast _previous;
        DeliveredCast _repeatTarget;

        public TemplateDeliveryRuntime(ITemplateDeliveryRuntimeHost host) => _host = host;

        public SkillResolution DeliverySkill => _skill;

        public void NoteDeferredHit(float share)
        {
            _stashedShare = Mathf.Max(_stashedShare, share);
            _stashed = true;
        }

        public void ArmTemplateDelivery(SkillResolution skill, PendingClosing pending, SkillMotionPlan motion)
        {
            _beats.Clear();
            _order = null;
            _stashedShare = 0f;
            _stashed = false;
            _detonated = false;
            _hostileNoted = false;
            _skill = skill;
            _pending = pending;
            _motion = motion;
            _slotCastId = _host.SlotQueryCastId;
            _repeatTarget = _previous;

            _host.Motion.TryPlayTemplate(skill.SkillId, out MotionTemplate template);
            float tick = _host.Combat != null ? _host.Combat.Manifestation.ExecutorFieldTickSec : 1f;
            _order = TemplateDelivery.Build(
                _host.LastMechanicPlan,
                skill.Engine,
                template,
                _host.MechanicEngine != null ? _host.MechanicEngine.Rules : null,
                tick);
            _startMs = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            foreach (DeliveryBeat beat in _order.Schedule())
            {
                _beats.Add(new ArmedBeat
                {
                    DueMs = _startMs + beat.AtSec * 1000.0,
                    Kind = beat.Kind,
                    Power = beat.Power
                });
            }

            FlushTemplateDelivery(_startMs);
            _previous = new DeliveredCast
            {
                Valid = true,
                Spawned = _order.SpawnActors,
                Skill = skill,
                Pending = pending,
                Motion = motion,
                SlotCastId = _slotCastId
            };
        }

        public void TickTemplateDelivery(double worldMs) => FlushTemplateDelivery(worldMs);

        void FlushTemplateDelivery(double worldMs)
        {
            for (int i = 0; i < _beats.Count;)
            {
                if (worldMs + 0.5 < _beats[i].DueMs)
                {
                    i++;
                    continue;
                }

                ArmedBeat beat = _beats[i];
                _beats.RemoveAt(i);
                PlayDeliveryBeat(beat);
            }
        }

        public bool ShouldDeferTemplateGameplay(double now)
        {
            if (_order == null || _order.OpeningPulse)
                return false;
            if (!_order.DelayedMark && !_order.RiseDelay)
                return false;
            if (_detonated)
                return false;
            double due = _startMs + System.Math.Max(0.05, _order.ActivationDelaySec) * 1000.0;
            return now + 1.0 < due;
        }

        public bool TemplateHitAlreadyDetonated() =>
            _detonated
            && _order != null
            && !_order.OpeningPulse
            && (_order.DelayedMark || _order.RiseDelay);

        public void NoteTemplateHostileHit(Vector3 origin)
        {
            if (_hostileNoted)
                return;
            _hostileNoted = true;
            TryTemplateKnockback();
            _host.TryLandWeaponStun(_skill.IsEmpty ? _host.Motion.TemplateSkill : _skill, false);
            TryTemplateCannonSplash(origin);
        }

        /// <summary>
        /// İtme her zaman oyuncudan dışarı. Vuruş noktası boss'un üstünde ya da ötesinde
        /// olabilir; oradan itmek boss'u oyuncuya yollar (1-6, 5-2).
        /// </summary>
        void TryTemplateKnockback()
        {
            if (_order == null || !_order.BossKnockback)
                return;
            if (_host.Boss == null || _host.Player == null || (_host.BossVitals != null && _host.BossVitals.IsDown))
                return;
            if (_host.Boss.PullActive)
                return;
            Vector3 from = _host.Player.position;
            SkillResolution skill = _skill.IsEmpty ? _host.Motion.TemplateSkill : _skill;
            float push = _order.PushM;
            bool landingWave = push > 0f && JsonEffectRules.LandingWavePush(_host.MechanicPlanFor(skill));
            if (!skill.IsEmpty && StatusApplicator.IsSelfTargeted(skill) && !landingWave)
                return;
            float knock = _host.Combat != null ? _host.Combat.Manifestation.BossKnockbackM : 1.35f;
            // JSON itme mesafesi yalnız zorla yer değiştirmeye izin varken; yoksa eski genel itme.
            if (push > 0f && ForcedDisplacement.Allows(_host.BossStatus != null ? _host.BossStatus.Board : null))
            {
                knock = push;
                _host.JsonLog($"itme {knock:0.##}m" + (landingWave ? " (iniş dalgası)" : ""));
            }
            float shake = _host.Combat != null ? _host.Combat.Manifestation.BossShakeSec * 0.45f : 0.12f;
            double now = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            _host.Boss.React(from, knock, 0.05f, shake, now);
        }

        void TryTemplateCannonSplash(Vector3 origin)
        {
            WeaponCombatProfile profile = _host.EquippedWeapon != null ? _host.EquippedWeapon.Profile : null;
            if (profile == null || profile.HitShape != "ballistic")
                return;
            float splash = profile.BasicRadiusM > 0f ? profile.BasicRadiusM : 3f;
            float arena = _host.Motor != null ? _host.Motor.Tuning.ArenaHalfSizeM : 50f;
            float bossR = _host.Boss != null && _host.Boss.BodyRadiusM > 0.01f ? _host.Boss.BodyRadiusM : 0.85f;
            _host.Cannon.PushCannonBodies(origin.x, origin.z, splash, arena, bossR);
        }

        public bool WeaponArcConnects(in MotionHit hit)
        {
            SkillResolution skill = _host.Motion.TemplateSkill.IsEmpty ? _skill : _host.Motion.TemplateSkill;
            WeaponPassiveMods mods = _host.HitMods(skill, false, false);
            if (mods.ArcDeg <= 0f || _host.Boss == null)
                return false;
            Vector3 origin = new Vector3(hit.OriginX, 0f, hit.OriginZ);
            Vector3 to = _host.Boss.transform.position - origin;
            to.y = 0f;
            Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                dir = _host.Player != null ? _host.Player.forward : Vector3.forward;
            float delta = to.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(dir, to);
            float reach = Mathf.Max(hit.LengthM, hit.RadiusM) + _host.Motion.BossBodyRadius();
            return MeleeArc.Hits(to.magnitude <= reach, delta, mods.ArcDeg, true, false, mods.ArcAllies);
        }

        void PlayDeliveryBeat(ArmedBeat beat)
        {
            int prev = _host.SlotQueryCastId;
            bool prevRecoil = _host.CasterRecoilSuppressed;
            _host.SlotQueryCastId = _slotCastId;
            _host.CasterRecoilSuppressed = true;
            try
            {
                switch (beat.Kind)
                {
                    case DeliveryBeatKind.SpawnActors:
                        _host.TryLaunchSkillExecutor(
                            SkillExecutorKind.Summon,
                            _pending,
                            _skill,
                            _motion,
                            1f,
                            null,
                            _slotCastId,
                            0f);
                        break;
                    case DeliveryBeatKind.Resolve:
                        PulseDelivery(_skill, _pending, _motion, beat.Power, true, true);
                        break;
                    case DeliveryBeatKind.Detonate:
                        if (_detonated)
                            break;
                        _detonated = true;
                        float share = _stashed ? Mathf.Max(_stashedShare, 1f) : 1f;
                        PulseDelivery(_skill, _pending, _motion, share * beat.Power, true, true);
                        break;
                    case DeliveryBeatKind.Duplicate:
                        PulseDelivery(_skill, _pending, _motion, beat.Power, false, false);
                        break;
                    case DeliveryBeatKind.Bounce:
                        if (!_host.TryBounceFriendly(beat.Power))
                            PulseDelivery(_skill, _pending, _motion, beat.Power, false, false);
                        break;
                    case DeliveryBeatKind.Pincer:
                        PulseDelivery(_skill, _pending, _motion, beat.Power, false, false);
                        _host.JsonLog($"kıskaç ikinci vuruş ×{beat.Power:0.##}");
                        break;
                    case DeliveryBeatKind.FieldTick:
                        if (!_host.LandingFieldAllows(_skill))
                            break;
                        PulseDelivery(_skill, _pending, _motion, beat.Power, false, false);
                        break;
                    case DeliveryBeatKind.RepeatPrevious:
                        RepeatDelivered(_repeatTarget);
                        break;
                    case DeliveryBeatKind.GlideHaste:
                        ApplyGlideHaste(beat.Power);
                        break;
                }
            }
            finally
            {
                _host.SlotQueryCastId = prev;
                _host.CasterRecoilSuppressed = prevRecoil;
            }
        }

        void PulseDelivery(
            SkillResolution skill,
            PendingClosing pending,
            SkillMotionPlan motion,
            float power,
            bool statuses,
            bool knockback)
        {
            if (skill.IsEmpty || power <= 0f)
                return;
            bool friendly = _host.IsFriendlyFieldVerb(skill) || _host.IsHealSkill(skill);
            MechanicPlan plan = _host.MechanicPlanFor(skill);
            if (!friendly)
            {
                _host.ApplyClosingDamage(
                    pending.Closing,
                    skill,
                    false,
                    motion.SlashCommitMult,
                    power,
                    _host.Motion.TemplateChain);
            }
            else if (_host.IsHealSkill(skill) && GuardTriggerDelivery.AllowImmediate(plan, "can"))
            {
                _host.ApplyClosingHeal(pending.Closing, skill, power, _host.Motion.TemplateChain);
            }

            if (statuses)
            {
                if (GuardTriggerDelivery.AllowImmediate(plan, "kalkan"))
                    _host.ApplyClosingStatuses(pending, skill, bossReached: !friendly);
                // Kuyruk oyuncuyu taşımaz; oyuncuyu yalnız hareket kalıbı taşır.
                if (!friendly)
                    _host.ApplyMechanicHitEffects(
                        plan,
                        _host.Boss != null ? _host.Boss.transform.position : Vector3.zero,
                        casterMoves: false);
            }

            if (knockback && !friendly && _host.Player != null)
                NoteTemplateHostileHit(_host.Player.position);
            _host.LastSkillEffectApplied = true;
        }

        void RepeatDelivered(DeliveredCast previous)
        {
            if (!previous.Valid || previous.Skill.IsEmpty)
                return;
            PulseDelivery(previous.Skill, previous.Pending, previous.Motion, 1f, true, false);
            if (!previous.Spawned)
                return;
            _host.TryLaunchSkillExecutor(
                SkillExecutorKind.Summon,
                previous.Pending,
                previous.Skill,
                previous.Motion,
                1f,
                null,
                previous.SlotCastId,
                0f);
        }

        void ApplyGlideHaste(float magnitude)
        {
            if (_host.PlayerStatus == null || _order == null)
                return;
            float mag = magnitude > 1f ? magnitude : 1.5f;
            float sec = _order.GlideDurationSec > 0.05f ? _order.GlideDurationSec : 3f;
            _host.PlayerStatus.Board.Apply(StatusKind.Haste, sec * 1000.0, mag, "suzulme");
            _host.LastSkillEffectApplied = true;
        }
    
    }
}
