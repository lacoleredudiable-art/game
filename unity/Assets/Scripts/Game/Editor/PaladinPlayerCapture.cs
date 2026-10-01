#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.EditorTools
{
    /// <summary>PR #53 Paladin doğrulama yakalamaları (task-paladin-fix.md).</summary>
    public static class PaladinPlayerCapture
    {
        public const string OutDir = @"C:\Users\lacol\_cleanup\paladin";
        const int W = 1600;
        const int H = 900;

        public static void PrepareSession()
        {
            Directory.CreateDirectory(OutDir);
            LookCapture.PrepareSession();
            LookPresets.Apply('A');
        }

        public static void EndSession() => LookCapture.EndSession();

        public static bool EnsureFight()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return false;
            }

            return AnimPreview.EnterFight();
        }

        public static void EquipDefaultSword()
        {
            AnimPreview.Equip("kilic");
            Thread.Sleep(200);
        }

        public static void LogAllArchetypeProps()
        {
            var player = FindPlayerAnimator();
            var synty = FindAllyAnimator();
            EquipDefaultSword();
            WeaponHandProps.LogPropDiagnostics(player, "Paladin-kilic");
            WeaponHandProps.LogPropDiagnostics(synty, "Synty-kilic");

            foreach (string key in new[] { "yay", "cekic", "asa" })
            {
                AnimPreview.Equip(key);
                Thread.Sleep(150);
                WeaponHandProps.LogPropDiagnostics(player, "Paladin-" + key);
                WeaponHandProps.LogPropDiagnostics(synty, "Synty-" + key);
            }

            EquipDefaultSword();
        }

        static Transform FindPlayerRoot()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.name != "Player")
                    continue;
                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                    continue;
                return t;
            }

            return null;
        }

        /// <summary>Play oturumunda tüm PNG'leri tek seferde yazar (RunCommand arasında Play düşmesin).</summary>
        public static string CaptureAllShots()
        {
            Directory.CreateDirectory(OutDir);
            PrepareSession();
            for (int i = 0; i < 8 && !AnimPreview.EnterFight(); i++)
                Thread.Sleep(500);
            Thread.Sleep(800);
            EquipDefaultSword();
            Thread.Sleep(400);

            Transform player = null;
            for (int i = 0; i < 40; i++)
            {
                player = FindPlayerRoot();
                if (player != null)
                    break;
                Thread.Sleep(250);
            }

            if (player == null)
            {
                Debug.LogError("[PaladinCapture] Player root missing");
                return "player-missing";
            }

            SetBlockersHidden(true);
            CaptureSoloOnRoot(player, true, "paladin-front-idle.png", idle: true);
            Thread.Sleep(200);
            CaptureSoloOnRoot(player, false, "paladin-back-idle.png", idle: true);
            Thread.Sleep(200);
            CaptureSoloOnRoot(player, true, "paladin-front-strike.png", idle: false);
            Thread.Sleep(200);
            SetBlockersHidden(false);
            CaptureGameplay();
            Thread.Sleep(200);
            CaptureSyntyCheckFront();
            EquipDefaultSword();
            Thread.Sleep(200);
            var anim = FindPlayerAnimator();
            if (anim != null)
                WeaponHandProps.LogPropDiagnostics(anim, "Paladin-final-kilic");
            return HashPngs();
        }

        public static void CaptureFrontIdle() => CaptureSolo(true, "paladin-front-idle.png", idle: true);

        public static void CaptureBackIdle() => CaptureSolo(false, "paladin-back-idle.png", idle: true);

        public static void CaptureFrontStrike() => CaptureSolo(true, "paladin-front-strike.png", idle: false);

        public static void CaptureGameplay()
        {
            EquipDefaultSword();
            SetBlockersHidden(false);
            LookPresets.Apply('A');
            Thread.Sleep(300);
            LookCaptureShot(Camera.main, Path.Combine(OutDir, "paladin-gameplay.png"));
        }

        public static void CaptureSyntyCheckFront()
        {
            EquipDefaultSword();
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            if (player != null)
                player.gameObject.SetActive(false);
            if (ally != null)
            {
                ally.gameObject.SetActive(true);
                CaptureSoloOnRoot(ally.transform, true, "synty-check-front.png", idle: true);
            }

            if (player != null)
                player.gameObject.SetActive(true);
        }

        public static string HashPngs()
        {
            var sb = new StringBuilder();
            foreach (string name in new[]
                     {
                         "paladin-front-idle.png", "paladin-back-idle.png", "paladin-front-strike.png",
                         "paladin-gameplay.png", "synty-check-front.png",
                     })
            {
                string path = Path.Combine(OutDir, name);
                long len = File.Exists(path) ? new FileInfo(path).Length : -1;
                sb.Append(name).Append('=').Append(len).Append('b').Append('/').Append(File.Exists(path) ? Sha256(path)[..12] : "missing").Append(' ');
            }

            return sb.ToString().TrimEnd();
        }

        static void CaptureSolo(bool front, string fileName, bool idle)
        {
            EquipDefaultSword();
            SetBlockersHidden(true);
            var player = FindPlayerRoot();
            if (player == null)
            {
                Debug.LogError("[PaladinCapture] Player yok");
                return;
            }

            CaptureSoloOnRoot(player, front, fileName, idle);
        }

        static void CaptureSoloOnRoot(Transform root, bool front, string fileName, bool idle)
        {
            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (idle)
                {
                    anim.Play("Locomotion", 0, 0f);
                    anim.Update(0f);
                }
                else
                {
                    anim.Play("BasicStrike", 0, 0.42f);
                    anim.Update(0f);
                }
            }

            Thread.Sleep(120);
            string path = Path.Combine(OutDir, fileName);
            CaptureTempCamera(root, front, path);
        }

        static void SetBlockersHidden(bool hide)
        {
            var allies = new System.Collections.Generic.List<AllyDummy>(AllyDummy.Live);
            foreach (AllyDummy ally in allies)
            {
                if (ally != null)
                    ally.gameObject.SetActive(!hide);
            }

            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (boss != null)
                boss.gameObject.SetActive(!hide);
        }

        static void CaptureTempCamera(Transform root, bool front, string outputPath)
        {
            const float dist = 3.35f;
            const float camHeight = 1.3f;
            Vector3 focus = root.position + Vector3.up * camHeight * 0.55f;
            Vector3 face = CombatFacing(root);
            Vector3 camPos = focus + (front ? face : -face) * dist + Vector3.up * camHeight * 0.25f;

            var camGo = new GameObject("PaladinCaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(focus - camPos, Vector3.up);

            FrameCharacter(cam, root, 0.60f);

            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            LookCaptureShot(cam, outputPath);
            Object.DestroyImmediate(camGo);
        }

        static Vector3 CombatFacing(Transform root)
        {
            Vector3 f = root.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f)
                f = Vector3.forward;
            return f.normalized;
        }

        static void FrameCharacter(Camera cam, Transform root, float heightFill)
        {
            if (!TryCharacterBounds(root, out Bounds b))
                return;
            float charHeight = Mathf.Max(0.5f, b.size.y);
            float dist = Vector3.Distance(cam.transform.position, b.center);
            float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float visibleHeight = 2f * dist * Mathf.Tan(vFovRad * 0.5f);
            if (visibleHeight <= 0.01f)
                return;
            float want = visibleHeight * heightFill;
            if (charHeight > want)
            {
                float ratio = charHeight / want;
                cam.transform.position = b.center - cam.transform.forward * (dist * ratio);
            }
        }

        static bool TryCharacterBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0)
                return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                bounds.Encapsulate(rs[i].bounds);
            return true;
        }

        public static Animator FindPlayerAnimator()
        {
            Transform t = FindPlayerRoot();
            return t != null ? t.GetComponentInChildren<Animator>() : null;
        }

        static Animator FindAllyAnimator()
        {
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            return ally != null ? ally.GetComponentInChildren<Animator>() : null;
        }

        static void LookCaptureShot(Camera cam, string outputPath)
        {
            if (cam == null)
                return;
            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null)
                urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);

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
            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
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
    }
}
#endif
