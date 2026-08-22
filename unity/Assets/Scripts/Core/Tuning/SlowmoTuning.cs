namespace Dovus.Core.Tuning
{
    /// <summary>Yavaş çekim — dovus-sistemi.md §7.</summary>
    [System.Serializable]
    public class SlowmoTuning
    {
        public float Factor = 0.22f;
        public int RampDownMs = 55;

        // T8.2: hold 190→900, rampUp 420→600. Eski sayılarla yavaş çekim HİÇBİR dokunuş
        // temposunda kelime sayısını değiştirmiyordu (ölçüldü), yani §7'nin "tavana ulaşmanı
        // sağlar" ödülü gramerde karşılıksızdı. Sebep faktör değil profilin yönü: pencereler
        // cümle büyüdükçe daralıyor (420/360/300) ama yavaş çekim zamanla zayıflıyor, üçüncü
        // boşluk yavaş çekim bittikten sonraya düşüyordu. Değerler §7'nin "bir mükemmel dodge
        // ≈ iki ekstra nokta" kurundan geriye çözüldü; daha uzunu beceri bandını siliyor.
        public int HoldMs = 900;
        public int RampUpMs = 600;
        public int AudioLowpassHz = 700;
        public DodgeGrade SlowmoMinGrade = DodgeGrade.Temiz;
        public int SlowmoBonusDots = 0;

        public void CopyFrom(SlowmoTuning other)
        {
            Factor = other.Factor;
            RampDownMs = other.RampDownMs;
            HoldMs = other.HoldMs;
            RampUpMs = other.RampUpMs;
            AudioLowpassHz = other.AudioLowpassHz;
            SlowmoMinGrade = other.SlowmoMinGrade;
            SlowmoBonusDots = other.SlowmoBonusDots;
        }

        public void ResetToDefaults() => CopyFrom(new SlowmoTuning());
    }
}
