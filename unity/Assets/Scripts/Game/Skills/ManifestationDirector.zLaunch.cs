using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Launch;
using Dovus.Game.Skills.Presentation;
using Dovus.Game.Skills.State;
using Dovus.App.Casting;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal MdLaunchServicesHost _launchHost;
        internal SkillExecutorLauncher _executorLauncher;
        internal HitboxSizingApplier _hitboxSizing;
        internal CastSideEffects _castSideEffects;
        internal SkillPresentation _skillPresentation;

        internal void EnsureLaunchServices()
        {
            if (_launchHost != null)
                return;
            _launchHost = new MdLaunchServicesHost(this);
            _hitboxSizing = new HitboxSizingApplier(_launchHost);
            _executorLauncher = new SkillExecutorLauncher(_launchHost, _hitboxSizing);
            _castSideEffects = new CastSideEffects(_launchHost);
            _skillPresentation = new SkillPresentation(_launchHost);
        }

        internal PresentationCatalog PresentationCatalog
        {
            get
            {
              EnsureLaunchServices();
                _skillPresentation.EnsureCatalog();
                return _skillPresentation.Catalog;
            }
        }

        internal void EnsurePresentationCatalog()
        {
          EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
        }
        internal bool TryLaunchSkillExecutor(
            SkillExecutorKind kind,
            PendingClosing pending,
            SkillResolution skill,
            in SkillMotionPlan motionPlan,
            float effectMult = 1f,
            LivingEffect capturedLogic = null,
            int slotCastId = -1,
            float? activationDelayOverride = null)
        {
          EnsureLaunchServices();
            return _executorLauncher.TryLaunch(
                kind, pending, skill, motionPlan, effectMult, capturedLogic, slotCastId, activationDelayOverride);
        }

        internal void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount)
        {
          EnsureLaunchServices();
            _hitboxSizing.ApplyVerbHitboxSizing(
                kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);
        }

        void ApplyResourceCost(SkillResolution skill)
        {
          EnsureLaunchServices();
            _castSideEffects.ApplyResourceCost(skill);
        }

        internal void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
          EnsureLaunchServices();
            _castSideEffects.ApplyCastMobility(skill, durationSec);
        }

        internal void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words)
        {
          EnsureLaunchServices();
            _castSideEffects.RefreshBuildingMobility(words);
        }

        void ApplyCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words, bool cosmeticIfDisabled)
        {
          EnsureLaunchServices();
            _castSideEffects.ApplyCooldown(skill, words, cosmeticIfDisabled);
        }

        SkillMotionPlan ResolveSkillMotion(SkillResolution skill)
        {
          EnsureLaunchServices();
            return _castSideEffects.ResolveSkillMotion(skill);
        }

        void ApplySkillMotionIframe(in SkillResolution skill, in SkillMotionPlan plan)
        {
          EnsureLaunchServices();
            _castSideEffects.ApplySkillMotionIframe(skill, plan);
        }

        void AnnotateMotion(SkillResolution skill, in SkillMotionPlan plan)
        {
          EnsureLaunchServices();
            _castSideEffects.AnnotateMotion(skill, plan);
        }

        void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
          EnsureLaunchServices();
            _skillPresentation.ShoutSkill(skill, words);
        }

        void ApplySkillAnimation(SkillResolution skill)
        {
          EnsureLaunchServices();
            _skillPresentation.ApplySkillAnimation(skill);
        }
    }
}
