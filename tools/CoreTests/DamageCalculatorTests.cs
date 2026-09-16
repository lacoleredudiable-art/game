using System.Collections.Generic;
using System.IO;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DamageCalculatorTests
{
    static string ElementSystemJsonPath()
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
        return path;
    }

    static string LoadJson() => File.ReadAllText(ElementSystemJsonPath());

    /// <summary>crit_system.adjective_crit_bonus anahtarlarını JSON'dan okur (uydurma id yok).</summary>
    static (float baseChance, float maxChance, float critMult, Dictionary<string, float> bonuses)
        ReadCritSystem(string json)
    {
        JsonValue crit = MiniJson.Parse(json)["crit_system"];
        var bonuses = new Dictionary<string, float>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, JsonValue> kv in crit["adjective_crit_bonus"].AsObject())
            bonuses[kv.Key] = kv.Value.AsFloat(0f);
        return (
            crit["base_crit_chance"].AsFloat(0f),
            crit["max_crit_chance"].AsFloat(0f),
            crit["crit_multiplier"].AsFloat(1f),
            bonuses);
    }

    [Test]
    public void AdjectiveCritBonuses_SumWithBase_MatchJson()
    {
        string json = LoadJson();
        var (baseChance, maxChance, _, bonuses) = ReadCritSystem(json);
        // Kabul: keskinlik / saflastirma / berraklik id'leri JSON'dan.
        Assert.That(bonuses.ContainsKey("keskinlik"), Is.True);
        Assert.That(bonuses.ContainsKey("saflastirma"), Is.True);
        Assert.That(bonuses.ContainsKey("berraklik"), Is.True);

        var calc = DamageCalculator.FromElementSystemJson(json, seed: 1);

        foreach (string id in new[] { "keskinlik", "saflastirma", "berraklik" })
        {
            float expected = System.Math.Min(baseChance + bonuses[id], maxChance);
            Assert.That(calc.CritChanceFor(id), Is.EqualTo(expected).Within(0.0001f), id);
        }

        // Bilinmeyen sıfat → yalnızca base.
        Assert.That(calc.CritChanceFor("yogunlastirma"), Is.EqualTo(baseChance).Within(0.0001f));
    }

    [Test]
    public void MaxCritChance_CapsSum()
    {
        string json = LoadJson();
        var (_, maxChance, critMult, _) = ReadCritSystem(json);
        // JSON'daki üç bonus toplamı max'ın altında; tavanı kanıtlamak için yüksek bonus.
        var calc = new DamageCalculator(
            seed: 0,
            baseCritChance: 0.05f,
            critMultiplier: critMult,
            maxCritChance: maxChance,
            adjectiveCritBonus: new Dictionary<string, float> { ["keskinlik"] = 0.95f });

        Assert.That(calc.CritChanceFor("keskinlik"), Is.EqualTo(maxChance).Within(0.0001f));
        Assert.That(0.05f + 0.95f, Is.GreaterThan(maxChance));
    }

    [Test]
    public void WeaknessBonus_GreaterThanOne_IncreasesDamage()
    {
        var calc = DamageCalculator.FromElementSystemJson(LoadJson(), seed: 42);
        // CritEligible false → crit gürültüsü yok, weakness karşılaştırması temiz.
        DamageHit normal = calc.Compute(40f, 1f, 1f, resistance: 0f, weaknessBonus: 1f,
            adjectiveId: "yogunlastirma", critEligible: false);
        DamageHit weak = calc.Compute(40f, 1f, 1f, resistance: 0f, weaknessBonus: 1.5f,
            adjectiveId: "yogunlastirma", critEligible: false);

        Assert.That(normal.Amount, Is.EqualTo(40f).Within(0.001f));
        Assert.That(weak.Amount, Is.EqualTo(60f).Within(0.001f));
        Assert.That(weak.Amount, Is.GreaterThan(normal.Amount));
    }

    [Test]
    public void Formula_AppliesResistanceAndMults_ThenCritMultiplier()
    {
        // Şans 1.0 clamp'li → her zaman crit (seed fark etmez).
        var calc = new DamageCalculator(
            seed: 7,
            baseCritChance: 1f,
            critMultiplier: 2f,
            maxCritChance: 1f,
            adjectiveCritBonus: new Dictionary<string, float>());

        // 40 × 0.85 × 1 × (1-0.2) × 1.5 = 40.8; crit ×2 = 81.6
        DamageHit hit = calc.Compute(40f, 0.85f, 1f, resistance: 0.2f, weaknessBonus: 1.5f,
            adjectiveId: "", critEligible: true);

        Assert.That(hit.WasCrit, Is.True);
        Assert.That(hit.Amount, Is.EqualTo(40f * 0.85f * 0.8f * 1.5f * 2f).Within(0.001f));
    }

    [Test]
    public void CritEligibleFalse_NeverCrits()
    {
        var calc = new DamageCalculator(
            seed: 1,
            baseCritChance: 1f,
            critMultiplier: 2f,
            maxCritChance: 1f,
            adjectiveCritBonus: new Dictionary<string, float> { ["keskinlik"] = 0.25f });

        DamageHit hit = calc.Compute(40f, 1f, 1f, 0f, 1f, "keskinlik", critEligible: false);
        Assert.That(hit.WasCrit, Is.False);
        Assert.That(hit.Amount, Is.EqualTo(40f).Within(0.001f));
        Assert.That(hit.CritChanceUsed, Is.EqualTo(0f));
    }

    [Test]
    public void SkillResolution_UsesBaseAndAdjectiveMult()
    {
        var calc = DamageCalculator.FromElementSystemJson(LoadJson(), seed: 3);
        var skill = new SkillResolution(
            "1", "Ateş", "Test", "t", "job",
            "saldiri", "Saldırı", "strike", "damage",
            40f, 15f, "projectile", "free_move", new[] { "burn" },
            "keskinlik", "Keskinlik", "focus",
            0.9f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty,
            critEligible: false);

        DamageHit hit = calc.Compute(in skill, lengthDamageMult: 1f, resistance: 0f, weaknessBonus: 1f);
        Assert.That(hit.Amount, Is.EqualTo(36f).Within(0.001f));
    }

    [Test]
    public void SameSeed_IsDeterministic()
    {
        string json = LoadJson();
        var a = DamageCalculator.FromElementSystemJson(json, seed: 99);
        var b = DamageCalculator.FromElementSystemJson(json, seed: 99);

        float[] amountsA = new float[20];
        float[] amountsB = new float[20];
        for (int i = 0; i < 20; i++)
        {
            amountsA[i] = a.Compute(40f, 1f, 1f, 0f, 1f, "keskinlik", true).Amount;
            amountsB[i] = b.Compute(40f, 1f, 1f, 0f, 1f, "keskinlik", true).Amount;
        }
        Assert.That(amountsA, Is.EqualTo(amountsB));
    }
}
