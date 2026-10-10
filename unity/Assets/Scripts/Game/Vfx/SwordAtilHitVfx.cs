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
                feel?.HitSpark(targetPos + Vector3.up * 1.1f, tint, crit: true);
            }
            else
                feel?.HitSpark(targetPos + Vector3.up * 1.0f, tint, crit: false);
        }

        static void SpawnSlashArc(Transform parent, Vector3 pos, Vector3 dir, Color tint)
        {
            var go = new GameObject("SwordSlashArc");
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = pos + Vector3.up * 1.05f;
            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

            var line = go.AddComponent<LineRenderer>();
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = RuleVfxDefaults.SlashWidthM;
            line.positionCount = 10;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            var mat = new Material(sh);
            Color hdr = tint * 2.5f;
            hdr.a = 1f;
            if (mat.HasProperty("_CoreColor"))
                mat.SetColor("_CoreColor", hdr);
            if (mat.HasProperty("_Intensity"))
                mat.SetFloat("_Intensity", 2.5f);
            line.sharedMaterial = mat;
            float r = RuleVfxDefaults.SlashRadiusM;
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.Lerp(-50f, 50f, i / 9f) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r * 0.35f, 0.05f));
            }

            line.startColor = hdr;
            line.endColor = hdr;
            Object.Destroy(go, VfxPlanDefaults.SlashArcSec + 0.05f);
            Object.Destroy(mat, VfxPlanDefaults.SlashArcSec + 0.1f);
        }

        static void SpawnClawMarks(Transform parent, Vector3 pos, Vector3 dir, Color tint)
        {
            Vector3 flat = dir;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.forward;
            flat.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, flat).normalized;
            Color hdr = tint * 2.8f;

            for (int i = 0; i < VfxPlanDefaults.ZararClawCount; i++)
            {
                float offset = (i - 1) * RuleVfxDefaults.ClawGapM;
                var go = new GameObject("ZararClawMark_" + i);
                if (parent != null)
                    go.transform.SetParent(parent, false);
                Vector3 p = pos + Vector3.up * (1.0f + i * 0.05f) + side * offset;
                go.transform.position = p;
                go.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);

                var line = go.AddComponent<LineRenderer>();
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.useWorldSpace = true;
                line.widthMultiplier = 0.05f;
                line.positionCount = 2;
                Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                    ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
                var mat = new Material(sh);
                if (mat.HasProperty("_CoreColor"))
                    mat.SetColor("_CoreColor", hdr);
                if (mat.HasProperty("_Intensity"))
                    mat.SetFloat("_Intensity", 3f);
                line.sharedMaterial = mat;
                Vector3 a = p - flat * RuleVfxDefaults.ClawLengthM * 0.35f + Vector3.up * 0.25f;
                Vector3 b = p + flat * RuleVfxDefaults.ClawLengthM * 0.65f - Vector3.up * 0.15f;
                // Pençe eğimi.
                a += side * (-0.05f * (i - 1));
                b += side * (0.08f * (i - 1));
                line.SetPosition(0, a);
                line.SetPosition(1, b);
                line.startColor = hdr;
                line.endColor = new Color(hdr.r, hdr.g, hdr.b, 0.2f);
                Object.Destroy(go, VfxPlanDefaults.ZararClawSec + 0.05f);
                Object.Destroy(mat, VfxPlanDefaults.ZararClawSec + 0.1f);
            }
        }
    }
}
