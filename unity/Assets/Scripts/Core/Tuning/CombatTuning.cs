namespace Dovus.Core.Tuning
{
    /// <summary>Tüm dövüş ayarlarını toplar; varsayılanlar belgedeki tablolardan gelir.</summary>
    [System.Serializable]
    public class CombatTuning
    {
        public DodgeTuning Dodge = new DodgeTuning();
        public SentenceTuning Sentence = new SentenceTuning();
        public SlowmoTuning Slowmo = new SlowmoTuning();
        public BossTuning Boss = new BossTuning();
        public GradeTuning Grade = new GradeTuning();
        public FeelTuning Feel = new FeelTuning();
        public ManifestationTuning Manifestation = new ManifestationTuning();

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
            Slowmo.CopyFrom(other.Slowmo);
            Boss.CopyFrom(other.Boss);
            Grade.CopyFrom(other.Grade);
            Feel.CopyFrom(other.Feel);
        }

        public void ResetToDefaults() => CopyFrom(new CombatTuning());
    }
}
