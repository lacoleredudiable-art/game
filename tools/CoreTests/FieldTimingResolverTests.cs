using Dovus.App.Casting;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class FieldTimingResolverTests
{
    static SkillResolution SkillWithEngine() => new(
        elementId: string.Empty,
        elementName: string.Empty,
        displayName: "t",
        skillId: "1-1",
        skillJob: string.Empty,
        verbId: "1",
        verbName: string.Empty,
        verbFamily: string.Empty,
        action: string.Empty,
        baseDamage: 0f,
        basePoise: 0f,
        hitbox: string.Empty,
        castMobility: string.Empty,
        mechanics: System.Array.Empty<string>(),
        adjectiveId: "1",
        adjectiveName: string.Empty,
        silhouetteAxis: string.Empty,
        damageMult: 1f,
        hitboxScaleMult: 1f,
        poiseDamageMult: 1f,
        length: 2,
        lengthRole: string.Empty,
        lengthCastMult: 1f,
        lengthMobility: string.Empty,
        flavorElement: string.Empty,
        engineModifiers: JsonValue.Null);

    [Test]
    public void CatalogLifetimeAndTickRateApply()
    {
        var skill = SkillWithEngine();
        FieldTimingResolver.Resolve(
            skill,
            catalogLifetimeSec: 4f,
            catalogTickSec: 0.5f,
            executorFieldTickSec: 0.25f,
            bangDurationSec: 0.2f,
            weaponDurationMult: 1f,
            regenSec: 0f,
            shieldSec: 0f,
            rootSec: 0f,
            hasteSec: 0f,
            slowSec: 0f,
            out float durationSec,
            out float tickSec,
            out float perTickShare);

        Assert.That(durationSec, Is.EqualTo(4f).Within(0.001f));
        Assert.That(tickSec, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(perTickShare, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
    }

    [Test]
    public void VerbFallbackUsesRegenWhenNoCatalog()
    {
        var skill = new SkillResolution(
            elementId: string.Empty,
            elementName: string.Empty,
            displayName: "regen",
            skillId: "2-1",
            skillJob: string.Empty,
            verbId: "2",
            verbName: string.Empty,
            verbFamily: string.Empty,
            action: string.Empty,
            baseDamage: 0f,
            basePoise: 0f,
            hitbox: string.Empty,
            castMobility: string.Empty,
            mechanics: System.Array.Empty<string>(),
            adjectiveId: "1",
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

        FieldTimingResolver.Resolve(
            skill,
            catalogLifetimeSec: 0f,
            catalogTickSec: 0f,
            executorFieldTickSec: 0.3f,
            bangDurationSec: 0.15f,
            weaponDurationMult: 1f,
            regenSec: 2.5f,
            shieldSec: 0f,
            rootSec: 0f,
            hasteSec: 0f,
            slowSec: 0f,
            out float durationSec,
            out _,
            out _);

        Assert.That(durationSec, Is.EqualTo(2.5f).Within(0.001f));
    }

    [Test]
    public void WeaponDurationMultScalesResolvedDuration()
    {
        var skill = SkillWithEngine();
        FieldTimingResolver.Resolve(
            skill,
            catalogLifetimeSec: 2f,
            catalogTickSec: 0.2f,
            executorFieldTickSec: 0.2f,
            bangDurationSec: 0.1f,
            weaponDurationMult: 1.5f,
            regenSec: 0f,
            shieldSec: 0f,
            rootSec: 0f,
            hasteSec: 0f,
            slowSec: 0f,
            out float durationSec,
            out _,
            out _);

        Assert.That(durationSec, Is.EqualTo(3f).Within(0.001f));
    }
}
