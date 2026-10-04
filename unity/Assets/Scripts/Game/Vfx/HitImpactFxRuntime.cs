using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.Diagnostics;
using Dovus.Game.Weapons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Boss isabet noktasında havuzlanmış parçacıklar: kılıç kıvılcımı, taş/toz, büyü rengi, koyu kırmızı sıçrama.
    /// </summary>
    public sealed class HitImpactFxRuntime
    {
        readonly FeelTuning _feel;
        readonly KenneyVfxTextures _kenney;
        readonly Queue<ParticleSystem> SparkPool = new();
        readonly Queue<ParticleSystem> BluntPool = new();
        readonly Queue<ParticleSystem> MagicPool = new();
        readonly Queue<ParticleSystem> SplashPool = new();
        readonly List<Active> Live = new();
        int _liveCount;

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

        public HitImpactFxRuntime(FeelTuning feel, KenneyVfxTextures kenney)
        {
            _feel = feel;
            _kenney = kenney ?? new KenneyVfxTextures(null);
        }

        public void Play(Vector3 worldPoint, string archetype, Color elementTint, bool isCrit, Transform bossRoot)
        {
            if (_feel == null || !_feel.HitImpactEnabled || _feel.HitImpactMaxConcurrent <= 0)
                return;

            TrimLive();
            if (_liveCount >= _feel.HitImpactMaxConcurrent)
                return;

            Vector3 pos = worldPoint + Vector3.up * HitImpactFxDefaults.WorldImpactLiftM;
            float life = _feel.HitImpactLifeSec;
            float mult = isCrit ? HitImpactFxDefaults.CritFxMult : 1f;

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

            EmitSplash(pos, life * HitImpactFxDefaults.SplashEmitterLifeMult);
            if (bossRoot != null)
            {
                BossHitFlinchView flinch = bossRoot.GetComponentInChildren<BossHitFlinchView>();
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

        void EmitSpark(Vector3 pos, Color tint, float mult, float life)
        {
            ParticleSystem ps = Rent(PoolSpark, SparkPool, true);
            ApplyTexture(ps, _kenney.TexHit, true);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(10 * mult), life, HitImpactFxDefaults.PsBurstCount, HitImpactFxDefaults.SharpBurstSpeedMps * mult, HitImpactFxDefaults.SharpBurstSizeM);
            FadeColor(ps, Color.white, tint);
            Track(ps, life, PoolSpark);
        }

        void EmitBlunt(Vector3 pos, float mult, float life)
        {
            ParticleSystem ps = Rent(PoolBlunt, BluntPool, false);
            ApplyTexture(ps, _kenney.TexEarth, false);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(14 * mult), life, HitImpactFxDefaults.WideBurstLifetimeSec, HitImpactFxDefaults.WideBurstSpeedMps * mult, HitImpactFxDefaults.WideBurstSizeM);
            Color stone = new(0.62f, 0.58f, 0.54f, 0.9f);
            FadeColor(ps, stone, new Color(0.45f, 0.42f, 0.4f, 0f));
            Track(ps, life, PoolBlunt);

            ParticleSystem dust = Rent(PoolBlunt, BluntPool, false);
            ApplyTexture(dust, _kenney.TexAir, false);
            ConfigureBurst(dust, pos, Quaternion.Euler(-90f, 0f, 0f), Mathf.RoundToInt(8 * mult), life * HitImpactFxDefaults.DustBurstLifetimeMult, HitImpactFxDefaults.DustBurstLifetimeSec, HitImpactFxDefaults.DustBurstSpeedMps, HitImpactFxDefaults.DustBurstSizeM);
            Color grey = new(0.55f, 0.52f, 0.5f, 0.55f);
            FadeColor(dust, grey, new Color(grey.r, grey.g, grey.b, 0f));
            Track(dust, life * HitImpactFxDefaults.BluntPoolTrackLifeMult, PoolBlunt);
        }

        void EmitMagic(Vector3 pos, Color tint, float mult, float life)
        {
            Color burst = tint.a > HitImpactFxDefaults.BurstColorAlphaThreshold ? tint : new Color(0.55f, 0.75f, 1f, 1f);
            ParticleSystem ps = Rent(PoolMagic, MagicPool, true);
            ApplyTexture(ps, _kenney.ClosestElementName(burst), true);
            ConfigureBurst(ps, pos, Quaternion.identity, Mathf.RoundToInt(16 * mult), life, HitImpactFxDefaults.MagicBurstLifetimeSec, HitImpactFxDefaults.MagicBurstSpeedMps * mult, HitImpactFxDefaults.MagicBurstSizeM);
            FadeColor(ps, burst, Color.Lerp(burst, Color.white, HitImpactFxDefaults.MagicBurstWhiteLerp));
            Track(ps, life, PoolMagic);
        }

        void EmitSplash(Vector3 pos, float life)
        {
            ParticleSystem ps = Rent(PoolSplash, SplashPool, false);
            ApplyTexture(ps, _kenney.TexDark, false);
            ConfigureBurst(ps, pos, Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f), 4, life * HitImpactFxDefaults.SplashBurstLifetimeMult, HitImpactFxDefaults.SplashBurstLifetimeSec, HitImpactFxDefaults.SplashBurstSpeedMps, HitImpactFxDefaults.SplashBurstSizeM);
            Color dark = new(0.32f, 0.03f, 0.05f, 0.55f);
            FadeColor(ps, dark, new Color(dark.r, dark.g, dark.b, 0f));
            Track(ps, life * HitImpactFxDefaults.SplashPoolTrackLifeMult, PoolSplash);
        }

        void Track(ParticleSystem ps, float life, int poolKind)
        {
            ps.Play(true);
            Live.Add(new Active { Ps = ps, DieAt = Time.unscaledTime + life, PoolKind = poolKind });
            _liveCount++;
        }

        void TrimLive()
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

        ParticleSystem Rent(int kind, Queue<ParticleSystem> pool, bool additive)
        {
            while (pool.Count > 0)
            {
                ParticleSystem ps = pool.Dequeue();
                if (ps != null)
                    return ps;
            }

            return CreateBurst(kind, additive);
        }

        void Return(ParticleSystem ps, int kind)
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

        ParticleSystem CreateBurst(int kind, bool additive)
        {
            var go = new GameObject("HitImpact_" + kind);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = HitImpactFxDefaults.EmitterMaxParticles;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = _kenney.GetParticleMaterial(_kenney.TexHit, additive);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        void ConfigureBurst(
            ParticleSystem ps, Vector3 pos, Quaternion rot, int count, float life, float size, float speed, float radius)
        {
            ps.transform.SetPositionAndRotation(pos, rot);
            var main = ps.main;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = 0.5f;
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, HitImpactFxDefaults.BurstParticleMaxCount)) });
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
        }

        void ApplyTexture(ParticleSystem ps, string texName, bool additive)
        {
            if (ps == null)
                return;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = _kenney.GetParticleMaterial(texName, additive);
        }

        void FadeColor(ParticleSystem ps, Color from, Color to)
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
