using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Hud
{
    static class CombatIconLoader
    {
        public static Sprite Load(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;
            Texture2D texture = AssetLoader.Load<Texture2D>(resourcePath, null);
            if (texture == null)
                return null;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
        }
    }
}
