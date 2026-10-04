using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Skills.Presentation;
using System.Collections.Generic;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector : ICastPresentationFeedback
    {
        public void BindCastPresentation(CastPresentationListener listener)
        {
            listener.Bind(_castPipeline, this);
        }

        public void OnCastDenial(CastDenialRequested e)
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

        public void OnCastCompatibility(WeaponSkillCompatibility compatibility) =>
            PublishWeaponCompatibility(compatibility);

        public void OnSkillShout(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            EnsureLaunchServices();
            _skillPresentation.ShoutSkill(skill, words);
        }

        public void OnMotionAnnotation(SkillResolution skill, in SkillMotionPlan plan)
        {
            EnsureLaunchServices();
            _castSideEffects.AnnotateMotion(skill, plan);
        }

        void PublishWeaponCompatibility(WeaponSkillCompatibility compatibility)
        {
            LastWeaponCompatible = compatibility.Compatible;
            LastWeaponPassiveEnabled = compatibility.PassiveEnabled;
            LastWeaponUiLabel = compatibility.UiLabel;
        }
    }
}
