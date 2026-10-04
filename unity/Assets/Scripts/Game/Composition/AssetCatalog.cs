using Dovus.Game.Audio;
using Dovus.Game.Assets;
using Dovus.Game.Hud;
using UnityEngine;

namespace Dovus.Game.Composition
{
    /// <summary>
    /// Salt-okunur runtime asset'leri composition kökünde bir kez yükler.
    /// Bootstrap dışı (editör, elle sahne) için <see cref="Standalone"/> aynı yedekleri kullanır.
    /// </summary>
    public sealed class AssetCatalog
    {
        static AssetCatalog _standalone;

        public HudTheme HudTheme { get; }
        public SfxLibrary Sfx { get; }

        AssetCatalog(HudTheme hudTheme, SfxLibrary sfx)
        {
            HudTheme = hudTheme;
            Sfx = sfx;
        }

        /// <summary>Bootstrap kurmadığı bağımsız tüketiciler için tek giriş (aynı yedek davranış).</summary>
        public static AssetCatalog Standalone => _standalone ??= Load();

        public static AssetCatalog Load() =>
            new AssetCatalog(ResolveHudTheme(), ResolveSfxLibrary());

        public static void ResetStandaloneForEditor() => _standalone = null;

        static HudTheme ResolveHudTheme()
        {
            HudTheme loaded = AssetLoader.Load<HudTheme>("HudTheme", null);
            if (loaded != null)
                return loaded;
            HudTheme fallback = ScriptableObject.CreateInstance<HudTheme>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }

        static SfxLibrary ResolveSfxLibrary()
        {
            SfxLibrary loaded = AssetLoader.Load<SfxLibrary>("SfxLibrary", null);
            if (loaded != null)
                return loaded;
            SfxLibrary fallback = ScriptableObject.CreateInstance<SfxLibrary>();
            fallback.hideFlags = HideFlags.DontSave;
            return fallback;
        }
    }
}
