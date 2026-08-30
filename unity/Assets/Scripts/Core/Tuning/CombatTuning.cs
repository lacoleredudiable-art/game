namespace Dovus.Core.Tuning
{
    /// <summary>Tüm dövüş ayarlarını toplar; varsayılanlar belgedeki tablolardan gelir.</summary>
    [System.Serializable]
    public class CombatTuning
    {
        public DodgeTuning Dodge = new DodgeTuning();
        public SentenceTuning Sentence = new SentenceTuning();
        public BossTuning Boss = new BossTuning();
        public GradeTuning Grade = new GradeTuning();
        public FeelTuning Feel = new FeelTuning();
        public ManifestationTuning Manifestation = new ManifestationTuning();

        /// <summary>
        /// §5: toplam etki × bu katsayı = boss canından düşen. Başlangıç 1'e 1
        /// (dovus-sistemi.md "Etkinin hasara çevrilmesi", 23 Ağustos kararı).
        /// </summary>
        public float ClosingDamagePerEffect = 1.0f;

        /// <summary>
        /// T10: panelin "Sıfırla" ve JSON-yükleme yolu. Alt nesnelerin KİMLİĞİ korunur —
        /// PentagonInput/DodgeState/SentenceEngine gibi tüketiciler `combat.Dodge` gibi alt
        /// nesnenin REFERANSINI tutuyor (Bind sırasında), üst nesneyi değil. `Manifestation`
        /// bilerek dışarıda: hiçbir UI onu değiştirmiyor, spec değerleri hep aynı kalıyor.
        /// </summary>
        public void CopyFrom(CombatTuning other)
        {
            Dodge.CopyFrom(other.Dodge);
            Sentence.CopyFrom(other.Sentence);
            Boss.CopyFrom(other.Boss);
            Grade.CopyFrom(other.Grade);
            Feel.CopyFrom(other.Feel);
            ClosingDamagePerEffect = other.ClosingDamagePerEffect;
        }

        public void ResetToDefaults() => CopyFrom(new CombatTuning());
    }
}
