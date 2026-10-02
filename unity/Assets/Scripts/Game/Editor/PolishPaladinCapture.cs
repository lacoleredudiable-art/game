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
    /// <summary>task-polish / polish2 yakalama + tutuş doğrulama.</summary>
    public static class PolishPaladinCapture
    {
        public const string OutDir = @"C:\Users\lacol\_cleanup\polish\v2";
        const int W = 1600;
        const int H = 900;

        static Renderer[] _hiddenTelegraphRenderers;
        static bool _captureSessionActive;

        public static void PrepareSession()
        {
            Directory.CreateDirectory(OutDir);
            _captureSessionActive = true;
            LookCapture.PrepareSession();
            DisableBossCombat();
            DestroyActiveVfx();
            HideTelegraphRenderers(true);
            LookPresets.Apply('B');
            Thread.Sleep(2000);
        }

        public static void EndSession()
        {
            HideTelegraphRenderers(false);
            _captureSessionActive = false;
            LookCapture.EndSession();
        }

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
            ResnapHandProps();
        }

        public static void CapturePaladinFront()
        {
            PrepareShot();
            Transform root = FindPlayerRoot();
            CaptureSoloOnRoot(root, true, "paladin-front.png", strike: false);
            LogCharacterPixelHeight(Path.Combine(OutDir, "paladin-front.png"), "paladin-front");
            LogShieldFaceDots(root, "idle-front");
        }

        public static void CapturePaladinBack()
        {
            PrepareShot();
            Transform root = FindPlayerRoot();
            CaptureSoloOnRoot(root, false, "paladin-back.png", strike: false);
            LogCharacterPixelHeight(Path.Combine(OutDir, "paladin-back.png"), "paladin-back");
            LogShieldFaceDots(root, "idle-back");
        }

        public static void CapturePaladinStrike()
        {
            PrepareShot();
            Transform root = FindPlayerRoot();
            Animator anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            float bestT = SampleBestStrikeTime(anim, out float bestMetric, out bool usedFallback);
            Debug.Log($"[PolishCapture] strike t={bestT:F3} handFwdMetric={bestMetric:F3} fallbackDist={usedFallback}");
            if (anim != null)
                ApplyStrikeSample(anim, bestT);

            CaptureSoloOnRoot(root, true, "paladin-strike.png", strike: true, skipAnimPose: true, strikePose: true);
            LogCharacterPixelHeight(Path.Combine(OutDir, "paladin-strike.png"), "paladin-strike");
            LogShieldFaceDots(root, "strike-front");
        }

        public static void CaptureGameplay()
        {
            Directory.CreateDirectory(OutDir);
            LookPresets.Apply('B');
            DestroyActiveVfx();
            HideTelegraphRenderers(true);
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
            Vector3 side = Vector3.Cross(Vector3.up, CombatFacing(player)).normalized;
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.right;
            var camGo = new GameObject("PolishGroundCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;
            Vector3 camPos = p + side * 2f + Vector3.up * 1.6f;
            Vector3 floorTarget = p + CombatFacing(player) * 2.5f + Vector3.up * 0.05f;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(floorTarget - camPos, Vector3.up);
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
            LogShieldFaceDots(anim.transform.root, "idle-verify");
            for (int i = 0; i < 5; i++)
            {
                float t = 0.12f + i * 0.16f;
                ApplyStrikeSample(anim, t);
                LogBladeAndLegs(anim, $"strike-sample-{i}");
            }
        }

        public static void LogTelegraphPaths()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled)
                    continue;
                Color c = ReadDominantColor(r);
                if (c.r > 0.85f && c.g > 0.25f && c.g < 0.75f && c.b < 0.35f)
                    list.Add(r);
            }

            foreach (Renderer r in list)
                Debug.Log($"[PolishCapture] orange-renderer path={GetPath(r.transform)} mat={r.sharedMaterial?.name}");
        }

        static void DisableBossCombat()
        {
            var boss = Object.FindAnyObjectByType<BossDirector>();
            if (boss != null)
            {
                boss.enabled = false;
                var manifest = boss.GetComponent<ManifestationDirector>();
                if (manifest != null)
                    manifest.enabled = false;
            }

            foreach (BossDirector dir in Object.FindObjectsByType<BossDirector>(FindObjectsSortMode.None))
                dir.enabled = false;
            foreach (ManifestationDirector md in Object.FindObjectsByType<ManifestationDirector>(FindObjectsSortMode.None))
                md.enabled = false;
        }

        static void DestroyActiveVfx()
        {
            foreach (ParticleSystem ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps == null)
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Object.Destroy(ps.gameObject);
            }
        }

        static void HideTelegraphRenderers(bool hide)
        {
            if (hide)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null)
                        continue;
                    string path = GetPath(r.transform);
                    if (path.IndexOf("Telegraph", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("HitboxVfx", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("FireCone", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("LavaDecor", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("LavaPool", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        list.Add(r);
                        continue;
                    }

                    Color c = ReadDominantColor(r);
                    if (c.r > 0.85f && c.g > 0.25f && c.g < 0.75f && c.b < 0.35f && r.bounds.max.y < 2.5f)
                        list.Add(r);
                }

                _hiddenTelegraphRenderers = list.ToArray();
                foreach (Renderer r in _hiddenTelegraphRenderers)
                {
                    if (r != null)
                        r.enabled = false;
                }

                foreach (Renderer r in _hiddenTelegraphRenderers)
                    Debug.Log($"[PolishCapture] telegraph-hidden path={GetPath(r.transform)}");
            }
            else if (_hiddenTelegraphRenderers != null)
            {
                foreach (Renderer r in _hiddenTelegraphRenderers)
                {
                    if (r != null)
                        r.enabled = true;
                }

                _hiddenTelegraphRenderers = null;
            }
        }

        static Color ReadDominantColor(Renderer r)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                return r.sharedMaterial.GetColor("_BaseColor");
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                return r.sharedMaterial.GetColor("_Color");
            return Color.black;
        }

        static string GetPath(Transform t)
        {
            if (t == null)
                return "";
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, '/');
                sb.Insert(0, t.name);
            }

            return sb.ToString();
        }

        static void LogShieldFaceDots(Transform root, string tag)
        {
            if (root == null)
                return;
            Transform shield = FindShieldTransform(root);
            if (shield == null)
            {
                Debug.Log($"[PolishCapture] shield-face {tag}: no shield mesh");
                return;
            }

            Vector3 faceNormal = shield.forward;
            Vector3 charFwd = CombatFacing(root);
            Vector3 charLeft = (-Vector3.Cross(Vector3.up, charFwd)).normalized;
            Debug.Log($"[PolishCapture] shield-face {tag} dotFwd={Vector3.Dot(faceNormal, charFwd):F3} dotLeft={Vector3.Dot(faceNormal, charLeft):F3} normal={faceNormal}");
        }

        static Transform FindShieldTransform(Transform root)
        {
            Animator anim = root.GetComponentInChildren<Animator>();
            Transform fore = anim != null ? anim.GetBoneTransform(HumanBodyBones.LeftLowerArm) : null;
            Transform hand = anim != null ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            foreach (Transform parent in new[] { fore, hand })
            {
                if (parent == null)
                    continue;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform c = parent.GetChild(i);
                    if (c.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (c.name.IndexOf("Shield", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                    foreach (Renderer r in c.GetComponentsInChildren<Renderer>())
                        return r.transform;
                }
            }

            return null;
        }

        static void LogCharacterPixelHeight(string pngPath, string label)
        {
            if (!File.Exists(pngPath))
                return;
            byte[] bytes = File.ReadAllBytes(pngPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            int minY = H;
            int maxY = 0;
            Color[] px = tex.GetPixels();
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color c = px[y * tex.width + x];
                    if (c.grayscale > 0.08f && c.a > 0.5f)
                    {
                        if (y < minY)
                            minY = y;
                        if (y > maxY)
                            maxY = y;
                    }
                }
            }

            Object.DestroyImmediate(tex);
            int height = maxY >= minY ? maxY - minY + 1 : 0;
            float pct = 100f * height / (float)H;
            Debug.Log($"[PolishCapture] frameFill {label} pixelHeight={height} ({pct:F1}% of {H})");
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

        static float SampleBestStrikeTime(Animator anim, out float bestMetric, out bool usedFallback)
        {
            bestMetric = -1f;
            usedFallback = false;
            if (anim == null)
                return 0.42f;

            AnimationClip clip = FindBasicStrikeClip(anim);
            if (clip == null)
            {
                Debug.LogWarning("[PolishCapture] BasicStrike clip missing — animator state fallback");
                return SampleStrikeViaAnimator(anim, out bestMetric, out usedFallback);
            }

            Transform chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                ?? anim.transform;
            Transform root = anim.transform;
            Vector3 fwd = CombatFacing(root);
            bool animWasEnabled = anim.enabled;
            anim.enabled = false;
            float bestT = 0f;
            float bestFwd = float.MinValue;
            float bestDist = -1f;
            float clipLen = clip.length;
            for (int i = 1; i <= 22; i++)
            {
                float t = (i / 23f) * clipLen;
                clip.SampleAnimation(anim.gameObject, t);
                ResnapHandProps();
                Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null)
                    continue;
                float fwdDot = Vector3.Dot(hand.position - chest.position, fwd);
                float dist = Vector3.Distance(hand.position, chest.position);
                if (fwdDot > bestFwd)
                {
                    bestFwd = fwdDot;
                    bestT = t / clipLen;
                    bestMetric = fwdDot;
                }

                if (dist > bestDist)
                    bestDist = dist;
            }

            anim.enabled = animWasEnabled;
            if (bestFwd <= 0f)
            {
                usedFallback = true;
                bestMetric = bestDist;
                bestT = ResampleMaxHandDistance(anim, clip, chest, clipLen);
            }

            return bestT;
        }

        static float ResampleMaxHandDistance(Animator anim, AnimationClip clip, Transform chest, float clipLen)
        {
            bool animWasEnabled = anim.enabled;
            anim.enabled = false;
            float bestT = 0f;
            float bestDist = -1f;
            for (int i = 1; i <= 22; i++)
            {
                float t = (i / 23f) * clipLen;
                clip.SampleAnimation(anim.gameObject, t);
                ResnapHandProps();
                Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null)
                    continue;
                float dist = Vector3.Distance(hand.position, chest.position);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestT = t / clipLen;
                }
            }

            anim.enabled = animWasEnabled;
            return bestT;
        }

        static float SampleStrikeViaAnimator(Animator anim, out float bestMetric, out bool usedFallback)
        {
            usedFallback = false;
            bestMetric = -1f;
            Transform chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                ?? anim.transform;
            float bestT = 0.42f;
            Vector3 fwd = CombatFacing(anim.transform);
            for (int i = 1; i <= 22; i++)
            {
                float t = i / 23f;
                anim.Play("BasicStrike", 0, t);
                anim.Update(0f);
                ResnapHandProps();
                Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null)
                    continue;
                float dist = Vector3.Dot(hand.position - chest.position, fwd);
                if (dist > bestMetric)
                {
                    bestMetric = dist;
                    bestT = t;
                }
            }

            if (bestMetric <= 0f)
                usedFallback = true;
            return bestT;
        }

        static void ApplyStrikeSample(Animator anim, float normalizedT)
        {
            AnimationClip clip = FindBasicStrikeClip(anim);
            if (clip != null)
            {
                bool was = anim.enabled;
                anim.enabled = false;
                clip.SampleAnimation(anim.gameObject, normalizedT * clip.length);
                anim.enabled = was;
                ResnapHandProps();
                return;
            }

            anim.Play("BasicStrike", 0, normalizedT);
            anim.Update(0f);
            ResnapHandProps();
        }

        static void ResnapHandProps()
        {
            foreach (WeaponHandProps whp in Object.FindObjectsByType<WeaponHandProps>(FindObjectsSortMode.None))
            {
                if (whp == null)
                    continue;
                whp.Apply("kilic");
            }
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
            if (!_captureSessionActive)
            {
                LookCapture.PrepareSession();
                DisableBossCombat();
                DestroyActiveVfx();
                HideTelegraphRenderers(true);
            }

            LookPresets.Apply('B');
            EquipKilic();
            SetBlockersHidden(true);
            Thread.Sleep(250);
        }

        static void CaptureSoloOnRoot(Transform root, bool front, string fileName, bool strike, bool skipAnimPose = false, bool strikePose = false)
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

                ResnapHandProps();
            }

            Thread.Sleep(120);
            string path = Path.Combine(OutDir, fileName);
            CaptureTempCamera(root, front, path, strikePose);
        }

        static void CaptureTempCamera(Transform root, bool front, string outputPath, bool strikePose)
        {
            Vector3 face = CombatFacing(root);
            Vector3 focus = root.position + Vector3.up * 1.05f;
            float dist = 3.2f;
            float camHeight = 1.05f;
            Vector3 camDir = front ? face : -face;
            if (strikePose)
            {
                Vector3 right = Vector3.Cross(Vector3.up, face).normalized;
                camDir = Quaternion.AngleAxis(35f, Vector3.up) * face;
                focus += right * 0.15f;
            }

            Vector3 camPos = root.position + camDir * dist + Vector3.up * camHeight;
            var camGo = new GameObject("PolishCaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null ? Camera.main.backgroundColor : new Color(0.56f, 0.6f, 0.66f);
            cam.fieldOfView = 38f;
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
            bool any = false;
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr == null)
                    continue;
                if (!any)
                {
                    bounds = smr.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(smr.bounds);
            }

            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                foreach (HumanBodyBones bone in new[]
                         {
                             HumanBodyBones.RightHand, HumanBodyBones.LeftHand,
                             HumanBodyBones.LeftLowerArm,
                         })
                {
                    Transform t = anim.GetBoneTransform(bone);
                    if (t == null)
                        continue;
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
                    {
                        if (!any)
                        {
                            bounds = r.bounds;
                            any = true;
                        }
                        else
                            bounds.Encapsulate(r.bounds);
                    }
                }
            }

            return any;
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
    }
}
#endif
