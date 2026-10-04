using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.App.Actors
{
    public readonly struct HpLossResult
    {
        public HpLossResult(HpLossKind kind) => Kind = kind;
        public HpLossKind Kind { get; }
    }
}
