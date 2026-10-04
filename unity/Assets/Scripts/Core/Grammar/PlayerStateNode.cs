using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>docs/element-sistemi.json state_machine.player_states[id] — bkz. ParseStateMachine.</summary>
    public readonly struct PlayerStateNode
    {
        public PlayerStateNode(
            string id, string canDraw, string canMove, string canDodge,
            bool iFrames, bool interruptible = false, string note = "", string canSwap = "false")
        {
            Id = id ?? string.Empty;
            CanDraw = canDraw ?? "false";
            CanMove = canMove ?? "false";
            CanDodge = canDodge ?? "false";
            CanSwap = canSwap ?? "false";
            IFrames = iFrames;
            Interruptible = interruptible;
            Note = note ?? string.Empty;
        }

        public string Id { get; }
        /// <summary>"true" / "false" / "partial".</summary>
        public string CanDraw { get; }
        /// <summary>"true" / "false" / "based_on_cast_mobility" / "limited" / "dodge_direction".</summary>
        public string CanMove { get; }
        /// <summary>"true" / "false" (JSON'da yoksa "false").</summary>
        public string CanDodge { get; }
        /// <summary>"true" / "false" (JSON'da yoksa "false") — savaş içi silah swap.</summary>
        public string CanSwap { get; }
        public bool IFrames { get; }
        public bool Interruptible { get; }
        public string Note { get; }
    }
}
