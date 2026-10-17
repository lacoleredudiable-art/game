using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>SI-3.7 karakter kenar ışığı (16-sanat-incili.md; durum ÖNERİ): renk #CFD8E0, üs 3, şiddet 0,35.</summary>
    public static class RimLightDefaults
    {
        public const string ShaderName = "Dovus/Characters/RimOverlay";
        public const string OverlayVisualChildName = "Visual";

        /// <summary>#CFD8E0.</summary>
        public static readonly Color RimColor = new Color32(0xCF, 0xD8, 0xE0, 0xFF);
        public const float Power = 3f;
        public const float Intensity = 0.35f;
    }
}
