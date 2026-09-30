using System.Collections.Generic;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Top geri tepmesi kalıbın son fazıdır. Ayrı bir konum yazısı tarama aracında
    /// ikinci hareket sistemi sayılır. Süre, 0,5 m'nin sıçrama sınırının altında kalması için.
    /// </summary>
    public static class CannonRecoilMotion
    {
        public const string PhaseName = "geri_tepme";
        /// <summary>0,5 m / 0,12 sn ≈ 4,2 m/s. 60 FPS karesi sınırın altında.</summary>
        public const float DurationSec = 0.12f;

        public static bool Contains(MotionTemplate template)
        {
            if (template == null)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                if (template.Phases[i].Name == PhaseName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Emici yerinde tutulan kalıba geri tepme eklenmez. Oyuncu 0,5 m geri kayınca
        /// tarama "geri" der ve çekilen boss gövdenin içine biner.
        /// </summary>
        public static bool Applies(
            string hitShape,
            float recoilM,
            float baseDamage,
            string adjectiveId,
            MotionTemplate template)
        {
            if (hitShape != "ballistic" || recoilM <= 0.01f || baseDamage <= 0.01f)
                return false;
            return !EmiciApproach.ShouldHoldCaster(adjectiveId, template);
        }

        public static MotionTemplate Append(MotionTemplate template, float recoilM)
        {
            if (template == null || recoilM <= 0.01f || Contains(template))
                return template;
            var phases = new List<MotionPhase>(template.Phases.Count + 1);
            for (int i = 0; i < template.Phases.Count; i++)
                phases.Add(template.Phases[i]);
            phases.Add(new MotionPhase(
                PhaseName, "retreat", DurationSec, "travel", "none", string.Empty, 0f,
                recoilM, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                0f, 0f, null, null));
            return template.WithPhases(phases);
        }
    }
}
