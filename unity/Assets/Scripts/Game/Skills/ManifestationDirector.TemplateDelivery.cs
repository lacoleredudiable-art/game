using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// Hareket kalıbı vuruşu teslimi bitirmez. Gramer planının kuyruğu
    /// (çağrı, yankı, gecikme, sekme, akış) kalıpla birlikte çalışır.
    /// Skill kimliğiyle dal yok; kuyruk <see cref="TemplateDelivery"/> kurar.
    /// </summary>
    public sealed partial class ManifestationDirector
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

        readonly List<ArmedBeat> _deliveryBeats = new();
        TemplateDeliveryOrder _deliveryOrder;
        double _deliveryStartMs;
        int _deliverySlotCastId;
        float _deliveryStashedShare;
        bool _deliveryStashed;
        bool _deliveryDetonated;
        bool _templateHostileNoted;
        SkillResolution _deliverySkill;
        PendingClosing _deliveryPending;
        SkillMotionPlan _deliveryMotion;
        DeliveredCast _previousDelivered;
        DeliveredCast _repeatTarget;

        void ArmTemplateDelivery(SkillResolution skill, PendingClosing pending, SkillMotionPlan motion)
        {
            _deliveryBeats.Clear();
            _deliveryOrder = null;
            _deliveryStashedShare = 0f;
            _deliveryStashed = false;
            _deliveryDetonated = false;
            _templateHostileNoted = false;
            _deliverySkill = skill;
            _deliveryPending = pending;
            _deliveryMotion = motion;
            _deliverySlotCastId = _slotQueryCastId;
            _repeatTarget = _previousDelivered;

            MotionCatalog.TryPlay(skill.SkillId, out MotionTemplate template);
            float tick = _combat != null ? _combat.Manifestation.ExecutorFieldTickSec : 1f;
            _deliveryOrder = TemplateDelivery.Build(
                LastMechanicPlan,
                skill.EngineModifiers,
                template,
                MechanicEngine != null ? MechanicEngine.Rules : null,
                tick);
            _deliveryStartMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            foreach (DeliveryBeat beat in _deliveryOrder.Schedule())
            {
                _deliveryBeats.Add(new ArmedBeat
                {
                    DueMs = _deliveryStartMs + beat.AtSec * 1000.0,
                    Kind = beat.Kind,
                    Power = beat.Power
                });
            }

            FlushTemplateDelivery(_deliveryStartMs);
            _previousDelivered = new DeliveredCast
            {
                Valid = true,
                Spawned = _deliveryOrder.SpawnActors,
                Skill = skill,
                Pending = pending,
                Motion = motion,
                SlotCastId = _deliverySlotCastId
            };
        }

        void TickTemplateDelivery(double worldMs) => FlushTemplateDelivery(worldMs);

        void FlushTemplateDelivery(double worldMs)
        {
            for (int i = 0; i < _deliveryBeats.Count;)
            {
                if (worldMs + 0.5 < _deliveryBeats[i].DueMs)
                {
                    i++;
                    continue;
                }

                ArmedBeat beat = _deliveryBeats[i];
                _deliveryBeats.RemoveAt(i);
                PlayDeliveryBeat(beat);
            }
        }

        bool ShouldDeferTemplateGameplay(double now)
        {
            if (_deliveryOrder == null || _deliveryOrder.OpeningPulse)
                return false;
            if (!_deliveryOrder.DelayedMark && !_deliveryOrder.RiseDelay)
                return false;
            if (_deliveryDetonated)
                return false;
            double due = _deliveryStartMs + System.Math.Max(0.05, _deliveryOrder.ActivationDelaySec) * 1000.0;
            return now + 1.0 < due;
        }

        bool TemplateHitAlreadyDetonated() =>
            _deliveryDetonated
            && _deliveryOrder != null
            && !_deliveryOrder.OpeningPulse
            && (_deliveryOrder.DelayedMark || _deliveryOrder.RiseDelay);

        void NoteTemplateHostileHit(Vector3 origin)
        {
            if (_templateHostileNoted)
                return;
            _templateHostileNoted = true;
            TryTemplateKnockback();
            TryLandWeaponStun(_deliverySkill.IsEmpty ? _templateSkill : _deliverySkill, false);
            TryTemplateCannonSplash(origin);
        }

        /// <summary>
        /// İtme her zaman oyuncudan dışarı. Vuruş noktası boss'un üstünde ya da ötesinde
        /// olabilir; oradan itmek boss'u oyuncuya yollar (1-6, 5-2).
        /// </summary>
        void TryTemplateKnockback()
        {
            if (_deliveryOrder == null || !_deliveryOrder.BossKnockback)
                return;
            if (_boss == null || _player == null || (_bossVitals != null && _bossVitals.IsDown))
                return;
            if (_boss.PullActive)
                return;
            Vector3 from = _player.position;
            SkillResolution skill = _deliverySkill.IsEmpty ? _templateSkill : _deliverySkill;
            float push = _deliveryOrder.PushM;
            bool landingWave = push > 0f && JsonEffectRules.LandingWavePush(MechanicPlanFor(skill));
            if (!skill.IsEmpty && StatusApplicator.IsSelfTargeted(skill) && !landingWave)
                return;
            float knock = _combat != null ? _combat.Manifestation.BossKnockbackM : 1.35f;
            // JSON itme mesafesi yalnız zorla yer değiştirmeye izin varken; yoksa eski genel itme.
            if (push > 0f && ForcedDisplacement.Allows(_bossStatus != null ? _bossStatus.Board : null))
            {
                knock = push;
                JsonLog($"itme {knock:0.##}m" + (landingWave ? " (iniş dalgası)" : ""));
            }
            float shake = _combat != null ? _combat.Manifestation.BossShakeSec * 0.45f : 0.12f;
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            _boss.React(from, knock, 0.05f, shake, now);
        }

        void TryTemplateCannonSplash(Vector3 origin)
        {
            WeaponCombatProfile profile = EquippedProfile;
            if (profile == null || profile.HitShape != "ballistic")
                return;
            float splash = profile.BasicRadiusM > 0f ? profile.BasicRadiusM : 3f;
            float arena = _motor != null ? _motor.Tuning.ArenaHalfSizeM : 50f;
            float bossR = _boss != null && _boss.BodyRadiusM > 0.01f ? _boss.BodyRadiusM : 0.85f;
            PushCannonBodies(origin.x, origin.z, splash, arena, bossR);
        }

        bool WeaponArcConnects(in MotionHit hit)
        {
            SkillResolution skill = _templateSkill.IsEmpty ? _deliverySkill : _templateSkill;
            WeaponPassiveMods mods = HitMods(skill, false, false);
            if (mods.ArcDeg <= 0f || _boss == null)
                return false;
            Vector3 origin = new Vector3(hit.OriginX, 0f, hit.OriginZ);
            Vector3 to = _boss.transform.position - origin;
            to.y = 0f;
            Vector3 dir = new Vector3(hit.DirX, 0f, hit.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                dir = _player != null ? _player.forward : Vector3.forward;
            float delta = to.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(dir, to);
            float reach = Mathf.Max(hit.LengthM, hit.RadiusM) + BossBodyRadius();
            return MeleeArc.Hits(to.magnitude <= reach, delta, mods.ArcDeg, true, false, mods.ArcAllies);
        }

        void PlayDeliveryBeat(ArmedBeat beat)
        {
            int prev = _slotQueryCastId;
            bool prevRecoil = _casterRecoilSuppressed;
            _slotQueryCastId = _deliverySlotCastId;
            _casterRecoilSuppressed = true;
            try
            {
                switch (beat.Kind)
                {
                    case DeliveryBeatKind.SpawnActors:
                        TryLaunchSkillExecutor(
                            SkillExecutorKind.Summon,
                            _deliveryPending,
                            _deliverySkill,
                            _deliveryMotion,
                            1f,
                            null,
                            _deliverySlotCastId,
                            0f);
                        break;
                    case DeliveryBeatKind.Resolve:
                        PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, beat.Power, true, true);
                        break;
                    case DeliveryBeatKind.Detonate:
                        if (_deliveryDetonated)
                            break;
                        _deliveryDetonated = true;
                        float share = _deliveryStashed ? Mathf.Max(_deliveryStashedShare, 1f) : 1f;
                        PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, share * beat.Power, true, true);
                        break;
                    case DeliveryBeatKind.Duplicate:
                        PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, beat.Power, false, false);
                        break;
                    case DeliveryBeatKind.Bounce:
                        if (!TryBounceFriendly(beat.Power))
                            PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, beat.Power, false, false);
                        break;
                    case DeliveryBeatKind.Pincer:
                        PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, beat.Power, false, false);
                        JsonLog($"kıskaç ikinci vuruş ×{beat.Power:0.##}");
                        break;
                    case DeliveryBeatKind.FieldTick:
                        if (!LandingFieldAllows(_deliverySkill))
                            break;
                        PulseDelivery(_deliverySkill, _deliveryPending, _deliveryMotion, beat.Power, false, false);
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
                _slotQueryCastId = prev;
                _casterRecoilSuppressed = prevRecoil;
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
            bool friendly = IsFriendlyFieldVerb(skill) || IsHealSkill(skill);
            MechanicPlan plan = MechanicPlanFor(skill);
            if (!friendly)
            {
                ApplyClosingDamage(
                    pending.Closing,
                    skill,
                    false,
                    motion.SlashCommitMult,
                    power,
                    _templateChain);
            }
            else if (IsHealSkill(skill) && GuardTriggerDelivery.AllowImmediate(plan, "can"))
            {
                ApplyClosingHeal(pending.Closing, skill, power, _templateChain);
            }

            if (statuses)
            {
                if (GuardTriggerDelivery.AllowImmediate(plan, "kalkan"))
                    ApplyClosingStatuses(pending, skill, bossReached: !friendly);
                // Kuyruk oyuncuyu taşımaz; oyuncuyu yalnız hareket kalıbı taşır.
                if (!friendly)
                    ApplyMechanicHitEffects(
                        plan,
                        _boss != null ? _boss.transform.position : Vector3.zero,
                        casterMoves: false);
            }

            if (knockback && !friendly && _player != null)
                NoteTemplateHostileHit(_player.position);
            LastSkillEffectApplied = true;
        }

        void RepeatDelivered(DeliveredCast previous)
        {
            if (!previous.Valid || previous.Skill.IsEmpty)
                return;
            PulseDelivery(previous.Skill, previous.Pending, previous.Motion, 1f, true, false);
            if (!previous.Spawned)
                return;
            TryLaunchSkillExecutor(
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
            if (_playerStatus == null || _deliveryOrder == null)
                return;
            float mag = magnitude > 1f ? magnitude : 1.5f;
            float sec = _deliveryOrder.GlideDurationSec > 0.05f ? _deliveryOrder.GlideDurationSec : 3f;
            _playerStatus.Board.Apply(StatusKind.Haste, sec * 1000.0, mag, "suzulme");
            LastSkillEffectApplied = true;
        }
    }
}
