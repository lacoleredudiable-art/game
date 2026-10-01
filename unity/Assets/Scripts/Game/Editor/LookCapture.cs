#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.EditorTools
{
    /// <summary>Play Mode'da look A/B/C yakalama (task-look.md / v2).</summary>
    public static class LookCapture
    {
        const string OutDir = @"C:\Users\lacol\_cleanup\look\v2";
        const int W = 1600;
        const int H = 900;

        static BossDirector _pausedBoss;

        /// <summary>Savaş sahnesi: boss AI kapalı (oturum), afterimage temiz.</summary>
        public static void PrepareSession()
        {
            _pausedBoss = Object.FindAnyObjectByType<BossDirector>();
            if (_pausedBoss != null)
                _pausedBoss.enabled = false;

            foreach (AfterimageTrail trail in Object.FindObjectsByType<AfterimageTrail>(FindObjectsSortMode.None))
                trail.Clear();
        }

        public static void EndSession()
        {
            if (_pausedBoss != null)
            {
                _pausedBoss.enabled = true;
                _pausedBoss = null;
            }
        }

        public static void CaptureAllPresets()
        {
            Directory.CreateDirectory(OutDir);
            var log = new StringBuilder();
            foreach (char p in new[] { 'A', 'B', 'C' })
            {
                LookPresets.Apply(p);
                Thread.Sleep(800);
                if (p == 'C')
                {
                    var ctrl = Object.FindAnyObjectByType<LookPresetController>();
                    ctrl?.ForceProbeRender();
                }
                log.AppendLine(LookPresets.DescribeActiveSettings());
                CaptureCamera(Camera.main, Path.Combine(OutDir, $"look-{p}-gameplay.png"));
                CaptureWide(Path.Combine(OutDir, $"look-{p}-wide.png"));
            }
            Debug.Log("[LookCapture]\n" + log);
        }

        public static void CapturePreset(char preset) => CapturePreset(preset, OutDir);

        /// <summary>task-look-v2b doğrulama: v2 çıktısını ezmeden ayrı bir tanı klasörüne yakalar.</summary>
        public static void CapturePreset(char preset, string outDir)
        {
            Directory.CreateDirectory(outDir);
            preset = char.ToUpperInvariant(preset);
            LookPresets.Apply(preset);
            if (preset == 'C')
            {
                var ctrl = Object.FindAnyObjectByType<LookPresetController>();
                ctrl?.ForceProbeRender();
            }
            Debug.Log("[LookCapture] " + LookPresets.DescribeActiveSettings());
            CaptureCamera(Camera.main, Path.Combine(outDir, $"look-{preset}-gameplay.png"));
            CaptureWide(Path.Combine(outDir, $"look-{preset}-wide.png"));
        }

        public static string HashGameplayPngs()
        {
            var sb = new StringBuilder();
            foreach (char p in new[] { 'A', 'B', 'C' })
            {
                string path = Path.Combine(OutDir, $"look-{p}-gameplay.png");
                sb.Append(p).Append('=').Append(File.Exists(path) ? Sha256(path) : "missing").Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
            var hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        static void CaptureWide(string outputPath)
        {
            var camGo = new GameObject("LookCaptureWide");
            var cam = camGo.AddComponent<Camera>();
            // Gökyüzü (task-look-v2b problem 1): bu tanı kamerası eskiden CameraClearFlags.Skybox
            // kullanıyordu; RenderSettings.skybox kasıtlı olarak null (SceneAtmosphere.Apply, sis +
            // düz renk tasarımı), bu yüzden Unity'nin yeni kameralar için varsayılan arka plan rengine
            // (stok mavi, ~RGB 45,80,140) düşüyordu — gerçek oyun kamerası hiç bu yola girmiyor çünkü
            // SolidColor + tuning grisini açıkça ayarlıyor. Tanı kamerası artık "kaynak" değeri —
            // Camera.main'in o anki (ön ayara göre) arka plan rengini — kopyalar.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 55f;
            var target = new Vector3(0f, 0f, 25f);
            cam.transform.position = new Vector3(0f, 9f, -38f);
            cam.transform.rotation = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);
            cam.farClipPlane = 600f;
            var urpData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            CaptureCamera(cam, outputPath);
            Object.DestroyImmediate(camGo);
        }

        static void CaptureCamera(Camera cam, string outputPath)
        {
            if (cam == null)
                return;

            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null)
                urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);

            VolumeManager.instance.Update(cam.transform, cam.cullingMask);

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
            File.WriteAllBytes(outputPath, png);
        }
    }
}
#endif
