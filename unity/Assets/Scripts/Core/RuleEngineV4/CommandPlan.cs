using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class CommandPlan
    {
        public CommandPlan(
            int verbRune,
            int adjectiveRune,
            int weaponId,
            IReadOnlyList<PhysicsCommand> commands,
            RuleEngineV4TargetResolution? target = null)
        {
            VerbRune = verbRune;
            AdjectiveRune = adjectiveRune;
            WeaponId = weaponId;
            Commands = commands;
            Target = target;
        }

        public int VerbRune { get; }
        public int AdjectiveRune { get; }
        public int WeaponId { get; }
        public IReadOnlyList<PhysicsCommand> Commands { get; }
        public RuleEngineV4TargetResolution? Target { get; }

        public bool IsValid => Commands.Count > 0;
    }
}
