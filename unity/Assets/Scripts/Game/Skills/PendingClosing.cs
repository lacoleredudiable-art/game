using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
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
