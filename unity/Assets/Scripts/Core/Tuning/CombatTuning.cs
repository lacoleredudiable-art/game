namespace Dovus.Core.Tuning
{
    /// <summary>Tüm dövüş ayarlarını toplar; varsayılanlar belgedeki tablolardan gelir.</summary>
    public class CombatTuning
    {
        public DodgeTuning Dodge = new DodgeTuning();
        public SentenceTuning Sentence = new SentenceTuning();
        public SlowmoTuning Slowmo = new SlowmoTuning();
        public BossTuning Boss = new BossTuning();
        public GradeTuning Grade = new GradeTuning();
        public FeelTuning Feel = new FeelTuning();
        public ManifestationTuning Manifestation = new ManifestationTuning();
    }
}
