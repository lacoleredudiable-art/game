using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    public readonly struct BossOnHitStatus
    {
        public BossOnHitStatus(string id, float durationSec)
        {
            Id = id ?? string.Empty;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public float DurationSec { get; }
        public bool IsValid => !string.IsNullOrEmpty(Id);
    }
}
