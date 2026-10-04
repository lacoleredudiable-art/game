using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;
namespace Dovus.Game.Casting.Input
{
    public enum FingerMode
        {
            None,
            CenterPending,
            SwapPending,
            Drawing
        }
    }
