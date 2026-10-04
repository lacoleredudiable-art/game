using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public struct PendingClosing
    {
        public LivingEffectView View;
        public ClosingHit Closing;
        public double BangAtWorldMs;
        public List<SentenceWord> Words;
        public bool IsBasicStrike;
        public Transform Target;
        public SkillAimMode AimMode;
    }
}
