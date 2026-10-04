using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public class SingletonInjectionTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", rel));

    static IEnumerable<string> GameCsFiles()
    {
        string root = Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("\\Editor\\") || path.Contains("/Editor/"))
                continue;
            yield return path;
        }
    }

    [Test]
    public void SourceGate_NoDisallowedStatics()
    {
        var instance = new Regex(@"static\s+\w+\s+Instance\b", RegexOptions.Compiled);
        var singletonField = new Regex(@"static\s+.*\s+_instance\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var themeCurrent = new Regex(@"HudTheme\.Current\b", RegexOptions.Compiled);
        var sfxCurrent = new Regex(@"SfxLibrary\.Current\b", RegexOptions.Compiled);
        var vfxCurrent = new Regex(@"VfxLibrary\.Current\b", RegexOptions.Compiled);

        foreach (string path in GameCsFiles())
        {
            string rel = Path.GetRelativePath(
                Path.Combine(Root(), "unity", "Assets", "Scripts", "Game"), path);
            if (string.Equals(rel, "Composition\\AssetCatalog.cs", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rel, "Composition/AssetCatalog.cs", StringComparison.Ordinal))
                continue;
            // İzinli istisna: host yaşam döngüsü AfterSceneLoad Boot + DontDestroyOnLoad (davranış korunur, 2B.8b inceleme).
            bool teamHost = rel.Replace('\\', '/') == "Team/TeamComboHost.cs";

            string src = File.ReadAllText(path);
            if (!teamHost)
                Assert.That(instance.IsMatch(src), Is.False, $"{rel}: static Instance");
            Assert.That(singletonField.IsMatch(src), Is.False, $"{rel}: static _instance");
            Assert.That(themeCurrent.IsMatch(src), Is.False, $"{rel}: HudTheme.Current");
            Assert.That(sfxCurrent.IsMatch(src), Is.False, $"{rel}: SfxLibrary.Current");
            Assert.That(vfxCurrent.IsMatch(src), Is.False, $"{rel}: VfxLibrary.Current");
        }
    }

    [Test]
    public void AssetCatalog_FallbackHudTheme_WhenResourceMissing()
    {
        string catalog = Game("Composition/AssetCatalog.cs");
        Assert.That(catalog, Does.Contain("ResolveHudTheme()"));
        Assert.That(catalog, Does.Contain("CreateInstance<HudTheme>()"));
        string theme = Game("Hud/HudTheme.cs");
        Assert.That(theme, Does.Contain("DamageTextColor = new(0.95f"));
    }

    [Test]
    public void Bootstrap_WiresAssetCatalog()
    {
        string boot = Game("Composition/GameBootstrapHost.cs");
        Assert.That(boot, Does.Contain("ctx.Assets = AssetCatalog.Standalone"));
    }
}
