#if UNITY_EDITOR
using Dovus.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Ayar sahnesi tutuş ekran görüntüleri (grip-check).</summary>
    public static class WeaponGripVisualCapture
    {
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
                EditorApplication.delayCall += () =>
                {
                    var panel = Object.FindObjectOfType<SettingsScenePanel>();
                    if (panel != null)
                        GripCaptureV2Runner.Start(panel);
                    else
                        Debug.LogError("[GripCapture] SettingsScenePanel yok");
                };
            };
        }

        public static bool Run(out string report)
        {
            report = GripCaptureV2Runner.OutDir;
            if (!EditorApplication.isPlaying)
            {
                report = "Play modu gerekli; menüden Capture çalıştırın.";
                return false;
            }

            var panel = Object.FindObjectOfType<SettingsScenePanel>();
            if (panel == null)
            {
                report = "SettingsScenePanel yok";
                return false;
            }

            GripCaptureV2Runner.Start(panel);
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

            var panel = Object.FindObjectOfType<SettingsScenePanel>();
            if (panel == null)
            {
                Debug.LogError("[GripCapture] panel yok");
                return;
            }

            GripCaptureV2Runner.Start(panel);
        }
    }
}
#endif
