using UnityEngine;

namespace Dovus.Game
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
            float e = Mathf.Max(3f, walkHalfM * 0.72f);
            Vector3[] centers =
            {
                new Vector3( e, 0.02f,  e),
                new Vector3(-e, 0.02f,  e),
                new Vector3( e, 0.02f, -e),
                new Vector3(-e, 0.02f, -e),
                new Vector3( 0f, 0.02f,  e * 0.15f),
                new Vector3( e * 0.2f, 0.02f, 0f)
            };

            float[] radii = { 1.8f, 1.5f, 1.6f, 1.4f, 2.2f, 1.3f };

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
            disc.transform.localScale = new Vector3(radiusM * 2f, 0.03f, radiusM * 2f);
            Object.Destroy(disc.GetComponent<Collider>());
            var rend = disc.GetComponent<Renderer>();
            rend.sharedMaterial = MakeLavaMat(new Color(1f, 0.28f, 0.05f));
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // İç parıltı
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "LavaCore";
            core.transform.SetParent(go.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            core.transform.localScale = new Vector3(radiusM * 1.1f, 0.025f, radiusM * 1.1f);
            Object.Destroy(core.GetComponent<Collider>());
            var coreRend = core.GetComponent<Renderer>();
            coreRend.sharedMaterial = MakeLavaMat(new Color(1f, 0.75f, 0.2f));
            coreRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AddHeatParticles(go.transform, radiusM);

            var lightGo = new GameObject("LavaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.4f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.12f);
            light.intensity = 2.4f;
            light.range = radiusM * 4.5f;
            light.shadows = LightShadows.None;
        }

        static void AddHeatParticles(Transform parent, float radiusM)
        {
            var go = new GameObject("Heat");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.05f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.55f, 0.1f, 0.55f),
                new Color(1f, 0.2f, 0.02f, 0.15f));
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.gravityModifier = -0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radiusM * 0.7f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.55f),
                    new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.7f, 0.12f),
                    new GradientAlphaKey(0.25f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Sprites/Default");
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
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 2.2f);
            }
            return mat;
        }
    }
}
