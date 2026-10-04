using Dovus.Game.Config;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// His efektleri (isabet kıvılcımı, dodge/ayak tozu, slam şok dalgası + çatlak, alev konisi).
    /// Önce <see cref="VfxLibrary"/> prefab'ı; yoksa koddan kurulan parçacık/çizgi. Yalnız
    /// görsel — hasar ve etki zamanlaması bunlara bağlı değil.
    /// </summary>
    public sealed class FeelVfxRuntime
    {
        /// <summary>Düz arena zemini (zemin collider'ı yok); yere oturan efektler bunu kullanır.</summary>
        public const float GroundY = 0.03f;

        readonly KenneyVfxTextures _kenney;
        Texture2D _crack;
        readonly VfxLibrary _vfx;

        public FeelVfxRuntime(GameTuning tuning, VfxLibrary vfx)
        {
            _vfx = vfx ?? throw new System.InvalidOperationException("VfxLibrary gerekli.");
            _kenney = new KenneyVfxTextures(tuning);
        }

        VfxLibrary Lib => _vfx ?? throw new System.InvalidOperationException("VfxLibrary gerekli.");

        public void HitSpark(Vector3 pos, Color tint, bool crit)
        {
            VfxLibrary lib = Lib;
            GameObject prefab = lib.TrySpawn(crit ? VfxLibrary.CritSpark : VfxLibrary.HitSpark, pos, Quaternion.identity);
            if (prefab != null)
            {
                if (!crit)
                    VfxLibrary.Tint(prefab, tint, lib.HitSparkTintStrength);
                return;
            }

            float mult = crit ? lib.CritSparkMult : 1f;
            ParticleSystem ps = NewBurst("HitSpark", pos, Quaternion.identity, additive: true, _kenney.TexHit);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lib.HitSparkLifeSec * FeelVfxDefaults.HitSparkLifetimeMinMult, lib.HitSparkLifeSec);
            main.startSpeed = new ParticleSystem.MinMaxCurve(lib.HitSparkSpeed * 0.5f, lib.HitSparkSpeed * mult);
            main.startSize = lib.HitSparkSize * mult;
            main.gravityModifier = FeelVfxDefaults.MainGravityModifier;
            Burst(ps, Mathf.RoundToInt(lib.HitSparkCount * mult));
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = FeelVfxDefaults.EmitterShapeRadiusM;
            FadeColor(ps, Color.white, tint);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = FeelVfxDefaults.RendererVelocityScale;
            r.lengthScale = FeelVfxDefaults.RendererLengthScale;
            ps.Play();
        }

        public void DodgeDust(Vector3 pos, Vector3 dir)
        {
            VfxLibrary lib = Lib;
            Vector3 ground = new(pos.x, GroundY, pos.z);
            Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-dir) : Quaternion.identity;
            if (lib.TrySpawn(VfxLibrary.DodgeDust, ground, rot) != null)
                return;
            Dust("DodgeDust", ground, lib.DodgeDustCount, lib.DodgeDustLifeSec, lib.DodgeDustSize, FeelVfxDefaults.DodgeDustIntensityMult, lib.DustColor);
        }

        public void FootDust(Vector3 pos, bool boss)
        {
            VfxLibrary lib = Lib;
            Vector3 ground = new(pos.x, GroundY, pos.z);
            if (lib.TrySpawn(boss ? VfxLibrary.BossStepDust : VfxLibrary.FootDust, ground, Quaternion.identity) != null)
                return;
            float m = boss ? lib.BossStepDustMult : 1f;
            Dust("FootDust", ground, Mathf.RoundToInt(lib.FootDustCount * m), lib.FootDustLifeSec * (boss ? FeelVfxDefaults.FootDustBossLifeMult : 1f),
                lib.FootDustSize * m, FeelVfxDefaults.FootDustSpeedMult * m, lib.DustColor);
        }

        public void SlamImpact(Vector3 center, float radiusM)
        {
            VfxLibrary lib = Lib;
            Vector3 ground = new(center.x, GroundY, center.z);
            float diameter = radiusM * 2f;
            bool wave = lib.TrySpawn(VfxLibrary.SlamShockwave, ground, Quaternion.identity, null, diameter) != null;
            bool crack = lib.TrySpawn(VfxLibrary.GroundCrack, ground, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                null, diameter) != null;

            if (!wave)
            {
                var go = new GameObject("SlamShockwave");
                go.transform.position = ground + Vector3.up * FeelVfxDefaults.DustEmitterGroundLiftM;
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = Mat(additive: true, textured: false);
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                FxTweenView.Ring(line, lib.ShockwaveColor, radiusM, lib.ShockwaveWidthM, lib.ShockwaveSec);
                Dust("SlamDust", ground, lib.DodgeDustCount * 2, lib.DodgeDustLifeSec * FeelVfxDefaults.SlamDustLifeSecMult,
                    lib.DodgeDustSize * FeelVfxDefaults.SlamDustSizeMult, radiusM * FeelVfxDefaults.SlamDustSizeMult, lib.DustColor, ringRadius: radiusM * FeelVfxDefaults.DodgeRingRadiusMult);
            }

            if (!crack)
            {
                var go = new GameObject("GroundCrack");
                go.transform.position = ground;
                go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                go.transform.localScale = Vector3.one * radiusM * FeelVfxDefaults.RingBurstLocalScaleMult;
                go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Mat(additive: false, textured: true, crack: true);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                FxTweenView.Surface(mr, lib.CrackColor, lib.CrackHoldSec, lib.CrackFadeSec);
            }
        }

        public void FireCone(Vector3 origin, Vector3 forward, float halfAngleDeg, float reachM)
        {
            VfxLibrary lib = Lib;
            forward.y = 0f;
            Quaternion rot = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward.normalized) : Quaternion.identity;
            if (lib.TrySpawn(VfxLibrary.FireCone, origin, rot, null, reachM) != null)
                return;

            ParticleSystem ps = NewBurst("FireCone", origin, rot, additive: true, _kenney.TexFire);
            var main = ps.main;
            main.duration = lib.FlameSec;
            float life = reachM / Mathf.Max(FeelVfxDefaults.FlameSpeedDenominatorMinMps, lib.FlameSpeed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * FeelVfxDefaults.DustLifetimeMinMult, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(lib.FlameSpeed * FeelVfxDefaults.FlameSpeedMinMult, lib.FlameSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(lib.FlameSize * 0.5f, lib.FlameSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.CeilToInt(lib.FlameRate * life) + 8;
            var em = ps.emission;
            em.rateOverTime = lib.FlameRate;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = Mathf.Clamp(halfAngleDeg, 1f, FeelVfxDefaults.ConeShapeMaxAngleDeg);
            sh.radius = FeelVfxDefaults.DustRingRadiusM;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, FeelVfxDefaults.FlameSizeOverLifeStart, 1f, FeelVfxDefaults.FlameSizeOverLifeMaxMult));
            FadeColor(ps, lib.FlameColorA, lib.FlameColorB);
            ps.Play();
        }

        void Dust(string name, Vector3 ground, int count, float life, float size, float speed, Color color,
            float ringRadius = FeelVfxDefaults.BurstRingRadiusM)
        {
            ParticleSystem ps = NewBurst(name, ground, Quaternion.Euler(-90f, 0f, 0f), additive: false, _kenney.TexEarth);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * FeelVfxDefaults.FlameLifetimeMinMult, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * FeelVfxDefaults.DustSpeedMinMult, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * FeelVfxDefaults.BurstSizeMinMult, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -FeelVfxDefaults.DustRiseGravityMult;
            Burst(ps, count);
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = ringRadius;
            sh.radiusThickness = 0f;
            var sizeLt = ps.sizeOverLifetime;
            sizeLt.enabled = true;
            sizeLt.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, FeelVfxDefaults.BurstSizeOverLifeMaxMult));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = speed * FeelVfxDefaults.VelocityLimitFraction;
            limit.dampen = FeelVfxDefaults.VelocityLimitDampen;
            Color end = color;
            end.a = 0f;
            FadeColor(ps, color, end);
            ps.Play();
        }

        ParticleSystem NewBurst(string name, Vector3 pos, Quaternion rot, bool additive, string kenneyTex)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, rot);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = FeelVfxDefaults.ShortBurstDurationSec;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = FeelVfxDefaults.ShortBurstMaxParticles;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = _kenney.GetParticleMaterial(kenneyTex, additive);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        void Burst(ParticleSystem ps, int count)
        {
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, FeelVfxDefaults.ParticleBurstMaxCount)) });
            var main = ps.main;
            main.maxParticles = Mathf.Max(main.maxParticles, count);
        }

        void FadeColor(ParticleSystem ps, Color from, Color to)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(from.a * FeelVfxDefaults.DustGradientMidAlphaMult, FeelVfxDefaults.DustGradientMidTime), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        Material Mat(bool additive, bool textured, bool crack = false)
        {
            if (!textured)
                return _kenney.GetParticleMaterial(null, additive);

            if (crack && _kenney.Load(KenneyVfxTextures.SlamCrackTexture) != null)
                return _kenney.GetParticleMaterial(KenneyVfxTextures.SlamCrackTexture, additive);

            Shader shader = PresentationParticleMaterials.ResolveShaderPublic();
            var m = new Material(shader) { name = "FeelVfx_Crack" };
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                m.SetInt("_ZWrite", 0);
            }
            m.renderQueue = (int)RenderQueue.Transparent;
            Texture2D tex = CrackTexture();
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            return m;
        }

        /// <summary>Merkezden dışa kırık çizgiler; beyaz maske, renk FxTweenView'den.</summary>
        Texture2D CrackTexture()
        {
            if (_crack != null)
                return _crack;
            const int size = 128;
            _crack = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "FxCrack" };
            var px = new Color[size * size];
            var rng = new System.Random(7);
            const int arms = 9;
            for (int a = 0; a < arms; a++)
            {
                float ang = (a + (float)rng.NextDouble() * FeelVfxDefaults.SparkArmAngleSpan) / arms * Mathf.PI * 2f;
                Vector2 p = new(size * 0.5f, size * 0.5f);
                float len = size * (FeelVfxDefaults.SparkLengthBaseMult + (float)rng.NextDouble() * FeelVfxDefaults.SparkLenJitterMult);
                float width = FeelVfxDefaults.SparkStrokeBaseWidthPx;
                for (float t = 0f; t < len; t += 1f)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * FeelVfxDefaults.SparkAngleRandomHalfSpan;
                    p += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    float w = width * (1f - t / len) + FeelVfxDefaults.SparkStrokeMinWidthPx;
                    Stamp(px, size, p, w);
                    if (rng.NextDouble() < FeelVfxDefaults.SparkBranchChance)
                    {
                        float bAng = ang + (rng.NextDouble() < 0.5 ? FeelVfxDefaults.SparkBranchAngleRad : -FeelVfxDefaults.SparkBranchAngleRad);
                        Vector2 q = p;
                        for (int k = 0; k < len * FeelVfxDefaults.SparkStampIterationsMult; k++)
                        {
                            q += new Vector2(Mathf.Cos(bAng), Mathf.Sin(bAng));
                            Stamp(px, size, q, w * FeelVfxDefaults.SparkBranchWidthMult);
                        }
                    }
                }
            }
            _crack.SetPixels(px);
            _crack.Apply(false, true);
            return _crack;
        }

        void Stamp(Color[] px, int size, Vector2 p, float r)
        {
            int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r);
            int y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.CeilToInt(p.y + r);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(size - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(size - 1, x1); x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), p);
                float a = Mathf.Clamp01(r - d);
                int i = y * size + x;
                if (a > px[i].a)
                    px[i] = new Color(1f, 1f, 1f, a);
            }
        }
    }
}
