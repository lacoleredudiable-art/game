using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// Hedef seçme açıkken parmağın altındaki seçilebilir hedefte ince kor halkası:
    /// sahibin renginde, VFX_Kor malzemesi, işaret decal'ıyla aynı çizgi dili.
    /// </summary>
    public sealed class RuleEngineV4TargetPickRingView : MonoBehaviour
    {
        LineRenderer _ring;
        Material _material;
        Vector3[] _points;
        System.Action _tick;

        public bool Visible => _ring != null && _ring.enabled;
        public TargetableHost ShownTarget { get; private set; }
        public Material RingMaterial => _material;

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
            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.alignment = LineAlignment.TransformZ;
            _ring.widthMultiplier = RuleEngineV4SurfaceMarkDefaults.DecalLineWidthM;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.startColor = Color.white;
            _ring.endColor = Color.white;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _material = new Material(sh);
            Color core = ownerColor * RuleVfxArtDefaults.CoreHdrMult;
            core.a = 1;
            if (_material.HasProperty("_CoreColor"))
                _material.SetColor("_CoreColor", core);
            if (_material.HasProperty("_Intensity"))
                _material.SetFloat("_Intensity", RuleEngineV4TargetPickRingDefaults.Intensity);
            if (_material.HasProperty("_Color"))
                _material.SetColor("_Color", ownerColor);
            _ring.sharedMaterial = _material;
            _points = new Vector3[RuleEngineV4SurfaceMarkDefaults.DecalSegments];
            _ring.enabled = false;
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
            if (_ring != null)
                _ring.enabled = false;
        }

        void Place(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > RuleEngineV4SurfaceMarkDefaults.VerticalNormalDot
                ? Vector3.forward
                : Vector3.up;
            transform.position = center;
            transform.rotation = Quaternion.LookRotation(normal, up);
            int n = _points.Length;
            for (int i = 0; i < n; i++)
            {
                float a = i * (2 * Mathf.PI / n);
                _points[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            }
            _ring.positionCount = n;
            _ring.SetPositions(_points);
            _ring.enabled = true;
        }

        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
