using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Stüdyo hilesi: alev/duman/rün = kameraya bakan quad/partikül, hacim mesh değil.
    /// Asset gelmeden önce de sahte “ember” ile pipeline doğrulanır.
    /// </summary>
    public sealed class BillboardVfx : MonoBehaviour
    {
        [SerializeField] ParticleSystem _system;
        [SerializeField] bool _faceCamera = true;

        public static BillboardVfx CreateEmberField(Transform parent, Color tint, float rate = 18f)
        {
            var go = new GameObject("BillboardEmbers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.2f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.4f;
            main.startSize = 0.08f;
            main.startColor = tint;
            main.startSpeed = 0.35f;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.5f;
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
                    new GradientAlphaKey(0.85f, 0.15f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLife.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
                renderer.sharedMaterial = mat;
            }

            var vfx = go.AddComponent<BillboardVfx>();
            vfx._system = ps;
            return vfx;
        }

        void LateUpdate()
        {
            // ParticleSystemRenderMode.Billboard kameraya bakar; ekstra kök rotasyonu yok.
            _ = _faceCamera;
        }

        public void SetTint(Color tint)
        {
            if (_system == null)
                return;
            var main = _system.main;
            main.startColor = tint;
        }
    }
}
