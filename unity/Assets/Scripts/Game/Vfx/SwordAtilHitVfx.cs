using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.1/§4 isabet: hedefte kesik yayı; Zarar ile üç paralel pençe kesiği
    /// + pençe biçimli kıvılcım patlaması.
    /// </summary>
    public static class SwordAtilHitVfx
    {
        public static void Spawn(
            Transform parent,
            Vector3 targetPos,
            Vector3 dashDir,
            in VfxPlan plan,
            FeelVfxRuntime feel)
        {
            Color tint = new Color(plan.CoreColor.R, plan.CoreColor.G, plan.CoreColor.B, 1f);
            if (plan.SlashArcOnHit)
                SpawnSlashArc(parent, targetPos, dashDir, tint);

            if (plan.ZararClawMarksOnHit)
            {
                SpawnClawMarks(parent, targetPos, dashDir, tint);
                feel?.HitSpark(targetPos + Vector3.up * RuleVfxArtDefaults.HitHeightSparkM, tint, crit: true);
            }
            else
                feel?.HitSpark(targetPos + Vector3.up * RuleVfxArtDefaults.HitHeightClawM, tint, crit: false);
        }

        static void SpawnSlashArc(Transform parent, Vector3 pos, Vector3 dir, Color tint)
        {
            var go = new GameObject("SwordSlashArc");
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = pos + Vector3.up * RuleVfxArtDefaults.HitHeightM;
            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                flat = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

            var line = go.AddComponent<LineRenderer>();
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = RuleVfxDefaults.SlashWidthM;
            line.positionCount = RuleVfxArtDefaults.SlashArcSegments;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            var mat = new Material(sh);
            Color hdr = tint * RuleVfxArtDefaults.CoreHdrHot;
            hdr.a = 1f;
            if (mat.HasProperty("_CoreColor"))
                mat.SetColor("_CoreColor", hdr);
            if (mat.HasProperty("_Intensity"))
                mat.SetFloat("_Intensity", RuleVfxArtDefaults.IntensityHit);
            line.sharedMaterial = mat;
            float r = RuleVfxDefaults.SlashRadiusM;
            for (int i = 0; i < RuleVfxArtDefaults.SlashArcSegments; i++)
            {
                float a = Mathf.Lerp(
                        -RuleVfxArtDefaults.SlashArcDeg,
                        RuleVfxArtDefaults.SlashArcDeg,
                        i / RuleVfxArtDefaults.SlashArcLastIndex)
                    * Mathf.Deg2Rad;
                line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Sin(a) * r,
                        Mathf.Cos(a) * r * RuleVfxArtDefaults.SlashArcYScale,
                        RuleVfxArtDefaults.SlashArcZ));
            }

            line.startColor = hdr;
            line.endColor = hdr;
            Object.Destroy(go, VfxPlanDefaults.SlashArcSec + RuleVfxDefaults.SlashArcLifePadSec);
            Object.Destroy(mat, VfxPlanDefaults.SlashArcSec + RuleVfxDefaults.ClawMatDestroyPadSec);
        }

        static void SpawnClawMarks(Transform parent, Vector3 pos, Vector3 dir, Color tint)
        {
            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                flat = Vector3.forward;
            flat.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, flat).normalized;
            Color hdr = tint * RuleVfxArtDefaults.CoreHdrHit;

            for (int i = 0; i < VfxPlanDefaults.ZararClawCount; i++)
            {
                float offset = (i - 1) * RuleVfxDefaults.ClawGapM;
                var go = new GameObject("ZararClawMark_" + i);
                if (parent != null)
                    go.transform.SetParent(parent, false);
                Vector3 p = pos
                    + Vector3.up * (RuleVfxArtDefaults.HitHeightClawM + i * RuleVfxArtDefaults.ClawRowLift)
                    + side * offset;
                go.transform.position = p;
                go.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);

                var line = go.AddComponent<LineRenderer>();
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.useWorldSpace = true;
                line.widthMultiplier = RuleVfxArtDefaults.ClawWidthM;
                line.positionCount = 2;
                Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                    ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
                var mat = new Material(sh);
                if (mat.HasProperty("_CoreColor"))
                    mat.SetColor("_CoreColor", hdr);
                if (mat.HasProperty("_Intensity"))
                    mat.SetFloat("_Intensity", RuleVfxArtDefaults.IntensityClaw);
                line.sharedMaterial = mat;
                Vector3 a = p
                    - flat * RuleVfxDefaults.ClawLengthM * RuleVfxArtDefaults.ClawStartFrac
                    + Vector3.up * RuleVfxArtDefaults.ClawUpStartM;
                Vector3 b = p
                    + flat * RuleVfxDefaults.ClawLengthM * RuleVfxArtDefaults.ClawEndFrac
                    - Vector3.up * RuleVfxArtDefaults.ClawUpEndM;
                a += side * (-RuleVfxArtDefaults.ClawSideStart * (i - 1));
                b += side * (RuleVfxArtDefaults.ClawSideEnd * (i - 1));
                line.SetPosition(0, a);
                line.SetPosition(1, b);
                line.startColor = hdr;
                line.endColor = new Color(hdr.r, hdr.g, hdr.b, RuleVfxArtDefaults.LightningEndAlpha);
                Object.Destroy(go, VfxPlanDefaults.ZararClawSec + RuleVfxDefaults.ClawDestroyPadSec);
                Object.Destroy(mat, VfxPlanDefaults.ZararClawSec + RuleVfxDefaults.ClawMatDestroyPadSec);
            }
        }
    }
}
