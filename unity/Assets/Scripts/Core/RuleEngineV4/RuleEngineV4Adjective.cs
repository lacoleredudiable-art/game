using System;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4Adjective
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public bool ChargeOnHostileOnly { get; init; }
    }
}
