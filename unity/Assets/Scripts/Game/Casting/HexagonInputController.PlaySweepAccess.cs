#if UNITY_EDITOR
using Dovus.Core.Grammar;
using System;

namespace Dovus.Game.Casting
{
    public sealed partial class HexagonInputController
    {
        public bool SweepInputLocked => _session.InputLocked;

        public Func<SkillResolution, bool> SweepSkillTargetGate => _session.SkillTargetGate;
    }
}
#endif
