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
        // v5.3 (16 Eylül): kaynakta 41 benzersiz sıfat var (42 değil) — veri gerçeği, uydurma değil.
        Assert.That(m.AdjectiveCount, Is.EqualTo(41));
        Assert.That(m.CoreName(1), Is.EqualTo("Ateş"));
        Assert.That(m.CoreName(3), Is.EqualTo("Hava"));
        Assert.That(m.CoreName(6), Is.EqualTo("Karanlık"));
    }

    [Test]
    public void Single_Core_IsRootSkill_NotCompoundCard()
    {
        // v5.3: kök elementlere de skill_name geldi ("Ateş" → "Ateş Dokunuşu").
        SkillResolution r = LoadFull().Resolve(new[] { 1 });
        Assert.That(r.DisplayName, Is.EqualTo("Ateş Dokunuşu"));
        Assert.That(r.VerbId, Is.EqualTo("saldiri"));
        Assert.That(r.AdjectiveId, Is.EqualTo("yogunlastirma"));
        Assert.That(r.SkillId, Is.EqualTo("core:1"));
    }

    [Test]
    public void Pair_AtesSu_IsBuharPerdesi_WithBlind()
    {
        // v5.3: 1-2'nin skill_name'i "Buhar Perdesi"'den "Yayılan Ateş"e değişti (element adı
        // "Buhar" aynı kaldı).
        SkillResolution r = LoadFull().Resolve(new[] { 1, 2 });
        Assert.That(r.ElementName, Is.EqualTo("Buhar"));
        Assert.That(r.DisplayName, Is.EqualTo("Yayılan Ateş"));
        Assert.That(r.VerbId, Is.EqualTo("gorus_kapatma"));
        Assert.That(r.Mechanics, Does.Contain("blind"));
        Assert.That(r.LengthRole, Is.EqualTo("Temel"));
    }

    [Test]
    public void Pair_AtesAtes_IsAtesTopu_SkillCard()
    {
        // v5.3: skill_name "Ateş Topu" olarak kaldı, ama skill_id alanı v5.3'te hiç yok —
        // SkillMotor'un "pair:" varsayılanına düşüyor (bug değil, kaynakta alan yok).
        SkillResolution r = LoadFull().Resolve(new[] { 1, 1 });
        Assert.That(r.ElementName, Is.EqualTo("Alev"));
        Assert.That(r.DisplayName, Is.EqualTo("Ateş Topu"));
        Assert.That(r.SkillId, Is.EqualTo("pair:1-1"));
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
    public void Core_Su_ReadsCooldownAndResource()
    {
        // Su kökü: verb_id=iyilestirme — animation_type/target_mode/base_cooldown_sec/
        // base_resource_cost hâlâ okunuyor. v5.3: "special" alanı verb'lerden tamamen
        // kaldırıldı (heal_value vb. artık JSON'da yok — gameplay hiç kullanmıyordu, kayıp yok).
        SkillResolution r = LoadFull().Resolve(new[] { 2 });
        Assert.That(r.VerbId, Is.EqualTo("iyilestirme"));
        Assert.That(r.AnimationType, Is.EqualTo("cast_self"));
        Assert.That(r.TargetMode, Is.EqualTo("self_or_ally"));
        Assert.That(r.BaseCooldownSec, Is.EqualTo(6f).Within(0.01f));
        Assert.That(r.BaseResourceCost, Is.EqualTo(12f).Within(0.01f));
        Assert.That(r.Special.IsNull, Is.True);
    }

    [Test]
    public void Compound_Camur_ZoneEffectMovedToManipulationLayers()
    {
        // 2-4 Çamur — v5.3'te verb.zone_effect kaldırıldı; zone verisi artık ayrı bir bölümde:
        // manipulation_layers.zone_layer.zones[].element == "Çamur" (SkillMotor Zones listesinde
        // okunuyor; dünyaya uygulama ayrı görev). Burada verb/mekanik doğru çözülüyor mu.
        SkillResolution r = LoadFull().Resolve(new[] { 2, 4 });
        Assert.That(r.DisplayName, Is.EqualTo("Çamur Şeridi"));
        Assert.That(r.VerbId, Is.EqualTo("zemin_kontrolu"));
        Assert.That(r.Mechanics, Does.Contain("slow"));
        Assert.That(r.Mechanics, Does.Contain("root"));
        Assert.That(r.ZoneEffect.IsNull, Is.True);
    }

    [Test]
    public void Compound_Murekkep_TargetBehaviorsMissingInV53()
    {
        // 2-6 Mürekkep — v5.3'te target_mode="selective" ama target_behaviors JSON'da YOK.
        // target_modes.selective açıkça "target_behaviors gerekir" diyor — bu v5.3'ün kendi
        // kuralına aykırı gerçek bir veri açığı (uydurmadım, boş kalıyor).
        SkillResolution r = LoadFull().Resolve(new[] { 2, 6 });
        Assert.That(r.VerbId, Is.EqualTo("kaynak_transferi"));
        Assert.That(r.TargetMode, Is.EqualTo("selective"));
        Assert.That(r.TargetBehaviors.Count, Is.EqualTo(0));
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
    public void V53_RevertedZehirAndPus_NowHaveRealResourceCost()
    {
        // 16 Eylül, 3. tur: sahibi "v5.3 otorite, geri al" dedi — 2-5 tekrar İksir/tam_arinma,
        // 3-2 tekrar Pus/hiz_gorunmezlik oldu (önceki turun "kilitli kalsın" kararı BİLEREK
        // geri alındı). v5.3 ikisine de gerçek base_resource_cost veriyor (eski kilitli
        // halde 0'dı — bilinen açıktı).
        var motor = LoadFull();
        SkillResolution iksir = motor.Resolve(new[] { 2, 5 });
        Assert.That(iksir.VerbId, Is.EqualTo("tam_arinma"));
        Assert.That(iksir.BaseResourceCost, Is.EqualTo(20f).Within(0.01f));

        SkillResolution pus = motor.Resolve(new[] { 3, 2 });
        Assert.That(pus.VerbId, Is.EqualTo("hiz_gorunmezlik"));
        Assert.That(pus.BaseResourceCost, Is.EqualTo(15f).Within(0.01f));
    }

    [Test]
    public void FullJson_ParsesPassivesChainsStatusInteractionsAndZones_MatchingJsonCounts()
    {
        // Sayılar uydurulmaz — aynı dosyadan MiniJson ile sayılır, motor birebir eşleşmeli.
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
        JsonValue root = MiniJson.Parse(json);

        int expectedPassives = root["passives"]["list"].AsArray().Count;
        int expectedChains = root["chain_mechanics"]["chains"].AsArray().Count;
        int expectedZones = root["manipulation_layers"]["zone_layer"]["zones"].AsArray().Count;
        int expectedMaxZones = root["manipulation_layers"]["zone_layer"]["max_active_zones"].AsInt();
        int expectedInteractions = 0;
        foreach (var kv in root["status_interaction_table"].AsObject())
        {
            if (kv.Value.Kind == JsonKind.Array)
                expectedInteractions += kv.Value.AsArray().Count;
        }

        var motor = SkillMotor.FromJson(json);
        Assert.That(motor.Passives.Count, Is.EqualTo(expectedPassives));
        Assert.That(motor.Chains.Count, Is.EqualTo(expectedChains));
        Assert.That(motor.StatusInteractions.Count, Is.EqualTo(expectedInteractions));
        Assert.That(motor.Zones.Count, Is.EqualTo(expectedZones));
        Assert.That(motor.MaxActiveZones, Is.EqualTo(expectedMaxZones));

        // Verb alanları da çözüme taşınıyor (saldiri: crit + Ateş + magical).
        SkillResolution r = motor.Resolve(new[] { 1 });
        Assert.That(r.CritEligible, Is.True);
        Assert.That(r.ElementOrigin, Is.EqualTo("Ateş"));
        Assert.That(r.DamageType, Is.EqualTo("magical"));
    }

    [Test]
    public void FullJson_ParsesSpaceEffects_MatchingJsonCountAndOptionalFields()
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
        JsonValue layer = MiniJson.Parse(json)["manipulation_layers"]["space_layer"];
        int expected = layer["effects"].AsArray().Count;
        int expectedMaxLinks = layer["max_active_links"].AsInt();

        var motor = SkillMotor.FromJson(json);
        Assert.That(motor.SpaceEffects.Count, Is.EqualTo(expected));
        Assert.That(motor.MaxActiveLinks, Is.EqualTo(expectedMaxLinks));

        SpaceEffectNode alev = default;
        SpaceEffectNode zenitsu = default;
        SpaceEffectNode tear = default;
        foreach (SpaceEffectNode e in motor.SpaceEffects)
        {
            if (e.Id == "alev_isinlanma") alev = e;
            if (e.Id == "yildirim_zenitsu") zenitsu = e;
            if (e.Id == "hiclik_yarik") tear = e;
        }

        Assert.That(alev.Id, Is.EqualTo("alev_isinlanma"));
        Assert.That(alev.Type, Is.EqualTo("short_blink"));
        Assert.That(alev.HasDistanceM, Is.True);
        Assert.That(alev.DistanceM, Is.EqualTo(3f).Within(0.01f));
        Assert.That(alev.HasIFrameMs, Is.True);
        Assert.That(alev.IFrameMs, Is.EqualTo(300));
        Assert.That(alev.HasDamageOnPass, Is.False);

        Assert.That(zenitsu.Type, Is.EqualTo("phase_blink"));
        Assert.That(zenitsu.HasDistanceM, Is.True);
        Assert.That(zenitsu.DistanceM, Is.EqualTo(8f).Within(0.01f));
        Assert.That(zenitsu.HasDamageOnPass, Is.True);
        Assert.That(zenitsu.DamageOnPass, Is.True);
        Assert.That(zenitsu.HasIFrameMs, Is.False);

        Assert.That(tear.Type, Is.EqualTo("tear"));
        Assert.That(tear.HasDurationSec, Is.True);
        Assert.That(tear.DurationSec, Is.EqualTo(3f).Within(0.01f));
        Assert.That(tear.HasDamageOnCross, Is.True);
        Assert.That(tear.DamageOnCross, Is.EqualTo(30f).Within(0.01f));
    }

    [Test]
    public void FullJson_ParsesRealityEffects_MatchingJsonCountAndTypes()
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
        JsonValue layer = MiniJson.Parse(json)["manipulation_layers"]["reality_layer"];
        int expected = layer["effects"].AsArray().Count;

        var motor = SkillMotor.FromJson(json);
        Assert.That(motor.RealityEffects.Count, Is.EqualTo(expected));
        Assert.That(expected, Is.EqualTo(3));

        RealityEffectNode revive = default;
        RealityEffectNode partial = default;
        RealityEffectNode full = default;
        foreach (RealityEffectNode e in motor.RealityEffects)
        {
            if (e.Id == "cehennem_dirilis_engeli") revive = e;
            if (e.Id == "karabasan_koruma_silme") partial = e;
            if (e.Id == "hiclik_varlik_silme") full = e;
        }

        Assert.That(revive.Type, Is.EqualTo("revive_block"));
        Assert.That(revive.HasDurationSec, Is.True);
        Assert.That(revive.DurationSec, Is.EqualTo(5f).Within(0.01f));
        Assert.That(revive.Element, Is.EqualTo("Cehennem"));

        Assert.That(partial.Type, Is.EqualTo("partial_erase"));
        Assert.That(partial.Targets.Count, Is.EqualTo(3));

        Assert.That(full.Type, Is.EqualTo("full_erase"));
        Assert.That(full.Targets.Count, Is.EqualTo(3));
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
