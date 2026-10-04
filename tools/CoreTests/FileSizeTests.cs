using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class FileSizeTests
{
    const int Cap = 500;

    /// <summary>Generated shim / intentionally monolithic — not split targets.</summary>
    static readonly HashSet<string> AllowOverCap = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static IEnumerable<string> ScannedCsFiles()
    {
        string[] roots =
        {
            Path.Combine(RepoRoot(), "unity", "Assets", "Scripts"),
            Path.Combine(RepoRoot(), "tools"),
        };
        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || path.Contains($"{Path.DirectorySeparatorChar}GameCompile{Path.DirectorySeparatorChar}"))
                    continue;
                yield return path;
            }
        }
    }

    [Test]
    public void Unity_scripts_and_tools_cs_files_are_at_most_500_lines_except_allowlist()
    {
        var violations = new List<string>();
        foreach (string path in ScannedCsFiles())
        {
            string file = Path.GetFileName(path);
            if (AllowOverCap.Contains(file))
                continue;
            int lines = File.ReadAllLines(path).Length;
            if (lines > Cap)
                violations.Add($"{file} ({lines} lines) — {path}");
        }

        Assert.That(
            violations,
            Is.Empty,
            () => "Files over 500 lines (add partial split or justified allowlist entry):\n" + string.Join("\n", violations));
    }
}
