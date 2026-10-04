using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace CoreTests;

public partial class MotionTemplateTests
{

    [Test]
    public void EveryTemplate_HitsTheStandingTarget_AndNeverEndsInside()
    {
        string elements = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "element-sistemi.json"));
        var grammar = new Dovus.Core.Mechanic.MechanicGrammar(Dovus.Core.Mechanic.MechanicRules.FromJson(elements));
        Assert.That(grammar.Rules.IsValid, Is.True);

        const float body = 0.5f;
        const float bossR = 0.85f;
        const float stop = 0.15f;
        var failures = new List<string>();

        for (int i = 0; i < _catalog.Templates.Count; i++)
        {
            MotionTemplate template = _catalog.Templates[i];
            MotionTarget target = StandingTarget(template, bossR);
            CastSample cast = SampleCast(template, target, body, stop);

            float endGap = MotionHitGeometry.EdgeGap(cast.X, cast.Z, body, target.X, target.Z, bossR);
            if (endGap <= 0.04f)
                failures.Add(template.Id + " bitiş içinde gap=" + endGap.ToString("0.00"));

            bool crosses = false;
            bool behind = false;
            bool overshoot = false;
            bool returns = false;
            bool retreats = false;
            for (int p = 0; p < template.Phases.Count; p++)
            {
                MotionPhase phase = template.Phases[p];
                if (phase.Land == "behind" || phase.OvershootM > 0.01f)
                    crosses = true;
                if (phase.Land == "behind")
                    behind = true;
                if (phase.OvershootM > 0.01f)
                    overshoot = true;
                if (phase.Motion == "return")
                    returns = true;
                if (phase.Motion == "retreat")
                    retreats = true;
            }

            if (!crosses)
            {
                for (int s = 0; s < cast.Gaps.Count; s++)
                {
                    if (cast.Gaps[s] <= 0.04f)
                    {
                        failures.Add(template.Id + " kare " + s + " içinde");
                        break;
                    }
                }
            }

            if (behind && cast.PeakZ <= target.Z)
                failures.Add(template.Id + " arkaya inmeli peak=" + cast.PeakZ.ToString("0.00"));

            if (overshoot && cast.Z <= target.Z)
                failures.Add(template.Id + " öte kenarda bitmeli z=" + cast.Z.ToString("0.00"));

            if (returns)
            {
                if (cast.PeakZ <= 1.2f)
                    failures.Add(template.Id + " işaretten uzaklaşmalı");
                if (MathF.Abs(cast.Z) > 0.2f)
                    failures.Add(template.Id + " işarete dönmeli z=" + cast.Z.ToString("0.00"));
            }
            else if (retreats && !behind && cast.Z >= -0.4f)
            {
                failures.Add(template.Id + " geri çekilmeli z=" + cast.Z.ToString("0.00"));
            }

            if (template.Id == "suzulme" && cast.PeakY < 0.6f)
                failures.Add("suzulme yerden kalkmadı");

            for (int h = 0; h < cast.Hits.Count; h++)
            {
                MotionHit hit = cast.Hits[h];
                if (hit.Payload is "none" or "marker")
                    continue;
                if (hit.Anchor == "side")
                {
                    if (MathF.Abs(hit.OriginX) <= 0.5f)
                        failures.Add(template.Id + " yan " + hit.Phase + " x=" + hit.OriginX.ToString("0.00"));
                    continue;
                }

                if (hit.Anchor is "self")
                    continue;
                if (hit.Anchor is not ("forward" or "shot" or "target" or "behind" or "target_side" or "plant" or "ring"))
                    continue;

                bool overlaps = MotionHitGeometry.Overlaps(
                    hit.OriginX, hit.OriginZ, hit.DirX, hit.DirZ,
                    hit.LengthM, hit.RadiusM, hit.Anchor,
                    target.X, target.Z, bossR);
                if (!overlaps)
                    failures.Add(template.Id + " " + hit.Phase + " " + hit.Anchor
                        + " hedefz=" + target.Z.ToString("0.00"));
            }
        }

        Assert.That(failures.Count, Is.EqualTo(0), string.Join(" | ", failures));

        for (int verb = 1; verb <= 12; verb++)
        for (int adjective = 1; adjective <= 12; adjective++)
        {
            string id = verb + "-" + adjective;
            Assert.That(_catalog.TryGet(id, out MotionBinding binding), Is.True, id);
            var plan = grammar.Compose(verb, adjective, 1);
            bool grammarMoves = false;
            foreach (var effect in plan.Effects)
            {
                if (PositionOwnership.Kind(effect.Atom, effect.Stat) != PositionStepKind.None)
                    grammarMoves = true;
            }
            bool templateMoves = PositionOwnership.MovesPlayer(binding.Template);
            Assert.That(
                PositionOwnershipOracle.PositionWriters(templateMoves, grammarMoves),
                Is.LessThanOrEqualTo(1),
                id);
        }
    }

    static MotionTarget StandingTarget(MotionTemplate template, float bossR)
    {
        float shot = 0f;
        float ring = 0f;
        bool aimed = false;
        bool behind = false;
        bool overshoot = false;
        bool returns = false;
        bool retreats = false;
        for (int i = 0; i < template.Phases.Count; i++)
        {
            MotionPhase phase = template.Phases[i];
            if (phase.Land == "behind")
                behind = true;
            if (phase.OvershootM > 0.01f)
                overshoot = true;
            if (phase.Motion == "return")
                returns = true;
            if (phase.Motion == "retreat")
                retreats = true;
            MotionHitSpec hit = phase.Hit;
            if (hit == null || hit.Payload is "none" or "marker")
                continue;
            if (hit.Anchor == "shot")
                shot = MathF.Max(shot, phase.ShotM * Math.Clamp(hit.At, 0.05f, 1f));
            if (hit.Anchor == "ring")
                ring = MathF.Max(ring, hit.RadiusM);
            if (hit.Anchor is "forward" or "target" or "behind" or "target_side" or "plant")
                aimed = true;
        }

        float z;
        if (returns)
            z = 4f;
        else if (shot > 2.2f && retreats)
            z = MathF.Max(1.9f, shot - 0.6f);
        else if (shot > 2.2f)
            z = shot;
        else if (behind || overshoot)
            z = 3.2f;
        else if (ring > 0.4f && !aimed)
            z = Math.Clamp(ring + bossR - 0.25f, 1.75f, 3f);
        else
            z = 1.9f;
        return new MotionTarget(true, 0f, z, bossR);
    }
}
