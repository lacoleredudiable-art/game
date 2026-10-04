using System.Collections.Generic;
using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;

namespace Dovus.Game.Skills.Presentation
{
    /// <summary>CastPipeline sunum olaylarının Game katmanı tüketicisi (ManifestationDirector dışında bağlanır).</summary>
    public interface ICastPresentationFeedback
    {
        void OnCastDenial(CastDenialRequested e);
        void OnCastCompatibility(WeaponSkillCompatibility compatibility);
        void OnSkillShout(SkillResolution skill, IReadOnlyList<SentenceWord> words);
        void OnMotionAnnotation(SkillResolution skill, in SkillMotionPlan plan);
    }
}
