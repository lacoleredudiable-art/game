using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Stüdyo hilesi: alev/duman/rün = kameraya bakan quad/partikül, hacim mesh değil.
    /// Asset gelmeden önce de sahte “ember” ile pipeline doğrulanır.
    /// </summary>
    public sealed class BillboardVfxView : MonoBehaviour
    {
        [SerializeField] ParticleSystem _system;
        [SerializeField] bool _faceCamera = true;

        public static BillboardVfxView CreateEmberField(Transform parent, Color tint, float rate = BillboardVfxViewDefaults.EmberSpawnRate)
        {
            var go = new GameObject("BillboardEmbers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * BillboardVfxViewDefaults.EmberEmitterLiftM;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = BillboardVfxViewDefaults.EmberStartLifetimeSec;
            main.startSize = BillboardVfxViewDefaults.EmberStartSizeM;
            main.startColor = tint;
            main.startSpeed = BillboardVfxViewDefaults.EmberStartSpeedMps;
            main.maxParticles = BillboardVfxViewDefaults.EmberMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = BillboardVfxViewDefaults.EmberShapeRadiusM;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(tint, 0f),
                    new GradientColorKey(new Color(tint.r, tint.g * 0.4f, 0f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(BillboardVfxViewDefaults.EmberAlphaPeak, BillboardVfxViewDefaults.EmberAlphaMidTime),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLife.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Particles/Unlit", null)
                         ?? AssetLoader.FindShader("Particles/Standard Unlit", null)
                         ?? AssetLoader.FindShader("Sprites/Default", null);
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
                renderer.sharedMaterial = mat;
            }

            var vfx = go.AddComponent<BillboardVfxView>();
            vfx._system = ps;
            return vfx;
        }

        void LateUpdate()
        {
            // ParticleSystemRenderMode.Billboard kameraya bakar; ekstra kök rotasyonu yok.
            _ = _faceCamera;
        }

    }
}
