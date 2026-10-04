#if UNITY_EDITOR
using Dovus.Game.Arena;
using Dovus.Game.Boss;
using Dovus.Game.Editor;
using Dovus.Game.Vfx;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Editor
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

            foreach (AfterimageTrailView trail in Object.FindObjectsByType<AfterimageTrailView>(FindObjectsSortMode.None))
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
                var ctrl = Object.FindAnyObjectByType<LookPresetController>();
                ctrl?.ApplyPreset(p);
                Thread.Sleep(800);
                if (p == 'C')
                    ctrl?.ForceProbeRender();
                log.AppendLine(ctrl != null ? ctrl.DescribeActiveSettings() : $"preset {p} (no controller)");
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
            var ctrl = Object.FindAnyObjectByType<LookPresetController>();
            ctrl?.ApplyPreset(preset);
            if (preset == 'C')
                ctrl?.ForceProbeRender();
            Debug.Log("[LookCapture] " + (ctrl != null ? ctrl.DescribeActiveSettings() : $"preset {preset} (no controller)"));
            CaptureCamera(Camera.main, Path.Combine(outDir, $"look-{preset}-gameplay.png"));
            CaptureWide(Path.Combine(outDir, $"look-{preset}-wide.png"));
        }

        public static string HashGameplayPngs()
        {
            var sb = new StringBuilder();
            foreach (char p in new[] { 'A', 'B', 'C' })
            {
                string path = Path.Combine(OutDir, $"look-{p}-gameplay.png");
                sb.Append(p).Append('=').Append(File.Exists(path) ? CaptureUtil.Sha256(path) : "missing").Append(' ');
            }
            return sb.ToString().TrimEnd();
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
            var lookCtrl = Object.FindAnyObjectByType<LookPresetController>();
            lookCtrl?.ApplyCameraOverrides(cam, lookCtrl.ActiveRequiresDepthTexture);
            CaptureUtil.CaptureCamera(cam, outputPath, W, H);
            Object.DestroyImmediate(camGo);
        }

        static void CaptureCamera(Camera cam, string outputPath) =>
            CaptureUtil.CaptureCamera(cam, outputPath, W, H);
    }
}
#endif
