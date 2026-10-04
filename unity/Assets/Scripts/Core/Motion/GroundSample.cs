using System;

namespace Dovus.Core.Motion
{
    /// <summary>Yere basma ölçüsü. Play Sweep aynı eşikleri okur.</summary>
    public readonly struct GroundSample
    {
        public GroundSample(float rootY, float visualOffsetY, bool airborne, bool landing)
        {
            RootY = rootY;
            VisualOffsetY = visualOffsetY;
            Airborne = airborne;
            Landing = landing;
        }

        public float RootY { get; }
        /// <summary>Kalan görsel ofset. Çözücü bunu her örnekte sıfırlar; cast'ler arası birikmez.</summary>
        public float VisualOffsetY { get; }
        public bool Airborne { get; }
        public bool Landing { get; }
    }
}
