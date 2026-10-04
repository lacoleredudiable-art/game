using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class SkillExecutorRouterTests
{
    SkillMotor _motor = null!;
    EquipmentCatalog _equipment = null!;
    SkillExecutorRouter _router = null!;

    [SetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        }

        string json = File.ReadAllText(path);
        _motor = SkillMotor.FromJson(json);
        _equipment = EquipmentCatalog.FromJson(json);
        _router = new SkillExecutorRouter();
    }

    [Test]
    public void StrikeAndBlastUseWeaponType()
    {
        EquipmentItem sword = _equipment.FindWeapon(4)!;
        EquipmentItem cannon = _equipment.FindWeapon(7)!;

        Assert.That(sword.Type, Is.EqualTo("medium"));
        Assert.That(cannon.Type, Is.EqualTo("ranged"));
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 1, 1 }), sword).Kind,
            Is.EqualTo(SkillExecutorKind.MeleeHitbox));
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 5, 1 }), sword).Kind,
            Is.EqualTo(SkillExecutorKind.MeleeHitbox));
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 1, 1 }), cannon).Kind,
            Is.EqualTo(SkillExecutorKind.Projectile));
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 5, 1 }), cannon).Kind,
            Is.EqualTo(SkillExecutorKind.Projectile));
    }

    [Test]
    public void AllCanonicalWeaponsRouteStrikeAndBlastByEffectiveRangeType()
    {
        Assert.That(_equipment.Items.Count, Is.EqualTo(10));
        foreach (EquipmentItem weapon in _equipment.Items)
        {
            SkillExecutorKind expected = SkillExecutorRouter.IsRangedWeapon(weapon)
                ? SkillExecutorKind.Projectile
                : SkillExecutorKind.MeleeHitbox;
            Assert.That(
                _router.Route(_motor.Resolve(new[] { 1, 1 }), weapon).Kind,
                Is.EqualTo(expected),
                weapon.Name + " Saldırı");
            Assert.That(
                _router.Route(_motor.Resolve(new[] { 5, 1 }), weapon).Kind,
                Is.EqualTo(expected),
                weapon.Name + " Patlama");
        }
    }

    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(12)]
    public void AuraVerbsUseField(int verbId)
    {
        SkillExecutorRoute route =
            _router.Route(_motor.Resolve(new[] { verbId, 1 }), _equipment.FindWeapon(4));

        Assert.That(route.Kind, Is.EqualTo(SkillExecutorKind.FieldAura));
        Assert.That(route.IsStub, Is.False);
    }

    [TestCase(3, SkillExecutorKind.Movement)]
    [TestCase(10, SkillExecutorKind.SelfState)]
    [TestCase(11, SkillExecutorKind.Summon)]
    public void SelfVerbsHaveDedicatedExecutors(int verbId, SkillExecutorKind expected)
    {
        SkillExecutorRoute route =
            _router.Route(_motor.Resolve(new[] { verbId, 1 }), _equipment.FindWeapon(4));

        Assert.That(route.Kind, Is.EqualTo(expected));
        Assert.That(route.IsStub, Is.False);
    }

    [Test]
    public void DebuffUsesWeaponTypeLikeStrike()
    {
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 7, 1 }), _equipment.FindWeapon(4)).Kind,
            Is.EqualTo(SkillExecutorKind.MeleeHitbox));
        Assert.That(
            _router.Route(_motor.Resolve(new[] { 7, 1 }), _equipment.FindWeapon(7)).Kind,
            Is.EqualTo(SkillExecutorKind.Projectile));
    }

    [Test]
    public void EveryVerbHasAnExecutor()
    {
        for (int verb = 1; verb <= 12; verb++)
        {
            SkillExecutorRoute route =
                _router.Route(_motor.Resolve(new[] { verb, 1 }), _equipment.FindWeapon(4));
            Assert.That(route.IsStub, Is.False, "fiil " + verb);
            Assert.That(route.Kind, Is.Not.EqualTo(SkillExecutorKind.Fallback), "fiil " + verb);
        }
    }
}
