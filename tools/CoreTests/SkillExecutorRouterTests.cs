using System.IO;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using NUnit.Framework;

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
            _router.Route(_motor.Resolve(new[] { 5, 1 }), cannon).Kind,
            Is.EqualTo(SkillExecutorKind.Projectile));
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

    [TestCase(3)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(11)]
    public void OutOfScopeVerbsStubToFallback(int verbId)
    {
        SkillExecutorRoute route =
            _router.Route(_motor.Resolve(new[] { verbId, 1 }), _equipment.FindWeapon(4));

        Assert.That(route.Kind, Is.EqualTo(SkillExecutorKind.Fallback));
        Assert.That(route.IsStub, Is.True);
        Assert.That(route.Reason, Is.Not.Empty);
    }
}
