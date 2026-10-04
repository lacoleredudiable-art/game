using System.Collections.Generic;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// verb_base dash_distance_m kalıbın dash/lunge mesafesini belirler.
    /// Işınlanma ve zıplama fazına dokunmaz; oyuncu ikinci bir sistemle kaymaz.
    /// </summary>
    public static class DashDistance
    {
        public static MotionTemplate Apply(MotionTemplate template, float dashM)
        {
            if (template == null || dashM <= MotionDefaults.MinDashM)
                return template;
            bool changed = false;
            var phases = new List<MotionPhase>(template.Phases.Count);
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                if (phase.Motion is "dash" or "lunge")
                {
                    phases.Add(phase.WithTravel(dashM, phase.BehindM, phase.Land));
                    if (System.Math.Abs(phase.DistanceM - dashM) > 0.001f)
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
