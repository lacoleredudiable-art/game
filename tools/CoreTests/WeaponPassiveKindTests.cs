using Dovus.Core.Equipment;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class WeaponPassiveKindTests
{
    [Test]
    public void Parse_KnownIds()
    {
        Assert.That(WeaponPassiveKinds.Parse("sirt_vurusu"), Is.EqualTo(WeaponPassiveKind.SirtVurusu));
        Assert.That(WeaponPassiveKinds.Parse("genis_yay"), Is.EqualTo(WeaponPassiveKind.GenisYay));
        Assert.That(WeaponPassiveKinds.Parse("yere_cakma"), Is.EqualTo(WeaponPassiveKind.YereCakma));
        Assert.That(WeaponPassiveKinds.Parse("karsi_saldiri"), Is.EqualTo(WeaponPassiveKind.KarsiSaldiri));
        Assert.That(WeaponPassiveKinds.Parse("kosu_atisi"), Is.EqualTo(WeaponPassiveKind.KosuAtisi));
        Assert.That(WeaponPassiveKinds.Parse("sabit_nisan"), Is.EqualTo(WeaponPassiveKind.SabitNisan));
        Assert.That(WeaponPassiveKinds.Parse("uzun_buyu"), Is.EqualTo(WeaponPassiveKind.UzunBuyu));
        Assert.That(WeaponPassiveKinds.Parse("kutsal_etki"), Is.EqualTo(WeaponPassiveKind.KutsalEtki));
        Assert.That(WeaponPassiveKinds.Parse("dolu_sayfa"), Is.EqualTo(WeaponPassiveKind.DoluSayfa));
        Assert.That(WeaponPassiveKinds.Parse("capraz_ates"), Is.EqualTo(WeaponPassiveKind.CaprazAtes));
    }

    [Test]
    public void Parse_UnknownNullEmpty_ReturnsNone()
    {
        Assert.That(WeaponPassiveKinds.Parse("not_a_passive"), Is.EqualTo(WeaponPassiveKind.None));
        Assert.That(WeaponPassiveKinds.Parse(null), Is.EqualTo(WeaponPassiveKind.None));
        Assert.That(WeaponPassiveKinds.Parse(""), Is.EqualTo(WeaponPassiveKind.None));
    }

    [Test]
    public void DocWeapons_PassiveIdsResolveToKind()
    {
        string json = File.ReadAllText(ElementPath());
        EquipmentCatalog catalog = EquipmentCatalog.FromJson(json);
        Assert.That(catalog.Items.Count, Is.EqualTo(10));
        foreach (EquipmentItem item in catalog.Items)
        {
            Assert.That(item.Profile.Passive.Kind, Is.Not.EqualTo(WeaponPassiveKind.None),
                () => $"weapon {item.Profile.Id} passive id={item.Profile.Passive.Id}");
            Assert.That(WeaponPassiveKinds.Parse(item.Profile.Passive.Id), Is.EqualTo(item.Profile.Passive.Kind));
        }
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        return path;
    }
}
