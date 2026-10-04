using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.Motion
{
    public interface IMotionTemplateRepository
    {
        MotionFallbacks Fallbacks { get; }
        MotionAnimTable Anims { get; }
        int SkillCount { get; }
        int TemplateCount { get; }
        int FamilyCount { get; }
        IReadOnlyList<MotionTemplate> Templates { get; }
        MotionTemplate BasicStrike { get; }

        bool TryGet(SkillId skillId, out MotionBinding binding);
        bool TryGetTemplate(string templateId, out MotionTemplate template);
        bool TryPlay(SkillId skillId, out MotionTemplate template);
        int CountImplementedFamilies();
        int CountReadySkills();
    }
}
