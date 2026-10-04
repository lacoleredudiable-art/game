using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        static void JsonLog(string message) => Mechanics.JsonEffectRuntime.JsonLog(message);

        int FriendlyTargetCap(in SkillResolution skill)
        {
            EnsureMechanicsServices();
            return _jsonEffects.FriendlyTargetCap(skill);
        }

        void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
            EnsureMechanicsServices();
            _jsonEffects.NoteJsonCast(skill, closing);
        }

        void TickJsonEffects(double worldMs)
        {
            EnsureMechanicsServices();
            _jsonEffects.Tick(worldMs);
        }

        void ApplyJsonSelfCast(MechanicPlan plan, float reflectRatio, float windowSec, double now)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyJsonSelfCast(plan, reflectRatio, windowSec, now);
        }

        void CaptureBuffOverflow(MechanicPlan plan, double now)
        {
            EnsureMechanicsServices();
            _jsonEffects.CaptureBuffOverflow(plan, now);
        }

        float ConsumeOverflowBonus(bool isBasicStrike)
        {
            EnsureMechanicsServices();
            return _jsonEffects.ConsumeOverflowBonus(isBasicStrike);
        }

        void OnJsonShieldBlocked()
        {
            EnsureMechanicsServices();
            _jsonEffects.OnJsonShieldBlocked();
        }

        void ApplyReflectedDamage(float amount)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyReflectedDamage(amount);
        }

        void ApplyStolenArmor(MechanicEffect e, List<string> applied)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyStolenArmor(e, applied);
        }

        void ApplyStatusAdd(MechanicEffect e, List<string> applied)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyStatusAdd(e, applied);
        }

        void LiftBoss(List<string> applied)
        {
            EnsureMechanicsServices();
            _jsonEffects.LiftBoss(applied);
        }

        void ApplyMirroredDebuff(MechanicPlan plan)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyMirroredDebuff(plan);
        }

        void ApplyPurgePower(in SkillResolution skill, int removed)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyPurgePower(skill, removed);
        }

        void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
            EnsureMechanicsServices();
            _jsonEffects.ShareFriendlyStatuses(skill, applied);
        }

        void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyHealOverflow(skill, amount, healed, toAlly);
        }

        bool TryBounceFriendly(float power)
        {
            EnsureMechanicsServices();
            return _jsonEffects.TryBounceFriendly(power);
        }

        bool LandingFieldAllows(in SkillResolution skill)
        {
            EnsureMechanicsServices();
            return _jsonEffects.LandingFieldAllows(skill);
        }

        bool AoeReachedMotionHit(in MotionHit hit)
        {
            EnsureMechanicsServices();
            return _jsonEffects.AoeReachedMotionHit(hit);
        }

        bool BasicCadenceReady(double now)
        {
            EnsureMechanicsServices();
            return _jsonEffects.BasicCadenceReady(now);
        }

        int BasicHitsNow()
        {
            EnsureMechanicsServices();
            return _jsonEffects.BasicHitsNow();
        }

        void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
            EnsureMechanicsServices();
            _jsonEffects.ScheduleBasicSubHits(closing, hits, reach);
        }

        void ApplyBasicExtras(float dealt, int hits)
        {
            EnsureMechanicsServices();
            _jsonEffects.ApplyBasicExtras(dealt, hits);
        }
    }
}
