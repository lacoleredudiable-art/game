using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal void NoteJsonCast(in SkillResolution skill, ClosingHit closing)
        {
        }

        internal int FriendlyTargetCap(in SkillResolution skill) => 1;

        void ApplyJsonSelfCast(MechanicPlan plan, float reflectRatio, float windowSec, double now)
        {
        }

        void CaptureBuffOverflow(MechanicPlan plan, double now)
        {
        }

        float ConsumeOverflowBonus(bool isBasicStrike) => 0f;

        internal void OnJsonShieldBlocked()
        {
        }

        internal void ApplyReflectedDamage(float amount)
        {
        }

        void ApplyStolenArmor(MechanicEffect e, List<string> applied)
        {
        }

        void ApplyStatusAdd(MechanicEffect e, List<string> applied)
        {
        }

        void LiftBoss(List<string> applied)
        {
        }

        internal void ApplyPurgePower(in SkillResolution skill, int removed)
        {
        }

        internal int JsonCleanseCount(SkillResolution skill) => 0;

        internal MechanicPlan MechanicPlanFor(SkillResolution skill) => default;

        static bool HasSelfReflect(MechanicPlan plan) => false;

        internal void ShareFriendlyStatuses(in SkillResolution skill, StatusBoard applied)
        {
        }

        internal void ApplyHealOverflow(in SkillResolution skill, int amount, int healed, bool toAlly)
        {
        }

        internal bool TryBounceFriendly(float power) => false;

        internal bool LandingFieldAllows(in SkillResolution skill) => true;

        internal bool BasicCadenceReady(double now)
        {
            if (_combat == null)
                return true;
            double last = SkillWorld.LastBasicStrikeMs;
            float gapSec = _skillNumbers != null
                && _skillNumbers.TryGetVerb(1, out _, out float cd, out _, out _, out _, out _)
                ? cd
                : (_skillNumbers?.GlobalCooldownSec ?? 1f);
            double gap = gapSec * Dovus.Core.Shared.Units.SecToMs;
            return now - last >= gap;
        }

        internal int BasicHitsNow() => 1;

        internal void ScheduleBasicSubHits(ClosingHit closing, int hits, float reach)
        {
        }

        internal void ApplyBasicExtras(float dealt, int hits)
        {
        }

        internal bool IsEnemyBody(Transform body)
        {
            if (body == null)
                return false;
            if (_boss != null && body == _boss.transform)
                return true;
            if (_liveTargetables != null)
            {
                foreach (TargetableHost t in _liveTargetables.Live)
                {
                    if (t != null && t.transform == body)
                        return true;
                }
            }
            return false;
        }
    }
}
