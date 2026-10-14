using System.IO;
using Dovus.App.Boss;
using Dovus.Core.Boss;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Adım 2.3 (envanter A2): dilimde can (O-13.1, O-6.11) ve boss saldırı hasarı (O-6.9, O-6.14–O-6.18) yalnız
/// kural-motoru-v4.json'dan. Oyun yolundaki ölçüm PlayMode <c>RuleEngineV4WorldPhysicsPlayModeTests.HpScale</c>'te.
/// </summary>
[TestFixture]
public sealed class RuleEngineV4HpScaleCatalogTests
{
    static string JsonText => File.ReadAllText(Path.GetFullPath(Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "..",
        "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json")));

    [Test]
    public void O_13_1_O_6_11_Json_Scale_HpMatchesSpec()
    {
        RuleEngineV4Scale s = RuleEngineV4Catalog.FromJson(JsonText).Scale;
        Assert.That(s.PlayerMaxHp, Is.EqualTo(1000f), "O-13.1 player_max_hp");
        Assert.That(s.BossMaxHp, Is.EqualTo(15000f), "O-6.11 boss_max_hp");
        Assert.That(s.MissingKeys, Is.Empty, "scale bloğunda eksik anahtar");
    }

    [Test]
    public void O_13_1_Scale_MissingKey_Recorded()
    {
        string json = JsonText.Replace("\"player_max_hp\": 1000,", string.Empty);
        Assert.That(json, Is.Not.EqualTo(JsonText), "test JSON'u değişmedi");
        RuleEngineV4Scale s = RuleEngineV4Catalog.FromJson(json).Scale;
        Assert.That(s.MissingKeys, Is.EqualTo(new[] { "player_max_hp" }),
            "eksik scale anahtarı sessiz kod varsayılanına düştü ([V4-FALLBACK] yazılamaz)");
    }

    [Test]
    public void O_6_14_O_6_18_Json_SliceBossAttacks_MatchesSpec()
    {
        RuleEngineV4SliceBossAttacks a = RuleEngineV4Catalog.FromJson(JsonText).SliceBossAttacks;
        Assert.That(a.HasLegSlamDamage && a.HasWebSpitDamagePerProjectile && a.HasPoisonBreathDamage
                    && a.HasPoisonBreathPoisonSec && a.HasPoisonBreathPoisonPerSec && a.HasPounceDamage, Is.True);
        Assert.That(a.LegSlamDamage, Is.EqualTo(220f), "O-6.14 Bacak Çakması");
        Assert.That(a.WebSpitDamagePerProjectile, Is.EqualTo(60f), "O-6.15 Ağ Tükürüğü mermi başına");
        Assert.That(a.PoisonBreathDamage, Is.EqualTo(180f), "O-6.17 Zehir Nefesi");
        Assert.That(a.PoisonBreathPoisonSec, Is.EqualTo(3f), "O-6.17 zehir süresi");
        Assert.That(a.PoisonBreathPoisonPerSec, Is.EqualTo(20f), "O-6.17 zehir saniyede");
        Assert.That(a.PounceDamage, Is.EqualTo(140f), "O-6.18 Sıçrayış");
    }

    [Test]
    public void O_6_17_Json_ZehirNefesi_KoniVeOnIsaret()
    {
        RuleEngineV4SliceBossAttacks a = RuleEngineV4Catalog.FromJson(JsonText).SliceBossAttacks;
        Assert.That(a.HasPoisonBreathConeRangeM && a.HasPoisonBreathConeAngleDeg && a.HasPoisonBreathWindupSec, Is.True);
        Assert.That(a.PoisonBreathConeRangeM, Is.EqualTo(8f), "O-6.17 koni menzili, boss merkezinden");
        Assert.That(a.PoisonBreathConeAngleDeg, Is.EqualTo(60f), "O-6.17 koni TAM açısı");
        Assert.That(a.PoisonBreathWindupSec, Is.EqualTo(0.8f).Within(1e-5f), "O-6.17 ön işaret");
    }

    [Test]
    public void O_6_9_SliceBossAttacks_MissingField_HasFalse_NoDefault()
    {
        RuleEngineV4SliceBossAttacks a = RuleEngineV4SliceBossAttacks.FromJson(
            MiniJson.Parse("{\"slice_boss_attacks\":{\"leg_slam_damage\":220,\"pounce_damage\":\"140\"}}"));
        Assert.That(a.HasLegSlamDamage, Is.True);
        Assert.That(a.HasPounceDamage, Is.False, "pounce_damage sayı değil");
        Assert.That(a.HasWebSpitDamagePerProjectile, Is.False);
        Assert.That(a.WebSpitDamagePerProjectile, Is.Zero);

        RuleEngineV4SliceBossAttacks none = RuleEngineV4SliceBossAttacks.FromJson(MiniJson.Parse("{}"));
        Assert.That(none.HasLegSlamDamage || none.HasPoisonBreathDamage || none.HasPounceDamage, Is.False);
    }

    [Test]
    public void O_6_9_BossAttack_DamageTable_ReplacesTuningDamage()
    {
        var table = new BossAttackDamage(220, 60, 180, 140, 3f, 20f);
        var attack = new BossAttack(new BossTuning(), table);
        attack.ApplyVariant(SlamVariant.Genis);
        Assert.That(attack.Damage, Is.EqualTo(220), "O-6.14");
        attack.ApplyVolley(enraged: true);
        Assert.That(attack.Damage, Is.EqualTo(60), "O-6.15");
        attack.ApplyFireCone();
        Assert.That(attack.Damage, Is.EqualTo(180), "O-6.17");
        attack.ApplyPounce();
        Assert.That(attack.Damage, Is.EqualTo(140), "O-6.18");
        attack.ApplyWebField();
        Assert.That(attack.Damage, Is.Zero);

        var legacy = new BossAttack(new BossTuning());
        Assert.That(legacy.DamageTable, Is.Null);
        Assert.That(legacy.Damage, Is.EqualTo(new BossTuning().Damage), "tablo yokken eski yol değişmedi");
    }

    [Test]
    public void O_6_17_FireCone_SliceTable_AppliesPoison3sx20()
    {
        var board = new StatusBoard();
        BossMechanicStatusMap.ApplyFireCone(
            board, new StatusTuning(), new[] { "poison" }, new BossAttackDamage(220, 60, 180, 140, 3f, 20f));
        Assert.That(board.TryGet(StatusKind.Poison, out double ms, out float perSec, out _), Is.True, "zehir yok");
        Assert.That(ms, Is.EqualTo(3000.0).Within(0.001), "O-6.17 zehir 3 s");
        Assert.That(perSec, Is.EqualTo(20f), "O-6.17 zehir saniyede 20");
    }
}
