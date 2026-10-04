using Dovus.App.Casting;
using Dovus.Game.Skills;

namespace Dovus.Game.Skills.Presentation
{
    /// <summary>CastPipeline sunum olaylarını tek üretim dinleyicide toplar.</summary>
    public sealed class CastPresentationListener
    {
        bool _wired;

        public void Bind(CastPipeline pipeline, ICastPresentationFeedback feedback)
        {
            if (_wired || pipeline == null || feedback == null)
                return;
            _wired = true;
            pipeline.DenialRequested += feedback.OnCastDenial;
            pipeline.CompatibilityPublished += e => feedback.OnCastCompatibility(e.Compatibility);
            pipeline.SkillShoutRequested += e =>
            {
                if (e.Context is PendingClosing pending)
                    feedback.OnSkillShout(e.Skill, pending.Words);
            };
            pipeline.MotionAnnotationRequested += e => feedback.OnMotionAnnotation(e.Skill, e.Plan);
        }
    }
}
