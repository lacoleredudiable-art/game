using System.IO;
using System.Linq;
using Dovus.Core.Mechanic;
using NUnit.Framework;

namespace CoreTests;

/// <summary>mechanic_grammar: 1440 skill'in anlamlılık kontrolleri (tools/AtomSim ile aynı motor).</summary>
[TestFixture]
public class MechanicGrammarTests
{
    MechanicGrammar _grammar = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..", "docs", "element-sistemi.json"));
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "docs", "element-sistemi.json"));
        var rules = MechanicRules.FromJson(File.ReadAllText(path));
        Assert.That(rules.IsValid, Is.True, "mechanic_grammar eksik");
        _grammar = new MechanicGrammar(rules);
    }

    MechanicPlan P(int verb, int adjective, int weapon) => _grammar.Compose(verb, adjective, weapon);

    [Test]
    public void All1440_AdjectiveChangesBehavior_NoDuplicates_NoContradictions()
    {
        var all = (from w in _grammar.Rules.Weapons
                   from v in Enumerable.Range(1, 12)
                   from s in Enumerable.Range(1, 12)
                   select _grammar.Compose(v, s, w)).ToList();
        Assert.That(all.Count, Is.EqualTo(1440));

        var same = all.Where(p => p.QualSignature() == P(p.Verb, 0, p.Weapon).QualSignature())
            .Select(p => p.SkillId + "/" + p.WeaponName).ToList();
        Assert.That(same, Is.Empty, "sıfat nitel bir şey değiştirmeli");

        var dupes = all.GroupBy(p => (p.Weapon, p.QualSignature())).Where(g => g.Count() > 1)
            .Select(g => string.Join("=", g.Select(p => p.SkillId))).ToList();
        Assert.That(dupes, Is.Empty, "aynı silahta iki skill aynı olmamalı");

        var bad = all.Where(p => p.Contradictions.Count > 0)
            .Select(p => $"{p.SkillId}/{p.WeaponName}: {string.Join(",", p.Contradictions)}").ToList();
        Assert.That(bad, Is.Empty);

        Assert.That(all.Where(p => p.Labels.Count == 0).Select(p => p.SkillId + "/" + p.WeaponName), Is.Empty,
            "her skill'in okunur bir etiketi olmalı");
    }

    [TestCase(12, 4, "Dondurma")]
    [TestCase(12, 12, "Zaman alanı")]
    [TestCase(12, 8, "Hızlanma")]
    [TestCase(12, 10, "Geri sarma")]
    [TestCase(12, 2, "Zaman çalma")]
    [TestCase(3, 10, "Portal")]
    [TestCase(3, 6, "Yer değiştirme")]
    [TestCase(3, 4, "İşaretle-geri dön")]
    [TestCase(3, 9, "Arkaya ışınlanma")]
    [TestCase(11, 11, "Klon")]
    [TestCase(11, 4, "Taret")]
    [TestCase(9, 2, "Durum aktarma")]
    [TestCase(4, 4, "Duvar")]
    public void MechanicsEmergeFromRules_OnEveryWeapon(int verb, int adjective, string label)
    {
        foreach (MechanicWeapon w in _grammar.Rules.Weapons.Where(w => !(label == "Duvar" && w.Path is "isin" or "yere_vurus")))
            Assert.That(P(verb, adjective, w.Id).Labels, Does.Contain(label), $"{verb}-{adjective} / {w.Name}");
    }

    [Test]
    public void WeaponChangesDelivery()
    {
        Assert.That(P(4, 4, 6).Labels, Does.Contain("Yerden taş duvar"), "Çekiç duvarı yerden çıkarır");
        Assert.That(P(4, 4, 8).Labels, Does.Contain("Hat duvar"), "Asa duvarı hat olarak çizer");
        Assert.That(P(3, 10, 7).Labels, Does.Contain("Fırlatılma"));
        Assert.That(P(3, 10, 9).Labels, Does.Contain("Işınlanma"));
        Assert.That(P(2, 0, 4).Body.BornAt, Is.EqualTo("onunde_yay"), "Kılıç'la heal önündeki yaya gider");
        Assert.That(P(2, 0, 10).Body.Attached, Is.True, "Kalkan'la heal sana yapışık aura");
        Assert.That(P(11, 0, 7).Find("aktor_yarat")!.Has("silahla:ucan"), Is.True, "Top'la çağrılan top atar");
    }

    [Test]
    public void IncompatibleWeapon_StillWorks_AtReducedEffect()
    {
        MechanicPlan asa = P(2, 0, 8);   // Asa: uyumlu
        MechanicPlan fist = P(2, 0, 1);  // Yumruk: uyumsuz
        Assert.That(asa.Compatible, Is.True);
        Assert.That(fist.Compatible, Is.False);
        Assert.That(asa.Find("can")!.Amount, Is.EqualTo(35).Within(0.001));
        Assert.That(fist.Find("can")!.Amount, Is.EqualTo(35 * 0.8).Within(0.001));
        Assert.That(fist.Body.CastTimeMult, Is.EqualTo(1.2).Within(0.001));
    }

    [Test]
    public void ReflectLivesWhereItsBodyIs()
    {
        Assert.That(P(10, 0, 10).Find("yansit")!.Target, Is.EqualTo("kendin"), "Kalkan: sende");
        Assert.That(P(10, 0, 1).Find("yansit")!.Target, Is.EqualTo("dost"), "Yumruk: dokunduğun dost");
        Assert.That(P(10, 0, 7).Find("yansit")!.Target, Is.EqualTo("alan"), "Top: indiği yerde");
    }

    [Test]
    public void DrainAndMirror_NeverTargetStatsTheBossLacks()
    {
        Assert.That(P(4, 2, 4).Find("kalkan", "dusman"), Is.Null);
        Assert.That(P(4, 2, 4).Find("can", "dusman")!.Has("emme"), Is.True);
        Assert.That(P(8, 10, 4).Find("hasar_buff", "dusman")!.Has("ters_kopya"), Is.True,
            "güç verirken ters kopya düşmanı zayıflatır");
    }

    [Test]
    public void WorldProfile_MapsBindingAtoms_NotPresentationLabels()
    {
        Assert.That(MechanicWorldProfile.From(P(4, 4, 1)).BlocksMovement, Is.True);
        Assert.That(MechanicWorldProfile.From(P(11, 4, 7)).ActorKind, Is.EqualTo(MechanicActorKind.Turret));
        Assert.That(MechanicWorldProfile.From(P(11, 11, 4)).ActorKind, Is.EqualTo(MechanicActorKind.Clone));
        Assert.That(MechanicWorldProfile.From(P(11, 10, 4)).ActorKind, Is.EqualTo(MechanicActorKind.MirrorClone));
        Assert.That(MechanicWorldProfile.From(P(12, 10, 4)).Rewind, Is.True);
        Assert.That(MechanicWorldProfile.From(P(12, 12, 4)).TempoField, Is.True);
        Assert.That(MechanicWorldProfile.From(P(1, 9, 7)).Homing, Is.True);
        Assert.That(MechanicWorldProfile.From(P(10, 0, 1)).Reflector, Is.True);
        Assert.That(MechanicWorldProfile.From(P(3, 11, 4)).Decoy, Is.True);
        Assert.That(MechanicWorldProfile.From(P(9, 2, 4)).StatusTransfer, Is.True);
        Assert.That(MechanicWorldProfile.From(P(9, 10, 4)).BuffPurge, Is.True);
    }

    [Test]
    public void TimedHistory_ReturnsPastSample_AndRetainsBoundary()
    {
        var history = new TimedHistory<string>(retentionMs: 2000, sampleIntervalMs: 100);
        history.Record(0, "zero");
        history.Record(50, "ignored");
        history.Record(100, "one");
        history.Record(1000, "ten");
        history.Record(2200, "twenty-two");

        Assert.That(history.TryGetAtOrBefore(250, out string? at250), Is.True);
        Assert.That(at250, Is.EqualTo("one"));
        Assert.That(history.TryGetAtOrBefore(-1, out string? oldest), Is.True);
        Assert.That(oldest, Is.EqualTo("one"), "retention sınırından önceki tek örnek tutulur");
    }
}
