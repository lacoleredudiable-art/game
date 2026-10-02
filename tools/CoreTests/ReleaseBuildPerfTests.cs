using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Denetim C: release build ve debug kapısı (K2), tuning.json sürümü (O5), %50 can bayrağı (O6),
/// mobil performans (O11), CI'da sessiz atlama (S13), Android ayarları (S14).
/// </summary>
[TestFixture]
public class ReleaseBuildPerfTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", rel));

    // --- K2 ---

    [Test]
    public void DebugGate_IsDefineBased_NotDevelopmentBuild()
    {
        string src = Game("DebugConfig.cs");
        Assert.That(src, Does.Not.Contain("isDebugBuild"));
        Assert.That(src, Does.Contain("#if UNITY_EDITOR || DOVUS_DEBUG"));
        Assert.That(src, Does.Contain("[Conditional(\"UNITY_EDITOR\"), Conditional(\"DOVUS_DEBUG\")]"));
    }

    [Test]
    public void AndroidBuilder_HasReleaseAndDevPaths_SameAppId()
    {
        string src = Game("Editor/AndroidBuilder.cs");
        Assert.That(src, Does.Contain("const string ApplicationId = \"com.dovus.prototip\";"));
        Assert.That(src, Does.Contain("[MenuItem(\"Dovus/Build Android APK\")]"));
        Assert.That(src, Does.Contain("[MenuItem(\"Dovus/Build Android APK (release)\")]"));
        Assert.That(src, Does.Contain("\"-dovusRelease\""));
        Assert.That(src, Does.Contain("? BuildOptions.CleanBuildCache\n                : BuildOptions.Development | BuildOptions.CleanBuildCache")
            .Or.Contain("? BuildOptions.CleanBuildCache\r\n                : BuildOptions.Development | BuildOptions.CleanBuildCache"));
        Assert.That(src, Does.Contain("release ? Array.Empty<string>() : new[] { DebugDefine }"));
        Assert.That(src, Does.Contain("extraScriptingDefines = DefinesFor(release)"));
    }

    [Test]
    public void DebugUi_OnlyBehindGate()
    {
        string boot = Game("PrototypeBootstrap.cs");
        Assert.That(Regex.Matches(boot, @"if \(DebugConfig\.Enabled\)\s*\{\s*var frameHud").Count, Is.EqualTo(1));
        Assert.That(Regex.Matches(boot, @"if \(DebugConfig\.Enabled\)\s*\{\s*var practice").Count, Is.EqualTo(1));
        Assert.That(Regex.Matches(boot, @"if \(DebugConfig\.Enabled\)\s*\{\s*var v6Panel").Count, Is.EqualTo(1));
        Assert.That(boot, Does.Contain("DebugConfig.Enabled && _tuning.ShowSentenceDebugHud"));
        string team = Game("TeamDebugMenu.cs");
        Assert.That(team, Does.Contain("bool _open = false;"));
        Assert.That(team, Does.Contain("#if UNITY_EDITOR || DOVUS_DEBUG"));
        Assert.That(team, Does.Not.Contain("DEVELOPMENT_BUILD"));
    }

    // --- O6 ---

    [Test]
    public void StartHp_IsFullByDefault_DevHpSeparateSwitch()
    {
        string boot = Game("PrototypeBootstrap.cs");
        Assert.That(boot, Does.Not.Contain("startRatio: 0.5f"));
        Assert.That(Regex.Matches(boot, @"startRatio: DebugConfig\.StartHpRatio").Count, Is.EqualTo(2));
        Assert.That(boot, Does.Contain("vitals.SetDevHp(DebugConfig.DevHpActive);"));
        string cfg = Game("DebugConfig.cs");
        Assert.That(cfg, Does.Contain("public static bool HalfHpStart = false;"));
        Assert.That(cfg, Does.Contain("StartHpRatio => Enabled && HalfHpStart ? 0.5f : 1f"));
        Assert.That(cfg, Does.Contain("DevHpActive => Enabled && DevHp && !HalfHpStart"));
    }

    // --- O5 ---

    [TestCase(0, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(1, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(2, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(3, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(4, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(5, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(6, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(7, TuningSchema.LoadDecision.DiscardStale)]
    [TestCase(8, TuningSchema.LoadDecision.Apply)]
    public void StaleSave_NeverWins(int stored, TuningSchema.LoadDecision expected)
    {
        Assert.That(TuningSchema.Decide(stored), Is.EqualTo(expected));
    }

    /// <summary>
    /// Kod varsayılanı değişirse bu test kırılır: <see cref="TuningSchema.Version"/>'ı artır ve
    /// parmak izini güncelle. Böylece eski cihaz kaydı yeni varsayılanı sessizce ezemez.
    /// </summary>
    [Test]
    public void DefaultsFingerprint_PinnedToSchemaVersion()
    {
        var options = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(new CombatTuning(), options);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).Substring(0, 16);
        Assert.That(TuningSchema.Version, Is.EqualTo(8));
        Assert.That(hash, Is.EqualTo("8AACEDC268A0EF45"),
            "CombatTuning varsayılanı değişti: TuningSchema.Version'ı artır, bu parmak izini ve sürüm satırını güncelle.");
    }

    [Test]
    public void TuningConfig_UsesSchema_LoadOnlyInDebug()
    {
        string cfg = Game("TuningConfig.cs");
        Assert.That(cfg, Does.Contain("version = TuningSchema.Version"));
        Assert.That(cfg, Does.Contain("TuningSchema.Decide(data.version) == TuningSchema.LoadDecision.DiscardStale"));
        Assert.That(cfg, Does.Not.Contain("BossDamageMigration.Apply"));
        Assert.That(Game("PrototypeBootstrap.cs"), Does.Match(@"if \(DebugConfig\.Enabled\)\s*tuningConfig\.TryLoad\(\);"));
    }

    // --- O11 ---

    [Test]
    public void NoPerFrameSearches_InHotPaths()
    {
        Assert.That(Game("PortalBorderTeamHost.cs"), Does.Not.Contain("FindObjectsOfType<AllyDummy>"));
        Assert.That(Regex.Matches(Game("PortalBorderTeamHost.cs"), @"_boss\.GetComponent<BossReactor>\(\)").Count, Is.EqualTo(1),
            "yalnız CachedBossReactor içinde");
        Assert.That(Game("DodgeMotion.cs"), Does.Contain("_reactorCache"));
        foreach (string f in new[] { "PlayerTargeting.cs", "ManifestationDirector.cs", "ManifestationDirector.Weapons10.cs" })
            Assert.That(Game(f), Does.Not.Contain("FindObjectsByType<Targetable>"), f);
        foreach (string f in new[] { "ManifestationDirector.cs", "ManifestationDirector.VerbExecution.cs", "ManifestationDirector.MechanicWorld.cs" })
            Assert.That(Regex.Matches(Game(f), @"(?<!_playerVitalsCache = )_player\.GetComponent<PlayerVitals>\(\)").Count, Is.EqualTo(0), f);
        string summon = Game("SkillExecution/SummonExecutor.cs");
        Assert.That(summon, Does.Contain("_weaponPathClass"));
        Assert.That(summon, Does.Contain("_targetCollider"));
    }

    [Test]
    public void NoMaterialInstancePerHit()
    {
        foreach (string f in new[]
                 {
                     "ManifestationDirector.MechanicWorld.cs", "ManifestationDirector.MotionTemplate.cs",
                     "SkillExecution/SummonExecutor.cs", "PortalBorderTeamHost.cs", "HostileProjectileHost.cs",
                     "AttackTelegraph.cs"
                 })
        {
            string src = Game(f);
            Assert.That(src, Does.Not.Contain(".material.color"), f);
            Assert.That(src, Does.Not.Contain("rend.material;"), f);
            Assert.That(src, Does.Not.Contain("new Material(Shader.Find(\"Sprites/Default\"))"), f);
        }
        Assert.That(Game("PlaceholderFactory.cs"), Does.Contain("GlowCache"));
    }

    [Test]
    public void NoUngatedLogs_InRuntimeGame()
    {
        string dir = Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");
        foreach (string path in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains(Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar)
                || path.EndsWith("DebugConfig.cs", StringComparison.Ordinal))
                continue;
            Assert.That(File.ReadAllText(path), Does.Not.Contain("Debug.Log("), path);
        }
    }

    // --- S13 ---

    [Test]
    public void CompileTest_FailsOnCi_WhenPythonMissing()
    {
        string src = File.ReadAllText(Path.Combine(Root(), "tools", "CoreTests", "GameLayerCompileTests.cs"));
        Assert.That(src, Does.Contain("if (IsCi())"));
        Assert.That(src, Does.Contain("Assert.Fail(\"CI: \" + missing);"));

        string? saved = Environment.GetEnvironmentVariable("CI");
        try
        {
            Environment.SetEnvironmentVariable("CI", "true");
            Assert.That(GameLayerCompileTests.IsCi(), Is.True);
            Environment.SetEnvironmentVariable("CI", "false");
            Assert.That(GameLayerCompileTests.IsCi(), Is.False);
            Environment.SetEnvironmentVariable("CI", null);
            Assert.That(GameLayerCompileTests.IsCi(), Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CI", saved);
        }
    }

    // --- S14 ---

    [Test]
    public void Android_ScreenStaysOn_QualityChosen()
    {
        string boot = Game("PrototypeBootstrap.cs");
        Assert.That(boot, Does.Contain("Screen.sleepTimeout = SleepTimeout.NeverSleep;"));
        Assert.That(boot, Does.Contain("Screen.sleepTimeout = SleepTimeout.SystemSetting;"));
        string q = File.ReadAllText(Path.Combine(Root(), "unity", "ProjectSettings", "QualitySettings.asset"));
        Assert.That(q, Does.Match(@"m_PerPlatformDefaultQuality:\r?\n    Android: 1"));
    }

    /// <summary>
    /// Release APK'da BUILD SEÇ dokunuş almıyordu: tek EventSystem debug ayar paneliyle
    /// (DebugConfig.Enabled kapısı içinde) kuruluyordu. EventSystem kapının DIŞINDA kurulmalı.
    /// </summary>
    [Test]
    public void EventSystem_IsCreated_OutsideDebugGate()
    {
        string boot = Game("PrototypeBootstrap.cs");
        int ensure = boot.IndexOf("EnsureEventSystem();", System.StringComparison.Ordinal);
        int gate = boot.IndexOf("CreateTuningPanel(tuningConfig, vitals, follow);", System.StringComparison.Ordinal);
        Assert.That(ensure, Is.GreaterThan(0));
        Assert.That(gate, Is.GreaterThan(ensure));
        string between = boot.Substring(ensure, gate - ensure);
        Assert.That(between, Does.Contain("if (DebugConfig.Enabled)"), "EnsureEventSystem debug kapısından ÖNCE çağrılmalı");
        int body = boot.IndexOf("static void CreateTuningPanel(", System.StringComparison.Ordinal);
        int next = boot.IndexOf("static void EnsureEventSystem(", System.StringComparison.Ordinal);
        Assert.That(next, Is.GreaterThan(body));
        Assert.That(boot.Substring(body, next - body), Does.Not.Contain("AddComponent<EventSystem>"));
        Assert.That(boot, Does.Contain("AddComponent<InputSystemUIInputModule>()"));
    }
}
