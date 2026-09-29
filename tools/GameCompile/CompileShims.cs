namespace Dovus.Game
{
    /// <summary>
    /// Unity 2021 referansında olmayan 2023 aramaları. Yalnız derleme kontrolü.
    /// </summary>
    public static class CompileShims
    {
        public static T FindAnyObjectByType<T>() where T : UnityEngine.Object => null;

        public static T[] FindObjectsByType<T>(SortMode sort) where T : UnityEngine.Object =>
            System.Array.Empty<T>();

        public enum SortMode
        {
            None,
            InstanceID
        }
    }
}
