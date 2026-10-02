#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Dovus.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.EditorTools
{
    /// <summary>task-polish.md yakalama + tutuş doğrulama (C:\Users\lacol\_cleanup\polish).</summary>
    public static class PolishPaladinCapture
    {
        public const string OutDir = @"C:\Users\lacol\_cleanup\polish";
        const int W = 1600;
        const int H = 900;

        public static void PrepareSession()
        {
            Directory.CreateDirectory(OutDir);
            LookCapture.PrepareSession();
            LookPresets.Apply('B');
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

        public static void EquipKilic()
        {
            AnimPreview.Equip("kilic");
            Thread.Sleep(200);
        }

        public static void CapturePaladinFront()
        {
            PrepareShot();
            CaptureSoloOnRoot(FindPlayerRoot(), true, "paladin-front.png", strike: false);
        }

        public static void CapturePaladinBack()
        {
            PrepareShot();
            CaptureSoloOnRoot(FindPlayerRoot(), false, "paladin-back.png", strike: false);
        }

        public static void CapturePaladinStrike()
        {
            PrepareShot();
            Transform root = FindPlayerRoot();
            Animator anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            float bestT = SampleBestStrikeTime(anim, out float bestDist);
            Debug.Log($"[PolishCapture] strike t={bestT:F3} handFwdDist={bestDist:F3}");
            if (anim != null)
                ApplyStrikeSample(anim, bestT);

            CaptureSoloOnRoot(root, true, "paladin-strike.png", strike: true, skipAnimPose: true);
        }

        public static void CaptureGameplay()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            SetBlockersHidden(false);
            EquipKilic();
            Thread.Sleep(300);
            RenderToFile(Camera.main, Path.Combine(OutDir, "gameplay.png"));
        }

        public static void CaptureGroundNear()
        {
            PrepareShot();
            Transform player = FindPlayerRoot();
            if (player == null)
                return;
            Vector3 p = player.position;
            var camGo = new GameObject("PolishGroundCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 40f;
            cam.transform.position = p + new Vector3(0.6f, 1.6f, 0.4f);
            cam.transform.rotation = Quaternion.Euler(35f, 200f, 0f);
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            RenderToFile(cam, Path.Combine(OutDir, "ground-near.png"));
            Object.DestroyImmediate(camGo);
        }

        public static void CaptureSyntyCheck()
        {
            EquipKilic();
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            if (player != null)
                player.gameObject.SetActive(false);
            if (ally != null)
            {
                ally.gameObject.SetActive(true);
                CaptureSoloOnRoot(ally.transform, true, "synty-check.png", strike: false);
            }

            if (player != null)
                player.gameObject.SetActive(true);
        }

        public static void LogGripVerification()
        {
            var anim = FindPlayerAnimator();
            if (anim == null)
                return;
            EquipKilic();
            Thread.Sleep(200);
            anim.Play("Locomotion", 0, 0f);
            anim.Update(0f);
            LogBladeAndLegs(anim, "idle");
            for (int i = 0; i < 5; i++)
            {
                float t = 0.12f + i * 0.16f;
                ApplyStrikeSample(anim, t);
                LogBladeAndLegs(anim, $"strike-sample-{i}");
            }
        }

        static void LogBladeAndLegs(Animator anim, string tag)
        {
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            float tipY = FindBladeTipWorldY(anim, out Transform tip);
            float handY = hand != null ? hand.position.y : -999f;
            bool tipAbove = tipY > handY;
            float legClear = MinBoneDistance(anim, tip, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips, 0.12f);
            Debug.Log($"[GripVerify] {tag} tipY={tipY:F3} handY={handY:F3} tipAboveHand={tipAbove} legClearMin={legClear:F3}");
        }

        static float MinBoneDistance(Animator anim, Transform tip, HumanBodyBones a, HumanBodyBones b, HumanBodyBones c, float radius)
        {
            if (tip == null)
                return -1f;
            float min = float.MaxValue;
            foreach (HumanBodyBones bone in new[] { a, b, c })
            {
                Transform t = anim.GetBoneTransform(bone);
                if (t == null)
                    continue;
                float d = Vector3.Distance(tip.position, t.position) - radius;
                if (d < min)
                    min = d;
            }

            return min == float.MaxValue ? -1f : min;
        }

        static float FindBladeTipWorldY(Animator anim, out Transform tipTransform)
        {
            tipTransform = null;
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null)
                return -999f;
            float maxY = float.MinValue;
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform c = hand.GetChild(i);
                if (c.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>())
                {
                    float y = r.bounds.max.y;
                    if (y > maxY)
                    {
                        maxY = y;
                        tipTransform = r.transform;
                    }
                }
            }

            return maxY == float.MinValue ? -999f : maxY;
        }

        static float SampleBestStrikeTime(Animator anim, out float bestDist)
        {
            bestDist = -1f;
            if (anim == null)
                return 0.42f;
            Transform chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                ?? anim.transform;
            float bestT = 0.42f;
            for (int i = 0; i < 20; i++)
            {
                float t = (i + 0.5f) / 20f;
                anim.Play("BasicStrike", 0, t);
                anim.Update(0f);
                Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null)
                    continue;
                Vector3 fwd = anim.transform.forward;
                fwd.y = 0f;
                fwd.Normalize();
                float dist = Vector3.Dot(hand.position - chest.position, fwd);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestT = t;
                }
            }

            return bestT;
        }

        static void ApplyStrikeSample(Animator anim, float normalizedT)
        {
            anim.Play("BasicStrike", 0, normalizedT);
            anim.Update(0f);
        }

        static AnimationClip FindBasicStrikeClip(Animator anim)
        {
            var ctrl = anim.runtimeAnimatorController;
            if (ctrl is AnimatorOverrideController aoc)
            {
                var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                aoc.GetOverrides(list);
                foreach (var kv in list)
                {
                    if (kv.Key != null && kv.Key.name.IndexOf("Strike", System.StringComparison.OrdinalIgnoreCase) >= 0
                        && kv.Value != null)
                        return kv.Value;
                }
            }

            foreach (AnimationClip c in ctrl.animationClips)
            {
                if (c.name.IndexOf("Strike", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || c.name.IndexOf("SS_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
            }

            return null;
        }

        static void PrepareShot()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            EquipKilic();
            SetBlockersHidden(true);
            Thread.Sleep(250);
        }

        static void CaptureSoloOnRoot(Transform root, bool front, string fileName, bool strike, bool skipAnimPose = false)
        {
            if (root == null)
                return;
            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim != null && !skipAnimPose)
            {
                if (!strike)
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

        static void CaptureTempCamera(Transform root, bool front, string outputPath)
        {
            Vector3 face = CombatFacing(root);
            Vector3 focus = root.position + Vector3.up * 1.05f;
            float dist = 2.6f;
            float camHeight = 1.2f;
            Vector3 camPos = root.position + (front ? face : -face) * dist + Vector3.up * camHeight;
            var camGo = new GameObject("PolishCaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(focus - camPos, Vector3.up);
            FrameCharacter(cam, root, 0.60f);
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            LookPresets.ApplyCameraOverrides(cam, LookPresets.ActiveRequiresDepthTexture);
            RenderToFile(cam, outputPath);
            Object.DestroyImmediate(camGo);
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

        static Vector3 CombatFacing(Transform root)
        {
            Vector3 f = root.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f)
                f = Vector3.forward;
            return f.normalized;
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

        public static Animator FindPlayerAnimator()
        {
            Transform t = FindPlayerRoot();
            return t != null ? t.GetComponentInChildren<Animator>() : null;
        }

        static void RenderToFile(Camera cam, string outputPath)
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

        public static string HashPngs()
        {
            var sb = new StringBuilder();
            foreach (string name in new[]
                     {
                         "paladin-front.png", "paladin-back.png", "paladin-strike.png", "gameplay.png",
                         "ground-near.png", "synty-check.png",
                     })
            {
                string path = Path.Combine(OutDir, name);
                long len = File.Exists(path) ? new FileInfo(path).Length : -1;
                sb.Append(name).Append('=').Append(len).Append('b').Append(' ');
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
    }
}
#endif
