using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class CoreFolderLayoutTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string CoreRoot => Path.Combine(RepoRoot(), "unity", "Assets", "Scripts", "Core");
    static readonly Regex NamespaceLine = new(@"^\s*namespace\s+(Dovus\.Core\.\w+)\s*(?:[;{]|$)", RegexOptions.Compiled);

    [Test]
    public void Combat_folder_is_gone()
    {
        string combat = Path.Combine(CoreRoot, "Combat");
        Assert.That(Directory.Exists(combat), Is.False, "Core/Combat should be removed after topic split");
    }

    [Test]
    public void Every_core_script_namespace_matches_parent_folder()
    {
        var problems = new List<string>();
        foreach (string rel in Directory.EnumerateFiles(CoreRoot, "*.cs", SearchOption.AllDirectories))
        {
            string relUnix = rel.Replace('\\', '/');
            string corePrefix = CoreRoot.Replace('\\', '/');
            if (!corePrefix.EndsWith('/'))
                corePrefix += '/';
            string afterCore = relUnix.Substring(corePrefix.Length);
            int slash = afterCore.IndexOf('/');
            if (slash < 0)
                continue;
            string folder = afterCore.Substring(0, slash);
            if (folder is "Compat")
                continue;
            string expected = $"Dovus.Core.{folder}";

            string? found = null;
            foreach (string line in File.ReadLines(rel))
            {
                Match m = NamespaceLine.Match(line);
                if (m.Success)
                {
                    found = m.Groups[1].Value;
                    break;
                }
            }

            if (found == null)
            {
                problems.Add($"namespace missing: {relUnix}");
                continue;
            }

            if (found != expected)
                problems.Add($"{relUnix}: expected {expected}, got {found}");
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }
}
