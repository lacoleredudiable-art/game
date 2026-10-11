using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// Hedef seçme açıkken parmağın altındaki seçilebilir hedefte ince kor halkası:
    /// sahibin renginde, ortak efekt atlasının halka hücresi, ortak VFX_Kor batch'i
    /// (<see cref="EjderVfxBatch"/>; siluet ve harf flaşıyla aynı materyal).
    /// Gösterilirken hafif nabız atar, gizlenince (parmak bırakılınca) kısa sürede söner
    /// (kural-motoru-v4.json slice_vfx.target_ring).
    /// </summary>
    public sealed class RuleEngineV4TargetPickRingView : MonoBehaviour, IEjderVfxQuadSource
    {
        System.Action _tick;
        bool _visible;
        bool _fading;
        float _shownAt;
        float _releasedAt;
        Vector3 _center;
        Vector3 _axisRight;
        Vector3 _axisUp;
        float _half;

        public bool Visible => _visible;
        /// <summary>Gizlendikten sonra sönerken çizilmeye devam ediyor.</summary>
        public bool Fading => _fading;
        public TargetableHost ShownTarget { get; private set; }
        /// <summary>Ortak batch materyali (siluet, harf, halka aynı).</summary>
        public Material RingMaterial => EjderVfxBatch.Instance.SharedMaterial;
        /// <summary>Halka kor rengi (sahip rengi × HDR); köşe rengi = bu × yoğunluk × nabız parlaklığı.</summary>
        public Color CoreColor { get; private set; }
        public RuleEngineV4UvRect RingUv => RuleVfxSettings.Slice.Atlas.RingUv;
        /// <summary>Son çizilen karenin nabız ölçeği, parlaklığı ve alfası.</summary>
        public float DrawScale { get; private set; } = 1f;
        public float DrawBrightness { get; private set; } = 1f;
        public float DrawAlpha { get; private set; }

        /// <summary><paramref name="tick"/> her kare çağrılır (basılı tutma durumunu halka sürer).</summary>
        public static RuleEngineV4TargetPickRingView Create(Color ownerColor, System.Action tick)
        {
            var go = new GameObject("TargetPickRing");
            var view = go.AddComponent<RuleEngineV4TargetPickRingView>();
            view._tick = tick;
            view.Build(ownerColor);
            return view;
        }

        void Update() => _tick?.Invoke();

        void Build(Color ownerColor)
        {
            Color core = ownerColor * RuleVfxArtDefaults.CoreHdrMult;
            core.a = 1;
            CoreColor = core;
            EjderVfxBatch.Register(this);
        }

        public void ShowOnTarget(TargetableHost target)
        {
            if (target == null)
            {
                Hide();
                return;
            }
            Vector3 p = target.transform.position;
            Place(
                new Vector3(p.x, target.GroundY + RuleEngineV4TargetPickRingDefaults.GroundOffsetM, p.z),
                Vector3.up,
                target.MarkerRadius(RuleEngineV4TargetPickRingDefaults.TargetPaddingM));
            ShownTarget = target;
        }

        public void ShowOnSurface(Vector3 point, Vector3 normal)
        {
            Vector3 n = normal.normalized;
            Place(point + n * RuleEngineV4SurfaceMarkDefaults.DecalSurfaceOffsetM, n, RuleEngineV4SurfaceMarkDefaults.DecalRadiusM);
            ShownTarget = null;
        }

        /// <summary>Halka mantıksal olarak hemen kapanır; görüntü <see cref="RuleEngineV4TargetRingFx.ReleaseFadeSec"/> içinde söner.</summary>
        public void Hide()
        {
            ShownTarget = null;
            if (_visible)
            {
                _fading = true;
                _releasedAt = Time.unscaledTime;
            }
            _visible = false;
        }

        void Place(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > RuleEngineV4SurfaceMarkDefaults.VerticalNormalDot
                ? Vector3.forward
                : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(normal, up);
            transform.SetPositionAndRotation(center, rot);
            _center = center;
            _axisRight = rot * Vector3.right;
            _axisUp = rot * Vector3.up;
            _half = RuleVfxSettings.Slice.Atlas.RingQuadHalfM(radius);
            if (!_visible)
                _shownAt = Time.unscaledTime;
            _visible = true;
            _fading = false;
        }

        public void EmitQuads(EjderVfxBatch batch)
        {
            RuleEngineV4TargetRingFx fx = RuleVfxSettings.Slice.TargetRing;
            float now = Time.unscaledTime;
            float alpha;
            float pulseAt;
            if (_visible)
            {
                alpha = 1f;
                pulseAt = now - _shownAt;
            }
            else if (_fading)
            {
                alpha = fx.ReleaseAlpha(now - _releasedAt);
                pulseAt = _releasedAt - _shownAt;
                if (alpha <= 0f)
                    _fading = false;
            }
            else
                alpha = pulseAt = 0f;

            DrawAlpha = alpha;
            if (alpha <= 0f)
                return;
            fx.Pulse(pulseAt, out float scale, out float brightness);
            DrawScale = scale;
            DrawBrightness = brightness;
            Color c = CoreColor * (RuleEngineV4TargetPickRingDefaults.Intensity * brightness);
            c.a = alpha;
            float half = _half * scale;
            batch.AddQuad(_center, _axisRight * half, _axisUp * half, batch.Layout.RingUv, c, 0f);
        }

        void OnDestroy() => EjderVfxBatch.Unregister(this);
    }
}
