using System.IO;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class GameCompileDevToolsTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void GameCompile_excludes_DevTools_from_release_like_build()
    {
        string check = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "GameCompile", "check.py"));
        Assert.That(check, Does.Contain("/DevTools/"));
    }
}
