using System;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;

namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// SkillResolution → LivingEffectPlan. Prezentasyon kataloğu varsa trajectory/hitbox
    /// metre-saniye değerlerini oradan alır; yoksa ManifestationTuning yedek.
    /// </summary>
    public static class SkillWorldPlanner
    {
        public static LivingEffectPlan Build(
            in SkillResolution skill,
            PresentationCatalog? catalog,
            ManifestationTuning? tuning = null)
        {
            tuning ??= new ManifestationTuning();
            if (skill.IsEmpty)
                return LivingEffectPlan.Empty;

            ResolveIds(skill, catalog, out string trajectoryId, out string hitboxId);
            LivingTravelKind kind = TravelKindFrom(trajectoryId, hitboxId, skill.Hitbox);
            EffectSilhouette silhouette = SilhouetteBuilder.FromSkill(skill, tuning);

            float speed = tuning.WaveSpeedMps;
            float maxRange = tuning.WaveMaxRadiusM;
            float bangRadius = tuning.ClosingBangRadiusM;
            float lifetimeAdd = 0f;

            if (catalog != null && catalog.TryGetTrajectory(trajectoryId, out TrajectoryNode traj))
            {
                float trajSpeed = traj.SpeedMpsDefault;
                if (trajSpeed > 0f)
                    speed = trajSpeed;
                float maxR = traj.GetFloat("max_radius_m", 0f);
                if (maxR > 0f)
                    maxRange = maxR;
                // Anlık / teleport: menzil = bang erişimi kadar kısa tut.
                if (kind is LivingTravelKind.Instant or LivingTravelKind.Static)
                    maxRange = MathF.Max(bangRadius, 0.5f);
            }
            else
            {
                ApplyLegacySpeedRange(kind, skill.Hitbox, tuning, ref speed, ref maxRange);
            }

            if (catalog != null && catalog.TryGetHitbox(hitboxId, out HitboxNode hb))
            {
                float r = hb.GetFloat("radius_m_default", 0f);
                if (r <= 0f)
                    r = hb.GetFloat("range_m_default", 0f);
                if (r > 0f)
                    bangRadius = r;
                float width = hb.GetFloat("width_m_default", 0f);
                if (r <= 0f && width > 0f)
                    bangRadius = MathF.Max(width * 4f, tuning.TravelHitRadiusM);
            }

            float scale = skill.HitboxScaleMult > 0f ? skill.HitboxScaleMult : 1f;
            bangRadius *= scale;

            // Yayılma sıfatı: menzili de biraz aç (expanding_wave max_radius ile uyumlu).
            if (kind == LivingTravelKind.ExpandingRadial && scale > 1f)
                maxRange = MathF.Max(maxRange, bangRadius * 1.25f);

            if (!skill.EngineModifiers.IsNull && skill.EngineModifiers.Has("lifetime_add"))
                lifetimeAdd = skill.EngineModifiers["lifetime_add"].AsFloat(0f);

            return new LivingEffectPlan(
                silhouette,
                kind,
                speed,
                maxRange,
                bangRadius,
                lifetimeAdd,
                trajectoryId,
                hitboxId,
                hasPlan: true);
        }

        /// <summary>
        /// Fiil base_hitbox + sıfat override. expanding_wave hitbox diye yazılmışsa
        /// (yayma veri hatası) trajectory’ye taşınır.
        /// </summary>
        public static void ResolveIds(
            in SkillResolution skill,
            PresentationCatalog? catalog,
            out string trajectoryId,
            out string hitboxId)
        {
            hitboxId = skill.Hitbox ?? string.Empty;
            trajectoryId = DefaultTrajectoryForHitbox(hitboxId);

            JsonValue mods = skill.EngineModifiers;
            if (!mods.IsNull)
            {
                string trajOver = mods["trajectory_override"].AsString();
                if (!string.IsNullOrEmpty(trajOver))
                    trajectoryId = trajOver;

                string hbOver = mods["hitbox_override"].AsString();
                if (!string.IsNullOrEmpty(hbOver))
                {
                    // Veri shim: expanding_wave / radial_burst bazen hitbox_override’da.
                    if (catalog != null && catalog.TryGetTrajectory(hbOver, out _)
                        && !catalog.TryGetHitbox(hbOver, out _))
                    {
                        trajectoryId = hbOver;
                    }
                    else
                    {
                        hitboxId = hbOver;
                    }
                }
            }

            if (string.IsNullOrEmpty(hitboxId))
                hitboxId = "projectile";
            if (string.IsNullOrEmpty(trajectoryId))
                trajectoryId = "duz";
        }

        static string DefaultTrajectoryForHitbox(string hitbox)
        {
            return hitbox switch
            {
                "projectile" or "chain_projectile" or "beam" or "raycast" => "duz",
                "cone" => "radial_burst",
                "ground_ring" or "ground_circle" or "ground_line" or "ground_surface"
                    or "static_cloud" or "wall" or "trail" => "static_anchor",
                "self" or "self_aura" or "single_target" or "target_ally" => "instant_hit",
                "radial_burst" => "expanding_wave",
                _ => "duz"
            };
        }

        static LivingTravelKind TravelKindFrom(string trajectoryId, string hitboxId, string verbHitbox)
        {
            if (trajectoryId is "expanding_wave" or "radial_burst")
                return LivingTravelKind.ExpandingRadial;
            if (trajectoryId is "instant_hit" or "raycast" or "teleport_to_target"
                or "self_teleport" or "stealth_shift")
                return LivingTravelKind.Instant;
            if (trajectoryId is "static_anchor" or "pull_to_caster" or "pull_to_center")
                return LivingTravelKind.Static;
            if (hitboxId is "self" or "self_aura" or "target_ally"
                || verbHitbox is "self" or "self_aura" or "target_ally")
                return LivingTravelKind.Instant;
            return LivingTravelKind.Linear;
        }

        static void ApplyLegacySpeedRange(
            LivingTravelKind kind,
            string hitbox,
            ManifestationTuning tuning,
            ref float speed,
            ref float maxRange)
        {
            switch (kind)
            {
                case LivingTravelKind.Instant:
                case LivingTravelKind.Static:
                    speed = tuning.WaveSpeedMps * 4f;
                    maxRange = MathF.Max(tuning.ClosingBangRadiusM, 1f);
                    break;
                case LivingTravelKind.ExpandingRadial:
                    speed = tuning.WaveSpeedMps * 1.5f;
                    maxRange = tuning.WaveMaxRadiusM;
                    break;
                default:
                    if (hitbox is "projectile" or "chain_projectile" or "beam")
                    {
                        speed = tuning.NeedleSpeedMps;
                        maxRange = tuning.NeedleMaxRangeM;
                    }
                    else if (hitbox is "static_cloud" or "ground_circle" or "ground_ring")
                    {
                        speed = tuning.SwarmSpeedMps;
                        maxRange = tuning.SwarmMaxRadiusM;
                    }
                    break;
            }
        }
    }
}
