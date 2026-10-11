using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    /// <summary>
    /// Ortam FBX içe aktarımı: Read/Write kapalı, kamera/ışık/collider yok, ölçek 1 ve beklenen
    /// boyutlar ±%5 (Blender X×Y×Z → Unity X×Z×Y; içe aktarma Z-yukarıyı Y-yukarıya çevirir).
    /// </summary>
    public sealed class RuleEngineV4OrtamImportEditModeTests
    {
        const string Root = "Assets/Art/Ortam/";
        const float SizeTol = 0.05f;
        const int KaburgaLod1Tris = 1499;

        static readonly (string Path, Vector3 BlenderSize)[] Models =
        {
            ("sutun/sutun.fbx", new Vector3(1.58f, 1.75f, 3.0f)),
            ("koza/koza.fbx", new Vector3(1.2f, 0.7f, 1.8f)),
            ("kaburga/kaburga.fbx", new Vector3(17.2f, 4.0f, 12.3f)),
            ("kaburga/kaburga_LOD1.fbx", new Vector3(17.2f, 4.0f, 12.3f)),
            ("runtasi/runtasi.fbx", new Vector3(0.8f, 1.2f, 0.4f)),
            ("duvar/duvar.fbx", new Vector3(3.0f, 0.5f, 1.2f)),
        };

        [Test]
        public void OrtamFbx_ImportSettings_NoReadWriteCamerasLightsColliders()
        {
            foreach ((string path, _) in Models)
            {
                var importer = AssetImporter.GetAtPath(Root + path) as ModelImporter;
                Assert.That(importer, Is.Not.Null, $"{path} içe aktarılmamış");
                Assert.That(importer.isReadable, Is.False, $"{path} Read/Write açık");
                Assert.That(importer.importCameras, Is.False, $"{path} kamera alıyor");
                Assert.That(importer.importLights, Is.False, $"{path} ışık alıyor");
                Assert.That(importer.addCollider, Is.False, $"{path} collider üretiyor");
                Assert.That(importer.globalScale, Is.EqualTo(1f), $"{path} ölçek çarpanı 1 değil");

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + path);
                Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty, $"{path} içinde kamera var");
                Assert.That(model.GetComponentsInChildren<Light>(true), Is.Empty, $"{path} içinde ışık var");
                Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty, $"{path} içinde collider var");
            }
        }

        [Test]
        public void OrtamFbx_Bounds_WithinFivePercent_UprightAtGround()
        {
            foreach ((string path, Vector3 blender) in Models)
            {
                Bounds b = WorldBounds(path);
                var expected = new Vector3(blender.x, blender.z, blender.y);
                for (int axis = 0; axis < 3; axis++)
                {
                    Assert.That(b.size[axis], Is.EqualTo(expected[axis]).Within(expected[axis] * SizeTol),
                        $"{path} ekseni {"XYZ"[axis]}: {b.size[axis]:0.000} m, beklenen {expected[axis]:0.000} m ±%5");
                }
                Assert.That(b.min.y, Is.EqualTo(0f).Within(expected.y * SizeTol), $"{path} tabanı y=0'da değil");
            }
        }

        [Test]
        public void OrtamFbx_KaburgaLod1_TriangleCount()
        {
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(Root + "kaburga/kaburga_LOD1.fbx").OfType<Mesh>().Single();
            long tris = Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)) / 3;
            Assert.That(tris, Is.EqualTo(KaburgaLod1Tris), "kaburga_LOD1 üçgen sayısı");
        }

        static Bounds WorldBounds(string path)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + path);
            GameObject inst = Object.Instantiate(model);
            try
            {
                Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
                Assert.That(renderers, Is.Not.Empty, $"{path} renderer yok");
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers.Skip(1))
                    b.Encapsulate(r.bounds);
                return b;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }
    }
}
