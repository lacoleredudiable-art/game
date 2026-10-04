using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Platform;
using Dovus.Core.Tuning;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal ClosingStatusApplier _closingStatus;
        ClosingHealResolver _closingHeal;
        ClosingDamageResolver _closingDamage;
        internal MdClosingHost _closingHost;

        void EnsureClosingServices()
        {
            if (_closingHost != null)
                return;
            _closingHost = new MdClosingHost(this);
            _closingStatus = new ClosingStatusApplier(_closingHost);
            _closingHeal = new ClosingHealResolver(_closingHost);
            _closingDamage = new ClosingDamageResolver(_closingHost);
        }
internal ClosingDamageResolver ClosingDamageCore
        {
            get
            {
                EnsureClosingServices();
                return _closingDamage;
            }
        }

        internal void SpawnClosingImpact(PendingClosing p)
        {
          EnsureCoreServices();
            _closingQueue.SpawnClosingImpact(p);
        }

        internal SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words)
        {
          EnsureCoreServices();
            return _closingQueue.ResolveSkillWords(words);
        }

        internal SkillResolution ResolvePendingSkill(PendingClosing p)
        {
          EnsureCoreServices();
            return _closingQueue.ResolvePendingSkill(p);
        }

        internal static bool IsFriendlyFieldVerb(in SkillResolution skill) =>
            SkillVerbRouting.IsFieldAuraVerb(skill.Identity.Verb);

        internal void ApplyClosingStatuses(PendingClosing p, SkillResolution skill, bool bossReached = true)
        {
            EnsureClosingServices();
            _closingStatus.Apply(p.Target, skill, bossReached);
        }

        internal void ApplyClosingHeal(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale = 1f,
            float? chainBonusOverride = null,
            Vector3? fieldCenter = null,
            float fieldRadiusM = 0f)
        {
            EnsureClosingServices();
            _closingHeal.Apply(closing, skill, effectScale, chainBonusOverride, fieldCenter, fieldRadiusM);
        }

        internal int CalculateClosingHealAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride)
        {
            EnsureClosingServices();
            return _closingHeal.CalculateAmount(closing, skill, effectScale, chainBonusOverride);
        }

        internal void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget = null)
        {
            EnsureClosingServices();
            _closingHeal.ApplyAmount(skill, amount, fieldCenter, fieldRadiusM, preferredTarget);
        }

        internal float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale = 1f,
            float? chainBonusOverride = null)
        {
            EnsureClosingServices();
            return _closingDamage.Apply(
                closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
        }

        internal void StampScar(LivingEffectView view, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.StampScar(view, closing);
        }

        internal void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosingBasic(logic, closing);
        }

        internal void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosing(logic, closing, skill);
        }

        internal bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.IsBossInStrikeCapsule(logic, reachM);
        }

        internal bool BasicTargetStillInReach(Transform target, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.BasicTargetStillInReach(target, reachM);
        }

        internal float BasicStrikeYawDeg(Transform target)
        {
            EnsureClosingServices();
            return _closingDamage.BasicStrikeYawDeg(target);
        }

        internal bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            return _closingDamage.IsClosingInRange(logic, closing);
        }

        internal static bool IsHealSkill(SkillResolution skill) => ClosingHealRules.IsHealSkill(skill);
    }
}
