using Dovus.Game.Skills;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Cast'in VFX çıkış anında (prezentasyon <c>spawn_vfx_at_frame</c>) elde kısa parçacık
    /// patlaması. Yalnız görsel; etki doğumu ve hasar zamanlaması buna bağlı değil.
    /// Parçacık değerleri <see cref="LivingEffectView"/> BangBurst'ünden alındı.
    /// </summary>
    public sealed class CastFlash : MonoBehaviour
    {
        ParticleSystem _ps;

        public int PlayCount { get; private set; }

        public void Play(Color tint, Animator animator)
        {
            EnsureSystem();
            Transform hand = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
            _ps.transform.position = hand != null ? hand.position : transform.position + Vector3.up;

            var col = _ps.colorOverLifetime;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            _ps.Play(true);
            PlayCount++;
        }

        void EnsureSystem()
        {
            if (_ps != null)
                return;
            var go = new GameObject("CastFlash");
            go.transform.SetParent(transform, false);
            _ps = go.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.startLifetime = 0.35f;
            main.startSpeed = 3.5f;
            main.startSize = 0.22f;
            main.maxParticles = 36;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var sh = _ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;
            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = AssetLoader.FindShader("Universal Render Pipeline/Particles/Unlit", null)
                         ?? AssetLoader.FindShader("Sprites/Default", null);
            if (shader != null)
                pr.sharedMaterial = new Material(shader);
        }
    }
}
