using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
namespace Dovus.Game.Config
{
    public enum TuningPreset
        {
            Agir,
            Cevik,
            Anime
        }
    
        /// <summary>
        /// Üç hazır his profili. Sayılar SPEC DEĞİL — durum.md'de T10 sapması olarak kayıtlı,
        /// telefonda hızlı deneme için icat edilmiş başlangıç noktaları. Her preset kendi içinde
        /// GradeTuning.TemizGapMaxMs &lt; DodgeTuning.IframeMs kısıtını (§6) korur.
        /// Alanlar TEK TEK yazılır — nesne referansları (Combat.Dodge vb.) DEĞİŞTİRİLMEZ, yoksa
        /// DodgeState/SentenceEngine gibi tüketiciler eski nesneye bakmayı sürdürür (bkz. CombatTuning).
        /// </summary>
    }
