using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Dodge
{
    public readonly struct ExchangeInput
    {
        public int TelegraphStartMs { get; init; }
        public int StrikeTimeMs { get; init; }
        public int? DodgePressMs { get; init; }
        public bool InEffectVolume { get; init; }
    }
}
