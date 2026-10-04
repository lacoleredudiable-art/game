using System;
using System.IO;
using Dovus.Game.Assets;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    public static class AssetReferenceReportMenu
    {
        const string ReportAssetPath = "docs/asset-references.md";

        [MenuItem("Dovus/Asset Referans Raporu")]
        public static void RunScan()
        {
            string unityAssets = Path.GetFullPath(Path.Combine(Application.dataPath));
            var scan = RuntimeResourcePathScanner.Scan(unityAssets);
            string markdown = RuntimeResourcePathScanner.FormatReportMarkdown(scan, DateTime.UtcNow);

            string repoRoot = Path.GetFullPath(Path.Combine(unityAssets, "..", ".."));
            string outPath = Path.Combine(repoRoot, ReportAssetPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? repoRoot);
            File.WriteAllText(outPath, markdown);

            int missing = scan.MissingResources.Count + scan.MissingShaders.Count;
            Debug.Log(
                $"[AssetRef] {scan.ResourcePaths.Count} Resources yolu, {scan.ShaderNames.Count} shader adı;"
                + $" bilinen eksik: {missing}. Rapor: {outPath}");
        }
    }
}
