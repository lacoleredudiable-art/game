#if UNITY_EDITOR
using System.IO;
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Play Mode'da look A/B/C yakalama (task-look.md).</summary>
    public static class LookCapture
    {
        const string OutDir = @"C:\Users\lacol\_cleanup\look";
        const int W = 1600;
        const int H = 900;

        public static void CaptureAllPresets()
        {
            Directory.CreateDirectory(OutDir);
            foreach (char p in new[] { 'A', 'B', 'C' })
            {
                LookPresets.Apply(p);
                if (p == 'C')
                {
                    var ctrl = Object.FindAnyObjectByType<LookPresetController>();
                    ctrl?.ForceProbeRender();
                }
                CaptureCamera(Camera.main, $"look-{p}-gameplay.png");
                CaptureWide($"look-{p}-wide.png");
            }
        }

        static void CaptureWide(string fileName)
        {
            var camGo = new GameObject("LookCaptureWide");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(-12f, 28f, -55f);
            cam.transform.rotation = Quaternion.Euler(22f, 35f, 0f);
            cam.farClipPlane = 400f;
            CaptureCamera(cam, fileName);
            Object.DestroyImmediate(camGo);
        }

        static void CaptureCamera(Camera cam, string fileName)
        {
            if (cam == null)
                return;

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            rt.Release();
            Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(Path.Combine(OutDir, fileName), png);
        }
    }
}
#endif
