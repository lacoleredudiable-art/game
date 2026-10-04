using System;

namespace Dovus.App.Boss
{
    /// <summary>BossDirector poise / pounce / oyuncu-down zamanlama alanları (tek kopya).</summary>
    public sealed class BossPoiseState
    {
        public bool PounceLeapActive { get; set; }
        public float PounceLandX { get; set; }
        public float PounceLandZ { get; set; }
        public bool PlayerWasDown { get; set; }

        public bool TryPlanPounceLeap(float homeX, float homeZ, float landX, float landZ, float minDistM)
        {
            PounceLandX = landX;
            PounceLandZ = landZ;
            float dx = landX - homeX;
            float dz = landZ - homeZ;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist < minDistM)
            {
                PounceLeapActive = false;
                return false;
            }

            PounceLeapActive = true;
            return true;
        }

        public void CompletePounceLeap() => PounceLeapActive = false;
    }
}
