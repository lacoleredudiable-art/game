using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §5.5 kenar freni: ATIL canlı/duvar kenarında durunca kısa kor kıvılcımı.
    /// </summary>
    public static class EdgeStopEmberSparks
    {
        public static void Spawn(Vector3 contactPos, in VfxColorRgb color, FeelVfxRuntime feel)
        {
            Color tint = new Color(color.R, color.G, color.B, 1f);
            Vector3 p = contactPos;
            p.y = FeelVfxRuntime.GroundY + RuleVfxDefaults.FootSparkLiftM;
            if (feel != null)
            {
                feel.HitSpark(p + Vector3.up * RuleVfxArtDefaults.EdgeSparkLift, tint, crit: false);
                feel.DodgeDust(p, Vector3.forward);
                return;
            }

            var go = new GameObject("EdgeStopEmber");
            go.transform.position = p;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = VfxPlanDefaults.EdgeStopSparkSec;
            main.startSpeed = RuleVfxArtDefaults.EdgeSparkSpeed;
            main.startSize = RuleVfxArtDefaults.EdgeSparkSize;
            main.startColor = tint;
            main.maxParticles = VfxPlanDefaults.EdgeStopSparkCount;
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)VfxPlanDefaults.EdgeStopSparkCount)
            });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = RuleVfxArtDefaults.EdgeSparkRadius;
            var r = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            r.sharedMaterial = new Material(shader);
            ps.Play();
            Object.Destroy(go, VfxPlanDefaults.EdgeStopSparkSec + RuleVfxArtDefaults.EdgeDestroyPad);
        }
    }
}
