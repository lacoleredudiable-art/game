using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Casting
{
    /// <summary>mobility_cc.i_frame satırı — kaynak skill id'si (verb-adjective) ya da dodge.</summary>
    public readonly struct IFrameRule
    {
        public IFrameRule(string source, int durationMs, string condition)
        {
            Source = source ?? string.Empty;
            DurationMs = Math.Max(0, durationMs);
            Condition = condition ?? string.Empty;
        }

        public string Source { get; }
        public int DurationMs { get; }
        public string Condition { get; }
    }
}
