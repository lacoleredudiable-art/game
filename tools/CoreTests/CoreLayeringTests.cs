using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Core üst klasörleri arası using / tam nitelikli referans grafiği; Tarjan SCC.
/// Hedef sıra: <c>docs/ARCHITECTURE.md</c> (Core katman sırası).
/// </summary>
[TestFixture]
public class CoreLayeringTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string CoreRoot => Path.Combine(RepoRoot(), "unity", "Assets", "Scripts", "Core");

    static readonly Regex UsingLine = new(
        @"^\s*using\s+Dovus\.Core\.(\w+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);

    static readonly Regex QualifiedRef = new(
        @"\bDovus\.Core\.(\w+)\.",
        RegexOptions.Compiled);

    /// <summary>
    /// Kırılamayan döngüler (2B.6c sonrası ölçüm). Yeni SCC eklenemez; sayı düşürülürken güncelle.
    /// </summary>
    static readonly string[][] AllowedStronglyConnectedComponents =
    {
        new[]
        {
            "Boss", "Casting", "Damage", "Dodge", "Equipment", "Grammar", "Passives", "Status"
        }
    };

    const int MaxNonTrivialSccCount = 1;

    static HashSet<string> TopLevelFolders()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!Directory.Exists(CoreRoot))
            return set;
        foreach (string dir in Directory.EnumerateDirectories(CoreRoot))
        {
            string name = Path.GetFileName(dir);
            if (name is "Compat")
                continue;
            set.Add(name);
        }

        return set;
    }

    static Dictionary<string, HashSet<string>> BuildDependencyGraph(IReadOnlySet<string> folders)
    {
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (string folder in folders)
            edges[folder] = new HashSet<string>(StringComparer.Ordinal);

        foreach (string folder in folders)
        {
            string folderDir = Path.Combine(CoreRoot, folder);
            foreach (string file in Directory.EnumerateFiles(folderDir, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                var deps = new HashSet<string>(UsingLine.Matches(text).Select(m => m.Groups[1].Value));
                foreach (Match m in QualifiedRef.Matches(text))
                    deps.Add(m.Groups[1].Value);

                foreach (string dep in deps)
                {
                    if (dep == folder || !folders.Contains(dep))
                        continue;
                    edges[folder].Add(dep);
                }
            }
        }

        return edges;
    }

    static List<HashSet<string>> TarjanScc(IReadOnlyDictionary<string, HashSet<string>> graph)
    {
        int index = 0;
        var stack = new Stack<string>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowlink = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<HashSet<string>>();

        void StrongConnect(string v)
        {
            indices[v] = index;
            lowlink[v] = index;
            index++;
            stack.Push(v);
            onStack.Add(v);

            if (!graph.TryGetValue(v, out HashSet<string>? outs))
                outs = new HashSet<string>(StringComparer.Ordinal);
            foreach (string w in outs)
            {
                if (!indices.ContainsKey(w))
                {
                    StrongConnect(w);
                    lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                }
                else if (onStack.Contains(w))
                    lowlink[v] = Math.Min(lowlink[v], indices[w]);
            }

            if (lowlink[v] != indices[v])
                return;

            var component = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                string w = stack.Pop();
                onStack.Remove(w);
                component.Add(w);
                if (w == v)
                    break;
            }

            bool nonTrivial = component.Count > 1
                || (component.Count == 1
                    && graph[component.First()].Contains(component.First()));
            if (nonTrivial)
                result.Add(component);
        }

        foreach (string v in graph.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!indices.ContainsKey(v))
                StrongConnect(v);
        }

        return result;
    }

    static string NormalizeComponent(HashSet<string> component) =>
        string.Join(", ", component.OrderBy(x => x, StringComparer.Ordinal));

    [Test]
    public void Core_top_level_folders_have_no_unlisted_dependency_cycles()
    {
        HashSet<string> folders = TopLevelFolders();
        var graph = BuildDependencyGraph(folders);
        List<HashSet<string>> sccs = TarjanScc(graph);

        Assert.That(
            sccs.Count,
            Is.LessThanOrEqualTo(MaxNonTrivialSccCount),
            () => "SCC count ratchet — yeni döngü:\n"
                  + string.Join("\n", sccs.Select(NormalizeComponent)));

        var allowed = new HashSet<string>(
            AllowedStronglyConnectedComponents.Select(c => string.Join(", ", c.OrderBy(x => x, StringComparer.Ordinal))),
            StringComparer.Ordinal);

        foreach (HashSet<string> scc in sccs)
        {
            string key = NormalizeComponent(scc);
            Assert.That(
                allowed,
                Does.Contain(key),
                $"İzinsiz SCC (listeye ekle veya döngüyü kır): [{key}]");
        }

        foreach (string[] allowedComponent in AllowedStronglyConnectedComponents)
        {
            bool found = sccs.Any(scc =>
                scc.SetEquals(allowedComponent));
            Assert.That(
                found,
                Is.True,
                $"Beklenen SCC artık yok (döngü kırıldı — allowlist ve MaxNonTrivialSccCount güncelle): [{string.Join(", ", allowedComponent)}]");
        }
    }
}
