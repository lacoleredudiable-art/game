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
    public sealed class BossHudSnapshot
    {
        public string Name { get; set; } = "BOSS";
        public string Subtitle { get; set; } = string.Empty;
        public List<(int Phase, string Name, float UpperFrac)> Phases { get; } = new();
        public Dictionary<string, string> AttackNamesById { get; } = new();
        public Dictionary<BossAttackKind, string> AttackNamesByKind { get; } = new();
    }
}
