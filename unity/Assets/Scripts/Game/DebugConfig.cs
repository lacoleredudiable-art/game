namespace Dovus.Game
{
    /// <summary>Editor ve development build'de true; release'te debug panelleri, dev HP ve log kapalı.</summary>
    public static class DebugConfig
    {
        public static bool Enabled => UnityEngine.Debug.isDebugBuild;

        public static void DevLog(string m)
        {
            if (Enabled)
                UnityEngine.Debug.Log(m);
        }
    }
}
