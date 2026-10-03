using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace IntegrationTests;

/// <summary>
/// unity/Assets/Scripts altindaki .meta hijyeni: Unity gecersiz GUID'li (32 hex olmayan)
/// .meta dosyasini reddeder ve ilgili .cs derlemeye girmez. Bu kapi, elle yazilan meta
/// hatalarini Unity acilmadan yakalar.
/// </summary>
[TestFixture]
public class MetaHygieneTests
{
    const string ScriptsRoot = "unity/Assets/Scripts/";

    static readonly Regex GuidLine = new(@"^guid:\s*(\S*)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex ValidGuid = new(@"^[0-9a-f]{32}$", RegexOptions.Compiled);

    [Test]
    public void ScriptMetas_HaveValid32HexGuid_AndAreUnique()
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (string rel in TrackedScriptPaths().Where(p => p.EndsWith(".meta", StringComparison.Ordinal)))
        {
            string text = File.ReadAllText(Path.Combine(RepoPaths.Root, rel.Replace('/', Path.DirectorySeparatorChar)));
            Match m = GuidLine.Match(text);
            if (!m.Success)
            {
                problems.Add($"guid satiri yok: {rel}");
                continue;
            }

            string guid = m.Groups[1].Value;
            if (!ValidGuid.IsMatch(guid))
            {
                problems.Add($"gecersiz guid ({guid.Length} karakter): {rel} [{guid}]");
                continue;
            }

            if (seen.TryGetValue(guid, out string other))
                problems.Add($"yinelenen guid {guid}: {other} ve {rel}");
            else
                seen[guid] = rel;
        }

        Assert.That(problems, Is.Empty, string.Join(Environment.NewLine, problems));
    }

    [Test]
    public void ScriptFilesAndFolders_HaveTrackedSiblingMeta()
    {
        var tracked = new HashSet<string>(TrackedScriptPaths(), StringComparer.Ordinal);
        var problems = new List<string>();
        var dirs = new HashSet<string>(StringComparer.Ordinal);

        foreach (string rel in tracked)
        {
            string name = rel.Substring(rel.LastIndexOf('/') + 1);
            if (name == ".meta")
            {
                problems.Add($"klasor ici '.meta' dosyasi (Unity yok sayar; '<klasor>.meta' olmali): {rel}");
                continue;
            }

            if (!rel.EndsWith(".meta", StringComparison.Ordinal) && !tracked.Contains(rel + ".meta"))
                problems.Add($".meta eksik: {rel}");

            int slash = rel.LastIndexOf('/');
            while (slash > ScriptsRoot.Length - 1)
            {
                string dir = rel.Substring(0, slash);
                if (!dirs.Add(dir))
                    break;
                slash = dir.LastIndexOf('/');
            }
        }

        foreach (string dir in dirs)
        {
            if (dir.Length < ScriptsRoot.Length)
                continue;
            if (!tracked.Contains(dir + ".meta"))
                problems.Add($"klasor .meta eksik: {dir}");
        }

        foreach (string rel in tracked.Where(p => p.EndsWith(".meta", StringComparison.Ordinal)))
        {
            string owner = rel.Substring(0, rel.Length - ".meta".Length);
            if (owner.EndsWith("/", StringComparison.Ordinal))
                continue;
            if (!tracked.Contains(owner) && !dirs.Contains(owner))
                problems.Add($"sahipsiz .meta: {rel}");
        }

        problems.Sort(StringComparer.Ordinal);
        Assert.That(problems, Is.Empty, string.Join(Environment.NewLine, problems));
    }

    static List<string> TrackedScriptPaths()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "-c core.quotepath=off ls-files " + ScriptsRoot,
            WorkingDirectory = RepoPaths.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        Assert.That(proc, Is.Not.Null, "git baslatilamadi");
        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        Assert.That(proc.ExitCode, Is.EqualTo(0), proc.StandardError.ReadToEnd());

        return output.Split('\n')
            .Select(l => l.Trim().Replace('\\', '/'))
            .Where(l => l.StartsWith(ScriptsRoot, StringComparison.Ordinal))
            .Where(l => File.Exists(Path.Combine(RepoPaths.Root, l.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();
    }
}
