using System.Collections.Generic;

namespace Dovus.Core.Motion
{
    /// <summary>Gövde güdümü (x-9) atış fazını hedefe kilitler. Oyuncunun yerini değiştirmez.</summary>
    public static class HomingDelivery
    {
        public static MotionTemplate TrackShots(MotionTemplate template)
        {
            if (template == null)
                return null;
            bool changed = false;
            var phases = new List<MotionPhase>(template.Phases.Count);
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                if (phase.Motion == "throw" && phase.Homing != "track")
                {
                    phases.Add(phase.WithHoming("track"));
                    changed = true;
                }
                else
                {
                    phases.Add(phase);
                }
            }

            return changed ? template.WithPhases(phases) : template;
        }
    }
}
