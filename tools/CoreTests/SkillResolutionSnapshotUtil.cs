using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Dovus.Core.Grammar;

namespace CoreTests;

/// <summary>PLAN 2B.14b: 144 kombo çözümünün dönüşüm öncesi string alan anlık görüntüsü.</summary>
static class SkillResolutionSnapshotUtil
{
    public static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public static string SnapshotPath() =>
        Path.Combine(RepoRoot(), "tools", "CoreTests", "Data", "skill-resolution-snapshot.tsv");

    public static SkillMotor LoadMotor()
    {
        string path = Path.Combine(RepoRoot(), "docs", "element-sistemi.json");
        if (!File.Exists(path))
            path = Path.Combine(RepoRoot(), "unity", "Assets", "Resources", "ElementSystem", "element-sistemi.json");
        return SkillMotor.FromJson(File.ReadAllText(path));
    }

    public static IReadOnlyList<string> CaptureLines(in SkillResolution skill)
    {
        var lines = new List<string>
        {
            "element_id\t" + skill.Identity.Element,
            "element_name\t" + skill.Identity.ElementName,
            "display_name\t" + skill.Identity.DisplayName,
            "skill_id\t" + skill.Identity.Id,
            "skill_job\t" + skill.Identity.SkillJob,
            "verb_id\t" + skill.Identity.Verb,
            "verb_name\t" + skill.Identity.VerbName,
            "verb_family\t" + skill.Presentation.VerbFamily,
            "action\t" + skill.Presentation.Action,
            "hitbox\t" + skill.Presentation.Hitbox,
            "cast_mobility\t" + skill.Targeting.CastMobility,
            "mechanics\t" + string.Join("|", skill.Mechanics ?? Array.Empty<string>()),
            "adjective_id\t" + skill.Identity.Adjective,
            "adjective_name\t" + skill.Identity.AdjectiveName,
            "silhouette_axis\t" + skill.Presentation.SilhouetteAxis,
            "damage_mult\t" + skill.Scaling.DamageMult.ToString(CultureInfo.InvariantCulture),
            "hitbox_scale_mult\t" + skill.Scaling.HitboxScaleMult.ToString(CultureInfo.InvariantCulture),
            "poise_damage_mult\t" + skill.Scaling.PoiseDamageMult.ToString(CultureInfo.InvariantCulture),
            "length\t" + skill.Length.RuneCount.ToString(CultureInfo.InvariantCulture),
            "length_role\t" + skill.Length.Role,
            "length_cast_mult\t" + skill.Length.CastMult.ToString(CultureInfo.InvariantCulture),
            "length_mobility\t" + skill.Length.Mobility,
            "length_resource_cost_mult\t" + skill.Length.ResourceCostMult.ToString(CultureInfo.InvariantCulture),
            "flavor_element\t" + skill.Identity.FlavorElement,
            "animation_type\t" + skill.Presentation.AnimationType,
            "target_mode\t" + skill.Targeting.Mode,
            "base_cooldown_sec\t" + skill.Costs.BaseCooldownSec.ToString(CultureInfo.InvariantCulture),
            "base_resource_cost\t" + skill.Costs.BaseResourceCost.ToString(CultureInfo.InvariantCulture),
            "base_damage\t" + skill.Combat.BaseDamage.ToString(CultureInfo.InvariantCulture),
            "base_poise\t" + skill.Combat.BasePoise.ToString(CultureInfo.InvariantCulture),
            "base_heal\t" + skill.Combat.BaseHeal.ToString(CultureInfo.InvariantCulture),
            "crit_eligible\t" + skill.Combat.CritEligible.ToString(CultureInfo.InvariantCulture),
            "element_origin\t" + skill.Identity.ElementOrigin,
            "damage_type\t" + skill.Combat.DamageType,
            "is_complete\t" + skill.IsComplete.ToString(CultureInfo.InvariantCulture),
            "passive_description\t" + skill.Prose.PassiveDescription,
            "prose_feel\t" + skill.Prose.Feel,
            "prose_visual\t" + skill.Prose.Visual,
        };
        if (skill.Targeting.Behaviors != null)
        {
            foreach (KeyValuePair<string, string> kv in skill.Targeting.Behaviors.OrderBy(k => k.Key, StringComparer.Ordinal))
                lines.Add("target_behavior\t" + kv.Key + "\t" + kv.Value);
        }
        return lines;
    }

    public static void WriteSnapshotFile(string path, SkillMotor motor)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sb = new StringBuilder();
        for (int verb = 1; verb <= 12; verb++)
        for (int adj = 1; adj <= 12; adj++)
        {
            SkillResolution skill = motor.Resolve(new[] { verb, adj });
            sb.AppendLine($"combo\t{verb}\t{adj}");
            foreach (string line in CaptureLines(skill))
                sb.AppendLine(line);
            sb.AppendLine("---");
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static string FlattenForAssert(in SkillResolution skill) =>
        string.Join("\n", CaptureLines(skill));
}
