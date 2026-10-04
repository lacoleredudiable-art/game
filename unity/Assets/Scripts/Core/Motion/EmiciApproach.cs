namespace Dovus.Core.Motion
{
    /// <summary>
    /// Emici çekme sürerken yerinde kalma, yalnız hedefe doğru ilerleyen fazlar içindir.
    /// İçinden geçen (overshoot / arkaya iniş) ve uzaklaşan fazlar durmaz.
    /// </summary>
    public static class EmiciApproach
    {
        public static bool IsEmici(string adjectiveId) => adjectiveId == "2";

        public static bool PassesThrough(MotionPhase phase) =>
            phase != null && (phase.OvershootM > MotionDefaults.MinDistM || phase.Land == "behind");

        /// <summary>Hedefe yaklaşan hamle. Geri çekilme, atış ve tutma buna girmez.</summary>
        public static bool AdvancesToward(MotionPhase phase)
        {
            if (phase == null || PassesThrough(phase))
                return false;
            return phase.Motion is "lunge" or "dash" or "leap" or "slam";
        }

        public static bool TemplatePassesThrough(MotionTemplate template)
        {
            if (template == null)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                if (PassesThrough(template.Phases[i]))
                    return true;
            }
            return false;
        }

        public static bool TemplateAdvancesToward(MotionTemplate template)
        {
            if (template == null || TemplatePassesThrough(template))
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                if (AdvancesToward(template.Phases[i]))
                    return true;
            }
            return false;
        }

        /// <summary>1-2 / 4-2 gibi: oyuncu yerinde, boss temas noktasına gelir.</summary>
        public static bool ShouldHoldCaster(string adjectiveId, MotionTemplate template) =>
            IsEmici(adjectiveId) && TemplateAdvancesToward(template);

        /// <summary>
        /// Tarama aracı: yerinde kalan Emici kalıbı "yerinde" bekler.
        /// Duran boss'lu simülasyon ileri hamle görse de oyun oyuncuyu yürütmez.
        /// </summary>
        public static string SweepStayCategory(string adjectiveId, MotionTemplate template) =>
            ShouldHoldCaster(adjectiveId, template) ? "yerinde" : null;

        /// <summary>Çekme bayrağı bu fazı dondurur mu?</summary>
        public static bool Freezes(MotionPhase phase, bool holdApproach) =>
            holdApproach && AdvancesToward(phase);
    }
}
