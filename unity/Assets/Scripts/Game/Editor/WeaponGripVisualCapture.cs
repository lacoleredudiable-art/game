#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Ayar sahnesi tutuş ekran görüntüleri (grip-check).</summary>
    public static class WeaponGripVisualCapture
    {
        const string OutDir = @"C:\Users\lacol\_cleanup\grip-check";
        const string PendingSessionKey = "Dovus.GripCaptureAfterPlay";

        [InitializeOnLoadMethod]
        static void HookPlayCapture()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode ||
                    !SessionState.GetBool(PendingSessionKey, false))
                    return;
                SessionState.SetBool(PendingSessionKey, false);
                EditorApplication.delayCall += RunCapturePass;
            };
        }

        public static bool Run(out string report)
        {
            report = OutDir;
            if (!EditorApplication.isPlaying)
            {
                report = "Play modu gerekli; menüden Capture çalıştırın.";
                return false;
            }

            RunCapturePass();
            return true;
        }

        [MenuItem("Dovus/Grip/Capture Settings Grips (Play)")]
        public static void CaptureFromMenu()
        {
            if (!EditorApplication.isPlaying)
            {
                SessionState.SetBool(PendingSessionKey, true);
                EditorSceneManager.OpenScene("Assets/Scenes/Settings.unity", OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
                return;
            }

            RunCapturePass();
        }

        public static void RunCapturePass()
        {
            Directory.CreateDirectory(OutDir);
            var panel = Object.FindFirstObjectByType<SettingsScenePanel>();
            Camera cam = Camera.main;
            if (panel == null || cam == null)
            {
                Debug.LogError("[GripCapture] panel veya kamera yok (Settings Play?)");
                return;
            }

            foreach (string key in WeaponFeelStore.WeaponKeys)
            {
                panel.SelectWeapon(key);
                Capture(key, "front", cam, new Vector3(0.6f, 1.1f, 1.4f));
                Capture(key, "side", cam, new Vector3(1.3f, 1.0f, 0.4f));
            }

            Debug.Log("[GripCapture] kaydedildi: " + OutDir);
        }

        static void Capture(string weapon, string angle, Camera template, Vector3 offset)
        {
            var camGo = Object.Instantiate(template.gameObject);
            var cam = camGo.GetComponent<Camera>();
            Transform target = Object.FindFirstObjectByType<TrainingDummy>()?.transform;
            if (target == null)
                target = Object.FindFirstObjectByType<ActorVisual>()?.transform;
            if (target != null)
            {
                Vector3 focus = target.position + Vector3.up * 1.2f;
                cam.transform.position = focus + offset;
                cam.transform.LookAt(focus);
            }

            string path = Path.Combine(OutDir, $"{weapon}_{angle}.png");
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
#endif
