using Dovus.Core.Casting;
using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    public readonly struct CastMotionAnnotationRequested
    {
        public CastMotionAnnotationRequested(SkillResolution skill, in SkillMotionPlan plan)
        {
            Skill = skill;
            Plan = plan;
        }

        public SkillResolution Skill { get; }
        public SkillMotionPlan Plan { get; }
    }
}
