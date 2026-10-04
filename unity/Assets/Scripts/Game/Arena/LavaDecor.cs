using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Arena
{
    /// <summary>
    /// Yer lav havuzları — eski kırmızı ember noktalarının yerine emissive disk + ısı partikülü.
    /// </summary>
    public static class LavaDecor
    {
        public static void Build(Transform arenaRoot, float walkHalfM)
        {
            if (arenaRoot == null)
                return;

            var root = new GameObject("LavaDecor");
            root.transform.SetParent(arenaRoot, false);

            // Köşe / kenar havuzları — yürüyüş alanının biraz içinde.
            float e = Mathf.Max(LavaDecorDefaults.MinPoolExtentM, walkHalfM * LavaDecorDefaults.WalkHalfExtentMult);
            Vector3[] centers =
            {
                new Vector3( e, LavaDecorDefaults.PoolSurfaceYM,  e),
                new Vector3(-e, LavaDecorDefaults.PoolSurfaceYM,  e),
                new Vector3( e, LavaDecorDefaults.PoolSurfaceYM, -e),
                new Vector3(-e, LavaDecorDefaults.PoolSurfaceYM, -e),
                new Vector3( 0f, LavaDecorDefaults.PoolSurfaceYM,  e * LavaDecorDefaults.CenterNorthZMult),
                new Vector3( e * LavaDecorDefaults.EastCenterXMult, LavaDecorDefaults.PoolSurfaceYM, 0f)
            };

            float[] radii = { LavaDecorDefaults.PoolRadiusPrimaryM, LavaDecorDefaults.PoolRadiusNorthM, LavaDecorDefaults.PoolRadiusEastM, LavaDecorDefaults.PoolRadiusSouthM, LavaDecorDefaults.PoolRadiusWestM, LavaDecorDefaults.PoolRadiusCenterM };

            for (int i = 0; i < centers.Length; i++)
                CreatePool(root.transform, centers[i], radii[i]);
        }

        static void CreatePool(Transform parent, Vector3 pos, float radiusM)
        {
            var go = new GameObject("LavaPool");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            // Disk (ezilmiş küre) — emissive turuncu.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "LavaDisc";
            disc.transform.SetParent(go.transform, false);
            disc.transform.localScale = new Vector3(radiusM * 2f, LavaDecorDefaults.DiscThicknessM, radiusM * 2f);
            Object.Destroy(disc.GetComponent<Collider>());
            var rend = disc.GetComponent<Renderer>();
            rend.sharedMaterial = MakeLavaMat(new Color(1f, 0.28f, 0.05f));
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // İç parıltı
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "LavaCore";
            core.transform.SetParent(go.transform, false);
            core.transform.localPosition = new Vector3(0f, LavaDecorDefaults.CoreLiftM, 0f);
            core.transform.localScale = new Vector3(radiusM * LavaDecorDefaults.CoreDiameterMult, LavaDecorDefaults.CoreThicknessM, radiusM * LavaDecorDefaults.CoreDiameterMult);
            Object.Destroy(core.GetComponent<Collider>());
            var coreRend = core.GetComponent<Renderer>();
            coreRend.sharedMaterial = MakeLavaMat(new Color(1f, 0.75f, 0.2f));
            coreRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AddHeatParticles(go.transform, radiusM);

            var lightGo = new GameObject("LavaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = Vector3.up * LavaDecorDefaults.PointLightHeightM;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.12f);
            light.intensity = LavaDecorDefaults.PointLightIntensity;
            light.range = radiusM * LavaDecorDefaults.PointLightRangeMult;
            light.shadows = LightShadows.None;
        }

        static void AddHeatParticles(Transform parent, float radiusM)
        {
            var go = new GameObject("Heat");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * LavaDecorDefaults.HeatEmitterLiftM;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(LavaDecorDefaults.HeatLifetimeMinSec, LavaDecorDefaults.HeatLifetimeMaxSec);
            main.startSize = new ParticleSystem.MinMaxCurve(LavaDecorDefaults.HeatSizeMinM, LavaDecorDefaults.HeatSizeMaxM);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.55f, 0.1f, 0.55f),
                new Color(1f, 0.2f, 0.02f, 0.15f));
            main.startSpeed = new ParticleSystem.MinMaxCurve(LavaDecorDefaults.HeatSpeedMinMps, LavaDecorDefaults.HeatSpeedMaxMps);
            main.maxParticles = LavaDecorDefaults.HeatMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.gravityModifier = -LavaDecorDefaults.HeatGravityRiseMult;

            var emission = ps.emission;
            emission.rateOverTime = LavaDecorDefaults.HeatRateOverTime;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radiusM * LavaDecorDefaults.HeatEmitterRadiusMult;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), LavaDecorDefaults.HeatColorGradientMid),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(LavaDecorDefaults.HeatAlphaPeak, LavaDecorDefaults.HeatAlphaPeakTime),
                    new GradientAlphaKey(LavaDecorDefaults.HeatAlphaMid, LavaDecorDefaults.HeatAlphaMidTime),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, LavaDecorDefaults.HeatSizeOverLifeMinMult, 1f, LavaDecorDefaults.HeatSizeOverLifeMaxMult));

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Particles/Unlit", null)
                         ?? AssetLoader.FindShader("Particles/Standard Unlit", null)
                         ?? AssetLoader.FindShader("Sprites/Default", null);
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
                pr.sharedMaterial = mat;
            }
        }

        static Material MakeLavaMat(Color c)
        {
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Lit", null)
                         ?? AssetLoader.FindShader("Universal Render Pipeline/Unlit", null)
                         ?? AssetLoader.FindShader("Standard", null);
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * LavaDecorDefaults.EmissionBoostMult);
            }
            return mat;
        }
    }
}
