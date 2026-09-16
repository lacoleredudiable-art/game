using System.IO;
using System.Linq;
using Dovus.Core.Equipment;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json equipment_system — 18 örnek + element_match_bonus.
/// </summary>
[TestFixture]
public class EquipmentCatalogTests
{
    static EquipmentCatalog LoadFull()
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
        Assert.That(File.Exists(path), Is.True, $"element-sistemi.json bulunamadı: {path}");
        return EquipmentCatalog.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void FullJson_Parses18ExampleItems()
    {
        var catalog = LoadFull();
        Assert.That(catalog.Items.Count, Is.EqualTo(18));

        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Ateş")!.Name, Is.EqualTo("Alev Kılıcı"));
        Assert.That(catalog.Find(EquipmentSlot.Armor, "Ateş")!.Name, Is.EqualTo("Ateş Zırhı"));
        Assert.That(catalog.Find(EquipmentSlot.Accessory, "Ateş")!.Name, Is.EqualTo("Kor Tılsımı"));

        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Su")!.Name, Is.EqualTo("Pınar Asası"));
        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Hava")!.Name, Is.EqualTo("Rüzgar Hançeri"));
        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Toprak")!.Name, Is.EqualTo("Kaya Çekici"));
        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Aydınlık")!.Name, Is.EqualTo("Güneş Asası"));
        Assert.That(catalog.Find(EquipmentSlot.Weapon, "Karanlık")!.Name, Is.EqualTo("Gölge Hançeri"));

        Assert.That(catalog.Items.Count(i => i.Slot == EquipmentSlot.Weapon), Is.EqualTo(6));
        Assert.That(catalog.Items.Count(i => i.Slot == EquipmentSlot.Armor), Is.EqualTo(6));
        Assert.That(catalog.Items.Count(i => i.Slot == EquipmentSlot.Accessory), Is.EqualTo(6));

        Assert.That(catalog.TryGet("weapon:Ateş", out EquipmentItem atesWeapon), Is.True);
        Assert.That(atesWeapon.Id, Is.EqualTo("weapon:Ateş"));
        Assert.That(atesWeapon.Element, Is.EqualTo("Ateş"));
        Assert.That(atesWeapon.Slot, Is.EqualTo(EquipmentSlot.Weapon));
    }

    [Test]
    public void ElementMatchBonus_MatchReturnsParsedMult_MismatchReturnsOne()
    {
        var catalog = LoadFull();
        Assert.That(catalog.ElementMatchBonusText, Is.EqualTo("+%10 etki"));

        var resolver = new EquipmentBonusResolver(catalog);
        // Yüzde JSON metninden — elle 1.1f yazılmadı; ParseMatchBonusMult doğrulanır.
        Assert.That(
            EquipmentBonusResolver.ParseMatchBonusMult(catalog.ElementMatchBonusText),
            Is.EqualTo(1.1f).Within(0.0001f));
        Assert.That(resolver.MatchBonusMult, Is.EqualTo(1.1f).Within(0.0001f));

        EquipmentItem weapon = catalog.Find(EquipmentSlot.Weapon, "Ateş")!;
        Assert.That(resolver.Resolve(weapon, "Ateş"), Is.EqualTo(1.1f).Within(0.0001f));
        Assert.That(resolver.Resolve(weapon.Element, "Ateş"), Is.EqualTo(1.1f).Within(0.0001f));
        Assert.That(resolver.Resolve(weapon, "Su"), Is.EqualTo(1.0f).Within(0.0001f));
        Assert.That(resolver.Resolve("Toprak", "Hava"), Is.EqualTo(1.0f).Within(0.0001f));
    }
}
