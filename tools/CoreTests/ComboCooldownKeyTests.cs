using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ComboCooldownKeyTests
{
    [Test]
    public void For_UsesSkillId_WhenPresent()
    {
        var skill = new SkillResolution(
            elementId: string.Empty,
            elementName: string.Empty,
            displayName: "test",
            skillId: "8-9",
            skillJob: string.Empty,
            verbId: "8",
            verbName: string.Empty,
            verbFamily: string.Empty,
            action: string.Empty,
            baseDamage: 0f,
            basePoise: 0f,
            hitbox: string.Empty,
            castMobility: string.Empty,
            mechanics: System.Array.Empty<string>(),
            adjectiveId: "9",
            adjectiveName: string.Empty,
            silhouetteAxis: string.Empty,
            damageMult: 1f,
            hitboxScaleMult: 1f,
            poiseDamageMult: 1f,
            length: 2,
            lengthRole: string.Empty,
            lengthCastMult: 1f,
            lengthMobility: string.Empty,
            flavorElement: string.Empty);

        Assert.That(ComboCooldownKey.For(skill), Is.EqualTo("8-9"));
    }

    [Test]
    public void For_FallsBackToVerbId_WhenSkillIdEmpty()
    {
        var skill = new SkillResolution(
            elementId: string.Empty,
            elementName: string.Empty,
            displayName: "verb",
            skillId: string.Empty,
            skillJob: string.Empty,
            verbId: "12",
            verbName: string.Empty,
            verbFamily: string.Empty,
            action: string.Empty,
            baseDamage: 0f,
            basePoise: 0f,
            hitbox: string.Empty,
            castMobility: string.Empty,
            mechanics: System.Array.Empty<string>(),
            adjectiveId: string.Empty,
            adjectiveName: string.Empty,
            silhouetteAxis: string.Empty,
            damageMult: 1f,
            hitboxScaleMult: 1f,
            poiseDamageMult: 1f,
            length: 1,
            lengthRole: string.Empty,
            lengthCastMult: 1f,
            lengthMobility: string.Empty,
            flavorElement: string.Empty);

        Assert.That(ComboCooldownKey.For(skill), Is.EqualTo("12"));
    }
}
