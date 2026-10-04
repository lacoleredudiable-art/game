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
    public sealed class BossAttackEntry
    {
        public string Id = string.Empty;
        public BossAttackKind Kind;
        public IReadOnlyList<string> Mechanics = System.Array.Empty<string>();
        public BossOnHitStatus OnHitStatus;
        public BossFieldSpec? Field;
        public BossLeapSpec? Leap;
    }
}
