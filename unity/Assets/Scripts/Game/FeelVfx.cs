using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game
{
    /// <summary>
    /// His efektleri (isabet kıvılcımı, dodge/ayak tozu, slam şok dalgası + çatlak, alev konisi).
    /// Önce <see cref="VfxLibrary"/> prefab'ı; yoksa koddan kurulan parçacık/çizgi. Yalnız
    /// görsel — hasar ve etki zamanlaması bunlara bağlı değil.
    /// </summary>
    public static class FeelVfx
    {
        const float GroundY = 0.03f;

        static Texture2D _dot;
        static Texture2D _crack;
        static readonly Dictionary<int, Material> Materials = new();

        public static void HitSpark(Vector3 pos, Color tint, bool crit)
        {
            VfxLibrary lib = VfxLibrary.Current;
            if (lib.TrySpawn(crit ? VfxLibrary.CritSpark : VfxLibrary.HitSpark, pos, Quaternion.identity) != null)
                return;

            float mult = crit ? lib.CritSparkMult : 1f;
            ParticleSystem ps = NewBurst("HitSpark", pos, Quaternion.identity, additive: true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lib.HitSparkLifeSec * 0.6f, lib.HitSparkLifeSec);
            main.startSpeed = new ParticleSystem.MinMaxCurve(lib.HitSparkSpeed * 0.5f, lib.HitSparkSpeed * mult);
            main.startSize = lib.HitSparkSize * mult;
            main.gravityModifier = 0.6f;
            Burst(ps, Mathf.RoundToInt(lib.HitSparkCount * mult));
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.1f;
            FadeColor(ps, Color.white, tint);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.035f;
            r.lengthScale = 1.2f;
            ps.Play();
        }

        public static void DodgeDust(Vector3 pos, Vector3 dir)
        {
            VfxLibrary lib = VfxLibrary.Current;
            Vector3 ground = new(pos.x, GroundY, pos.z);
            Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-dir) : Quaternion.identity;
            if (lib.TrySpawn(VfxLibrary.DodgeDust, ground, rot) != null)
                return;
            Dust("DodgeDust", ground, lib.DodgeDustCount, lib.DodgeDustLifeSec, lib.DodgeDustSize, 2.6f, lib.DustColor);
        }

        public static void FootDust(Vector3 pos, bool boss)
        {
            VfxLibrary lib = VfxLibrary.Current;
            Vector3 ground = new(pos.x, GroundY, pos.z);
            if (lib.TrySpawn(boss ? VfxLibrary.BossStepDust : VfxLibrary.FootDust, ground, Quaternion.identity) != null)
                return;
            float m = boss ? lib.BossStepDustMult : 1f;
            Dust("FootDust", ground, Mathf.RoundToInt(lib.FootDustCount * m), lib.FootDustLifeSec * (boss ? 1.5f : 1f),
                lib.FootDustSize * m, 0.9f * m, lib.DustColor);
        }

        public static void SlamImpact(Vector3 center, float radiusM)
        {
            VfxLibrary lib = VfxLibrary.Current;
            Vector3 ground = new(center.x, GroundY, center.z);
            bool wave = lib.TrySpawn(VfxLibrary.SlamShockwave, ground, Quaternion.identity) != null;
            bool crack = lib.TrySpawn(VfxLibrary.GroundCrack, ground, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)) != null;

            if (!wave)
            {
                var go = new GameObject("SlamShockwave");
                go.transform.position = ground + Vector3.up * 0.02f;
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = Mat(additive: true, textured: false);
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                FxTween.Ring(line, lib.ShockwaveColor, radiusM, lib.ShockwaveWidthM, lib.ShockwaveSec);
                Dust("SlamDust", ground, lib.DodgeDustCount * 2, lib.DodgeDustLifeSec * 1.4f,
                    lib.DodgeDustSize * 1.6f, radiusM * 1.6f, lib.DustColor, ringRadius: radiusM * 0.25f);
            }

            if (!crack)
            {
                var go = new GameObject("GroundCrack");
                go.transform.position = ground;
                go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                go.transform.localScale = Vector3.one * radiusM * 1.3f;
                go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Mat(additive: false, textured: true, crack: true);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                FxTween.Surface(mr, lib.CrackColor, lib.CrackHoldSec, lib.CrackFadeSec);
            }
        }

        public static void FireCone(Vector3 origin, Vector3 forward, float halfAngleDeg, float reachM)
        {
            VfxLibrary lib = VfxLibrary.Current;
            forward.y = 0f;
            Quaternion rot = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward.normalized) : Quaternion.identity;
            if (lib.TrySpawn(VfxLibrary.FireCone, origin, rot) != null)
                return;

            ParticleSystem ps = NewBurst("FireCone", origin, rot, additive: true);
            var main = ps.main;
            main.duration = lib.FlameSec;
            float life = reachM / Mathf.Max(0.1f, lib.FlameSpeed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(lib.FlameSpeed * 0.8f, lib.FlameSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(lib.FlameSize * 0.5f, lib.FlameSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.CeilToInt(lib.FlameRate * life) + 8;
            var em = ps.emission;
            em.rateOverTime = lib.FlameRate;
            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = Mathf.Clamp(halfAngleDeg, 1f, 89f);
            sh.radius = 0.25f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            FadeColor(ps, lib.FlameColorA, lib.FlameColorB);
            ps.Play();
        }

        static void Dust(string name, Vector3 ground, int count, float life, float size, float speed, Color color,
            float ringRadius = 0.25f)
        {
            ParticleSystem ps = NewBurst(name, ground, Quaternion.Euler(-90f, 0f, 0f), additive: false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f;
            Burst(ps, count);
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = ringRadius;
            sh.radiusThickness = 0f;
            var sizeLt = ps.sizeOverLifetime;
            sizeLt.enabled = true;
            sizeLt.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = speed * 0.3f;
            limit.dampen = 0.25f;
            Color end = color;
            end.a = 0f;
            FadeColor(ps, color, end);
            ps.Play();
        }

        static ParticleSystem NewBurst(string name, Vector3 pos, Quaternion rot, bool additive)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, rot);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 64;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = Mat(additive, textured: true);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void Burst(ParticleSystem ps, int count)
        {
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 200)) });
            var main = ps.main;
            main.maxParticles = Mathf.Max(main.maxParticles, count);
        }

        static void FadeColor(ParticleSystem ps, Color from, Color to)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(from.a * 0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static Material Mat(bool additive, bool textured, bool crack = false)
        {
            int key = (additive ? 1 : 0) | (textured ? 2 : 0) | (crack ? 4 : 0);
            if (Materials.TryGetValue(key, out Material m) && m != null)
                return m;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            m = new Material(shader) { name = "FeelVfx_" + key };
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
            if (textured)
            {
                Texture2D tex = crack ? CrackTexture() : DotTexture();
                if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            Materials[key] = m;
            return m;
        }

        static Texture2D DotTexture()
        {
            if (_dot != null)
                return _dot;
            const int size = 32;
            _dot = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "FxDot" };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Clamp01(1f - d);
                _dot.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            _dot.Apply(false, true);
            return _dot;
        }

        /// <summary>Merkezden dışa kırık çizgiler; beyaz maske, renk FxTween'den.</summary>
        static Texture2D CrackTexture()
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
                float ang = (a + (float)rng.NextDouble() * 0.6f) / arms * Mathf.PI * 2f;
                Vector2 p = new(size * 0.5f, size * 0.5f);
                float len = size * (0.28f + (float)rng.NextDouble() * 0.2f);
                float width = 2.4f;
                for (float t = 0f; t < len; t += 1f)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * 0.25f;
                    p += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    float w = width * (1f - t / len) + 0.6f;
                    Stamp(px, size, p, w);
                    if (rng.NextDouble() < 0.03)
                    {
                        float bAng = ang + (rng.NextDouble() < 0.5 ? 0.7f : -0.7f);
                        Vector2 q = p;
                        for (int k = 0; k < len * 0.25f; k++)
                        {
                            q += new Vector2(Mathf.Cos(bAng), Mathf.Sin(bAng));
                            Stamp(px, size, q, w * 0.6f);
                        }
                    }
                }
            }
            _crack.SetPixels(px);
            _crack.Apply(false, true);
            return _crack;
        }

        static void Stamp(Color[] px, int size, Vector2 p, float r)
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
