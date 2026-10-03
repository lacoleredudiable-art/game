using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Ayar sahnesi el tutuşu yakın plan görüntüleri (Play).</summary>
    public static class GripCaptureV2Runner
    {
        public const string OutDir = @"C:\Users\lacol\_cleanup\grip-check\v2";
        const float HandCamDistM = 0.7f;
        const float HandCamFov = 35f;
        const int CaptureW = 1280;
        const int CaptureH = 720;

        public static void Start(SettingsScenePanel panel)
        {
            if (panel != null)
                panel.StartCoroutine(CaptureRoutine(panel));
        }

        public static IEnumerator CaptureRoutine(SettingsScenePanel panel)
        {
            yield return null;
            yield return null;
            Directory.CreateDirectory(OutDir);

            var registry = Resources.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry");
            TrainingDummy dummy = Object.FindObjectOfType<TrainingDummy>();
            Transform characterRoot = dummy != null ? dummy.transform : null;
            if (characterRoot == null)
            {
                var actor = Object.FindObjectOfType<ActorVisual>();
                characterRoot = actor != null ? actor.transform : null;
            }

            if (characterRoot == null || registry == null || panel == null)
            {
                Debug.LogError("[GripCapture] kukla, registry veya panel yok");
                yield break;
            }

            Animator animator = characterRoot.GetComponentInChildren<Animator>();
            float savedAnimSpeed = animator != null ? animator.speed : 1f;
            if (animator != null)
            {
                animator.speed = 0f;
                animator.Update(0f);
            }

            var disabledCams = new List<Camera>();
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c == null || !c.enabled)
                    continue;
                disabledCams.Add(c);
                c.enabled = false;
            }

            var camGo = new GameObject("GripCheckCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = HandCamFov;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.92f, 0.92f, 0.92f);

            int savedW = Screen.width;
            int savedH = Screen.height;
            Screen.SetResolution(CaptureW, CaptureH, false);

            foreach (string key in WeaponFeelStore.WeaponKeys)
            {
                panel.SelectWeapon(key);
                yield return null;
                yield return new WaitForEndOfFrame();

                HumanBodyBones bone = ResolveGripBone(key, registry);
                Transform hand = animator != null ? animator.GetBoneTransform(bone) : null;
                if (hand == null)
                {
                    Debug.LogWarning($"[GripCapture] {key} el kemiği yok");
                    continue;
                }

                Vector3 focus = hand.position;
                PlaceCam(cam, focus, focus + ForwardOffset(characterRoot) * -HandCamDistM + Vector3.up * 0.04f);
                yield return SaveScreenshot(key, "front");

                PlaceCam(cam, focus, focus + RightOffset(characterRoot) * HandCamDistM);
                yield return SaveScreenshot(key, "side");

                Vector3 topDiag = (-ForwardOffset(characterRoot) * 0.55f + RightOffset(characterRoot) * 0.45f + Vector3.up * 0.65f).normalized;
                PlaceCam(cam, focus, focus + topDiag * HandCamDistM);
                yield return SaveScreenshot(key, "topdiag");
            }

            Screen.SetResolution(savedW, savedH, false);
            Object.Destroy(camGo);
            foreach (Camera c in disabledCams)
            {
                if (c != null)
                    c.enabled = true;
            }

            if (animator != null)
                animator.speed = savedAnimSpeed;

            Debug.Log("[GripCapture] v2 kaydedildi: " + OutDir);
        }

        static IEnumerator SaveScreenshot(string weapon, string angle)
        {
            string path = Path.Combine(OutDir, $"{weapon}_{angle}.png");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
        }

        static void PlaceCam(Camera cam, Vector3 focus, Vector3 camPos)
        {
            cam.transform.position = camPos;
            cam.transform.LookAt(focus, Vector3.up);
        }

        static Vector3 ForwardOffset(Transform root) =>
            root != null ? Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized : Vector3.forward;

        static Vector3 RightOffset(Transform root) =>
            root != null ? Vector3.Cross(Vector3.up, ForwardOffset(root)).normalized : Vector3.right;

        static HumanBodyBones ResolveGripBone(string key, WeaponVisualRegistry registry)
        {
            WeaponVisualRegistry.PropEntry entry = registry.FindProps(key);
            if (entry == null)
                return HumanBodyBones.RightHand;
            if (entry.RightHandPrefab != null)
                return HumanBodyBones.RightHand;
            if (entry.LeftHandPrefab != null)
                return HumanBodyBones.LeftHand;
            return HumanBodyBones.RightHand;
        }
    }
}
