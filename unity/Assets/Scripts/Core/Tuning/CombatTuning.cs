
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
        public StatusTuning Status = new StatusTuning();
        public SkillMotionTuning SkillMotion = new SkillMotionTuning();

        /// <summary>
        /// §5: toplam etki × bu katsayı = boss canından düşen (commit).
        /// His retune: 1.0 → 3.5 — bar okunur; ~5×4-rün strike ile düşer (Karadul).
        /// Skill ölçeği ayrıca ClosingDamageMath (fiil base_damage).
        /// </summary>
        public float ClosingDamagePerEffect = 3.5f;

        /// <summary>
        /// Düz vuruşun ölçeklenmemiş gücü. Zırh 100 (%50) sonrası ×4000 ile ~25K
        /// (20–30K bandı, ±%5 sapma içinde). Eski yol totalEffect×3,5 ≈ 14K idi.
        /// </summary>
        public float BasicStrikePower = 12.5f;

        /// <summary>
        /// Skill gücü zırhtan önce bununla çarpılır. Zırh 100 hasarı yarıya indirdiği için
        /// 2, eski 175–222K bandını korur. Delme ve Zafiyet kırılması bundan sonra işler.
        /// </summary>
        public float SkillPreArmorScale = 2f;

        /// <summary>
        /// true olunca DamageCalculator kullanılır, false=eski ClosingDamageMath.
        /// 17 Eyl sahip: deneme süresinde formül+crit açık.
        /// </summary>
        public bool UseFormulaDamage = true;

        /// <summary>
        /// true: mana base_resource_cost'tan azsa cümle başlamaz (Bağlama 3).
        /// Varsayılan açık; panelden kapatılabilir.
        /// </summary>
        public bool EnforceResourceCost = true;

        /// <summary>
        /// true: fiil base_cooldown_sec + global_cooldown_sec dolmadan yeniden cast yok
        /// (Bağlama 4). Varsayılan açık; panelden kapatılabilir.
        /// </summary>
        public bool EnforceCooldown = true;

        /// <summary>
        /// T10: panelin "Sıfırla" ve JSON-yükleme yolu. Alt nesnelerin KİMLİĞİ korunur —
        /// HexagonInputController/DodgeState/SentenceEngine gibi tüketiciler `combat.Dodge` gibi alt
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
            Status.CopyFrom(other.Status);
            SkillMotion.CopyFrom(other.SkillMotion);
            ClosingDamagePerEffect = other.ClosingDamagePerEffect;
            BasicStrikePower = other.BasicStrikePower;
            SkillPreArmorScale = other.SkillPreArmorScale;
            UseFormulaDamage = other.UseFormulaDamage;
            EnforceResourceCost = other.EnforceResourceCost;
            EnforceCooldown = other.EnforceCooldown;
        }

        public void ResetToDefaults() => CopyFrom(new CombatTuning());
    }
}
