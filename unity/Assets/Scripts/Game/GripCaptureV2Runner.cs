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
            Transform characterRoot = panel.GripRoot;
            if (characterRoot == null)
            {
                TrainingDummy dummy = Object.FindObjectOfType<TrainingDummy>();
                characterRoot = dummy != null ? dummy.transform : null;
            }

            if (characterRoot == null)
            {
                var actor = Object.FindObjectOfType<ActorVisual>();
                characterRoot = actor != null ? actor.transform : null;
            }

            var hiddenActors = new List<GameObject>();
            foreach (ActorVisual av in Object.FindObjectsOfType<ActorVisual>())
            {
                if (av == null)
                    continue;
                Transform t = av.transform;
                if (t == characterRoot || t.IsChildOf(characterRoot))
                    continue;
                hiddenActors.Add(av.gameObject);
                av.gameObject.SetActive(false);
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

            panel.SetCaptureUiVisible(false);
            DodgePractice.SuppressImGui = true;
            var hiddenCanvases = new List<Canvas>();
            foreach (Canvas cv in Object.FindObjectsOfType<Canvas>())
            {
                if (cv == null || !cv.enabled)
                    continue;
                hiddenCanvases.Add(cv);
                cv.enabled = false;
            }

            var disabledIdle = new List<WeaponPropIdleMotion>();
            foreach (WeaponPropIdleMotion idle in Object.FindObjectsOfType<WeaponPropIdleMotion>())
            {
                if (idle == null || !idle.enabled)
                    continue;
                disabledIdle.Add(idle);
                idle.enabled = false;
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

                HumanBodyBones bone = WeaponGripHands.PrimaryIsRight(key)
                    ? HumanBodyBones.RightHand
                    : HumanBodyBones.LeftHand;
                Transform hand = animator != null ? animator.GetBoneTransform(bone) : null;
                if (hand == null)
                {
                    Debug.LogWarning($"[GripCapture] {key} el kemiği yok");
                    continue;
                }

                Transform prop = GripPalmMetrics.FindPropOnHandPublic(hand);
                Vector3 focus = prop != null ? prop.position : hand.position;
                bool rightHand = bone == HumanBodyBones.RightHand;
                Vector3 charRight = RightOffset(characterRoot);
                Vector3 palmOut = -hand.forward;
                if (palmOut.sqrMagnitude < 1e-6f)
                    palmOut = ForwardOffset(characterRoot);
                palmOut.Normalize();

                PlaceCam(cam, focus, focus + palmOut * HandCamDistM + Vector3.up * 0.05f);
                yield return SaveScreenshot(key, "front");

                PlaceCam(cam, focus, focus + charRight * (rightHand ? HandCamDistM : -HandCamDistM) + Vector3.up * 0.03f);
                yield return SaveScreenshot(key, "side");

                Vector3 topDiag = (-ForwardOffset(characterRoot) * 0.55f + RightOffset(characterRoot) * 0.45f + Vector3.up * 0.65f).normalized;
                PlaceCam(cam, focus, focus + topDiag * HandCamDistM);
                yield return SaveScreenshot(key, "topdiag");

                bool isRight = rightHand;
                GripPalmMetrics.Sample sample = GripPalmMetrics.Measure(animator, key, isRight);
                GripPalmMetrics.LogSample(sample);
            }

            foreach (GameObject go in hiddenActors)
            {
                if (go != null)
                    go.SetActive(true);
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

            foreach (WeaponPropIdleMotion idle in disabledIdle)
            {
                if (idle != null)
                    idle.enabled = true;
            }

            foreach (Canvas cv in hiddenCanvases)
            {
                if (cv != null)
                    cv.enabled = true;
            }

            panel.SetCaptureUiVisible(true);
            DodgePractice.SuppressImGui = false;

            DebugConfig.DevLog("[GripCapture] v2 kaydedildi: " + OutDir);
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
