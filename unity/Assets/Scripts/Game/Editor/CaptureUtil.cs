#if UNITY_EDITOR
using Dovus.Game.Arena;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Editor
{
    /// <summary>LookCapture / FixedAngleCapture ortak PNG yakalama.</summary>
    internal static class CaptureUtil
    {
        internal static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
            var hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        internal static void CaptureCamera(Camera cam, string outputPath, int width, int height)
        {
            if (cam == null)
                return;

            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null)
                urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            var lookCtrl = Object.FindAnyObjectByType<LookPresetController>();
            lookCtrl?.ApplyCameraOverrides(cam, lookCtrl != null && lookCtrl.ActiveRequiresDepthTexture);

            VolumeManager.instance.Update(cam.transform, cam.cullingMask);

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
