namespace Dovus.Core.Tuning
{
    /// <summary>
    /// space_layer link/tear his sayıları.
    /// JSON'da yalnız duration_sec + damage_on_cross var; drain/heal/range/tick
    /// 17 Eyl sahip görevinde verildi (durum.md).
    /// </summary>
    [System.Serializable]
    public class SpaceLayerTuning
    {
        /// <summary>Link tick aralığı — 2s / 0.5s = 4 tick.</summary>
        public float LinkTickIntervalSec = 0.5f;
        public float LinkDrainPerTick = 3f;
        public float LinkHealPerTick = 1.5f;
        public float LinkMaxRangeM = 12f;
        public float LinkLineWidthM = 0.15f;

        /// <summary>Tear kapsül (yükseklik × genişlik × derinlik).</summary>
        public float TearHeightM = 3f;
        public float TearWidthM = 0.5f;
        public float TearDepthM = 0.2f;
        public float TearCloseAnimSec = 0.35f;

        public SpaceLayerTuning Clone()
        {
            var c = new SpaceLayerTuning();
            c.CopyFrom(this);
            return c;
        }

        public void CopyFrom(SpaceLayerTuning other)
        {
            if (other == null) return;
            LinkTickIntervalSec = other.LinkTickIntervalSec;
            LinkDrainPerTick = other.LinkDrainPerTick;
            LinkHealPerTick = other.LinkHealPerTick;
            LinkMaxRangeM = other.LinkMaxRangeM;
            LinkLineWidthM = other.LinkLineWidthM;
            TearHeightM = other.TearHeightM;
            TearWidthM = other.TearWidthM;
            TearDepthM = other.TearDepthM;
            TearCloseAnimSec = other.TearCloseAnimSec;
        }
    }
}
