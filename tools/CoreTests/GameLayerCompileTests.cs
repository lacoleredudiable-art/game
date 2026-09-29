using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class GameLayerCompileTests
{
    [Test]
    public void RuntimeGameScripts_CompileWithoutTheUnityEditor()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
        string script = Path.Combine(root, "tools", "GameCompile", "check.py");
        Assert.That(File.Exists(script), Is.True, script);

        var start = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = "\"" + script + "\"",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using Process process = Process.Start(start);
        Assert.That(process, Is.Not.Null);
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0), stdout + "\n" + stderr);
        Assert.That(stdout, Does.Contain("game layer compiled"));
    }
}
