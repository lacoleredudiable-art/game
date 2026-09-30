using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public enum PositionStepKind
    {
        None = 0,
        Displace = 1,
        Behind = 2,
        Return = 3
    }

    /// <summary>Gramerin oyuncuyu oynatan bir konum adımı. Mesafe, kalıpta yoksa yedek veridir.</summary>
    public readonly struct GrammarPositionStep
    {
        public GrammarPositionStep(string stat, double amount)
        {
            Stat = stat ?? string.Empty;
            Amount = amount;
        }

        public string Stat { get; }
        public double Amount { get; }
    }

    /// <summary>Bu cast'te kalıbın oynatacağı gövde. Konum adımı yoksa kalıbın kendisidir.</summary>
    public readonly struct PositionPlayback
    {
        public PositionPlayback(bool ownsPosition, MotionTemplate template, bool placeReturnMark)
        {
            OwnsPosition = ownsPosition;
            Template = template;
            PlaceReturnMark = placeReturnMark;
        }

        public bool OwnsPosition { get; }
        public MotionTemplate Template { get; }
        public bool PlaceReturnMark { get; }
    }

    /// <summary>
    /// Kalıp oyuncuyu oynatıyorsa o cast'te konumu yalnız kalıp yönetir.
    /// yer_degistir ve hedefin_arkasina ışınlanmaz; kalıpta eğri ya da arkaya iniş yoksa
    /// gramer mesafesi yedek veridir. isaret_geri_don işareti durur, dönüş kalıbın içinde
    /// kısa bir atılmadır. Kalıp oynatmıyorsa gramer eskisi gibi konum adımını uygular.
    /// </summary>
    public static class PositionOwnership
    {
        public const string Displace = "yer_degistir";
        public const string Behind = "hedefin_arkasina";
        public const string ReturnMark = "isaret_geri_don";

        public static PositionStepKind Kind(string atom, string stat)
        {
            if (!string.Equals(atom, "konum", StringComparison.Ordinal))
                return PositionStepKind.None;
            if (stat == Displace)
                return PositionStepKind.Displace;
            if (stat == Behind)
                return PositionStepKind.Behind;
            if (stat == ReturnMark)
                return PositionStepKind.Return;
            return PositionStepKind.None;
        }

        /// <summary>Uygulanmış kalıbın bir fazı oyuncunun yerini değiştirmek üzere.</summary>
        public static bool MovesPlayer(MotionTemplate template)
        {
            if (template == null || !template.Implemented)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                if (PhaseIntendsMove(template.Phases[i]))
                    return true;
            }
            return false;
        }

        public static bool PhaseIntendsMove(MotionPhase phase)
        {
            if (phase == null)
                return false;
            switch (phase.Motion)
            {
                case "lunge":
                case "dash":
                case "retreat":
                case "sidestep":
                case "hop":
                case "leap":
                case "slam":
                case "pull":
                case "blink":
                case "return":
                    return true;
                case "channel":
                    return phase.DriftM > 0.01f || phase.WalkMps > 0.01f;
                default:
                    return phase.Land == "behind";
            }
        }

        public static void LogSuppressed(string skillId, string stat)
        {
            if (string.IsNullOrEmpty(skillId))
                skillId = "?";
            DesignWarnings.Once(
                "motion.pos." + skillId,
                "Hareket kalıbı konumu yönetiyor; gramer konum adımı atlandı: " + skillId + " " + stat);
        }

        public static PositionPlayback Prepare(
            MotionTemplate template,
            IReadOnlyList<GrammarPositionStep> steps,
            float returnDashSec,
            float fallbackDistanceM)
        {
            if (template == null)
                return new PositionPlayback(false, null, false);
            if (!MovesPlayer(template))
                return new PositionPlayback(false, template, false);

            bool wantDisplace = false;
            bool wantBehind = false;
            bool wantReturn = false;
            double grammarDistance = 0;
            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    PositionStepKind kind = Kind("konum", steps[i].Stat);
                    if (kind == PositionStepKind.Displace)
                        wantDisplace = true;
                    else if (kind == PositionStepKind.Behind)
                        wantBehind = true;
                    else if (kind == PositionStepKind.Return)
                        wantReturn = true;
                    else
                        continue;
                    if (steps[i].Amount > grammarDistance)
                        grammarDistance = steps[i].Amount;
                }
            }

            float fallback = grammarDistance > 0.01
                ? (float)grammarDistance
                : (fallbackDistanceM > 0.01f ? fallbackDistanceM : MotionFallbacks.Coded.StepM);
            float dashSec = returnDashSec > 0.01f ? returnDashSec : MotionFallbacks.Coded.PhaseSec;

            var phases = new List<MotionPhase>(template.Phases.Count + 2);
            bool hasCurve = false;
            bool hasBehind = false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                if (HasTravelCurve(phase))
                    hasCurve = true;
                if (phase.Land == "behind")
                    hasBehind = true;
                phases.Add(phase);
            }

            bool changed = false;
            if ((wantDisplace || wantBehind) && !hasCurve)
            {
                for (int i = 0; i < phases.Count; i++)
                {
                    if (!NeedsDistanceFallback(phases[i]))
                        continue;
                    string land = wantBehind ? "behind" : phases[i].Land;
                    // Silah menzili kalıbın kendi yolunu kısaltamaz. Gramer ancak uzatır.
                    float authored = Math.Max(phases[i].DistanceM, Math.Max(phases[i].ForwardM, phases[i].DriftM));
                    float travel = MotionTravel.Protect(authored, fallback);
                    phases[i] = phases[i].WithTravel(travel, wantBehind ? travel : phases[i].BehindM, land);
                    changed = true;
                    if (land == "behind")
                        hasBehind = true;
                    hasCurve = true;
                    break;
                }
            }

            if (wantBehind && !hasBehind)
            {
                phases.Add(BehindFallback(dashSec, fallback));
                changed = true;
            }

            if (wantReturn && !HasReturn(phases))
            {
                phases.Add(ReturnFallback(dashSec, fallback));
                changed = true;
            }

            MotionTemplate playback = changed ? template.WithPhases(phases) : template;
            return new PositionPlayback(true, playback, wantReturn);
        }

        static bool HasTravelCurve(MotionPhase phase)
        {
            if (phase == null || !PhaseIntendsMove(phase))
                return false;
            if (phase.Land == "behind")
                return true;
            if (phase.DistanceM > 0.01f || phase.ForwardM > 0.01f || phase.DriftM > 0.01f)
                return true;
            if (phase.Motion is "pull" or "blink" or "return")
                return true;
            return phase.Homing == "track"
                && phase.Motion is "lunge" or "dash" or "leap" or "slam" or "hop";
        }

        static bool NeedsDistanceFallback(MotionPhase phase) =>
            PhaseIntendsMove(phase) && !HasTravelCurve(phase);

        static bool HasReturn(List<MotionPhase> phases)
        {
            for (int i = 0; i < phases.Count; i++)
            {
                if (phases[i].Motion == "return")
                    return true;
            }
            return false;
        }

        static MotionPhase BehindFallback(float sec, float distanceM) =>
            new MotionPhase(
                "arkaya", "hop", sec, "target", "track", string.Empty, 0f,
                distanceM, 0f, 1f, 0f, 0.4f, 0f, 0f, 0f, 0f, 0f, 0f,
                distanceM, 0f, null, null, "behind", false);

        static MotionPhase ReturnFallback(float sec, float distanceM) =>
            new MotionPhase(
                "geri_don", "return", sec, "travel", "none", string.Empty, 0f,
                distanceM, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                0f, 0f, null, null);
    }

    /// <summary>Silah çarpanı hareket kalıbının yazılmış mesafesini kısaltamaz.</summary>
    public static class MotionTravel
    {
        public static float Protect(float authoredM, float weaponScaledM)
        {
            float authored = Math.Max(0f, authoredM);
            float scaled = Math.Max(0f, weaponScaledM);
            return Math.Max(authored, scaled);
        }
    }
}
