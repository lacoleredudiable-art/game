using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DamagePipelineTests
{
    [Test]
    public void Order_IsBaseThenCritThenBuffsThenArmorThenReduction()
    {
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            AttackPower = 2f,
            CanCrit = true,
            CritChance = 0.10f,
            CritMultiplier = 1.5f,
            CritRoll01 = 0f,
            AdditivePercent = 0.10f,
            Multiplier = 2f,
            Armor = 100f,
            DamageTakenFactor = 0.5f
        });

        // 100×2 = 200, kritik ×1.5 = 300, buff ×1.1×2 = 660, zırh 100 yarıya = 330, alınan ×0.5 = 165.
        Assert.That(hit.WasCrit, Is.True);
        Assert.That(hit.PreArmor, Is.EqualTo(660f).Within(0.01f));
        Assert.That(hit.ArmorAfterPen, Is.EqualTo(100f).Within(0.01f));
        Assert.That(hit.Amount, Is.EqualTo(165f).Within(0.01f));
        Assert.That(hit.Poise, Is.EqualTo(0f));
    }

    [Test]
    public void Armor_UsesLolFormula_IncludingNegativeAndPenetration()
    {
        float half = ArmorMath.Mitigate(200f, 100f);
        Assert.That(half, Is.EqualTo(100f).Within(0.01f));

        float boosted = ArmorMath.Mitigate(200f, -100f);
        Assert.That(boosted, Is.EqualTo(300f).Within(0.01f));

        float afterPen = ArmorMath.AfterPenetration(100f, flatPen: 20f, percentPen: 0.5f);
        Assert.That(afterPen, Is.EqualTo(30f).Within(0.01f));
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 130f,
            Armor = 100f,
            ArmorPenFlat = 20f,
            ArmorPenPercent = 0.5f
        });
        Assert.That(hit.ArmorAfterPen, Is.EqualTo(30f).Within(0.01f));
        Assert.That(hit.Amount, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void Crit_UsesDefaultChanceAndMultiplier_WhenRollIsInside()
    {
        var crit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            CanCrit = true,
            CritChance = -1f,
            CritRoll01 = 0.09f
        });
        Assert.That(crit.WasCrit, Is.True);
        Assert.That(crit.Amount, Is.EqualTo(150f).Within(0.01f));

        var miss = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            CanCrit = true,
            CritChance = DamagePipeline.DefaultCritChance,
            CritRoll01 = 0.10f
        });
        Assert.That(miss.WasCrit, Is.False);
        Assert.That(miss.Amount, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void Variance_IsDeterministicForTheSameSeed_AndStaysInsideFivePercent()
    {
        var a = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 1000f,
            CanCrit = false,
            ApplyVariance = true,
            VarianceSeed = 4242
        });
        var b = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 1000f,
            CanCrit = false,
            ApplyVariance = true,
            VarianceSeed = 4242
        });
        Assert.That(a.Amount, Is.EqualTo(b.Amount).Within(0.001f));
        Assert.That(a.Variance, Is.InRange(0.95f, 1.05f));
        Assert.That(a.Amount, Is.InRange(950f, 1050f));

        var low = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 1000f,
            ApplyVariance = true,
            VarianceRoll01 = 0f
        });
        var high = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 1000f,
            ApplyVariance = true,
            VarianceRoll01 = 1f
        });
        Assert.That(low.Amount, Is.EqualTo(950f).Within(0.01f));
        Assert.That(high.Amount, Is.EqualTo(1050f).Within(0.01f));
    }

    [Test]
    public void Heal_SkipsArmorCritAndVariance()
    {
        var heal = DamagePipeline.Resolve(new DamageQuery
        {
            Heal = true,
            HealPower = 100f,
            HealMultiplier = 1.5f,
            Armor = 100f,
            CanCrit = true,
            CritRoll01 = 0f,
            ApplyVariance = true,
            VarianceRoll01 = 0f,
            ThreatMultiplier = 2f
        });
        Assert.That(heal.Amount, Is.EqualTo(150f).Within(0.01f));
        Assert.That(heal.WasCrit, Is.False);
        Assert.That(heal.Threat, Is.EqualTo(300f).Within(0.01f));
        Assert.That(heal.Variance, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void DamageReduction_NeverExceedsNinetyPercent()
    {
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            DamageTakenFactor = 0.04f
        });
        Assert.That(hit.Amount, Is.EqualTo(10f).Within(0.01f));
    }

    [Test]
    public void Scale_MultipliesSkillPowerOnce()
    {
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 40f,
            ScaleMagnitudes = true,
            Poise = 12f
        });
        Assert.That(hit.Amount, Is.EqualTo(40f * CombatScale.DamageAndHp).Within(0.01f));
        Assert.That(hit.Poise, Is.EqualTo(12f).Within(0.001f));
    }

    [Test]
    public void Threat_ScalesWithTheResolvedAmount()
    {
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            ThreatMultiplier = 2f
        });
        Assert.That(hit.Threat, Is.EqualTo(200f).Within(0.01f));
    }

    [Test]
    public void Shield_IsRemovedBeforeVariance_AndReportedInPoolUnits()
    {
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            Shield = 100f,
            ApplyVariance = true,
            VarianceRoll01 = 1f,
            ScaleMagnitudes = true
        });
        Assert.That(hit.Amount, Is.EqualTo(0f).Within(0.01f));
        Assert.That(hit.ShieldAbsorbed, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void LandMultiplier_AppliesInTheBuffStage_OnlyWhenTheHitHasPower()
    {
        int calls = 0;
        var hit = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            Multiplier = 2f,
            LandMultiplier = () =>
            {
                calls++;
                return 1.3f;
            }
        });
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(hit.PreArmor, Is.EqualTo(260f).Within(0.01f));
        Assert.That(hit.Amount, Is.EqualTo(260f).Within(0.01f));

        int skipped = 0;
        DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            Invulnerable = true,
            LandMultiplier = () =>
            {
                skipped++;
                return 9f;
            }
        });
        DamagePipeline.Resolve(new DamageQuery
        {
            Heal = true,
            HealPower = 40f,
            LandMultiplier = () =>
            {
                skipped++;
                return 9f;
            }
        });
        DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 0f,
            LandMultiplier = () =>
            {
                skipped++;
                return 9f;
            }
        });
        Assert.That(skipped, Is.EqualTo(0));
    }

    [Test]
    public void Format_UsesKAndM()
    {
        Assert.That(DamageNumberFormat.Format(24300f), Is.EqualTo("24.3K"));
        Assert.That(DamageNumberFormat.Format(1_200_000f), Is.EqualTo("1.2M"));
        Assert.That(DamageNumberFormat.Format(20000f), Is.EqualTo("20K"));
        Assert.That(DamageNumberFormat.Format(800f), Is.EqualTo("800"));
    }

    [Test]
    public void BossProfile_ScalesHp_AndLeavesArmorUnscaled()
    {
        var profile = BossCombatProfile.FromJson(File.ReadAllText(ElementPath()));
        Assert.That(profile.PlayerMaxHp, Is.EqualTo(100f * CombatScale.DamageAndHp).Within(0.1f));
        Assert.That(profile.BossMaxHp, Is.EqualTo(22000f * CombatScale.DamageAndHp).Within(0.1f));
        Assert.That(profile.Armor, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(profile.BossMaxHpHard, Is.EqualTo(profile.BossMaxHp).Within(0.1f));
        Assert.That(profile.ArmorHard, Is.EqualTo(profile.Armor).Within(0.0001f));
    }

    [Test]
    public void WeaponArmor_DefaultsToZero_AndReadsBaseArmorWhenPresent()
    {
        var empty = WeaponArmorCatalog.FromJson(File.ReadAllText(ElementPath()));
        Assert.That(empty.ArmorOf("1"), Is.EqualTo(0f));
        var catalog = WeaponArmorCatalog.FromJson(@"{""weapons"":[{""id"":1,""base_armor"":12},{""id"":2}]}");
        Assert.That(catalog.ArmorOf("1"), Is.EqualTo(12f));
        Assert.That(catalog.ArmorOf("weapon:1"), Is.EqualTo(12f));
        Assert.That(catalog.ArmorOf("2"), Is.EqualTo(0f));
    }

    [Test]
    public void Zafiyet_ShredsArmor_FromEngineValue()
    {
        DesignWarnings.ResetForTests();
        var motor = SkillMotor.FromJson(File.ReadAllText(ElementPath()));
        SkillResolution weak = motor.Resolve(new[] { 7, 6 });
        SkillResolution nine = motor.Resolve(new[] { 7, 9 });
        Assert.That(weak.EngineModifiers["debuff_armor"].AsFloat(0f), Is.EqualTo(-0.2f).Within(0.0001f));
        Assert.That(nine.EngineModifiers["debuff_armor"].AsFloat(0f), Is.EqualTo(-0.2f).Within(0.0001f));
        Assert.That(nine.EngineModifiers["ignore_armor"].AsBool(false), Is.True);

        var sheet = new ArmorSheet { Base = 100f };
        sheet.ApplyShred(0.2f, 0, 4000);
        Assert.That(sheet.Effective(0), Is.EqualTo(80f).Within(0.01f));

        var thirty = new ArmorSheet { Base = 100f };
        thirty.ApplyShred(0.30f, 0, 4000);
        Assert.That(thirty.Effective(0), Is.EqualTo(70f).Within(0.01f));

        var ignored = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 200f,
            Armor = 100f,
            ArmorPenPercent = 1f
        });
        Assert.That(ignored.ArmorAfterPen, Is.EqualTo(0f).Within(0.01f));
        Assert.That(ignored.Amount, Is.EqualTo(200f).Within(0.01f));
    }

    static string ElementPath()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "docs", "element-sistemi.json"));
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }
}
