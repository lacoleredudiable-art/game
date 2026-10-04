using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>VfxLibrary Resources yüklemesi — Platform/AssetCatalog döngüsünden ayrı.</summary>
    public static class VfxLibraryStandalone
    {
        static VfxLibrary _standalone;

        public static VfxLibrary Shared => _standalone ??= Load();

        public static VfxLibrary Load()
        {
            VfxLibrary loaded = AssetLoader.Load<VfxLibrary>("VfxLibrary", null);
            if (loaded != null)
                return loaded;
            VfxLibrary fallback = ScriptableObject.CreateInstance<VfxLibrary>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }

        public static void ResetForEditor() => _standalone = null;
    }
}
