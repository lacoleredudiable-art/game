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
    /// (Blender "34" açısı: kamera yönü Blender (0.7,-0.7,0) → Unity (-0.7,0,0.7)).
    /// Batch: -executeMethod Dovus.Game.Editor.K3PoseCapture.CaptureBatch -k3PoseOut &lt;klasör&gt; [-k3PoseYaw &lt;derece,derece&gt;]
    /// -k3PoseYaw: çekim başına kamera Y dönüşü. Bake Into Pose (Body Orientation) klibin başlangıç gövde yönünü
    /// sıfırlar; Blender karesiyle aynı gövde-kamera açısı için o fark verilir.
    /// </summary>
    public static class K3PoseCapture
    {
        const string OutArg = "-k3PoseOut";
        const string YawArg = "-k3PoseYaw";
        const string DefaultOut = "../tools/verify-out/k3";
        const int Size = 900;
        const float CameraDistance = 4f;
        const float FullOrthoWidth = 2.4f;
        const float HandOrthoWidth = 1.0f;
        const float FullCenterHeight = 1.0f;
        static readonly Vector3 ViewDir = new Vector3(-0.7f, 0f, 0.7f).normalized;
        static readonly Color Background = new(0.25f, 0.25f, 0.25f);

        static readonly (string Clip, int Frame, string Weapon)[] Shots =
        {
            ("Kilic_VUR", 7, "Silah_Kilic"),
            ("Yay_VUR", 11, "Silah_Yay"),
        };

        [MenuItem("Dovus/k3/Poz görüntüsü (Kilic_VUR 7, Yay_VUR 11)")]
        public static void Capture() => Capture(DefaultOut, new float[Shots.Length]);

        public static void CaptureBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }
            float[] yaws = new float[Shots.Length];
            string[] yawText = Arg(YawArg)?.Split(',') ?? Array.Empty<string>();
            for (int i = 0; i < yawText.Length && i < yaws.Length; i++)
                yaws[i] = float.Parse(yawText[i], System.Globalization.CultureInfo.InvariantCulture);
            Capture(Arg(OutArg) ?? DefaultOut, yaws);
        }

        static void Capture(string outDir, float[] yaws)
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

            for (int shot = 0; shot < Shots.Length; shot++)
            {
                (string clipName, int frame, string weaponName) = Shots[shot];
                Vector3 view = Quaternion.AngleAxis(yaws[shot], Vector3.up) * ViewDir;
                light.transform.rotation = Quaternion.LookRotation(-view + Vector3.down * 0.8f);
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
                Shoot(cam, rt, view, full, FullOrthoWidth, $"{outDir}/{clipName}_k{frame:00}_tam_34.png");
                Shoot(cam, rt, view, hands, HandOrthoWidth, $"{outDir}/{clipName}_k{frame:00}_el_34.png");
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

        static void Shoot(Camera cam, RenderTexture rt, Vector3 view, Vector3 center, float orthoWidth, string path)
        {
            cam.orthographicSize = orthoWidth / 2f;
            cam.transform.position = center + view * CameraDistance;
            cam.transform.rotation = Quaternion.LookRotation(-view, Vector3.up);
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
