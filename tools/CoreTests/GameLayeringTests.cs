using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Game üst klasörleri arası using / tam nitelikli referans grafiği; Tarjan SCC (Editor hariç).
/// PLAN 2B.18a — Composition kök, DevTools yaprak, Platform/Diagnostics çapraz kesen.
/// </summary>
[TestFixture]
public class GameLayeringTests
{
    static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string GameRoot => Path.Combine(RepoRoot(), "unity", "Assets", "Scripts", "Game");

    static readonly Regex UsingLine = new(
        @"^\s*using\s+Dovus\.Game\.(\w+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);

    static readonly Regex QualifiedRef = new(
        @"\bDovus\.Game\.(\w+)\.",
        RegexOptions.Compiled);

    /// <summary>2B.18a sonrası kalan SCC (Composition ayrıldı). Yeni SCC eklenemez.</summary>
    static readonly string[][] AllowedStronglyConnectedComponents =
    {
        new[]
        {
            "Actors", "Arena", "Boss", "Cameras", "Casting", "Config", "Data", "DevTools", "Feel", "Hud",
            "Platform", "Skills", "Team", "Vfx", "Weapons"
        }
    };

    const int MaxNonTrivialSccCount = 1;
    const int MaxAllowedSccSize = 15;

    /// <summary>DevTools HUD statikleri — arayüz tersine çevirme 2B.18a kapsamı dışı (PR listesi).</summary>
    static readonly HashSet<string> DevToolsReferenceAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Actors/PlayerTargetingController.cs",
        "Casting/MoveInputController.cs",
        "Casting/Input/PointerRouter.cs",
        "Hud/FrameTimeHud.cs",
    };

    static HashSet<string> TopLevelFolders()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!Directory.Exists(GameRoot))
            return set;
        foreach (string dir in Directory.EnumerateDirectories(GameRoot))
        {
            string name = Path.GetFileName(dir);
            if (name is "Editor")
                continue;
            set.Add(name);
        }

        return set;
    }

    static string TopFolderForFile(string filePath)
    {
        string rel = Path.GetRelativePath(GameRoot, filePath).Replace('\\', '/');
        return rel.Split('/')[0];
    }

    static string RelativeGamePath(string filePath) =>
        Path.GetRelativePath(GameRoot, filePath).Replace('\\', '/');

    static Dictionary<string, HashSet<string>> BuildDependencyGraph(IReadOnlySet<string> folders)
    {
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (string folder in folders)
            edges[folder] = new HashSet<string>(StringComparer.Ordinal);

        foreach (string folder in folders)
        {
            string folderDir = Path.Combine(GameRoot, folder);
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
    public void Game_top_level_folders_have_no_unlisted_dependency_cycles()
    {
        HashSet<string> folders = TopLevelFolders();
        var graph = BuildDependencyGraph(folders);
        List<HashSet<string>> sccs = TarjanScc(graph);

        Assert.That(
            sccs.Count,
            Is.LessThanOrEqualTo(MaxNonTrivialSccCount),
            () => "SCC count ratchet — yeni döngü:\n"
                  + string.Join("\n", sccs.Select(NormalizeComponent)));

        int largest = sccs.Count == 0 ? 0 : sccs.Max(c => c.Count);
        Assert.That(
            largest,
            Is.LessThanOrEqualTo(MaxAllowedSccSize),
            () => $"SCC boyutu ratchet — en büyük [{largest}] > {MaxAllowedSccSize}: "
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
            bool found = sccs.Any(scc => scc.SetEquals(allowedComponent));
            Assert.That(
                found,
                Is.True,
                $"Beklenen SCC artık yok (döngü kırıldı — allowlist güncelle): [{string.Join(", ", allowedComponent)}]");
        }
    }

    [Test]
    public void Runtime_folders_outside_Composition_do_not_reference_Composition_namespace()
    {
        foreach (string file in Directory.EnumerateFiles(GameRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (TopFolderForFile(file) is "Editor" or "Composition")
                continue;

            string text = File.ReadAllText(file);
            bool usesComposition = UsingLine.Matches(text).Any(m => m.Groups[1].Value == "Composition")
                || QualifiedRef.Matches(text).Any(m => m.Groups[1].Value == "Composition");
            Assert.That(
                usesComposition,
                Is.False,
                $"Composition dışı klasör Composition kullanıyor: {RelativeGamePath(file)}");
        }
    }

    [Test]
    public void Runtime_folders_outside_DevTools_and_Composition_do_not_reference_DevTools_namespace()
    {
        foreach (string file in Directory.EnumerateFiles(GameRoot, "*.cs", SearchOption.AllDirectories))
        {
            string top = TopFolderForFile(file);
            if (top is "Editor" or "DevTools" or "Composition")
                continue;

            string rel = RelativeGamePath(file);
            if (DevToolsReferenceAllowlist.Contains(rel))
                continue;

            string text = File.ReadAllText(file);
            bool usesDevTools = UsingLine.Matches(text).Any(m => m.Groups[1].Value == "DevTools")
                || QualifiedRef.Matches(text).Any(m => m.Groups[1].Value == "DevTools");
            Assert.That(
                usesDevTools,
                Is.False,
                $"DevTools dışı klasör DevTools kullanıyor (allowlist veya arayüz): {rel}");
        }
    }

    [Test]
    public void Composition_is_not_inside_the_main_runtime_SCC()
    {
        HashSet<string> folders = TopLevelFolders();
        var graph = BuildDependencyGraph(folders);
        List<HashSet<string>> sccs = TarjanScc(graph);
        HashSet<string> main = sccs.SingleOrDefault(s => s.Contains("Skills"))
            ?? new HashSet<string>(StringComparer.Ordinal);
        Assert.That(main, Does.Not.Contain("Composition"), "Composition ana SCC'de — kök ayrışmadı.");
    }
}
