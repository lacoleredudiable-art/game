namespace Dovus.Core.Tuning
{
    /// <summary>Yavaş çekim — dovus-sistemi.md §7.</summary>
    public class SlowmoTuning
    {
        public float Factor = 0.22f;
        public int RampDownMs = 55;
        public int HoldMs = 190;
        public int RampUpMs = 420;
        public int AudioLowpassHz = 700;
        public DodgeGrade SlowmoMinGrade = DodgeGrade.Temiz;
        public int SlowmoBonusDots = 0;
    }
}
