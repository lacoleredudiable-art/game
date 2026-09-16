using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// docs/element-sistemi.json "passives" — trigger_combo ile açılır, DurationSec kadar
/// sürer; birden fazla aynı anda aktif olabilir (ActiveModeDirector'dan farkı).
/// </summary>
[TestFixture]
public class PassiveDirectorTests
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
    public void Motor_Parses_TenPassives_WithTriggerCombos()
    {
        var motor = LoadFull();
        // gorev-listesi "12" yazıyor; JSON otorite: passives.list = 10.
        Assert.That(motor.Passives.Count, Is.EqualTo(10));

        var ids = new HashSet<string>();
        foreach (var p in motor.Passives)
        {
            Assert.That(p.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(p.TriggerCombo, Is.Not.Null.And.Not.Empty);
            Assert.That(p.DurationSec, Is.GreaterThan(0f));
            Assert.That(ids.Add(p.Id), Is.True, $"çift pasif id: {p.Id}");
        }
    }

    [Test]
    public void TryTrigger_MatchesEveryPassiveTriggerCombo()
    {
        var motor = LoadFull();
        var director = new PassiveDirector(motor.Passives);

        foreach (var p in motor.Passives)
        {
            // Her pasifi ayrı director'da tetikle — süre çakışması karıştırmasın.
            var solo = new PassiveDirector(motor.Passives);
            var activated = solo.TryTrigger(p.TriggerCombo, worldMs: 0);
            Assert.That(activated, Is.Not.Null, $"eşleşmedi: {p.Id}");
            Assert.That(activated.Value.Id, Is.EqualTo(p.Id));
            Assert.That(solo.IsActive(p.Id), Is.True);
        }

        // Yanlış dizi hiçbirini açmamalı.
        Assert.That(director.TryTrigger(new[] { 9, 9, 9, 9 }, 0), Is.Null);
        Assert.That(director.ActiveCount, Is.EqualTo(0));
    }

    [Test]
    public void Tick_ExpiresAfterDuration()
    {
        var motor = LoadFull();
        var p = motor.Passives[0];
        var director = new PassiveDirector(motor.Passives);

        Assert.That(director.TryTrigger(p.TriggerCombo, worldMs: 1000), Is.Not.Null);
        Assert.That(director.IsActive(p.Id), Is.True);

        double almostDone = 1000 + (p.DurationSec * 1000.0) - 1;
        Assert.That(director.Tick(almostDone), Is.False);
        Assert.That(director.IsActive(p.Id), Is.True);
        Assert.That(director.RemainingSec(p.Id, almostDone), Is.GreaterThan(0f));

        double done = 1000 + (p.DurationSec * 1000.0) + 1;
        Assert.That(director.Tick(done), Is.True);
        Assert.That(director.IsActive(p.Id), Is.False);
        Assert.That(director.ActiveCount, Is.EqualTo(0));
    }

    [Test]
    public void TwoPassives_CanBeActiveSimultaneously()
    {
        var motor = LoadFull();
        // alev 15s, karabasan 8s — kısa olan düşerken diğeri kalmalı.
        PassiveNode longP = motor.Passives.First(x => x.Id == "alev_hiddeti");
        PassiveNode shortP = motor.Passives.First(x => x.Id == "karabasan_acisi");

        var director = new PassiveDirector(motor.Passives);
        Assert.That(director.TryTrigger(longP.TriggerCombo, worldMs: 0)?.Id, Is.EqualTo(longP.Id));
        Assert.That(director.TryTrigger(shortP.TriggerCombo, worldMs: 0)?.Id, Is.EqualTo(shortP.Id));

        Assert.That(director.ActiveCount, Is.EqualTo(2));
        Assert.That(director.IsActive(longP.Id), Is.True);
        Assert.That(director.IsActive(shortP.Id), Is.True);

        double afterShort = shortP.DurationSec * 1000.0 + 1;
        Assert.That(director.Tick(afterShort), Is.True);
        Assert.That(director.IsActive(shortP.Id), Is.False);
        Assert.That(director.IsActive(longP.Id), Is.True);
        Assert.That(director.ActiveCount, Is.EqualTo(1));
    }

    [Test]
    public void TryTrigger_RefreshesDuration_WhenSamePassiveRetriggered()
    {
        var motor = LoadFull();
        var p = motor.Passives.First(x => x.Id == "alev_hiddeti");
        var director = new PassiveDirector(motor.Passives);

        director.TryTrigger(p.TriggerCombo, worldMs: 0);
        director.TryTrigger(p.TriggerCombo, worldMs: 5000);
        Assert.That(director.ActiveCount, Is.EqualTo(1));
        Assert.That(director.RemainingSec(p.Id, 5000), Is.EqualTo(p.DurationSec).Within(0.01f));
    }

    [Test]
    public void AlevHiddeti_DamageMult_AppliesWhileActive()
    {
        var motor = LoadFull();
        var p = motor.Passives.First(x => x.Id == "alev_hiddeti");
        var director = new PassiveDirector(motor.Passives);

        Assert.That(director.DamageMult, Is.EqualTo(1f));
        director.TryTrigger(p.TriggerCombo, worldMs: 0);
        Assert.That(director.DamageMult, Is.EqualTo(1.15f).Within(0.001f));
        director.Tick(p.DurationSec * 1000.0 + 1);
        Assert.That(director.DamageMult, Is.EqualTo(1f));
    }
}
