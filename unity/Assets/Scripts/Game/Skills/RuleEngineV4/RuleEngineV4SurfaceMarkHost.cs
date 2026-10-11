using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// MarkOnly yüzeydeki sabit işaret: çarpma noktasında, çapalı (itilmez, hedeflenmez).
    /// Görünüm: yüzey normaline hizalı halka decal, sahibin renginde.
    /// </summary>
    public sealed class RuleEngineV4SurfaceMarkHost : MonoBehaviour
    {
        Material _material;

        public RuleEngineV4MarkOnlySurfaceHost Surface { get; private set; }
        public Vector3 Normal { get; private set; }
        public Color OwnerColor { get; private set; }
        public LineRenderer Decal { get; private set; }

        public void Configure(RuleEngineV4MarkOnlySurfaceHost surface, Vector3 normal, Color ownerColor)
        {
            Surface = surface;
            Normal = normal.normalized;
            OwnerColor = ownerColor;
            BuildDecal();
        }

        void BuildDecal()
        {
            var go = new GameObject("SurfaceMarkDecal");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position + Normal * RuleEngineV4SurfaceMarkDefaults.DecalSurfaceOffsetM;
            Vector3 up = Mathf.Abs(Vector3.Dot(Normal, Vector3.up)) > RuleEngineV4SurfaceMarkDefaults.VerticalNormalDot
                ? Vector3.forward
                : Vector3.up;
            go.transform.rotation = Quaternion.LookRotation(Normal, up);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.alignment = LineAlignment.TransformZ;
            lr.widthMultiplier = RuleEngineV4SurfaceMarkDefaults.DecalLineWidthM;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            _material = new Material(Shader.Find(RuleEngineV4SurfaceMarkDefaults.DecalShader));
            lr.sharedMaterial = _material;
            lr.startColor = OwnerColor;
            lr.endColor = OwnerColor;

            int n = RuleEngineV4SurfaceMarkDefaults.DecalSegments;
            float r = RuleEngineV4SurfaceMarkDefaults.DecalRadiusM;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * (2f * Mathf.PI / n);
                pts[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }
            lr.positionCount = n;
            lr.SetPositions(pts);
            Decal = lr;
        }

        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
