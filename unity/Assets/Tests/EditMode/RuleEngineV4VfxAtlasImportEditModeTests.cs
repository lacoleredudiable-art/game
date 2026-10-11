using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    /// <summary>Ortak efekt atlası (siluet + harf + halka): içe aktarma ayarı ve tek VFX_Kor materyali.</summary>
    public sealed class RuleEngineV4VfxAtlasImportEditModeTests
    {
        const string CatalogPath = "Assets/Resources/RuleEngineV4/kural-motoru-v4.json";
        const string ResourcesRoot = "Assets/Resources/";
        const string KorShader = "Dovus/Vfx/VFX_Kor";

        static RuleEngineV4VfxAtlas Layout()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(json, Is.Not.Null, CatalogPath);
            return RuleEngineV4SliceVfx.FromJson(json.text).Atlas;
        }

        [Test]
        public void Atlas_Imported_Astc6x6_LinearMipmapped()
        {
            RuleEngineV4VfxAtlas layout = Layout();
            string path = ResourcesRoot + layout.Resource + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(ti, Is.Not.Null, path);
            Assert.That(ti.sRGBTexture, Is.False, "sRGB kapalı olmalı");
            Assert.That(ti.mipmapEnabled, Is.True, "mipmap açık olmalı");
            Assert.That(ti.maxTextureSize, Is.GreaterThanOrEqualTo(layout.SizePx), "atlas küçültülüyor");
            foreach (string platform in new[] { "Android", "iPhone" })
            {
                TextureImporterPlatformSettings s = ti.GetPlatformTextureSettings(platform);
                Assert.That(s.overridden, Is.True, platform + " ayarı yok");
                Assert.That(s.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6), platform + " ASTC 6×6 değil");
                Assert.That(s.maxTextureSize, Is.GreaterThanOrEqualTo(layout.SizePx), platform + " atlas küçültülüyor");
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex.width, Is.EqualTo(layout.SizePx));
            Assert.That(tex.height, Is.EqualTo(layout.SizePx));
        }

        [Test]
        public void SharedMaterial_KorShader_VertexDissolve_UsesAtlas()
        {
            RuleEngineV4VfxAtlas layout = Layout();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ResourcesRoot + layout.Material + ".mat");
            Assert.That(mat, Is.Not.Null, layout.Material);
            Assert.That(mat.shader.name, Is.EqualTo(KorShader));
            Assert.That(mat.IsKeywordEnabled("_VERTEX_DISSOLVE"), Is.True, "köşe dissolve kapalı");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesRoot + layout.Resource + ".png");
            Assert.That(mat.GetTexture("_MainTex"), Is.SameAs(tex), "_MainTex ortak atlas değil");
            Assert.That(mat.GetTexture("_DissolveTex"), Is.SameAs(tex), "_DissolveTex ortak atlas değil");
            Assert.That(mat.GetTextureScale("_MainTex"), Is.EqualTo(Vector2.one));
            Assert.That(mat.GetTextureOffset("_MainTex"), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SourceAtlases_Kept()
        {
            foreach (string src in new[] { "Vfx/Ejder/Ejder_Glif_Atlas.png", "Vfx/Ejder/Ejder_Siluet_Atlas.png" })
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesRoot + src), Is.Not.Null, src + " silinmiş");
        }
    }
}
