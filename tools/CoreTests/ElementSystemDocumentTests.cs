using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public class ElementSystemDocumentTests
{
    static string ElementJson()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(root, "docs")))
            root = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string path = Path.Combine(root, "docs", "element-sistemi.json");
        Assert.That(File.Exists(path), Is.True);
        return File.ReadAllText(path);
    }

    [Test]
    public void FromJson_Matches_FromDocument_ForAllConsumers()
    {
        string text = ElementJson();
        ElementSystemDocument doc = ElementSystemDocument.Parse(text);

        Assert.That(ElementSystemHeader.TryParse(text, 300, out ElementSystemHeader hText), Is.True);
        Assert.That(ElementSystemHeader.TryParse(doc, 300, out ElementSystemHeader hDoc), Is.True);
        Assert.That(hDoc.Version, Is.EqualTo(hText.Version));
        Assert.That(hDoc.Binding, Is.EqualTo(hText.Binding));
        Assert.That(hDoc.SelectionTransitionMs, Is.EqualTo(hText.SelectionTransitionMs));

        SkillMotor motorText = SkillMotor.FromJson(text);
        SkillMotor motorDoc = SkillMotor.FromDocument(doc);
        Assert.That(motorDoc.RuneCount, Is.EqualTo(motorText.RuneCount));
        Assert.That(motorDoc.SkillCount, Is.EqualTo(motorText.SkillCount));
        Assert.That(motorDoc.Version, Is.EqualTo(motorText.Version));
        Assert.That(motorDoc.IsV61, Is.EqualTo(motorText.IsV61));

        EquipmentCatalog equipText = EquipmentCatalog.FromJson(text);
        EquipmentCatalog equipDoc = EquipmentCatalog.FromDocument(doc);
        Assert.That(equipDoc.Items.Count, Is.EqualTo(equipText.Items.Count));

        AnimationDatabase animText = AnimationDatabase.FromJson(text);
        AnimationDatabase animDoc = AnimationDatabase.FromDocument(doc);
        Assert.That(animDoc.Count, Is.EqualTo(animText.Count));

        CritSystem critText = CritSystem.FromJson(text);
        CritSystem critDoc = CritSystem.FromDocument(doc);
        Assert.That(critDoc.BaseChance, Is.EqualTo(critText.BaseChance));
        Assert.That(critDoc.Multiplier, Is.EqualTo(critText.Multiplier));
        Assert.That(critDoc.MaxChance, Is.EqualTo(critText.MaxChance));

        BossCombatProfile bossText = BossCombatProfile.FromJson(text);
        BossCombatProfile bossDoc = BossCombatProfile.FromDocument(doc);
        Assert.That(bossDoc.PlayerMaxHp, Is.EqualTo(bossText.PlayerMaxHp));
        Assert.That(bossDoc.BossMaxHp, Is.EqualTo(bossText.BossMaxHp));
        Assert.That(bossDoc.Armor, Is.EqualTo(bossText.Armor));

        WeaponArmorCatalog armorText = WeaponArmorCatalog.FromJson(text);
        WeaponArmorCatalog armorDoc = WeaponArmorCatalog.FromDocument(doc);
        Assert.That(armorDoc.ArmorOf("1"), Is.EqualTo(armorText.ArmorOf("1")));

        SkillNumberCatalog numsText = SkillNumberCatalog.FromJson(text);
        SkillNumberCatalog numsDoc = SkillNumberCatalog.FromDocument(doc);
        Assert.That(numsDoc.VerbDamageReference, Is.EqualTo(numsText.VerbDamageReference));
        Assert.That(numsDoc.AllySkillRangeM, Is.EqualTo(numsText.AllySkillRangeM));

        WeaponSwapRules swapText = WeaponSwapRules.FromJson(text);
        WeaponSwapRules swapDoc = WeaponSwapRules.FromDocument(doc);
        Assert.That(swapDoc.Enabled, Is.EqualTo(swapText.Enabled));
        Assert.That(swapDoc.CooldownSec, Is.EqualTo(swapText.CooldownSec));

        VerbExecutionData verbText = VerbExecutionData.FromJson(text);
        VerbExecutionData verbDoc = VerbExecutionData.FromDocument(doc);
        Assert.That(verbDoc.TryGetHitbox(1, out VerbHitboxSpec hbDoc), Is.True);
        Assert.That(verbText.TryGetHitbox(1, out VerbHitboxSpec hbText), Is.True);
        Assert.That(hbDoc.SizeA, Is.EqualTo(hbText.SizeA));

        MobilityCcData mobText = MobilityCcData.FromJson(text);
        MobilityCcData mobDoc = MobilityCcData.FromDocument(doc);
        Assert.That(mobDoc.RootImmunityMs, Is.EqualTo(mobText.RootImmunityMs));

        MechanicRules rulesText = MechanicRules.FromJson(text);
        MechanicRules rulesDoc = MechanicRules.FromDocument(doc);
        Assert.That(rulesDoc.IsValid, Is.EqualTo(rulesText.IsValid));
        Assert.That(rulesDoc.Weapons.Count, Is.EqualTo(rulesText.Weapons.Count));

        Assert.That(
            PassiveSlotPolicy.RequiresWeaponCompatibility(text),
            Is.EqualTo(PassiveSlotPolicy.RequiresWeaponCompatibility(doc)));
    }

    [Test]
    public void SharedDocument_DoesNotMutateBetweenConsumers()
    {
        string text = ElementJson();
        ElementSystemDocument doc = ElementSystemDocument.Parse(text);
        SkillMotor baseline = SkillMotor.FromDocument(doc);

        EquipmentCatalog.FromDocument(doc);
        MechanicRules.FromDocument(doc);
        SkillNumberCatalog.FromDocument(doc);
        VerbExecutionData.FromDocument(doc);
        MobilityCcData.FromDocument(doc);

        SkillMotor after = SkillMotor.FromDocument(doc);
        Assert.That(after.RuneCount, Is.EqualTo(baseline.RuneCount));
        Assert.That(after.SkillCount, Is.EqualTo(baseline.SkillCount));
        Assert.That(after.Version, Is.EqualTo(baseline.Version));
    }
}
