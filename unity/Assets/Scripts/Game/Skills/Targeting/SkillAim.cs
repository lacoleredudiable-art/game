using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Core.Equipment;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Targeting
{
    public sealed class SkillAim
    {
        readonly ISkillAimHost _host;

        public SkillAim(ISkillAimHost host) => _host = host;

        public Transform ArmedTarget { get; private set; }
        public string ArmedSkillId { get; private set; } = string.Empty;
        public Transform CastFacingTarget { get; private set; }
        public bool DirectionalAttack { get; set; }

        public void ResetDirectionalWhenIdle() => DirectionalAttack = false;

        public Transform CurrentFacingTarget
        {
            get
            {
                switch (FacingKind())
                {
                    case AttackFaceKind.LockedTarget:
                        return AttackLockTarget();
                    case AttackFaceKind.Movement:
                        return _host.Targeting != null ? _host.Targeting.SelectedTransform : null;
                    default:
                        return null;
                }
            }
        }

        public bool CombatFacingLocked =>
            FacingKind() != AttackFaceKind.Movement || CurrentFacingTarget != null;

        AttackFaceKind FacingKind()
        {
            bool performing = _host.PerformingAttack;
            return AttackFacingRules.Resolve(
                performing,
                performing && DirectionalAttack,
                AttackLockTarget() != null);
        }

        public Transform AttackLockTarget()
        {
            if (CastFacingTarget != null && CastFacingTarget != _host.Player)
                return CastFacingTarget;
            return _host.Targeting != null ? _host.Targeting.SelectedTransform : null;
        }

        public void FaceAim(in SkillResolution skill)
        {
            if (_host.Player == null)
                return;
            bool directional = !skill.IsEmpty
                && TargetingRules.AimMode(skill) == SkillAimMode.Directional;
            DirectionalAttack = directional;
            if (directional)
            {
                Vector3 aim = ResolveAimFacing(_host.Player.position);
                if (aim.sqrMagnitude < 0.0001f)
                    return;
                _host.Player.rotation = Quaternion.LookRotation(aim, Vector3.up);
                return;
            }

            FaceTarget(AttackLockTarget());
        }

        public void FaceTarget(Transform target)
        {
            if (_host.Player == null || target == null || target == _host.Player)
                return;
            FacePoint(target.position);
        }

        public void FacePoint(Vector3 point)
        {
            if (_host.Player == null)
                return;
            Vector3 to = point - _host.Player.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                _host.Player.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        /// <summary>v4 basılı tutma: tek seferlik seçilen hedefe dön; cast boyunca yüz ona kilitli.</summary>
        public void FaceCastTarget(Transform target)
        {
            CastFacingTarget = target != _host.Player ? target : null;
            FaceTarget(target);
        }

        /// <summary>v4 basılı tutma: seçilen yüzey noktasına dön.</summary>
        public void FaceCastPoint(Vector3 point)
        {
            CastFacingTarget = null;
            FacePoint(point);
        }

        public void CaptureBasicFacing()
        {
            CastFacingTarget = null;
            if (_host.Targeting == null)
                return;

            float reach = BasicStrikeWeaponReachM();
            float aimGate = StrikeCapsule.CenterRange(_host.PlayerBodyRadiusM(), reach);

            Transform selected = _host.Targeting.SelectedTransform;
            if (selected != null && selected != _host.Player && _host.IsEnemyBody(selected)
                && EnemyWithinBasicReach(selected, reach))
            {
                CastFacingTarget = selected;
                return;
            }

            if (TryFindBossInRange(reach, out Transform boss))
                CastFacingTarget = boss;
            else if (_host.Targeting.TryResolveBasicEnemy(aimGate, out Transform auto) && auto != null)
                CastFacingTarget = auto;
        }

        /// <summary>Kuşanılı silahın düz vuruş menzili; yoksa fiil-1 jab yedeği.</summary>
        public float BasicStrikeWeaponReachM()
        {
            float fallback = _host.Combat != null
                ? _host.Combat.Manifestation.BasicStrikeRangeM
                : SkillNumberFallbacks.RangeM;
            EquipmentItem weapon = _host.EquippedWeapon;
            if (weapon?.Profile != null && weapon.Profile.BasicReachM > 0f)
                return weapon.Profile.BasicReachM;
            return fallback;
        }

        /// <summary>Yay gibi menzilli düz vuruş: ok/ volley profili.</summary>
        public bool BasicStrikeUsesMissileDelivery()
        {
            WeaponCombatProfile profile = _host.EquippedWeapon?.Profile;
            if (profile == null)
                return false;
            return profile.HitShape == "arrow" || profile.BasicKind == "volley";
        }

        public bool TryFindBossInRange(float reachM, out Transform target)
        {
            target = null;
            if (_host.Boss == null || _host.Player == null)
                return false;
            if (!StrikeCapsule.EdgeInReach(
                    PlanarEdgeDistance(_host.Boss.transform),
                    _host.PlayerBodyRadiusM(),
                    reachM))
                return false;
            target = _host.Boss.transform;
            return true;
        }

        bool EnemyWithinBasicReach(Transform enemy, float reachM)
        {
            if (_host.Player == null || enemy == null)
                return false;
            return StrikeCapsule.EdgeInReach(
                PlanarEdgeDistance(enemy),
                _host.PlayerBodyRadiusM(),
                reachM);
        }

        float PlanarEdgeDistance(Transform body)
        {
            TargetableHost mark = body.GetComponent<TargetableHost>();
            if (mark == null)
                mark = body.GetComponentInParent<TargetableHost>();
            if (mark != null)
                return mark.DistanceFrom(_host.Player.position);
            return PlanarMath.FlatDistance(
                _host.Player.position.x,
                _host.Player.position.z,
                body.position.x,
                body.position.z);
        }

        public Vector3 FacingOrBody(Transform target, Vector3 pos)
        {
            if (target != null && target != _host.Player)
            {
                Vector3 to = target.position - pos;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                    return to.normalized;
            }
            return FlatBodyForward();
        }

        public Vector3 FlatBodyForward()
        {
            Vector3 facing = _host.Player != null ? _host.Player.forward : Vector3.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return facing.normalized;
        }

        public bool TryArmSkillTarget(SkillResolution skill)
        {
            SkillAimMode aimMode = TargetingRules.AimMode(skill);
            float range = TargetingRangeFor(skill);
            Transform target;
            TargetFailure failure;
            bool allowed = _host.Targeting != null
                ? _host.Targeting.TryResolve(skill, range, out target, out failure)
                : TryResolveLegacyTarget(skill, aimMode, range, out target, out failure);
            if (!allowed)
            {
                if (failure == TargetFailure.OutOfRange && target != null
                    && aimMode == SkillAimMode.Targeted)
                {
                    ArmedTarget = target;
                    ArmedSkillId = skill.Identity.Id;
                    DirectionalAttack = false;
                    CastFacingTarget = target != _host.Player ? target : null;
                    FaceTarget(AttackLockTarget());
                    return true;
                }

                ArmedTarget = null;
                ArmedSkillId = string.Empty;
                CastFacingTarget = null;
                _host.Readout?.NoteDenied(
                    failure == TargetFailure.OutOfRange ? "menzil dışı" : "hedef yok",
                    "mana ve soğuma harcanmadı");
                return false;
            }

            ArmedTarget = target;
            ArmedSkillId = skill.Identity.Id;
            DirectionalAttack = aimMode == SkillAimMode.Directional;
            CastFacingTarget = aimMode == SkillAimMode.Targeted && target != _host.Player
                ? target
                : null;
            if (DirectionalAttack)
                FaceAim(skill);
            else
                FaceTarget(AttackLockTarget());
            return true;
        }

        public void ClearArmedTarget()
        {
            ArmedTarget = null;
            ArmedSkillId = string.Empty;
        }

        bool TryResolveLegacyTarget(
            in SkillResolution skill,
            SkillAimMode aimMode,
            float range,
            out Transform target,
            out TargetFailure failure)
        {
            failure = TargetFailure.None;
            if (aimMode != SkillAimMode.Targeted || skill.Targeting.Mode == "self_only"
                || skill.Targeting.Mode == "self_or_ally")
            {
                target = _host.Player;
                return true;
            }
            if (_host.Boss != null && PlanarMath.FlatDistance(
                    _host.Player.position.x, _host.Player.position.z,
                    _host.Boss.transform.position.x, _host.Boss.transform.position.z) <= range)
            {
                target = _host.Boss.transform;
                return true;
            }
            if (_host.Boss != null)
            {
                target = _host.Boss.transform;
                failure = TargetFailure.OutOfRange;
                return false;
            }
            target = null;
            failure = TargetFailure.NoTarget;
            return false;
        }

        public float TargetingRangeFor(in SkillResolution skill)
        {
            if (CardEffectRules.PrefersAlly(skill.Targeting.Mode, skill.Presentation.Action))
            {
                float allyRange = _host.SkillNumbers != null
                    ? _host.SkillNumbers.AllySkillRangeM
                    : SkillNumberFallbacks.AllySkillRangeM;
                return MotionCastReach.GateRangeM(
                    Mathf.Max(SkillAimDefaults.MinRadiusM, CardEffectRules.ResolveRange(true, allyRange, 0f)),
                    _host.PlayerBodyRadiusM());
            }

            _host.EnsurePresentationCatalog();
            ManifestationTuning tuning = _host.Combat != null
                ? _host.Combat.Manifestation
                : new ManifestationTuning();
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _host.PresentationCatalog, tuning);
            float rangeMult = _host.EquippedWeapon != null ? _host.EquippedWeapon.RangeMult : 1f;
            float radius = plan.BangRadiusM > 0f ? plan.BangRadiusM : tuning.TravelHitRadiusM;
            float range = Mathf.Max(radius, plan.MaxRangeM * rangeMult);
            float edge = Mathf.Max(SkillAimDefaults.MinRadiusM, range);
            if (!skill.IsEmpty
                && _host.TryGetMotionBinding(skill.Identity.Id, out MotionBinding motion)
                && motion.Implemented)
                edge = MotionCastReach.ComboEdgeReach(edge, motion.Template);
            return MotionCastReach.GateRangeM(edge, _host.PlayerBodyRadiusM());
        }

        public Vector3 ResolveAimFacing(Vector3 pos)
        {
            float velX = 0f;
            float velZ = 0f;
            if (_host.Motor != null)
            {
                Vector3 vel = _host.Motor.Velocity;
                velX = vel.x;
                velZ = vel.z;
            }

            Vector3 playerForward = _host.Player != null ? _host.Player.forward : Vector3.forward;
            float softRange = _host.Colors != null ? _host.Colors.Camera.SoftAimRangeM : SkillAimDefaults.SoftAimRangeFallbackM;
            float softCone = _host.Colors != null ? _host.Colors.Camera.SoftAimConeDeg : SkillAimDefaults.SoftAimConeFallbackDeg;
            bool hasBoss = _host.Boss != null;
            float bossX = hasBoss ? _host.Boss.transform.position.x : 0f;
            float bossZ = hasBoss ? _host.Boss.transform.position.z : 0f;

            SoftAimResolver.Resolve(
                playerForward.x,
                playerForward.z,
                velX,
                velZ,
                SkillAimDefaults.MinAimEdgeM,
                hasBoss,
                bossX,
                bossZ,
                pos.x,
                pos.z,
                softRange,
                softCone,
                out float facingX,
                out float facingZ);

            return new Vector3(facingX, 0f, facingZ);
        }
    }
}
