using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public sealed class GlobalStateTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", rel));

    static IEnumerable<string> GameRuntimeCs()
    {
        string root = Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}"))
                continue;
            yield return path;
        }
    }

    static readonly HashSet<string> AllowMutableStaticFields = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "Composition/AssetCatalog.cs",
        "Vfx/VfxLibraryStandalone.cs",
        "Data/ElementSystemRuntimeCache.cs",
    };

    [Test]
    public void Allowlisted_asset_statics_only()
    {
        Assert.That(AllowMutableStaticFields, Has.Count.EqualTo(3));
        Assert.That(Game("Composition/AssetCatalog.cs"), Does.Contain("Standalone"));
        Assert.That(Game("Vfx/VfxLibraryStandalone.cs"), Does.Contain("Shared"));
        Assert.That(Game("Data/ElementSystemRuntimeCache.cs"), Does.Contain("class ElementSystemRuntimeCache"));
    }

    [Test]
    public void SceneLiveRegistry_is_per_scene_instance()
    {
        string src = Game("Actors/SceneLiveRegistry.cs");
        Assert.That(src, Does.Contain("public void Register("));
        Assert.That(src, Does.Contain("public void Unregister("));
        Assert.That(src, Does.Not.Contain("static readonly List"));
    }

    [Test]
    public void DebugFlags_own_dev_hp_and_start_ratio()
    {
        string flags = Game("Diagnostics/DebugFlags.cs");
        Assert.That(flags, Does.Contain("public bool DevHp { get; set; }"));
        Assert.That(flags, Does.Contain("StartHpRatio => DebugConfig.Enabled && HalfHpStart ? 0.5f : 1f"));
        Assert.That(Game("Composition/Builders/ActorsBuilder.cs"), Does.Contain("debugFlags.StartHpRatio"));
    }

    [Test]
    public void TeamComboAccess_configured_from_bootstrap()
    {
        string boot = Game("Composition/GameBootstrapHost.cs");
        Assert.That(boot, Does.Contain("ctx.TeamAccess = new TeamComboAccess()"));
        Assert.That(boot, Does.Contain("ctx.TeamAccess.Configure(ctx.TeamComboHost)"));
        Assert.That(Game("Team/TeamComboAccess.cs"), Does.Contain("public TeamComboHost Host => _host"));
    }

    [Test]
    public void Mutable_static_field_ceiling_in_game_runtime()
    {
        var fieldRx = new Regex(
            @"^\s*(?:public|internal|private|protected)\s+static\s+(?!readonly|const)\w",
            RegexOptions.Multiline);
        string gameRoot = Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");
        int total = 0;
        foreach (string path in GameRuntimeCs())
        {
            string rel = Path.GetRelativePath(gameRoot, path).Replace('\\', '/');
            if (AllowMutableStaticFields.Contains(rel))
                continue;
            string src = File.ReadAllText(path);
            total += fieldRx.Matches(src).Count;
        }

        TestContext.WriteLine($"Mutable static fields (excl. allowlist): {total}");
        Assert.That(total, Is.LessThanOrEqualTo(280));
    }
}
