using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>Ortak atlas batch'ine her kare dörtgen yazan efekt (siluet, harf flaşı, hedef halkası).</summary>
    public interface IEjderVfxQuadSource
    {
        void EmitQuads(EjderVfxBatch batch);
    }

    /// <summary>
    /// Kuyruk silueti + harf flaşı + hedef halkası: tek dünya-uzayı mesh, tek VFX_Kor materyali
    /// (Resources <see cref="RuleEngineV4VfxAtlas.Material"/>), tek atlas → tek draw call.
    /// Renk/yoğunluk köşe renginde, dissolve UV1.x'te (shader _VERTEX_DISSOLVE).
    /// Kamera ve gövdeler yerleştikten sonra (LateUpdate sonu) kurulur.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class EjderVfxBatch : MonoBehaviour
    {
        static EjderVfxBatch _instance;
        static bool _warnedMissingMaterial;

        readonly List<IEjderVfxQuadSource> _sources = new();
        readonly List<Vector3> _verts = new();
        readonly List<Vector2> _uv0 = new();
        readonly List<Vector2> _uv1 = new();
        readonly List<Color> _colors = new();
        readonly List<int> _tris = new();
        Mesh _mesh;
        MeshRenderer _mr;
        Material _mat;
        RuleEngineV4VfxAtlas _layout;

        public static EjderVfxBatch Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Create();
                return _instance;
            }
        }

        /// <summary>Var olan batch (yoksa null; oluşturmaz).</summary>
        public static EjderVfxBatch Current => _instance;

        public Material SharedMaterial => _mat;
        public Texture Atlas => _mat != null ? _mat.mainTexture : null;
        public RuleEngineV4VfxAtlas Layout => _layout;
        public MeshRenderer Renderer => _mr;
        public Mesh BatchMesh => _mesh;
        /// <summary>Son kurulan dörtgen sayısı.</summary>
        public int QuadCount { get; private set; }

        public static void Register(IEjderVfxQuadSource source)
        {
            EjderVfxBatch b = Instance;
            if (!b._sources.Contains(source))
                b._sources.Add(source);
        }

        public static void Unregister(IEjderVfxQuadSource source)
        {
            if (_instance != null)
                _instance._sources.Remove(source);
        }

        static EjderVfxBatch Create()
        {
            var go = new GameObject("EjderVfxBatch");
            var b = go.AddComponent<EjderVfxBatch>();
            b.Build();
            return b;
        }

        void Build()
        {
            _layout = RuleVfxSettings.Slice.Atlas;
            _mesh = new Mesh { name = "EjderVfxBatchMesh" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mr.lightProbeUsage = LightProbeUsage.Off;
            _mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _mat = Resources.Load<Material>(_layout.Material);
            if (_mat == null && !_warnedMissingMaterial)
            {
                _warnedMissingMaterial = true;
                Debug.LogError("[EjderVfxBatch] Resources/" + _layout.Material
                    + " yok; ortak efekt materyali kurulmadan siluet/harf/halka çizilmez (Dovus/Vfx/Ortak atlas materyali).");
            }
            _mr.sharedMaterial = _mat;
            _mr.enabled = false;
        }

        /// <summary>Merkez + yarı kenar vektörleri (sağ, yukarı) ile dörtgen.</summary>
        public void AddQuad(Vector3 center, Vector3 halfRight, Vector3 halfUp, in RuleEngineV4UvRect uv, Color color, float dissolve)
        {
            AddQuad(
                center - halfRight - halfUp,
                center - halfRight + halfUp,
                center + halfRight + halfUp,
                center + halfRight - halfUp,
                uv,
                color,
                dissolve);
        }

        /// <summary>Köşeler sol alt, sol üst, sağ üst, sağ alt sırasıyla; UV aynı köşelere.</summary>
        public void AddQuad(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, in RuleEngineV4UvRect uv, Color color, float dissolve)
        {
            int i = _verts.Count;
            _verts.Add(bl);
            _verts.Add(tl);
            _verts.Add(tr);
            _verts.Add(br);
            _uv0.Add(new Vector2(uv.X, uv.Y));
            _uv0.Add(new Vector2(uv.X, uv.YMax));
            _uv0.Add(new Vector2(uv.XMax, uv.YMax));
            _uv0.Add(new Vector2(uv.XMax, uv.Y));
            var d = new Vector2(dissolve, 0f);
            for (int k = 0; k < 4; k++)
            {
                _uv1.Add(d);
                _colors.Add(color);
            }
            _tris.Add(i);
            _tris.Add(i + 1);
            _tris.Add(i + 2);
            _tris.Add(i);
            _tris.Add(i + 2);
            _tris.Add(i + 3);
        }

        void LateUpdate() => Rebuild();

        /// <summary>Kaynaklardan dörtgenleri toplar, mesh'i tek seferde yükler.</summary>
        public void Rebuild()
        {
            _verts.Clear();
            _uv0.Clear();
            _uv1.Clear();
            _colors.Clear();
            _tris.Clear();
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                IEjderVfxQuadSource s = _sources[i];
                if (s == null || (s is Object o && o == null))
                {
                    _sources.RemoveAt(i);
                    continue;
                }
                s.EmitQuads(this);
            }

            QuadCount = _verts.Count / 4;
            _mesh.Clear();
            if (QuadCount > 0)
            {
                _mesh.SetVertices(_verts);
                _mesh.SetUVs(0, _uv0);
                _mesh.SetUVs(1, _uv1);
                _mesh.SetColors(_colors);
                _mesh.SetTriangles(_tris, 0, true);
            }
            _mr.enabled = QuadCount > 0 && _mat != null;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (_mesh != null)
                Destroy(_mesh);
        }
    }
}
