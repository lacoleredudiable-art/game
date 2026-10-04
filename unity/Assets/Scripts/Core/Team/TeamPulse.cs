using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Team
{
    public readonly struct TeamPulse
    {
        public TeamPulse(
            bool stunned,
            float stunSec,
            float bossIncoming,
            float armorSec,
            bool burned,
            bool attackBroken,
            float mineMult,
            string copiedSkill)
        {
            Stunned = stunned;
            StunSec = stunSec;
            BossIncomingMult = bossIncoming;
            ArmorSec = armorSec;
            Burned = burned;
            AttackBroken = attackBroken;
            MineMult = mineMult;
            CopiedSkill = copiedSkill ?? string.Empty;
        }

        public bool Stunned { get; }
        public float StunSec { get; }
        public float BossIncomingMult { get; }
        public float ArmorSec { get; }
        public bool Burned { get; }
        public bool AttackBroken { get; }
        public float MineMult { get; }
        public string CopiedSkill { get; }

        public static TeamPulse None => new TeamPulse(false, 0f, 1f, 0f, false, false, 0f, string.Empty);
    }
}
