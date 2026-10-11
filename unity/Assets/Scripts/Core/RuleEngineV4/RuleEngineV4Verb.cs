using System;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4Verb
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public bool Hostile { get; init; }
        /// <summary>Bu fiilin çalışması için gereken en az rün (nokta) sayısı.</summary>
        public int MinComboLen { get; init; }
    }
}
