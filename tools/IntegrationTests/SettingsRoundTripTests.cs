using System;
using System.IO;
using Dovus.Core.Tuning;
using Dovus.Game;
using NUnit.Framework;
using UnityEngine;

namespace IntegrationTests;

[TestFixture]
public class SettingsRoundTripTests
{
    string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dovus-integration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Application.persistentDataPath = _tempDir;
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // temp cleanup best-effort
        }
    }

    [Test]
    public void SaveAndTryLoad_RoundTripsCombatAndPrototypeFields()
    {
        var combat = new CombatTuning();
        combat.ClosingDamagePerEffect = 4.25f;
        combat.BasicStrikePower = 11f;

        var prototype = new PrototypeTuning();
        prototype.DodgeGlideSpeedMps = 9.5f;
        prototype.PlayerMaxHp = 420;

        var original = TuningConfig.Create(combat, prototype);
        original.Save();

        var loaded = TuningConfig.Create(new CombatTuning(), new PrototypeTuning());
        Assert.That(loaded.TryLoad(), Is.True);
        Assert.That(loaded.Combat.ClosingDamagePerEffect, Is.EqualTo(4.25f).Within(0.001f));
        Assert.That(loaded.Combat.BasicStrikePower, Is.EqualTo(11f).Within(0.001f));
        Assert.That(loaded.Prototype.DodgeGlideSpeedMps, Is.EqualTo(9.5f).Within(0.001f));
        Assert.That(loaded.Prototype.PlayerMaxHp, Is.EqualTo(420));
    }

    [Test]
    public void StaleVersion_DiscardsAndBacksUp()
    {
        string path = TuningConfig.FilePath;
        string staleJson =
            "{\"version\":1,\"combat\":{\"ClosingDamagePerEffect\":99.0},\"prototype\":{\"ArenaHalfSizeM\":99.0}}";
        File.WriteAllText(path, staleJson);

        var config = TuningConfig.Create(new CombatTuning(), new PrototypeTuning());
        Assert.That(config.TryLoad(), Is.False);
        Assert.That(config.LastLoadDiscardedStale, Is.True);
        Assert.That(File.Exists(TuningConfig.StaleBackupPath(1)), Is.True);

        float defaultClosing = new CombatTuning().ClosingDamagePerEffect;
        Assert.That(config.Combat.ClosingDamagePerEffect, Is.EqualTo(defaultClosing).Within(0.001f));
    }

    [Test]
    public void CorruptOrEmptyFile_ReturnsFalseWithoutThrowing()
    {
        string path = TuningConfig.FilePath;
        File.WriteAllText(path, "");
        var config = TuningConfig.Create(new CombatTuning(), new PrototypeTuning());
        Assert.That(config.TryLoad(), Is.False);

        File.WriteAllText(path, "{not-json");
        Assert.That(config.TryLoad(), Is.False);
    }
}
