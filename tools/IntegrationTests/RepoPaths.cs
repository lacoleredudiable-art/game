using System;
using System.IO;
using NUnit.Framework;

namespace IntegrationTests;

static class RepoPaths
{
    public static string Root
    {
        get
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                string agents = Path.Combine(dir.FullName, "AGENTS.md");
                string unity = Path.Combine(dir.FullName, "unity", "Assets");
                if (File.Exists(agents) && Directory.Exists(unity))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new InvalidOperationException("Repo kökü bulunamadı (AGENTS.md yok).");
        }
    }

    public static string Docs(string name) => Path.Combine(Root, "docs", name);

    public static string UnityAssets => Path.Combine(Root, "unity", "Assets");

    public static string UnityGame => Path.Combine(UnityAssets, "Scripts", "Game");

    public static string RuleEngineV4Json =>
        Path.Combine(UnityAssets, "Resources", "RuleEngineV4", "kural-motoru-v4.json");

    public static string BuildSliceGrammarJson()
    {
        string v4 = File.ReadAllText(RuleEngineV4Json);
        return Dovus.Core.RuleEngineV4.SliceGrammarDefaults.BuildGrammarJson(v4);
    }
}
