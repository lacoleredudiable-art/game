using Dovus.Core.Grammar;
using Dovus.Game.Cameras;
using Dovus.Game.Config;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Feel
{
    /// <summary>
    /// Element rengi (çizgi) + aileye göre boss tepki/kamera.
    /// Mevcut Ateş rengi sıcak magenta.
    /// </summary>
    public static class SkillFeel
    {
        /// <summary>Fiil rünü çizgi, sıfat/son rün blob — basit çizgiler element renginde.</summary>
        public static void ElementPalette(
            IReadOnlyList<SentenceWord> words,
            PrototypeTuning t,
            out Color line,
            out Color blob)
        {
            Color cyan = t != null ? t.InkCyan : new Color(0.373f, 0.941f, 1f);
            Color purple = t != null ? t.InkPurple : new Color(0.725f, 0.549f, 1f);

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
            return string.Join(" · ", mechanics);
        }

        /// <summary>
        /// Sıfat katkısı — HUD “etki” satırında 3’lü/4’lü farkı görünsün.
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
                parts.Add("alan×" + skill.HitboxScaleMult.ToString("0.#"));
            if (!skill.EngineModifiers.IsNull)
            {
                string traj = skill.EngineModifiers["trajectory_override"].AsString();
                if (string.IsNullOrEmpty(traj))
                    traj = skill.EngineModifiers["hitbox_override"].AsString();
                if (!string.IsNullOrEmpty(traj))
                    parts.Add(traj);
            }
            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }

        /// <summary>Kapanışta kamera vuruşu — aileye göre ağırlık.</summary>
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
