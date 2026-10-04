using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace CoreTests;

[TestFixture]
public class CompositionOrderTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Game(string rel) =>
        File.ReadAllText(Path.Combine(Root(), "unity", "Assets", "Scripts", "Game", rel));

    [Test]
    public void Bootstrap_CallsBuilders_InWorldSetupOrder()
    {
        string boot = Game("Composition/PrototypeBootstrap.cs");
        int arena = boot.IndexOf("arenaBuilder.BuildArena(ctx)", System.StringComparison.Ordinal);
        int player = boot.IndexOf("new PlayerBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int boss = boot.IndexOf("new BossBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int sun = boot.IndexOf("arenaBuilder.BuildSun(ctx)", System.StringComparison.Ordinal);
        int camera = boot.IndexOf("cameraBuilder.Build(ctx)", System.StringComparison.Ordinal);
        int atmosphere = boot.IndexOf("cameraBuilder.ApplyAtmosphere(ctx)", System.StringComparison.Ordinal);
        int hex = boot.IndexOf("new HexagonInputBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int hud = boot.IndexOf("new HudBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int skill = boot.IndexOf("new SkillSystemBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int debug = boot.IndexOf("new DebugToolsBuilder().Build(ctx)", System.StringComparison.Ordinal);

        Assert.That(arena, Is.GreaterThan(0));
        Assert.That(player, Is.GreaterThan(arena));
        Assert.That(boss, Is.GreaterThan(player));
        Assert.That(sun, Is.GreaterThan(boss));
        Assert.That(camera, Is.GreaterThan(sun));
        Assert.That(atmosphere, Is.GreaterThan(camera));
        Assert.That(hex, Is.GreaterThan(atmosphere));
        Assert.That(hud, Is.GreaterThan(hex));
        Assert.That(skill, Is.GreaterThan(hud));
        Assert.That(debug, Is.GreaterThan(skill));
    }

    [Test]
    public void Bootstrap_ClockAndTuningLoad_BeforeArena()
    {
        string boot = Game("Composition/PrototypeBootstrap.cs");
        int tryLoad = boot.IndexOf("tuningConfig.TryLoad()", System.StringComparison.Ordinal);
        int clock = boot.IndexOf("AddComponent<GameClock>()", System.StringComparison.Ordinal);
        int arena = boot.IndexOf("arenaBuilder.BuildArena(ctx)", System.StringComparison.Ordinal);
        Assert.That(tryLoad, Is.GreaterThan(0));
        Assert.That(clock, Is.GreaterThan(tryLoad));
        Assert.That(arena, Is.GreaterThan(clock));
    }
}
