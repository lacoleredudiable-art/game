using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        bool _castPresentationWired;

        public void WireCastPresentationFeedback()
        {
            if (_castPresentationWired)
                return;
            _castPresentationWired = true;

            _castPipeline.DenialRequested += OnCastDenialRequested;
            _castPipeline.CompatibilityPublished += OnCastCompatibilityPublished;
            _castPipeline.SkillShoutRequested += OnSkillShoutRequested;
            _castPipeline.MotionAnnotationRequested += OnMotionAnnotationRequested;
        }

        void OnCastDenialRequested(CastDenialRequested e)
        {
            switch (e.Reason)
            {
                case CastDenialReason.NeedsTwoRunes:
                    _readout?.NoteDenied("2 rün gerekli");
                    break;
                case CastDenialReason.BasicCadenceNotReady:
                    _readout?.NoteDenied("Düz vuruş", "hazır değil");
                    break;
            }
        }

        void OnCastCompatibilityPublished(CastCompatibilityPublished e)
        {
            LastWeaponCompatible = e.Compatibility.Compatible;
            LastWeaponPassiveEnabled = e.Compatibility.PassiveEnabled;
            LastWeaponUiLabel = e.Compatibility.UiLabel;
        }

        void OnSkillShoutRequested(CastSkillShoutRequested e)
        {
            if (e.Context is not PendingClosing pending)
                return;
            EnsureLaunchServices();
            _skillPresentation.ShoutSkill(e.Skill, pending.Words);
        }

        void OnMotionAnnotationRequested(CastMotionAnnotationRequested e)
        {
            EnsureLaunchServices();
            _castSideEffects.AnnotateMotion(e.Skill, e.Plan);
        }
    }
}
