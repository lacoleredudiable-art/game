using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    /// <summary>Sunum katmanı skill bağırma; <see cref="Context"/> oyun PendingClosing vb.</summary>
    public readonly struct CastSkillShoutRequested
    {
        public CastSkillShoutRequested(SkillResolution skill, object context)
        {
            Skill = skill;
            Context = context;
        }

        public SkillResolution Skill { get; }
        public object Context { get; }
    }
}
