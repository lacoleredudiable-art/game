using UnityEngine;

namespace Dovus.Game.Arena
{
    /// <summary>Sabit üstel sis (O-8.7). Verildiğinde kamera arka planı da sis rengidir; görünüm ön ayarı sise dokunmaz.</summary>
    public readonly struct ArenaFog
    {
        public ArenaFog(Color color, float density)
        {
            Color = color;
            Density = density;
        }

        public Color Color { get; }
        public float Density { get; }
    }
}
