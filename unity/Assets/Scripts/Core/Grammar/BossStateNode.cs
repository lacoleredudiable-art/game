using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>docs/element-sistemi.json state_machine.boss_states[id] — bkz. ParseStateMachine.</summary>
    public readonly struct BossStateNode
    {
        public BossStateNode(string id, string[] exitsTo, bool hasDuration, float durationSec)
        {
            Id = id ?? string.Empty;
            ExitsTo = exitsTo ?? Array.Empty<string>();
            HasDuration = hasDuration;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public IReadOnlyList<string> ExitsTo { get; }
        public bool HasDuration { get; }
        public float DurationSec { get; }
    }
}
