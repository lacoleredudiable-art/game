using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
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

        internal int FriendlyTargetCap(in SkillResolution skill)
        {
          EnsureMechanicsServices();
            return _jsonEffects.FriendlyTargetCap(skill);
        }

        internal void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
          EnsureMechanicsServices();
            _jsonEffects.NoteJsonCast(skill, closing);
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

        internal void OnJsonShieldBlocked()
        {
          EnsureMechanicsServices();
            _jsonEffects.OnJsonShieldBlocked();
        }

        internal void ApplyReflectedDamage(float amount)
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

        internal void ApplyPurgePower(in SkillResolution skill, int removed)
        {
          EnsureMechanicsServices();
            _jsonEffects.ApplyPurgePower(skill, removed);
        }

        internal void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
          EnsureMechanicsServices();
            _jsonEffects.ShareFriendlyStatuses(skill, applied);
        }

        internal void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
          EnsureMechanicsServices();
            _jsonEffects.ApplyHealOverflow(skill, amount, healed, toAlly);
        }

        internal bool TryBounceFriendly(float power)
        {
          EnsureMechanicsServices();
            return _jsonEffects.TryBounceFriendly(power);
        }

        internal bool LandingFieldAllows(in SkillResolution skill)
        {
          EnsureMechanicsServices();
            return _jsonEffects.LandingFieldAllows(skill);
        }

        internal bool AoeReachedMotionHit(in MotionHit hit)
        {
          EnsureMechanicsServices();
            return _jsonEffects.AoeReachedMotionHit(hit);
        }

        internal bool BasicCadenceReady(double now)
        {
          EnsureMechanicsServices();
            return _jsonEffects.BasicCadenceReady(now);
        }

        internal int BasicHitsNow()
        {
          EnsureMechanicsServices();
            return _jsonEffects.BasicHitsNow();
        }

        internal void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
          EnsureMechanicsServices();
            _jsonEffects.ScheduleBasicSubHits(closing, hits, reach);
        }

        internal void ApplyBasicExtras(float dealt, int hits)
        {
          EnsureMechanicsServices();
            _jsonEffects.ApplyBasicExtras(dealt, hits);
        }
    }
}
