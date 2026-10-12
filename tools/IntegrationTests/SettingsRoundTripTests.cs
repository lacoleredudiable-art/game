using System;
using System.IO;
using Dovus.Core.Tuning;
using Dovus.Game.Config;
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
        var prototype = new GameTuning();
        var original = TuningConfig.Create(combat, prototype);
        original.RecordPanelEdit(() =>
        {
            combat.ClosingDamagePerEffect = 4.25f;
            combat.BasicStrikePower = 11f;
            combat.Sentence.Steps[1].RecoverySec = 0.9f;
            prototype.Player.DodgeGlideSpeedMps = 9.5f;
            prototype.Player.PlayerMaxHp = 420;
        });
        Assert.That(original.SaveFromPanel(), Is.True);

        var loaded = TuningConfig.Create(new CombatTuning(), new GameTuning());
        Assert.That(loaded.TryLoad(), Is.True);
        Assert.That(loaded.Combat.ClosingDamagePerEffect, Is.EqualTo(4.25f).Within(0.001f));
        Assert.That(loaded.Combat.BasicStrikePower, Is.EqualTo(11f).Within(0.001f));
        Assert.That(loaded.Combat.Sentence.Steps[1].RecoverySec, Is.EqualTo(0.9f).Within(0.001f));
        Assert.That(loaded.Prototype.Player.DodgeGlideSpeedMps, Is.EqualTo(9.5f).Within(0.001f));
        Assert.That(loaded.Prototype.Player.PlayerMaxHp, Is.EqualTo(420));
        Assert.That(loaded.IsFromFile("combat.Sentence.Steps[1].RecoverySec"), Is.True);
    }

    /// <summary>O-17.8: dosyada olup panelden KAYDET ile işaretlenmemiş alan kod değerinde kalır.</summary>
    [Test]
    public void O_17_8_UnmarkedFileValue_NotApplied()
    {
        var combat = new CombatTuning();
        var prototype = new GameTuning();
        var original = TuningConfig.Create(combat, prototype);
        original.RecordPanelEdit(() => prototype.Camera.LookAheadM = 2.2f);
        combat.BasicStrikePower = 11f;
        prototype.Player.DodgeGlideSpeedMps = 9.5f;
        Assert.That(original.SaveFromPanel(), Is.True);

        var loaded = TuningConfig.Create(new CombatTuning(), new GameTuning());
        Assert.That(loaded.TryLoad(), Is.True);
        Assert.That(loaded.Prototype.Camera.LookAheadM, Is.EqualTo(2.2f).Within(0.001f));
        Assert.That(loaded.Combat.BasicStrikePower, Is.EqualTo(new CombatTuning().BasicStrikePower).Within(0.001f));
        Assert.That(loaded.Prototype.Player.DodgeGlideSpeedMps,
            Is.EqualTo(new GameTuning().Player.DodgeGlideSpeedMps).Within(0.001f));
        Assert.That(loaded.PanelSavedFields, Is.EquivalentTo(new[] { "prototype.LookAheadM" }));
    }

    [Test]
    public void ApplyPanelFields_OldJsonShape_MapsToSections()
    {
        const string json =
            "{\"PlayerMaxHp\":17,\"DodgeGlideSpeedMps\":4.1,\"BossApproachStopPadM\":0.5," +
            "\"FollowSmoothTimeSec\":0.2,\"LookAheadM\":1.1,\"CameraShakePxToM\":0.02," +
            "\"CameraDistanceM\":6.5,\"CameraLookHeightM\":0.5,\"CameraDefaultPitchDeg\":18," +
            "\"CameraBossAimHeightM\":2.1,\"CameraLockOnMinDistanceM\":5.5," +
            "\"CameraLockOnMaxDistanceM\":9.0,\"CameraLockOnDistancePerSepM\":0.2," +
            "\"CameraLockOnMaxExtraDistanceM\":2.5,\"CameraWindupDistanceMul\":1.5," +
            "\"CameraWindupExtraHeightM\":0.7,\"ReadoutAnchorRight\":false," +
            "\"ReadoutPunchInSec\":0.15,\"ShowFrameTimeHud\":true,\"ShowDamageNumbers\":false}";

        var fields = JsonUtility.FromJson<GameTuning.PanelFields>(json);
        var tuning = new GameTuning();
        tuning.ApplyPanelFields(fields);

        Assert.Multiple(() =>
        {
            Assert.That(tuning.Player.PlayerMaxHp, Is.EqualTo(17));
            Assert.That(tuning.Player.DodgeGlideSpeedMps, Is.EqualTo(4.1f).Within(0.001f));
            Assert.That(tuning.Boss.BossApproachStopPadM, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(tuning.Camera.CameraDistanceM, Is.EqualTo(6.5f).Within(0.001f));
            Assert.That(tuning.Hud.ShowDamageNumbers, Is.False);
            Assert.That(tuning.Hud.ShowFrameTimeHud, Is.True);
        });
    }

    [Test]
    public void StaleVersion_DiscardsAndBacksUp()
    {
        string path = TuningConfig.FilePath;
        string staleJson =
            "{\"version\":1,\"combat\":{\"ClosingDamagePerEffect\":99.0},\"prototype\":{\"ArenaHalfSizeM\":99.0}}";
        File.WriteAllText(path, staleJson);

        var config = TuningConfig.Create(new CombatTuning(), new GameTuning());
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
        var config = TuningConfig.Create(new CombatTuning(), new GameTuning());
        Assert.That(config.TryLoad(), Is.False);

        File.WriteAllText(path, "{not-json");
        Assert.That(config.TryLoad(), Is.False);
    }
}
