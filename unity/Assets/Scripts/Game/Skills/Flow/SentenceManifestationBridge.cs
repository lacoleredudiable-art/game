using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Casting;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Game.Casting;
using Dovus.Game.Skills;
using Dovus.App.Team;
using Dovus.Game.Team;
using System;
using System.Collections.Generic;

namespace Dovus.Game.Skills.Flow
{
    public sealed class SentenceManifestationBridge
    {
        readonly ISentenceManifestationHost _host;

        public SentenceManifestationBridge(ISentenceManifestationHost host) => _host = host;

        public void SyncBuilding(double worldMs)
        {
            var state = _host.Engine.State;
            if (state.Phase != SentencePhase.Building)
                return;

            int count = state.Words.Count;
            if (count == 0)
                return;

            if (_host.BuildingView == null || _host.BuildingView.Logic == null
                || _host.BuildingView.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                _host.BuildingView = _host.EffectSpawner.SpawnEffect(state.Words, worldMs);
                _host.EffectSpawner.PulseActor(state.Words[0].Rune, state.Words, worldMs);
                _host.LastWordCount = count;
                return;
            }

            _host.BuildingView.Logic.SetWords(state.Words);
            _host.EffectSpawner.ApplySkillWorldPlan(_host.BuildingView.Logic, state.Words);
            _host.EffectSpawner.ApplySkillTint(_host.BuildingView, state.Words);
            _host.RefreshBuildingMobility(state.Words);
            if (count > _host.LastWordCount)
                _host.EffectSpawner.PulseActor(state.Words[count - 1].Rune, state.Words, worldMs);

            _host.LastWordCount = count;
        }

        public void OnCompleted(CompletedSentence sentence)
        {
            LivingEffectView view = _host.BuildingView;
            _host.BuildingView = null;
            int basicRune = _host.Colors != null ? _host.Colors.Input.BasicStrikeDot : 1;
            bool sentenceIsBasic = sentence.Words.Count == 1 && (int)sentence.Words[0].Rune == basicRune;
            if (view != null && BasicStrikeInput.ReplaceStaleView(sentenceIsBasic, view.IsBasicStrike))
            {
                if (view.Logic != null)
                    view.Logic.Abort();
                view = null;
            }
            _host.LastWordCount = 0;

            if (sentence.Phase == SentencePhase.Aborted || !sentence.Closing.HasValue)
            {
                if (view != null && view.Logic != null)
                    view.Logic.Abort();
                return;
            }

            bool spawnedForBasicStrike = false;
            if (view == null || view.Logic == null
                || view.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                int basicDot = _host.Colors != null ? _host.Colors.Input.BasicStrikeDot : 1;
                spawnedForBasicStrike = sentence.Words.Count == 1
                    && (int)sentence.Words[0].Rune == basicDot;
                view = _host.EffectSpawner.SpawnEffect(
                    sentence.Words, _host.Clock.Director.WorldTimeMs, spawnedForBasicStrike);
                if (spawnedForBasicStrike)
                {
                    _host.SyncVisualDelivery();
                    _host.Visual?.PulseBasicStrike();
                    _host.TryBeginBasicStrikeStep();
                    _host.Pose?.PulseRune(sentence.Words[0].Rune, _host.Clock.Director.WorldTimeMs);
                }
                else
                {
                    _host.EffectSpawner.PulseActor(
                        sentence.Words[0].Rune, sentence.Words, _host.Clock.Director.WorldTimeMs);
                }
            }

            view.Logic.SetWords(sentence.Words);
            if (!spawnedForBasicStrike)
                _host.EffectSpawner.ApplySkillWorldPlan(view.Logic, sentence.Words);

            ClosingHit closing = sentence.Closing.Value;
            view.Logic.ArmClosing(closing);

            float recoverySec = _host.Combat.Sentence.StepForDots(closing.DotCount).RecoverySec;
            float castMult = 1f;
            SkillResolution armedSkill = SkillResolution.Empty;
            if (!spawnedForBasicStrike && _host.Skills != null)
            {
                armedSkill = _host.ResolveSkillWords(sentence.Words);
                castMult = SkillMobility.CastTimeMult(armedSkill);
                castMult *= _host.WeaponCompatibilityFor(armedSkill).CastTimeMult;
            }

            UnityEngine.Transform pendingTarget = spawnedForBasicStrike ? _host.Aim.CastFacingTarget : null;
            SkillAimMode pendingAimMode = SkillAimMode.Targeted;
            if (!spawnedForBasicStrike && !armedSkill.IsEmpty)
            {
                pendingAimMode = TargetingRules.AimMode(armedSkill);
                if (!string.Equals(_host.Aim.ArmedSkillId, armedSkill.Identity.Id, StringComparison.Ordinal)
                    && !_host.TryArmSkillTarget(armedSkill))
                {
                    view.Logic.Abort();
                    return;
                }
                pendingTarget = _host.Aim.ArmedTarget;
                _host.FaceTarget(_host.Aim.CastFacingTarget);
            }
            TeamModifierHub teamHub = _host.TeamAccess != null ? _host.TeamAccess.Hub : TeamModifierHub.Neutral;
            float atkSpd = teamHub.AttackSpeedMult;
            if (atkSpd > 0f)
                castMult /= atkSpd;
            recoverySec *= castMult;

            double bangAt = _host.Clock.Director.WorldTimeMs
                            + recoverySec * SkillsTimeDefaults.SecToMs
                            + _host.Combat.Feel.PostHitSilenceMs;

            _host.Pose?.BeginRecovery(recoverySec, _host.Clock.Director.WorldTimeMs);
            _host.PosedForRecovery = true;

            if (!spawnedForBasicStrike && !armedSkill.IsEmpty)
            {
                float lockSec = recoverySec + _host.Combat.Feel.PostHitSilenceMs / 1000f;
                _host.ApplyCastMobility(armedSkill, lockSec);
            }

            bool basic = view != null && view.IsBasicStrike;
            _host.Pending.Add(new PendingClosing
            {
                View = view,
                Closing = closing,
                BangAtWorldMs = bangAt,
                Words = new List<SentenceWord>(sentence.Words),
                IsBasicStrike = basic,
                Target = pendingTarget,
                AimMode = pendingAimMode
            });
            _host.Aim.ClearArmedTarget();
        }
    }
}
