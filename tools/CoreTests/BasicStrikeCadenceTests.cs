using Dovus.App.Casting;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using System.IO;

namespace CoreTests;

[TestFixture]
public sealed class BasicStrikeCadenceTests
{
    const float YayPrefire = 0.35f;
    const float YayRecovery = 0.20f;
    const float KilicPrefire = 0.20f;
    const float KilicRecovery = 0.20f;

    static RuleEngineV4Catalog Catalog()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "unity", "Assets", "Resources", "RuleEngineV4", "kural-motoru-v4.json"));
        return RuleEngineV4Catalog.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void SliceGrammar_BasicInterval_IsPrefirePlusRecovery()
    {
        string json = SliceGrammarTestPaths.ReadSliceGrammarJson();
        var doc = GrammarJsonDocument.Parse(json);
        Assert.That(doc.Root["weapons"].AsArray(), Has.Count.EqualTo(2));

        float yayInterval = ReadWeaponBasicInterval(doc, 2);
        float kilicInterval = ReadWeaponBasicInterval(doc, 4);
        Assert.That(yayInterval, Is.EqualTo(YayPrefire + YayRecovery).Within(1e-5f));
        Assert.That(kilicInterval, Is.EqualTo(KilicPrefire + KilicRecovery).Within(1e-5f));
    }

    [Test]
    public void Catalog_YayAndKilic_CadenceMatchesJson()
    {
        var catalog = Catalog();
        Assert.That(catalog.TryGetWeapon(2, out RuleEngineV4Weapon yay), Is.True);
        Assert.That(catalog.TryGetWeapon(4, out RuleEngineV4Weapon kilic), Is.True);
        Assert.That(BasicStrikeCadenceRules.ClipSec(yay.PrefireSec, yay.RecoverySec),
            Is.EqualTo(0.55f).Within(1e-5f));
        Assert.That(BasicStrikeCadenceRules.ClipSec(kilic.PrefireSec, kilic.RecoverySec),
            Is.EqualTo(0.40f).Within(1e-5f));
    }

    [Test]
    public void BasicCadenceReady_UsesFullClipBetweenHarvests()
    {
        double gap = BasicStrikeCadenceRules.ClipMs(YayPrefire, YayRecovery);
        Assert.That(BasicStrikeCadenceRules.CadenceReady(0, -1, YayPrefire, YayRecovery), Is.True);
        Assert.That(BasicStrikeCadenceRules.CadenceReady(gap - 1, 0, YayPrefire, YayRecovery), Is.False);
        Assert.That(BasicStrikeCadenceRules.CadenceReady(gap, 0, YayPrefire, YayRecovery), Is.True);
    }

    [Test]
    public void BangAtWorldMs_IsNowPlusPrefire()
    {
        Assert.That(BasicStrikeCadenceRules.BangAtWorldMs(1000, KilicPrefire),
            Is.EqualTo(1000 + KilicPrefire * 1000.0).Within(0.5));
    }

    [Test]
    public void PrefireLock_EndsWhenPrefireEnds_NotAfterFullClip()
    {
        double start = 500;
        double unlock = start + YayPrefire * 1000.0;
        Assert.That(unlock, Is.LessThan(start + BasicStrikeCadenceRules.ClipMs(YayPrefire, YayRecovery)));
    }

    [Test]
    public void Y24_QueuesOneTapDuringClipAndFiresAtClipEnd()
    {
        float clip = BasicStrikeCadenceRules.ClipSec(KilicPrefire, KilicRecovery);
        var gate = new BasicStrikeTapGate();
        Assert.That(gate.TryAcceptTap(0, clip), Is.True);

        Assert.That(gate.TryAcceptTap(50, clip), Is.False);
        Assert.That(gate.TryAcceptTap(150, clip), Is.False);
        Assert.That(gate.HasQueuedTap, Is.True);

        int starts = 0;
        Assert.That(gate.Tick(399, () => starts++), Is.False);
        Assert.That(gate.Tick(400.0, () => starts++), Is.True);
        Assert.That(starts, Is.EqualTo(1));
    }

    [Test]
    public void EquipmentProfile_BasicInterval_MatchesGrammarAfterV4Build()
    {
        var catalog = EquipmentCatalog.FromJson(SliceGrammarTestPaths.ReadSliceGrammarJson());
        Assert.That(catalog.FindWeapon(2).Profile.BasicIntervalSec, Is.EqualTo(0.55f).Within(1e-5f));
        Assert.That(catalog.FindWeapon(4).Profile.BasicIntervalSec, Is.EqualTo(0.40f).Within(1e-5f));
    }

    static float ReadWeaponBasicInterval(GrammarJsonDocument doc, int weaponId)
    {
        foreach (var row in doc.Root["weapons"].AsArray())
        {
            if (row["id"].AsInt() != weaponId)
                continue;
            return row["basic"]["interval_sec"].AsFloat();
        }
        Assert.Fail($"weapon {weaponId} yok");
        return 0f;
    }
}
