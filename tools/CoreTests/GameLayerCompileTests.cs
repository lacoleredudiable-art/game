using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class GameLayerCompileTests
{
    internal static bool IsCi()
    {
        string ci = System.Environment.GetEnvironmentVariable("CI");
        return !string.IsNullOrEmpty(ci)
            && !string.Equals(ci, "false", System.StringComparison.OrdinalIgnoreCase)
            && ci != "0";
    }

    [Test]
    public void RuntimeGameScripts_CompileWithoutTheUnityEditor()
    {
        string root = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));
        string script = Path.Combine(root, "tools", "GameCompile", "check.py");
        Assert.That(File.Exists(script), Is.True, script);

        if (!PythonLaunch.TryResolve(out string fileName, out string prefix))
        {
            const string missing = "Python yok (python3, python, py -3 denendi). Oyun katmanı derlemesi atlandı.";
            // S13: CI'da (CI=true) sessiz atlama yok — oyun katmanı derlenmeden yeşil görünmesin.
            if (IsCi())
                Assert.Fail("CI: " + missing);
            Assert.Ignore(missing);
        }

        var start = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = prefix + "\"" + script + "\"",
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

static class PythonLaunch
{
    public static bool TryResolve(out string fileName, out string argumentPrefix)
    {
        if (Probe("python3", ""))
        {
            fileName = "python3";
            argumentPrefix = "";
            return true;
        }

        if (Probe("python", ""))
        {
            fileName = "python";
            argumentPrefix = "";
            return true;
        }

        if (Probe("py", "-3 "))
        {
            fileName = "py";
            argumentPrefix = "-3 ";
            return true;
        }

        fileName = "";
        argumentPrefix = "";
        return false;
    }

    static bool Probe(string fileName, string argumentPrefix)
    {
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = argumentPrefix + "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using Process process = Process.Start(start);
            if (process == null)
                return false;
            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // sonda yok say
                }
                return false;
            }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
