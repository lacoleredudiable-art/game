using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §5.5 kenar freni: ATIL canlı/duvar kenarında durunca kısa kor kıvılcımı
    /// (8–12 parçacık, 0,2 s, Hareket rengi). Yalnız görünüm.
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
                feel.HitSpark(p + Vector3.up * 0.4f, tint, crit: false);
                feel.DodgeDust(p, Vector3.forward);
                return;
            }

            // Feel yoksa küçük prosedürel burst.
            var go = new GameObject("EdgeStopEmber");
            go.transform.position = p;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = VfxPlanDefaults.EdgeStopSparkSec;
            main.startSpeed = 2.5f;
            main.startSize = 0.08f;
            main.startColor = tint;
            main.maxParticles = VfxPlanDefaults.EdgeStopSparkCount;
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, VfxPlanDefaults.EdgeStopSparkCount)
            });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            r.sharedMaterial = new Material(shader);
            ps.Play();
            Object.Destroy(go, VfxPlanDefaults.EdgeStopSparkSec + 0.15f);
        }
    }
}
