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
        string boot = Game("Composition/GameBootstrap.cs");
        int arena = boot.IndexOf("arenaBuilder.BuildArena(ctx)", System.StringComparison.Ordinal);
        int actors = boot.IndexOf("new ActorsBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int sun = boot.IndexOf("arenaBuilder.BuildSun(ctx)", System.StringComparison.Ordinal);
        int camera = boot.IndexOf("cameraBuilder.Build(ctx)", System.StringComparison.Ordinal);
        int atmosphere = boot.IndexOf("cameraBuilder.ApplyAtmosphere(ctx)", System.StringComparison.Ordinal);
        int hex = boot.IndexOf("new HexagonInputBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int hud = boot.IndexOf("new HudBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int skill = boot.IndexOf("new SkillSystemBuilder().Build(ctx)", System.StringComparison.Ordinal);
        int debug = boot.IndexOf("new DebugToolsBuilder().Build(ctx)", System.StringComparison.Ordinal);

        Assert.That(arena, Is.GreaterThan(0));
        Assert.That(actors, Is.GreaterThan(arena));
        Assert.That(sun, Is.GreaterThan(actors));
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
        string boot = Game("Composition/GameBootstrap.cs");
        int tryLoad = boot.IndexOf("tuningConfig.TryLoad()", System.StringComparison.Ordinal);
        int clock = boot.IndexOf("AddComponent<GameClock>()", System.StringComparison.Ordinal);
        int arena = boot.IndexOf("arenaBuilder.BuildArena(ctx)", System.StringComparison.Ordinal);
        Assert.That(tryLoad, Is.GreaterThan(0));
        Assert.That(clock, Is.GreaterThan(tryLoad));
        Assert.That(arena, Is.GreaterThan(clock));
    }

    [Test]
    public void ActorsBuilder_KeepsOriginalInterleavedComponentOrder()
    {
        string src = Game("Composition/Builders/ActorsBuilder.cs");
        string[] ordered =
        {
            "\"Player\"",
            "\"AllyDummy\"",
            "ctx.AllyDummy = ctx.Ally.AddComponent<AllyDummy>()",
            "\"Boss\"",
            "ctx.Boss.AddComponent<CapsuleCollider>()",
            "ctx.Player.AddComponent<MoveInput>()",
            "ctx.Player.AddComponent<ActorVisual>()",
            "ctx.Boss.AddComponent<BossVisual>()",
            "ctx.Player.AddComponent<HitFlash>()",
            "ctx.Boss.AddComponent<HitFlash>()",
            "ctx.Player.AddComponent<PlayerVitals>()",
            "ctx.PlayerStatus = ctx.Player.AddComponent<ActorStatus>()",
            "ctx.BossStatus = ctx.Boss.AddComponent<ActorStatus>()",
            "ctx.Player.AddComponent<AfterimageTrail>()",
            "ctx.Player.AddComponent<ActorGrounding>()",
            "ctx.Boss.AddComponent<ActorGrounding>()",
            "ctx.Boss.AddComponent<BossReactor>()",
            "new BossVitals(",
            "ctx.Ally.AddComponent<Targetable>()",
            "ctx.Boss.AddComponent<Targetable>()",
            "ctx.Boss.AddComponent<BossTelegraph>()",
        };
        int prev = -1;
        foreach (string token in ordered)
        {
            int at = src.IndexOf(token, prev + 1, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(prev), token);
            prev = at;
        }
    }
}
