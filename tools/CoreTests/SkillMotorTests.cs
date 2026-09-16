using System.IO;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class SkillMotorTests
{
    static SkillMotor LoadFull()
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
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void FullJson_Has42ElementsAndDictionaries()
    {
        var m = LoadFull();
        Assert.That(m.ElementCount, Is.EqualTo(42));
        Assert.That(m.CoreCount, Is.EqualTo(6));
        Assert.That(m.VerbCount, Is.EqualTo(42));
        Assert.That(m.AdjectiveCount, Is.EqualTo(42));
        Assert.That(m.CoreName(1), Is.EqualTo("Ateş"));
        Assert.That(m.CoreName(3), Is.EqualTo("Hava"));
        Assert.That(m.CoreName(6), Is.EqualTo("Karanlık"));
    }

    [Test]
    public void Single_Core_IsRootSkill_NotCompoundCard()
    {
        SkillResolution r = LoadFull().Resolve(new[] { 1 });
        Assert.That(r.DisplayName, Is.EqualTo("Ateş"));
        Assert.That(r.VerbId, Is.EqualTo("saldiri"));
        Assert.That(r.AdjectiveId, Is.EqualTo("yogunlastirma"));
        Assert.That(r.SkillId, Is.EqualTo("core:1"));
    }

    [Test]
    public void Pair_AtesSu_IsBuharPerdesi_WithBlind()
    {
        SkillResolution r = LoadFull().Resolve(new[] { 1, 2 });
        Assert.That(r.ElementName, Is.EqualTo("Buhar"));
        Assert.That(r.DisplayName, Is.EqualTo("Buhar Perdesi"));
        Assert.That(r.VerbId, Is.EqualTo("gorus_kapatma"));
        Assert.That(r.Mechanics, Does.Contain("blind"));
        Assert.That(r.LengthRole, Is.EqualTo("Temel"));
    }

    [Test]
    public void Pair_AtesAtes_IsAtesTopu_SkillCard()
    {
        SkillResolution r = LoadFull().Resolve(new[] { 1, 1 });
        Assert.That(r.ElementName, Is.EqualTo("Alev"));
        Assert.That(r.DisplayName, Is.EqualTo("Ateş Topu"));
        Assert.That(r.SkillId, Is.EqualTo("ates_topu"));
        Assert.That(r.VerbId, Is.EqualTo("kritik_vurus"));
        Assert.That(r.AdjectiveId, Is.EqualTo("keskinlik"));
    }

    [Test]
    public void Triple_IsCompoundPlusThirdAdjective_NotPairSkillCard()
    {
        // (Ateş+Ateş)=Alev bileşik + 3. Ateş sıfatı — Ateş Topu DEĞİL
        SkillResolution r = LoadFull().Resolve(new[] { 1, 1, 1 });
        Assert.That(r.DisplayName, Is.Not.EqualTo("Ateş Topu"));
        Assert.That(r.DisplayName, Does.Contain("Alev"));
        Assert.That(r.VerbId, Is.EqualTo("kritik_vurus")); // Alev fiili
        Assert.That(r.AdjectiveId, Is.EqualTo("yogunlastirma")); // Ateş sıfatı
        Assert.That(r.AdjectiveId, Is.Not.EqualTo("keskinlik"));
        Assert.That(r.SkillId, Does.StartWith("fold3:"));
        Assert.That(r.LengthRole, Is.EqualTo("Durumsal"));
        Assert.That(r.LengthCastMult, Is.EqualTo(1.4f).Within(0.01f));
    }

    [Test]
    public void Triple_AlevPlusSuAdjective()
    {
        SkillResolution r = LoadFull().Resolve(new[] { 1, 1, 2 });
        Assert.That(r.DisplayName, Is.Not.EqualTo("Ateş Topu"));
        Assert.That(r.VerbId, Is.EqualTo("kritik_vurus"));
        Assert.That(r.AdjectiveId, Is.EqualTo("yayma"));
        Assert.That(r.DisplayName, Does.Contain("Alev"));
    }

    [Test]
    public void Quad_AlevPlusAlev_IsNotAtesTopu()
    {
        SkillResolution r = LoadFull().Resolve(new[] { 1, 1, 1, 1 });
        Assert.That(r.DisplayName, Is.EqualTo("Alev+Alev"));
        Assert.That(r.DisplayName, Is.Not.EqualTo("Ateş Topu"));
        Assert.That(r.SkillId, Does.StartWith("fold4:"));
        Assert.That(r.VerbId, Is.EqualTo("kritik_vurus")); // sol Alev fiili
        Assert.That(r.AdjectiveId, Is.EqualTo("keskinlik")); // sağ Alev sıfatı
        Assert.That(r.LengthRole, Is.EqualTo("Dar Cevap"));
        Assert.That(r.LengthMobility, Is.EqualTo("rooted"));
        Assert.That(r.LengthCastMult, Is.EqualTo(2.0f).Within(0.01f));
    }

    [Test]
    public void Quad_MixedCompounds()
    {
        // (1-2)=Buhar + (5-6)=? 
        SkillResolution r = LoadFull().Resolve(new[] { 1, 2, 5, 6 });
        Assert.That(r.DisplayName, Is.Not.EqualTo("Buhar Perdesi"));
        Assert.That(r.SkillId, Does.StartWith("fold4:"));
        Assert.That(r.VerbId, Is.EqualTo("gorus_kapatma")); // Buhar fiili
        Assert.That(r.LengthRole, Is.EqualTo("Dar Cevap"));
    }

    [Test]
    public void EmbeddedFallback_StillResolvesCore()
    {
        SkillResolution r = SkillMotor.CreateDefault().Resolve(new[] { 1 });
        Assert.That(r.DisplayName, Is.EqualTo("Ateş"));
        Assert.That(r.VerbName, Is.EqualTo("Saldırı"));
        Assert.That(r.Mechanics, Does.Contain("burn"));
    }

    // --- v4.2.2 / motor_parse_extension adım 1: yeni alanlar ---

    [Test]
    public void Core_Su_ReadsCooldownResourceAndSpecial()
    {
        // Su kökü: verb_id=iyilestirme — animation_type/target_mode/base_cooldown_sec zaten
        // JSON'daydı ama motor okumuyordu; base_resource_cost + special bu turda eklendi.
        SkillResolution r = LoadFull().Resolve(new[] { 2 });
        Assert.That(r.VerbId, Is.EqualTo("iyilestirme"));
        Assert.That(r.AnimationType, Is.EqualTo("cast_self"));
        Assert.That(r.TargetMode, Is.EqualTo("self_or_ally"));
        Assert.That(r.BaseCooldownSec, Is.EqualTo(6f).Within(0.01f));
        Assert.That(r.BaseResourceCost, Is.EqualTo(12f).Within(0.01f));
        Assert.That(r.Special["heal_value"].AsFloat(), Is.EqualTo(30f).Within(0.01f));
        Assert.That(r.Special["regen_per_sec"].AsFloat(), Is.EqualTo(3f).Within(0.01f));
    }

    [Test]
    public void Compound_Camur_ReadsZoneEffect()
    {
        // 2-4 Çamur — verb_id=zemin_kontrolu, zone_effect ground_line/slow/root_after_3_sec.
        SkillResolution r = LoadFull().Resolve(new[] { 2, 4 });
        Assert.That(r.DisplayName, Is.EqualTo("Çamur Şeridi"));
        Assert.That(r.ZoneEffect["type"].AsString(), Is.EqualTo("ground_line"));
        Assert.That(r.ZoneEffect["duration_sec"].AsFloat(), Is.EqualTo(8f).Within(0.01f));
        Assert.That(r.ZoneEffect["effects"]["slow"].AsFloat(), Is.EqualTo(0.4f).Within(0.01f));
        Assert.That(r.ZoneEffect["effects"]["root_after_3_sec"].AsBool(), Is.True);
    }

    [Test]
    public void Compound_Murekkep_ReadsTargetBehaviors()
    {
        // 2-6 Mürekkep — verb_id=kaynak_transferi, selective target_mode + target_behaviors.
        SkillResolution r = LoadFull().Resolve(new[] { 2, 6 });
        Assert.That(r.VerbId, Is.EqualTo("kaynak_transferi"));
        Assert.That(r.TargetMode, Is.EqualTo("selective"));
        Assert.That(r.TargetBehaviors.ContainsKey("self"), Is.True);
        Assert.That(r.TargetBehaviors.ContainsKey("enemy"), Is.True);
        Assert.That(r.TargetBehaviors.ContainsKey("ally"), Is.True);
        Assert.That(r.TargetBehaviors["self"], Is.Not.Empty);
    }

    [Test]
    public void Adjective_Yogunlastirma_ExposesFullEngineModifiers()
    {
        // engine_modifiers artık üç sabit alanla sınırlı değil — pierce_armor_flat gibi
        // 20+ alandan herhangi biri EngineModifiers[key] ile okunabilir.
        SkillResolution r = LoadFull().Resolve(new[] { 1 });
        Assert.That(r.AdjectiveId, Is.EqualTo("yogunlastirma"));
        Assert.That(r.EngineModifiers["pierce_armor_flat"].AsFloat(), Is.EqualTo(30f).Within(0.01f));
        Assert.That(r.EngineModifiers["trajectory_override"].AsString(), Is.EqualTo("raycast"));
        Assert.That(r.DamageMult, Is.EqualTo(0.85f).Within(0.01f)); // eski tipli alan hâlâ çalışıyor
    }

    [Test]
    public void KnownConflicts_StayLockedNotOverwritten()
    {
        // 2-5 Zehir (aktif_zehirlenme) ve 3-2 Pus (kisisel_isinlanma): v5.2 kaynağı bunları
        // İksir/tam_arinma ve hız+görünmezlik'e geri almak istiyordu; sahibi kararı: kilitli
        // hâli koru. base_resource_cost bilerek eklenmedi (bkz. docs/durum.md).
        var motor = LoadFull();
        SkillResolution zehir = motor.Resolve(new[] { 2, 5 });
        Assert.That(zehir.VerbId, Is.EqualTo("aktif_zehirlenme"));
        Assert.That(zehir.BaseResourceCost, Is.EqualTo(0f));

        SkillResolution pus = motor.Resolve(new[] { 3, 2 });
        Assert.That(pus.VerbId, Is.EqualTo("kisisel_isinlanma"));
        Assert.That(pus.BaseResourceCost, Is.EqualTo(0f));
    }
}

[TestFixture]
public class MiniJsonTests
{
    [Test]
    public void Parses_NestedObjectsArraysAndScalars()
    {
        JsonValue v = MiniJson.Parse(@"{
            ""name"": ""Test"",
            ""num"": 12.5,
            ""flag"": true,
            ""nil"": null,
            ""list"": [1, 2, 3],
            ""nested"": { ""inner"": ""value"" }
        }");
        Assert.That(v["name"].AsString(), Is.EqualTo("Test"));
        Assert.That(v["num"].AsFloat(), Is.EqualTo(12.5f).Within(0.001f));
        Assert.That(v["flag"].AsBool(), Is.True);
        Assert.That(v["nil"].IsNull, Is.True);
        Assert.That(v["list"].AsArray().Count, Is.EqualTo(3));
        Assert.That(v["nested"]["inner"].AsString(), Is.EqualTo("value"));
    }

    [Test]
    public void MissingField_ReturnsNullSentinel_NoThrow()
    {
        JsonValue v = MiniJson.Parse(@"{""a"": 1}");
        Assert.That(v["b"].IsNull, Is.True);
        Assert.That(v["b"]["c"].IsNull, Is.True);
        Assert.That(v["b"].AsFloat(7f), Is.EqualTo(7f));
    }

    [Test]
    public void AsTextOrField_HandlesFlatStringAndNestedReadAs()
    {
        JsonValue flat = MiniJson.Parse(@"{""self"": ""kendine_topla""}")["self"];
        Assert.That(flat.AsTextOrField("read_as"), Is.EqualTo("kendine_topla"));

        JsonValue nested = MiniJson.Parse(@"{""self"": {""read_as"": ""Kendine topla""}}")["self"];
        Assert.That(nested.AsTextOrField("read_as"), Is.EqualTo("Kendine topla"));
    }
}
