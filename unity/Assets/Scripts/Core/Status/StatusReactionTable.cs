using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Grammar;

namespace Dovus.Core.Status
{
    /// <summary>Bir reaksiyon kuralının hangi tarafı(nı) değiştirdiği.</summary>
    public enum ReactionTarget
    {
        A,
        B,
        Both
    }

    /// <summary>
    /// İki status aynı hedefte aynı anda varken ne olur.
    /// Kaynak: docs/element-sistemi.json status_interaction_table → SkillMotor.StatusInteractions.
    /// Elle kural listesi YOK — Rebuild ile motor listesinden türetilir.
    /// Üç kombinasyon tabloda yok (StatusBoard/StatusApplicator özel): burn+poison (ekstra tick),
    /// shield+burn (kalkan aşınması), stun+knockback (Knockback kalıcı status değil).
    /// </summary>
    public readonly struct StatusReactionRule
    {
        public StatusReactionRule(
            StatusKind a, StatusKind b, string name, string readAs, ReactionTarget target,
            float magnitudeMult = 1f, float? magnitudeSet = null,
            float durationMult = 1f, double durationAddMs = 0)
        {
            A = a;
            B = b;
            Name = name;
            ReadAs = readAs;
            Target = target;
            MagnitudeMult = magnitudeMult;
            MagnitudeSet = magnitudeSet;
            DurationMult = durationMult;
            DurationAddMs = durationAddMs;
        }

        public StatusKind A { get; }
        public StatusKind B { get; }
        public string Name { get; }
        public string ReadAs { get; }
        public ReactionTarget Target { get; }
        public float MagnitudeMult { get; }
        public float? MagnitudeSet { get; }
        public float DurationMult { get; }
        public double DurationAddMs { get; }
    }

    public static class StatusReactionTable
    {
        static StatusReactionRule[] Rules = Array.Empty<StatusReactionRule>();
        static Dictionary<(StatusKind, StatusKind), int> Index =
            new Dictionary<(StatusKind, StatusKind), int>();

        /// <summary>
        /// motor.StatusInteractions → genellenebilir magnitude/süre kuralları.
        /// Eşleşmeyen StatusKind id veya özel mekanik (tick/kalkan/knockback) sessizce atlanır.
        /// </summary>
        public static void Rebuild(IReadOnlyList<StatusInteractionNode> interactions)
        {
            var list = new List<StatusReactionRule>();
            if (interactions != null)
            {
                for (int i = 0; i < interactions.Count; i++)
                {
                    if (TryConvert(interactions[i], out StatusReactionRule rule))
                        list.Add(rule);
                }
            }

            Rules = list.ToArray();
            Index = BuildIndex(Rules);
        }

        static Dictionary<(StatusKind, StatusKind), int> BuildIndex(StatusReactionRule[] rules)
        {
            var map = new Dictionary<(StatusKind, StatusKind), int>();
            for (int i = 0; i < rules.Length; i++)
                map[(rules[i].A, rules[i].B)] = i;
            return map;
        }

        static bool TryConvert(StatusInteractionNode node, out StatusReactionRule rule)
        {
            rule = default;
            if (!StatusKindUtil.TryParse(node.A, out StatusKind kindA) || kindA == StatusKind.None)
                return false;
            if (!StatusKindUtil.TryParse(node.B, out StatusKind kindB) || kindB == StatusKind.None)
                return false;

            // Knockback kalıcı status değil — StatusApplicator özel dalı.
            if (kindA == StatusKind.Knockback || kindB == StatusKind.Knockback)
                return false;

            if (!TryParseEffect(
                    node.Effect, kindA, kindB, node.A, node.B,
                    out ReactionTarget target,
                    out float magMult, out float? magSet,
                    out float durMult, out double durAdd))
                return false;

            bool identity = magSet == null
                && Math.Abs(magMult - 1f) < 0.0001f
                && Math.Abs(durMult - 1f) < 0.0001f
                && Math.Abs(durAdd) < 0.0001;
            if (identity)
                return false;

            rule = new StatusReactionRule(
                kindA, kindB, node.Name, node.ReadAs, target,
                magnitudeMult: magMult, magnitudeSet: magSet,
                durationMult: durMult, durationAddMs: durAdd);
            return true;
        }

        /// <summary>
        /// effect metnindeki sayıları StatusReactionRule alanlarına çevirir.
        /// Yeni kural icat etmez — yalnızca JSON effect string'ini okur.
        /// </summary>
        static bool TryParseEffect(
            string effect, StatusKind kindA, StatusKind kindB, string idA, string idB,
            out ReactionTarget target,
            out float magnitudeMult, out float? magnitudeSet,
            out float durationMult, out double durationAddMs)
        {
            magnitudeMult = 1f;
            magnitudeSet = null;
            durationMult = 1f;
            durationAddMs = 0;
            target = ReactionTarget.A;

            if (string.IsNullOrWhiteSpace(effect))
                return false;

            string e = effect;

            Match setMatch = Regex.Match(e, @"→\s*(\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);
            if (!setMatch.Success)
                setMatch = Regex.Match(e, @"->\s*(\d+(?:\.\d+)?)", RegexOptions.CultureInvariant);
            if (setMatch.Success &&
                float.TryParse(setMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float setVal))
            {
                magnitudeSet = setVal;
                target = ResolveTarget(e, idA, idB, preferA: true);
                return true;
            }

            Match pct = Regex.Match(e, @"%(\d+)\s*azal", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (pct.Success &&
                int.TryParse(pct.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pctVal))
            {
                magnitudeMult = 1f - pctVal / 100f;
                target = ResolveTarget(e, idA, idB, preferA: true);
                return true;
            }

            Match add = Regex.Match(e, @"\+(\d+)\s*sn", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (add.Success &&
                int.TryParse(add.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sec))
                durationAddMs = sec * 1000.0;

            // "süresi 1.5x" / "süre 1.5x" — "süresince" YOK
            Match dur = Regex.Match(
                e, @"süre(?:si)?\s+(\d+(?:\.\d+)?)x",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            bool hasExplicitDurationMult = false;
            if (dur.Success &&
                float.TryParse(dur.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float dMult))
            {
                durationMult = dMult;
                hasExplicitDurationMult = true;
            }

            float? foundMag = null;
            foreach (Match m in Regex.Matches(e, @"(\d+(?:\.\d+)?)x", RegexOptions.CultureInvariant))
            {
                if (hasExplicitDurationMult && dur.Success &&
                    m.Index >= dur.Index && m.Index < dur.Index + dur.Length)
                    continue;
                if (float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float mx))
                {
                    foundMag = mx;
                    break;
                }
            }

            bool both = ContainsBothMarker(e);
            if (foundMag.HasValue && !hasExplicitDurationMult && Math.Abs(durationAddMs) < 0.0001)
            {
                if (both)
                {
                    if (IsCc(kindA) && IsCc(kindB))
                        durationMult = foundMag.Value;
                    else
                        magnitudeMult = foundMag.Value;
                }
                else
                {
                    StatusKind mentioned = MentionedKind(e, idA, idB, kindA, kindB);
                    if (mentioned != StatusKind.None && IsCc(mentioned))
                        durationMult = foundMag.Value;
                    else
                        magnitudeMult = foundMag.Value;
                }
            }
            else if (foundMag.HasValue)
            {
                magnitudeMult = foundMag.Value;
            }

            bool changed = magnitudeSet != null
                || Math.Abs(magnitudeMult - 1f) >= 0.0001f
                || Math.Abs(durationMult - 1f) >= 0.0001f
                || Math.Abs(durationAddMs) >= 0.0001;
            if (!changed)
                return false;

            target = ResolveTarget(e, idA, idB, preferA: true);
            return true;
        }

        static bool ContainsBothMarker(string effect) =>
            effect.IndexOf("kisi de", StringComparison.OrdinalIgnoreCase) >= 0;

        static bool IsCc(StatusKind k) =>
            StatusKindUtil.IsHardCc(k) || StatusKindUtil.IsSoftCc(k);

        static StatusKind MentionedKind(string effect, string idA, string idB, StatusKind a, StatusKind b)
        {
            bool hasA = !string.IsNullOrEmpty(idA) &&
                        effect.IndexOf(idA, StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasB = !string.IsNullOrEmpty(idB) &&
                        effect.IndexOf(idB, StringComparison.OrdinalIgnoreCase) >= 0;
            if (hasA && !hasB) return a;
            if (hasB && !hasA) return b;
            return StatusKind.None;
        }

        static ReactionTarget ResolveTarget(string effect, string idA, string idB, bool preferA)
        {
            if (ContainsBothMarker(effect))
                return ReactionTarget.Both;

            bool hasA = !string.IsNullOrEmpty(idA) &&
                        effect.IndexOf(idA, StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasB = !string.IsNullOrEmpty(idB) &&
                        effect.IndexOf(idB, StringComparison.OrdinalIgnoreCase) >= 0;

            if (effect.IndexOf("heal_reduction", StringComparison.OrdinalIgnoreCase) >= 0)
                return ReactionTarget.A;

            if (!hasA && !hasB &&
                effect.IndexOf("tick", StringComparison.OrdinalIgnoreCase) >= 0)
                return ReactionTarget.A;

            if (hasA && !hasB) return ReactionTarget.A;
            if (hasB && !hasA) return ReactionTarget.B;
            if (hasA && hasB)
            {
                int iA = effect.IndexOf(idA, StringComparison.OrdinalIgnoreCase);
                int iB = effect.IndexOf(idB, StringComparison.OrdinalIgnoreCase);
                return iA <= iB ? ReactionTarget.A : ReactionTarget.B;
            }

            return preferA ? ReactionTarget.A : ReactionTarget.B;
        }

        /// <summary>
        /// incoming/other eşleşirse kuralı döner. incomingIsA=true ise incoming==rule.A.
        /// </summary>
        public static bool TryGetRule(
            StatusKind incoming, StatusKind other, out StatusReactionRule rule, out bool incomingIsA)
        {
            if (Index.TryGetValue((incoming, other), out int i))
            {
                rule = Rules[i];
                incomingIsA = true;
                return true;
            }

            if (Index.TryGetValue((other, incoming), out int j))
            {
                rule = Rules[j];
                incomingIsA = false;
                return true;
            }

            rule = default;
            incomingIsA = false;
            return false;
        }

        public static IReadOnlyList<StatusReactionRule> All => Rules;
    }
}
