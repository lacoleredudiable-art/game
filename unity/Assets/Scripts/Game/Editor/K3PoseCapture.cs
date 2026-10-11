using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// k3 prefabını kaydedilmeyen geçici sahnede klip karesine örnekleyip ortografik PNG çeker
    /// (Blender "34" açısı: kamera yönü Blender (0.7,-0.7,0) → Unity (-0.7,0,0.7); ayrıca önden ve yandan).
    /// Batch: -executeMethod Dovus.Game.Editor.K3PoseCapture.CaptureBatch -k3PoseOut &lt;klasör&gt;
    /// </summary>
    public static class K3PoseCapture
    {
        const string OutArg = "-k3PoseOut";
        const string DefaultOut = "../tools/verify-out/k3";
        const int Size = 900;
        const float CameraDistance = 4f;
        const float FullOrthoWidth = 2.4f;
        const float HandOrthoWidth = 1.0f;
        const float FullCenterHeight = 1.0f;
        static readonly Color Background = new(0.25f, 0.25f, 0.25f);

        // Karakter +Z'ye bakar; "yan" sol taraftan (yay sol elde).
        static readonly Dictionary<string, Vector3> Views = new()
        {
            ["34"] = new Vector3(-0.7f, 0f, 0.7f).normalized,
            ["on"] = Vector3.forward,
            ["yan"] = Vector3.left,
        };

        static readonly (string Clip, int Frame, string Weapon, string[] Views)[] Shots =
        {
            ("Kilic_VUR", 7, "Silah_Kilic", new[] { "34", "on" }),
            ("Yay_VUR", 11, "Silah_Yay", new[] { "34", "on", "yan" }),
            ("Loko_Kilic_Idle", 54, "Silah_Kilic", new[] { "34", "on" }),
            ("Loko_Yay_Idle", 77, "Silah_Yay", new[] { "34", "on" }),
        };

        [MenuItem("Dovus/k3/Poz görüntüsü (VUR vuruş kareleri + idle)")]
        public static void Capture() => Capture(DefaultOut);

        public static void CaptureBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, OutArg);
            Capture(i >= 0 && i + 1 < args.Length ? args[i + 1] : DefaultOut);
        }

        static void Capture(string outDir)
        {
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(K3AssetSetup.PrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Animator anim = go.GetComponentInChildren<Animator>(true);
            Dictionary<string, AnimationClip> clips = K3AssetSetup.LoadClips();
            Transform[] weapons = go.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Silah_", StringComparison.Ordinal)).ToArray();

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            var rt = new RenderTexture(Size, Size, 24);
            cam.targetTexture = rt;

            foreach ((string clipName, int frame, string weaponName, string[] views) in Shots)
            {
                foreach (Transform w in weapons)
                    w.gameObject.SetActive(w.name == weaponName);
                PlayableGraph graph = PlayableGraph.Create("k3Pose");
                var output = AnimationPlayableOutput.Create(graph, "k3", anim);
                var playable = AnimationClipPlayable.Create(graph, clips[clipName]);
                output.SetSourcePlayable(playable);
                playable.SetTime((frame - 1) / K3AssetSetup.Fps);
                graph.Evaluate();
                List<GameObject> baked = BakeSkins(go);
                Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                Vector3 full = new(hips.position.x, FullCenterHeight, hips.position.z);
                Vector3 hands = (anim.GetBoneTransform(HumanBodyBones.LeftHand).position
                                 + anim.GetBoneTransform(HumanBodyBones.RightHand).position) / 2f;
                foreach (string view in views)
                {
                    Vector3 dir = Views[view];
                    light.transform.rotation = Quaternion.LookRotation(-dir + Vector3.down * 0.8f);
                    Shoot(cam, rt, dir, full, FullOrthoWidth, $"{outDir}/{clipName}_k{frame:00}_tam_{view}.png");
                    Shoot(cam, rt, dir, hands, HandOrthoWidth, $"{outDir}/{clipName}_k{frame:00}_el_{view}.png");
                }
                foreach (GameObject b in baked)
                    UnityEngine.Object.DestroyImmediate(b);

                Transform weapon = weapons.First(t => t.name == weaponName);
                Debug.Log($"[k3poz] {clipName} k{frame} {weaponName} lp={weapon.localPosition:F4} lr={weapon.localRotation:F4} " +
                          $"tip={K3AssetSetup.FindDeep(weapon, "Vfx_Tip").position:F3} " +
                          string.Join(" ", new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftHand, HumanBodyBones.RightHand }
                              .Select(b => $"{b}={anim.GetBoneTransform(b).position:F3}")));
                graph.Destroy();
            }
            rt.Release();
            Debug.Log("[k3poz] tamam → " + Path.GetFullPath(outDir));
        }

        /// <summary>Batch modda SkinnedMeshRenderer render çağrısında yeni pozu derilemez; poz pişmiş mesh olarak çizilir.</summary>
        static List<GameObject> BakeSkins(GameObject root)
        {
            var baked = new List<GameObject>();
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null)
                    continue;
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var go = new GameObject(smr.name + "_Baked");
                go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                smr.enabled = false;
                baked.Add(go);
            }
            return baked;
        }

        static void Shoot(Camera cam, RenderTexture rt, Vector3 viewDir, Vector3 center, float orthoWidth, string path)
        {
            cam.orthographicSize = orthoWidth / 2f;
            cam.transform.position = center + viewDir * CameraDistance;
            cam.transform.rotation = Quaternion.LookRotation(-viewDir, Vector3.up);
            // İlk Render shader/mesh yüklemesinde boş kare verebiliyor; ısınma çizimi.
            cam.Render();
            cam.Render();
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
