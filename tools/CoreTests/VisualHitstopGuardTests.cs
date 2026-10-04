using NUnit.Framework;
using System.IO;

namespace CoreTests;

/// <summary>PLAN 1.2: kaçış/vurulma hit-stop dünya saatini durdurmaz; Game katmanı TriggerHitstop çağırmaz.</summary>
[TestFixture]
public class VisualHitstopGuardTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void GameScripts_DoNotCall_TriggerHitstop()
    {
        string gameDir = Path.Combine(Root(), "unity", "Assets", "Scripts", "Game");
        foreach (string file in Directory.EnumerateFiles(gameDir, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            Assert.That(text, Does.Not.Contain("TriggerHitstop("), file);
        }
    }
}
