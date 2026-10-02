using System.Collections.Generic;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game
{
    /// <summary>
    /// Boss isabet noktasında havuzlanmış parçacıklar: kılıç kıvılcımı, taş/toz, büyü rengi, koyu kırmızı sıçrama.
    /// </summary>
    public static class HitImpactFx
    {
        static FeelTuning _feel;
        static readonly Queue<ParticleSystem> SparkPool = new();
        static readonly Queue<ParticleSystem> BluntPool = new();
        static readonly Queue<ParticleSystem> MagicPool = new();
        static readonly Queue<ParticleSystem> SplashPool = new();
        static readonly List<Active> Live = new();
        static int _liveCount;

        struct Active
        {
            public ParticleSystem Ps;
            public float DieAt;
            public int PoolKind;
        }

        const int PoolSpark = 0;
        const int PoolBlunt = 1;
        const int PoolMagic = 2;
        const int PoolSplash = 3;

        public static void Configure(FeelTuning feel) => _feel = feel;

        public static void Play(Vector3 worldPoint, string archetype, Color elementTint, bool isCrit, Transform bossRoot)
        {
            if (_feel == null || !_feel.HitImpactEnabled || _feel.HitImpactMaxConcurrent <= 0)
                return;

            TrimLive();
            if (_liveCount >= _feel.HitImpactMaxConcurrent)
                return;

            Vector3 pos = worldPoint + Vector3.up * 0.05f;
            float life = _feel.HitImpactLifeSec;
            float mult = isCrit ? 1.35f : 1f;

            switch (archetype)
            {
                case WeaponArchetypeMap.Hammer:
                case WeaponArchetypeMap.Gun:
                    EmitBlunt(pos, mult, life);
                    break;
                case WeaponArchetypeMap.Caster:
                    EmitMagic(pos, elementTint, mult, life);
                    break;
                default:
                    EmitSpark(pos, elementTint, mult, life);
                    break;
            }

            EmitSplash(pos, life * 0.85f);
            if (bossRoot != null)
            {
                BossHitFlinch flinch = bossRoot.GetComponentInChildren<BossHitFlinch>();
                flinch?.KickFromWorldPoint(worldPoint, bossRoot.position);
            }

            DebugConfig.DevLog(
                $"[Feel2Verify] impact archetype={archetype} crit={isCrit} live={_liveCount} type={ImpactLabel(archetype)}");
        }

        static string ImpactLabel(string archetype) => archetype switch
        {
            WeaponArchetypeMap.Hammer or WeaponArchetypeMap.Gun => "blunt",
            WeaponArchetypeMap.Caster => "magic",
            _ => "spark"
        };

        static void EmitSpark(Vector3 pos, Color tint, float mult, float life)
        {
            ParticleSystem ps = Rent(PoolSpark, SparkPool, true);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(10 * mult), life, 0.08f, 4.5f * mult, 0.06f);
            FadeColor(ps, Color.white, tint);
            Track(ps, life, PoolSpark);
        }

        static void EmitBlunt(Vector3 pos, float mult, float life)
        {
            ParticleSystem ps = Rent(PoolBlunt, BluntPool, false);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(14 * mult), life, 0.12f, 3.2f * mult, 0.1f);
            Color stone = new(0.62f, 0.58f, 0.54f, 0.9f);
            FadeColor(ps, stone, new Color(0.45f, 0.42f, 0.4f, 0f));
            Track(ps, life, PoolBlunt);

            ParticleSystem dust = Rent(PoolBlunt, BluntPool, false);
            ConfigureBurst(dust, pos, Quaternion.Euler(-90f, 0f, 0f), Mathf.RoundToInt(8 * mult), life * 1.1f, 0.15f, 1.2f, 0.18f);
            Color grey = new(0.55f, 0.52f, 0.5f, 0.55f);
            FadeColor(dust, grey, new Color(grey.r, grey.g, grey.b, 0f));
            Track(dust, life * 1.1f, PoolBlunt);
        }

        static void EmitMagic(Vector3 pos, Color tint, float mult, float life)
        {
            Color burst = tint.a > 0.01f ? tint : new Color(0.55f, 0.75f, 1f, 1f);
            ParticleSystem ps = Rent(PoolMagic, MagicPool, true);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(16 * mult), life, 0.1f, 5f * mult, 0.09f);
            FadeColor(ps, burst, Color.Lerp(burst, Color.white, 0.4f));
            Track(ps, life, PoolMagic);
        }

        static void EmitSplash(Vector3 pos, float life)
        {
            ParticleSystem ps = Rent(PoolSplash, SplashPool, false);
            ConfigureBurst(ps, pos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 4, life * 0.9f, 0.05f, 0.35f, 0.14f);
            Color dark = new(0.32f, 0.03f, 0.05f, 0.55f);
            FadeColor(ps, dark, new Color(dark.r, dark.g, dark.b, 0f));
            Track(ps, life * 0.9f, PoolSplash);
        }

        static void Track(ParticleSystem ps, float life, int poolKind)
        {
            ps.Play(true);
            Live.Add(new Active { Ps = ps, DieAt = Time.unscaledTime + life, PoolKind = poolKind });
            _liveCount++;
        }

        static void TrimLive()
        {
            float now = Time.unscaledTime;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                Active a = Live[i];
                if (a.Ps == null || now >= a.DieAt)
                {
                    if (a.Ps != null)
                    {
                        a.Ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        Return(a.Ps, a.PoolKind);
                    }
                    Live.RemoveAt(i);
                    _liveCount = Mathf.Max(0, _liveCount - 1);
                }
            }
        }

        static ParticleSystem Rent(int kind, Queue<ParticleSystem> pool, bool additive)
        {
            while (pool.Count > 0)
            {
                ParticleSystem ps = pool.Dequeue();
                if (ps != null)
                    return ps;
            }

            return CreateBurst(kind, additive);
        }

        static void Return(ParticleSystem ps, int kind)
        {
            Queue<ParticleSystem> pool = kind switch
            {
                PoolBlunt => BluntPool,
                PoolMagic => MagicPool,
                PoolSplash => SplashPool,
                _ => SparkPool
            };
            if (pool.Count < 12)
                pool.Enqueue(ps);
            else
                Object.Destroy(ps.gameObject);
        }

        static ParticleSystem CreateBurst(int kind, bool additive)
        {
            var go = new GameObject("HitImpact_" + kind);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = additive ? PresentationParticleMaterials.AdditiveTextured : PresentationParticleMaterials.AlphaTextured;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void ConfigureBurst(
            ParticleSystem ps, Vector3 pos, Quaternion rot, int count, float life, float size, float speed, float radius)
        {
            ps.transform.SetPositionAndRotation(pos, rot);
            var main = ps.main;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = 0.5f;
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 32)) });
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
        }

        static void FadeColor(ParticleSystem ps, Color from, Color to)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

    }
}
