using System.Collections.Generic;

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

        bool TryGet(string skillId, out MotionBinding binding);
        bool TryGetTemplate(string templateId, out MotionTemplate template);
        bool TryPlay(string skillId, out MotionTemplate template);
        int CountImplementedFamilies();
        int CountReadySkills();
    }
}
