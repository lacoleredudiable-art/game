using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Cameras;
using Dovus.Game.Config;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Feel
{
    /// <summary>
    /// Element rengi (Ã§izgi) + aileye gÃ¶re boss tepki/kamera.
    /// Mevcut AteÅŸ rengi sÄ±cak magenta.
    /// </summary>
    public static class SkillFeel
    {
        /// <summary>Fiil rÃ¼nÃ¼ Ã§izgi, sÄ±fat/son rÃ¼n blob â€” basit Ã§izgiler element renginde.</summary>
        public static void ElementPalette(
            IReadOnlyList<SentenceWord> words,
            PrototypeTuning t,
            out Color line,
            out Color blob)
        {
            Color cyan = t != null ? t.Visuals.InkCyan : new Color(0.373f, 0.941f, 1f);
            Color purple = t != null ? t.Visuals.InkPurple : new Color(0.725f, 0.549f, 1f);

            if (words == null || words.Count == 0 || t == null)
            {
                line = cyan;
                blob = purple;
                return;
            }

            line = t.ColorForRune(words[0].Rune);
            if (words.Count >= 2)
                blob = t.ColorForRune(words[words.Count - 1].Rune);
            else
                blob = Color.Lerp(line, Color.white, 0.35f);
        }

        public static string MechanicShort(string[] mechanics)
        {
            if (mechanics == null || mechanics.Length == 0)
                return string.Empty;
            return string.Join(" Â· ", mechanics);
        }

        /// <summary>
        /// SÄ±fat katkÄ±sÄ± â€” HUD â€œetkiâ€ satÄ±rÄ±nda 3â€™lÃ¼/4â€™lÃ¼ farkÄ± gÃ¶rÃ¼nsÃ¼n.
        /// </summary>
        public static string AdjectiveShort(SkillResolution skill)
        {
            if (skill.IsEmpty)
                return string.Empty;
            var parts = new List<string>(4);
            if (!string.IsNullOrEmpty(skill.AdjectiveName))
                parts.Add(skill.AdjectiveName);
            if (!string.IsNullOrEmpty(skill.SilhouetteAxis)
                && !string.Equals(skill.SilhouetteAxis, "none", System.StringComparison.Ordinal))
                parts.Add(skill.SilhouetteAxis);
            if (skill.HitboxScaleMult > 0f && System.Math.Abs(skill.HitboxScaleMult - 1f) > 0.05f)
                parts.Add("alanÃ—" + skill.HitboxScaleMult.ToString("0.#"));
            if (!skill.Engine.IsNull)
            {
                string traj = skill.Engine.TrajectoryOverride("");
                if (string.IsNullOrEmpty(traj))
                    traj = skill.Engine.HitboxOverride("");
                if (!string.IsNullOrEmpty(traj))
                    parts.Add(traj);
            }
            return parts.Count == 0 ? string.Empty : string.Join(" Â· ", parts);
        }

        /// <summary>KapanÄ±ÅŸta kamera vuruÅŸu â€” aileye gÃ¶re aÄŸÄ±rlÄ±k.</summary>
        public static void CameraKick(string verbFamily, FollowCamera cam, Dovus.Core.Tuning.FeelTuning feel)
        {
            if (cam == null || feel == null)
                return;
            float kick;
            float shakePx;
            switch (verbFamily)
            {
                case "strike":
                    kick = feel.SkillKickStrike;
                    shakePx = feel.SkillShakeStrikePx;
                    break;
                case "disrupt":
                    kick = feel.SkillKickDisrupt;
                    shakePx = feel.SkillShakeDisruptPx;
                    break;
                case "control":
                case "guard":
                    kick = feel.SkillKickControl;
                    shakePx = feel.SkillShakeControlPx;
                    break;
                case "zone":
                    kick = feel.SkillKickZone;
                    shakePx = feel.SkillShakeZonePx;
                    break;
                case "motion":
                    kick = feel.SkillKickMotion;
                    shakePx = feel.SkillShakeMotionPx;
                    break;
                default:
                    kick = feel.SkillKickDefault;
                    shakePx = feel.SkillShakeDefaultPx;
                    break;
            }

            cam.Punch(kick, feel.SkillKickRollDeg, shakePx, feel.SkillKickDecay);
        }
    }
}
