using Dovus.Core.Elements;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ElementResolverTests
{
    ElementCatalog _catalog = null!;
    ElementResolver _resolver = null!;

    [OneTimeSetUp]
    public void LoadLockedSpec()
    {
        _catalog = ElementCatalogLoader.LoadFromRepo();
        _resolver = new ElementResolver(_catalog);
    }

    [Test]
    public void Spec_IsLocked_Version42()
    {
        Assert.That(_catalog.Locked, Is.True);
        Assert.That(_catalog.Version, Is.EqualTo("4.2"));
        Assert.That(_catalog.ElementCount, Is.EqualTo(42));
        Assert.That(_catalog.VerbCount, Is.EqualTo(42));
        Assert.That(_catalog.AdjectiveCount, Is.EqualTo(42));
    }

    [Test]
    public void LengthEconomy_DoesNotScaleDamageOrPoise()
    {
        Assert.That(_catalog.TryGetLength(2, out var l2), Is.True);
        Assert.That(_catalog.TryGetLength(3, out var l3), Is.True);
        Assert.That(_catalog.TryGetLength(4, out var l4), Is.True);

        Assert.That(l2.DamageMult, Is.EqualTo(1f));
        Assert.That(l3.DamageMult, Is.EqualTo(1f));
        Assert.That(l4.DamageMult, Is.EqualTo(1f));
        Assert.That(l2.PoiseDamageMult, Is.EqualTo(1f));
        Assert.That(l3.PoiseDamageMult, Is.EqualTo(1f));
        Assert.That(l4.PoiseDamageMult, Is.EqualTo(1f));
        Assert.That(l2.CastTimeMult, Is.LessThan(l3.CastTimeMult));
        Assert.That(l3.CastTimeMult, Is.LessThan(l4.CastTimeMult));
    }

    [Test]
    public void AllCompoundPairs_Exist()
    {
        for (int a = 1; a <= 6; a++)
        for (int b = 1; b <= 6; b++)
        {
            string id = ElementCatalog.CompoundId(a, b);
            Assert.That(_catalog.TryGetElement(id, out var el), Is.True, id);
            Assert.That(el.IsCore, Is.False, id);
            Assert.That(el.SkillId, Is.Not.Null.And.Not.Empty, id);
        }
    }

    [Test]
    public void TwoRunes_FireFire_IdentitySkill_AtesTopu()
    {
        Assert.That(_resolver.TryResolve(new[] { 1, 1 }, out var r), Is.True);
        Assert.That(r.Kind, Is.EqualTo(ElementResolveKind.IdentitySkill));
        Assert.That(r.SkillId, Is.EqualTo("ates_topu"));
        Assert.That(r.SkillName, Is.EqualTo("Ateş Topu"));
        Assert.That(r.CompoundId, Is.EqualTo("1-1"));
        Assert.That(r.Verb.Id, Is.EqualTo("kritik_vurus"));
        Assert.That(r.Adjective, Is.Null);
        Assert.That(r.FinalDamage, Is.EqualTo(40f));
        Assert.That(r.FinalMobility, Is.EqualTo(CastMobility.SlowedMove));
    }

    [Test]
    public void ThreeRunes_OpensVerbJob_NotStrongerFireball()
    {
        Assert.That(_resolver.TryResolve(new[] { 1, 1 }, out var two), Is.True);
        Assert.That(_resolver.TryResolve(new[] { 1, 1, 1 }, out var three), Is.True);

        Assert.That(three.Kind, Is.EqualTo(ElementResolveKind.VerbPlusCoreAdjective));
        Assert.That(three.SkillId, Is.Null);
        Assert.That(three.VerbUnlocksJob, Is.EqualTo(two.VerbUnlocksJob));
        Assert.That(three.Adjective!.Value.Id, Is.EqualTo("yogunlastirma"));
        Assert.That(three.FinalDamage, Is.LessThan(two.FinalDamage));
        Assert.That(three.CastTimeMult, Is.EqualTo(1.4f));
    }

    [Test]
    public void FourRunes_CompoundAdjective_CommitCostNotDamage()
    {
        Assert.That(_resolver.TryResolve(new[] { 1, 1, 1 }, out var three), Is.True);
        Assert.That(_resolver.TryResolve(new[] { 1, 1, 1, 1 }, out var four), Is.True);

        Assert.That(four.Kind, Is.EqualTo(ElementResolveKind.VerbPlusCompoundAdjective));
        Assert.That(four.Adjective!.Value.Id, Is.EqualTo("keskinlik"));
        Assert.That(four.Verb.BaseDamage, Is.EqualTo(three.Verb.BaseDamage));
        Assert.That(four.CastTimeMult, Is.EqualTo(2f));
        Assert.That(four.FinalMobility, Is.EqualTo(CastMobility.Rooted));
        Assert.That(four.SkillId, Is.Null);
    }

    [Test]
    public void KritikEqualsSaldiri_BaseDamage()
    {
        Assert.That(_catalog.TryGetVerb("saldiri", out var s), Is.True);
        Assert.That(_catalog.TryGetVerb("kritik_vurus", out var k), Is.True);
        Assert.That(k.BaseDamage, Is.EqualTo(s.BaseDamage));
        Assert.That(k.BaseDamage, Is.EqualTo(40f));
    }

    [Test]
    public void OrderMatters_FireWater_DiffersFrom_WaterFire()
    {
        Assert.That(_resolver.TryResolve(new[] { 1, 2 }, out var ab), Is.True);
        Assert.That(_resolver.TryResolve(new[] { 2, 1 }, out var ba), Is.True);
        Assert.That(ab.SkillId, Is.Not.EqualTo(ba.SkillId));
        Assert.That(ab.CompoundId, Is.EqualTo("1-2"));
        Assert.That(ba.CompoundId, Is.EqualTo("2-1"));
    }

    [Test]
    public void MotionFamily_IgnoresLengthRoot()
    {
        Assert.That(_catalog.TryGetElement("3-3", out var wind), Is.True);
        Assert.That(_catalog.TryGetVerb(wind.VerbId, out var verb), Is.True);
        Assert.That(verb.IsMotionFamily, Is.True);
        Assert.That(verb.CastMobility, Is.EqualTo(CastMobility.FreeMove));

        Assert.That(_resolver.TryResolve(new[] { 3, 3, 3, 3 }, out var four), Is.True);
        Assert.That(four.FinalMobility, Is.EqualTo(CastMobility.FreeMove));
    }

    [Test]
    public void StrikeFour_TakesRootFromLength()
    {
        Assert.That(_resolver.TryResolve(new[] { 1, 1, 2, 2 }, out var four), Is.True);
        Assert.That(four.Verb.IsMotionFamily, Is.False);
        Assert.That(four.FinalMobility, Is.EqualTo(CastMobility.Rooted));
    }

    [Test]
    public void InvalidInput_Fails()
    {
        Assert.That(_resolver.TryResolve(new[] { 1 }, out _), Is.False);
        Assert.That(_resolver.TryResolve(new[] { 1, 1, 1, 1, 1 }, out _), Is.False);
        Assert.That(_resolver.TryResolve(new[] { 0, 1 }, out _), Is.False);
        Assert.That(_resolver.TryResolve(new[] { 1, 7 }, out _), Is.False);
    }

    [Test]
    public void SpecFile_LockedAndSecondaryNonMechanical()
    {
        string json = System.IO.File.ReadAllText(ElementCatalogLoader.FindSpecPath());
        Assert.That(json, Does.Contain("\"locked\": true"));
        Assert.That(json, Does.Contain("\"mechanical\": false"));
    }

    [Test]
    public void AllAdjectives_DamageMultAtMostPointNine()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(
            System.IO.File.ReadAllText(ElementCatalogLoader.FindSpecPath()));
        foreach (var a in doc.RootElement.GetProperty("adjectives").EnumerateArray())
        {
            float dm = 1f;
            if (a.GetProperty("engine_modifiers").TryGetProperty("damage_mult", out var el))
                dm = (float)el.GetDouble();
            Assert.That(dm, Is.LessThanOrEqualTo(0.9f), a.GetProperty("id").GetString());
        }
    }

    [Test]
    public void VarlikSilme_InStrikeBand()
    {
        Assert.That(_catalog.TryGetVerb("varlik_silme", out var v), Is.True);
        Assert.That(v.BaseDamage, Is.InRange(40f, 50f));
        Assert.That(v.CastMobility, Is.EqualTo(CastMobility.Rooted));
    }
}
