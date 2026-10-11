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
    /// </summary>
    public sealed class RuleEngineV4TargetPickRingView : MonoBehaviour, IEjderVfxQuadSource
    {
        System.Action _tick;
        bool _visible;
        Vector3 _center;
        Vector3 _halfRight;
        Vector3 _halfUp;

        public bool Visible => _visible;
        public TargetableHost ShownTarget { get; private set; }
        /// <summary>Ortak batch materyali (siluet, harf, halka aynı).</summary>
        public Material RingMaterial => EjderVfxBatch.Instance.SharedMaterial;
        /// <summary>Halka kor rengi (sahip rengi × HDR); köşe rengi = bu × yoğunluk.</summary>
        public Color CoreColor { get; private set; }
        public RuleEngineV4UvRect RingUv => RuleVfxSettings.Slice.Atlas.RingUv;

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

        public void Hide()
        {
            ShownTarget = null;
            _visible = false;
        }

        void Place(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > RuleEngineV4SurfaceMarkDefaults.VerticalNormalDot
                ? Vector3.forward
                : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(normal, up);
            float half = RuleVfxSettings.Slice.Atlas.RingQuadHalfM(radius);
            transform.SetPositionAndRotation(center, rot);
            _center = center;
            _halfRight = rot * Vector3.right * half;
            _halfUp = rot * Vector3.up * half;
            _visible = true;
        }

        public void EmitQuads(EjderVfxBatch batch)
        {
            if (!_visible)
                return;
            Color c = CoreColor * RuleEngineV4TargetPickRingDefaults.Intensity;
            c.a = 1f;
            batch.AddQuad(_center, _halfRight, _halfUp, batch.Layout.RingUv, c, 0f);
        }

        void OnDestroy() => EjderVfxBatch.Unregister(this);
    }
}
