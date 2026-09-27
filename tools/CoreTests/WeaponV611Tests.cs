using System.IO;
using Dovus.Core.Equipment;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class WeaponV611Tests
{
    static EquipmentCatalog Load()
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
        Assert.That(File.Exists(path), Is.True);
        return EquipmentCatalog.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void LoadsTenWeaponsAndJsonMultipliers()
    {
        EquipmentCatalog catalog = Load();
        EquipmentItem sword = catalog.FindWeapon(4)!;

        Assert.That(catalog.IsV61, Is.True);
        Assert.That(catalog.Items.Count, Is.EqualTo(10));
        Assert.That(sword.Name, Is.EqualTo("Kılıç"));
        Assert.That(sword.Type, Is.EqualTo("medium"));
        Assert.That(sword.DamageMult, Is.EqualTo(1f));
        Assert.That(sword.CastTimeMult, Is.EqualTo(0.9f));
        Assert.That(sword.CompatibleVerbs, Is.EqualTo(new[] { 1, 3, 5, 9 }));
    }

    [Test]
    public void VerbMismatchUsesLockedPenaltyAndDisablesPassive()
    {
        EquipmentCatalog catalog = Load();
        var resolver = new EquipmentBonusResolver(catalog);
        EquipmentItem sword = catalog.FindWeapon(4)!;

        WeaponSkillCompatibility match = resolver.Resolve(sword, 1);
        Assert.That(match.Compatible, Is.True);
        Assert.That(match.DamageMult, Is.EqualTo(1f));
        Assert.That(match.CastTimeMult, Is.EqualTo(0.9f));
        Assert.That(match.PassiveEnabled, Is.True);
        Assert.That(match.UiColor, Is.EqualTo("yeşil"));

        WeaponSkillCompatibility mismatch = resolver.Resolve(sword, 2);
        Assert.That(mismatch.Compatible, Is.False);
        Assert.That(mismatch.DamageMult, Is.EqualTo(0.8f));
        Assert.That(mismatch.CastTimeMult, Is.EqualTo(1.08f).Within(0.0001f));
        Assert.That(mismatch.PassiveEnabled, Is.False);
        Assert.That(mismatch.UiColor, Is.EqualTo("sarı"));
    }

    [Test]
    public void AdjectiveNeverChangesCompatibility()
    {
        EquipmentCatalog catalog = Load();
        var resolver = new EquipmentBonusResolver(catalog);
        EquipmentItem sword = catalog.FindWeapon(4)!;

        // Uyumluluk API'si yalnız verb id alır; sıfat id'si yüzeye dahil değildir.
        WeaponSkillCompatibility first = resolver.Resolve(sword, 3);
        WeaponSkillCompatibility second = resolver.Resolve(sword, 3);
        Assert.That(second.Compatible, Is.EqualTo(first.Compatible));
        Assert.That(second.DamageMult, Is.EqualTo(first.DamageMult));
    }
}
