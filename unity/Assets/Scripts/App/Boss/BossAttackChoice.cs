using System;
using Dovus.Core.Shared;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.App.Boss
{
    public readonly struct BossAttackChoice
    {
        public BossAttackChoice(BossAttackKind kind, SlamVariant? variant)
        {
            Kind = kind;
            Variant = variant;
        }

        public BossAttackKind Kind { get; }
        public SlamVariant? Variant { get; }
    }
}
