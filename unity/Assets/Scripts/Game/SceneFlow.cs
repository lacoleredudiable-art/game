using UnityEngine.SceneManagement;

namespace Dovus.Game
{
    /// <summary>Prototype ↔ Ayar sahnesi geçişi (Build Settings'te her iki sahne açık).</summary>
    public static class SceneFlow
    {
        public const string PrototypeScene = "Prototype";
        public const string SettingsScene = "Settings";

        public static void LoadSettings() => SceneManager.LoadScene(SettingsScene);

        public static void LoadPrototype() => SceneManager.LoadScene(PrototypeScene);
    }
}
