using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dovus.Game.Assets
{
    /// <summary>
    /// Kaynak taraması: sabit Resources yolları ve Shader.Find adları (IntegrationTests + Editor menüsü).
    /// </summary>
    public static class RuntimeResourcePathScanner
    {
        static readonly Regex LoadLiteral = new(
            @"(?:Resources\.Load|AssetLoader\.Load)<[^>]+>\(\s*""([^""]+)""",
            RegexOptions.Compiled);

        static readonly Regex LoadAllLiteral = new(
            @"(?:Resources\.LoadAll|AssetLoader\.LoadAll)<[^>]+>\(\s*""([^""]+)""",
            RegexOptions.Compiled);

        static readonly Regex ShaderLiteral = new(
            @"(?:Shader\.Find|AssetLoader\.FindShader)\(\s*""([^""]+)""",
            RegexOptions.Compiled);

        static readonly Regex ConstResourcePath = new(
            @"const\s+string\s+\w+\s*=\s*""([^""]+)"";",
            RegexOptions.Compiled);

        static readonly HashSet<string> PackageShaderNames = new(StringComparer.Ordinal)
        {
            "Sprites/Default",
            "Standard",
            "Unlit/Color",
            "Hidden/Internal-Colored",
            "Particles/Standard Unlit",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
        };

        public sealed class ScanResult
        {
            public List<string> ResourcePaths { get; } = new();
            public List<string> MissingResources { get; } = new();
            public List<string> ShaderNames { get; } = new();
            public List<string> MissingShaders { get; } = new();
        }

        public static ScanResult Scan(string unityAssetsRoot)
        {
            var result = new ScanResult();
            string gameRoot = Path.Combine(unityAssetsRoot, "Scripts", "Game");
            var resourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var shaderNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string file in Directory.EnumerateFiles(gameRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    continue;

                string text = File.ReadAllText(file);
                if (!text.Contains("Load", StringComparison.Ordinal) && !text.Contains("FindShader", StringComparison.Ordinal)
                    && !text.Contains("Shader.Find", StringComparison.Ordinal))
                    continue;

                foreach (Match m in LoadLiteral.Matches(text))
                    resourcePaths.Add(m.Groups[1].Value);
                foreach (Match m in LoadAllLiteral.Matches(text))
                    resourcePaths.Add(m.Groups[1].Value);
                foreach (Match m in ShaderLiteral.Matches(text))
                    shaderNames.Add(m.Groups[1].Value);

                if (text.Contains("ResourcePath", StringComparison.Ordinal)
                    || text.Contains("FontResource", StringComparison.Ordinal)
                    || text.Contains("AnchorResourcePath", StringComparison.Ordinal)
                    || text.Contains("CatalogResourcePath", StringComparison.Ordinal)
                    || text.Contains("ElementResourcePath", StringComparison.Ordinal))
                {
                    foreach (Match m in ConstResourcePath.Matches(text))
                    {
                        string value = m.Groups[1].Value;
                        if (LooksLikeResourcePath(value))
                            resourcePaths.Add(value);
                    }
                }
            }

            var shaderIndex = BuildShaderNameIndex(unityAssetsRoot);

            foreach (string path in resourcePaths.OrderBy(p => p, StringComparer.Ordinal))
            {
                result.ResourcePaths.Add(path);
                if (!ResourceFileExists(unityAssetsRoot, path))
                    result.MissingResources.Add(path);
            }

            foreach (string name in shaderNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                result.ShaderNames.Add(name);
                if (!shaderIndex.Contains(name) && !PackageShaderNames.Contains(name))
                    result.MissingShaders.Add(name);
            }

            return result;
        }

        public static string FormatReportMarkdown(ScanResult scan, DateTime generatedUtc)
        {
            var lines = new List<string>
            {
                "# Runtime asset referans raporu",
                "",
                $"Üretim: {generatedUtc:yyyy-MM-dd HH:mm} UTC (PLAN 2B.10).",
                "",
                "IntegrationTests bu dosyadaki **bilinen eksikler** listesini tarama sonucuyla karşılaştırır; yeni eksik → test kırmızı.",
                "",
                "## Bilinen eksikler (Resources)",
            };

            if (scan.MissingResources.Count == 0)
                lines.Add("- (yok)");
            else
            {
                foreach (string path in scan.MissingResources)
                    lines.Add("- `" + path + "`");
            }

            lines.Add("");
            lines.Add("## Bilinen eksikler (Shader.Find)");
            if (scan.MissingShaders.Count == 0)
                lines.Add("- (yok)");
            else
            {
                foreach (string name in scan.MissingShaders)
                    lines.Add("- `" + name + "`");
            }

            lines.Add("");
            lines.Add("## Taranan sabit Resources yolları");
            foreach (string path in scan.ResourcePaths)
                lines.Add("- `" + path + "`");

            lines.Add("");
            lines.Add("## Taranan Shader.Find adları");
            foreach (string name in scan.ShaderNames)
                lines.Add("- `" + name + "`");

            return string.Join("\n", lines) + "\n";
        }

        public static void ParseKnownMissingFromReport(
            string markdown,
            out HashSet<string> missingResources,
            out HashSet<string> missingShaders)
        {
            missingResources = new HashSet<string>(StringComparer.Ordinal);
            missingShaders = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(markdown))
                return;

            string section = null;
            foreach (string raw in markdown.Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## Bilinen eksikler (Resources)", StringComparison.Ordinal))
                {
                    section = "res";
                    continue;
                }

                if (line.StartsWith("## Bilinen eksikler (Shader.Find)", StringComparison.Ordinal))
                {
                    section = "shader";
                    continue;
                }

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    section = null;
                    continue;
                }

                if (section == null || !line.StartsWith("- `", StringComparison.Ordinal))
                    continue;

                if (line == "- (yok)")
                    continue;

                int start = line.IndexOf('`') + 1;
                int end = line.LastIndexOf('`');
                if (end <= start)
                    continue;

                string value = line.Substring(start, end - start);
                if (section == "res")
                    missingResources.Add(value);
                else if (section == "shader")
                    missingShaders.Add(value);
            }
        }

        static bool LooksLikeResourcePath(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Contains("${", StringComparison.Ordinal))
                return false;
            if (value.StartsWith("dovus.", StringComparison.Ordinal) || value.StartsWith("ProjectSettings/", StringComparison.Ordinal))
                return false;
            return value.Contains('/', StringComparison.Ordinal)
                   || value.EndsWith("HudTheme", StringComparison.Ordinal)
                   || value.EndsWith("SfxLibrary", StringComparison.Ordinal)
                   || value.EndsWith("VfxLibrary", StringComparison.Ordinal);
        }

        static bool ResourceFileExists(string unityAssetsRoot, string resourcePath)
        {
            foreach (string resourcesDir in Directory.EnumerateDirectories(unityAssetsRoot, "Resources", SearchOption.AllDirectories))
            {
                string combined = Path.Combine(resourcesDir, resourcePath.Replace('/', Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(combined);
                string fileName = Path.GetFileName(combined);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    continue;

                if (!Directory.Exists(directory))
                    continue;

                foreach (string file in Directory.EnumerateFiles(directory, fileName + ".*"))
                {
                    if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                string asFolder = Path.Combine(resourcesDir, resourcePath.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(asFolder))
                    return true;
            }

            return false;
        }

        static HashSet<string> BuildShaderNameIndex(string unityAssetsRoot)
        {
            var names = new HashSet<string>(PackageShaderNames, StringComparer.Ordinal);
            var shaderDecl = new Regex(@"^\s*Shader\s+""([^""]+)""\s*\{", RegexOptions.Compiled);
            foreach (string file in Directory.EnumerateFiles(unityAssetsRoot, "*.shader", SearchOption.AllDirectories))
            {
                foreach (string line in File.ReadLines(file))
                {
                    Match m = shaderDecl.Match(line);
                    if (m.Success)
                        names.Add(m.Groups[1].Value);
                }
            }

            return names;
        }
    }
}
