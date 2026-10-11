using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Dovus.Tests.EditMode
{
    /// <summary>
    /// v4 dilim APK'sı: ortam dokuları, k3 maske/emisyon dokuları ve Ejder atlasları Android'de ASTC override'lı;
    /// Android max boyutu kaynağın altına inmez (telefonda glif/maske hücresi bulanıklaşmaz).
    /// </summary>
    public sealed class RuleEngineV4AndroidTextureEditModeTests
    {
        const string Android = "Android";
        static readonly string[] SliceTextureFolders =
        {
            "Assets/Art/Ortam",
            "Assets/Art/Characters/k3/Model",
            "Assets/Resources/Vfx/Ejder",
        };

        static IEnumerable<string> SliceTexturePaths() =>
            AssetDatabase.FindAssets("t:Texture2D", SliceTextureFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => AssetImporter.GetAtPath(p) is TextureImporter)
                .Distinct();

        [Test]
        public void SliceTextures_AndroidOverride_Astc_NoDownscale()
        {
            string[] paths = SliceTexturePaths().ToArray();
            Assert.That(paths, Is.Not.Empty, "dilim dokusu bulunamadı");
            var bad = new List<string>();
            foreach (string path in paths)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                TextureImporterPlatformSettings a = ti.GetPlatformTextureSettings(Android);
                ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                if (!a.overridden)
                    bad.Add($"{path}: Android override yok");
                else if (!a.format.ToString().StartsWith("ASTC"))
                    bad.Add($"{path}: Android biçimi {a.format}");
                else if (a.maxTextureSize < System.Math.Max(w, h))
                    bad.Add($"{path}: Android max {a.maxTextureSize} < kaynak {w}x{h}");
            }
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }
    }
}
