using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public enum MeshTopology { Triangles = 0, Quads = 2, Lines = 3, LineStrip = 4, Points = 5 }
    public enum TextureFormat { Alpha8 = 1, ARGB32 = 5, RGB24 = 3, RGBA32 = 4, RGBAFloat = 20, R8 = 63 }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum SpriteMeshType { FullRect, Tight }
    public enum RenderTextureFormat { ARGB32 = 0, Depth = 1, Default = 7, DefaultHDR = 9 }
    public enum LightType { Spot, Directional, Point, Area, Rectangle = 3, Disc = 4 }
    public enum LightShadows { None, Hard, Soft }
    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }
    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum LineTextureMode { Stretch, Tile, DistributePerSegment, RepeatPerSegment, Static }
    public enum LineAlignment { View, Local, TransformZ = 1 }
    public enum GradientMode { Blend, Fixed, PerceptualBlend }

    [Flags]
    public enum MaterialGlobalIlluminationFlags
    {
        None = 0, RealtimeEmissive = 1, BakedEmissive = 2, EmissiveIsBlack = 4, AnyEmissive = 3,
    }

    public class Mesh : Object
    {
        Vector3[] _vertices = Array.Empty<Vector3>();
        int[] _triangles = Array.Empty<int>();
        Bounds _bounds;

        public Mesh() { }

        public Vector3[] vertices
        {
            get => (Vector3[])_vertices.Clone();
            set
            {
                _vertices = value != null ? (Vector3[])value.Clone() : Array.Empty<Vector3>();
                RecalculateBounds();
            }
        }

        public int[] triangles
        {
            get => (int[])_triangles.Clone();
            set => _triangles = value != null ? (int[])value.Clone() : Array.Empty<int>();
        }

        public Vector3[] normals { get; set; } = Array.Empty<Vector3>();
        public Vector2[] uv { get; set; } = Array.Empty<Vector2>();
        public Vector2[] uv2 { get; set; } = Array.Empty<Vector2>();
        public Color[] colors { get; set; } = Array.Empty<Color>();
        public Color32[] colors32 { get; set; } = Array.Empty<Color32>();
        public Vector4[] tangents { get; set; } = Array.Empty<Vector4>();
        public int vertexCount => _vertices.Length;
        public int subMeshCount { get; set; } = 1;
        public bool isReadable => true;
        public Rendering.IndexFormat indexFormat { get; set; }

        public Bounds bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public void Clear()
        {
            _vertices = Array.Empty<Vector3>();
            _triangles = Array.Empty<int>();
            _bounds = default;
        }

        public void Clear(bool keepVertexLayout) => Clear();

        public void RecalculateBounds()
        {
            if (_vertices.Length == 0)
            {
                _bounds = default;
                return;
            }
            var b = new Bounds(_vertices[0], Vector3.zero);
            for (int i = 1; i < _vertices.Length; i++) b.Encapsulate(_vertices[i]);
            _bounds = b;
        }

        public void RecalculateNormals() { }
        public void RecalculateTangents() { }
        public void MarkDynamic() { }
        public void Optimize() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
        public void SetVertices(List<Vector3> inVertices) => vertices = inVertices.ToArray();
        public void SetVertices(Vector3[] inVertices) => vertices = inVertices;
        public void SetTriangles(int[] tris, int submesh) => triangles = tris;
        public void SetTriangles(List<int> tris, int submesh) => triangles = tris.ToArray();
        public void SetTriangles(int[] tris, int submesh, bool calculateBounds) => triangles = tris;
        public void SetIndices(int[] indices, MeshTopology topology, int submesh) => triangles = indices;
        public void SetNormals(List<Vector3> n) => normals = n.ToArray();
        public void SetUVs(int channel, List<Vector2> uvs) => uv = uvs.ToArray();
        public void SetUVs(int channel, Vector2[] uvs) => uv = uvs;
        public void SetColors(List<Color> c) => colors = c.ToArray();
        public void SetColors(Color[] c) => colors = c;
        public void GetVertices(List<Vector3> result)
        {
            result.Clear();
            result.AddRange(_vertices);
        }

        static readonly Dictionary<PrimitiveType, Mesh> s_prims = new();

        internal static Mesh Primitive(PrimitiveType type)
        {
            if (s_prims.TryGetValue(type, out Mesh m)) return m;
            Vector3 half = type switch
            {
                PrimitiveType.Sphere => new Vector3(0.5f, 0.5f, 0.5f),
                PrimitiveType.Capsule => new Vector3(0.5f, 1f, 0.5f),
                PrimitiveType.Cylinder => new Vector3(0.5f, 1f, 0.5f),
                PrimitiveType.Cube => new Vector3(0.5f, 0.5f, 0.5f),
                PrimitiveType.Plane => new Vector3(5f, 0f, 5f),
                _ => new Vector3(0.5f, 0.5f, 0f),
            };
            m = new Mesh { name = type.ToString() };
            var verts = new List<Vector3>();
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                verts.Add(new Vector3(half.x * x, half.y * y, half.z * z));
            m.vertices = verts.ToArray();
            s_prims[type] = m;
            return m;
        }

        internal static Mesh BuiltinByName(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            if (n.Contains("capsule")) return Primitive(PrimitiveType.Capsule);
            if (n.Contains("sphere")) return Primitive(PrimitiveType.Sphere);
            if (n.Contains("cylinder")) return Primitive(PrimitiveType.Cylinder);
            if (n.Contains("cube")) return Primitive(PrimitiveType.Cube);
            if (n.Contains("plane")) return Primitive(PrimitiveType.Plane);
            if (n.Contains("quad")) return Primitive(PrimitiveType.Quad);
            return null;
        }
    }

    public class MeshFilter : Component
    {
        public Mesh sharedMesh { get; set; }
        public Mesh mesh { get => sharedMesh; set => sharedMesh = value; }
    }

    public class Material : Object
    {
        readonly Dictionary<string, object> _props = new();

        public Material(Shader shader) { this.shader = shader; }
        public Material(Material source)
        {
            if (source == null) return;
            shader = source.shader;
            foreach (var kv in source._props) _props[kv.Key] = kv.Value;
            renderQueue = source.renderQueue;
        }

        public Shader shader { get; set; }
        public int renderQueue { get; set; } = 2000;
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public bool enableInstancing { get; set; }
        public string[] shaderKeywords { get; set; } = Array.Empty<string>();

        public Color color
        {
            get => GetColor("_Color");
            set => SetColor("_Color", value);
        }

        public Texture mainTexture
        {
            get => GetTexture("_MainTex");
            set => SetTexture("_MainTex", value);
        }

        public Vector2 mainTextureScale { get; set; } = Vector2.one;
        public Vector2 mainTextureOffset { get; set; }

        public bool HasProperty(string name) => true;
        public bool HasProperty(int id) => true;
        public void SetColor(string name, Color c) => _props[name] = c;
        public void SetColor(int id, Color c) => _props["#" + id] = c;
        public Color GetColor(string name) => _props.TryGetValue(name, out object v) && v is Color c ? c : Color.white;
        public Color GetColor(int id) => _props.TryGetValue("#" + id, out object v) && v is Color c ? c : Color.white;
        public void SetFloat(string name, float f) => _props[name] = f;
        public void SetFloat(int id, float f) => _props["#" + id] = f;
        public float GetFloat(string name) => _props.TryGetValue(name, out object v) && v is float f ? f : 0f;
        public float GetFloat(int id) => _props.TryGetValue("#" + id, out object v) && v is float f ? f : 0f;
        public void SetInt(string name, int i) => _props[name] = i;
        public void SetInt(int id, int i) => _props["#" + id] = i;
        public void SetInteger(string name, int i) => _props[name] = i;
        public int GetInt(string name) => _props.TryGetValue(name, out object v) && v is int i ? i : 0;
        public void SetVector(string name, Vector4 v) => _props[name] = v;
        public void SetVector(int id, Vector4 v) => _props["#" + id] = v;
        public Vector4 GetVector(string name) => _props.TryGetValue(name, out object v) && v is Vector4 x ? x : default;
        public void SetTexture(string name, Texture t) => _props[name] = t;
        public void SetTexture(int id, Texture t) => _props["#" + id] = t;
        public Texture GetTexture(string name) => _props.TryGetValue(name, out object v) ? v as Texture : null;
        public Texture GetTexture(int id) => _props.TryGetValue("#" + id, out object v) ? v as Texture : null;
        public void SetTextureScale(string name, Vector2 scale) => _props[name + "_scale"] = scale;
        public Vector2 GetTextureScale(string name) =>
            _props.TryGetValue(name + "_scale", out object v) && v is Vector2 s ? s : Vector2.one;
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
        public bool IsKeywordEnabled(string k) => false;
        public void SetOverrideTag(string tag, string val) { }
        public void SetPass(int pass) { }
        public void CopyPropertiesFromMaterial(Material m) { }
    }

    public class MaterialPropertyBlock
    {
        readonly Dictionary<string, object> _props = new();
        public void Clear() => _props.Clear();
        public void SetColor(string name, Color c) => _props[name] = c;
        public void SetColor(int id, Color c) => _props["#" + id] = c;
        public void SetFloat(string name, float f) => _props[name] = f;
        public void SetFloat(int id, float f) => _props["#" + id] = f;
        public void SetVector(string name, Vector4 v) => _props[name] = v;
        public void SetTexture(string name, Texture t) => _props[name] = t;
        public Color GetColor(string name) => _props.TryGetValue(name, out object v) && v is Color c ? c : Color.white;
        public float GetFloat(string name) => _props.TryGetValue(name, out object v) && v is float f ? f : 0f;
        public bool isEmpty => _props.Count == 0;
    }

    public class Shader : Object
    {
        static readonly Dictionary<string, int> s_ids = new();
        public static Shader Find(string name) => new() { name = name };
        public static int PropertyToID(string name)
        {
            if (!s_ids.TryGetValue(name, out int id)) s_ids[name] = id = s_ids.Count + 1;
            return id;
        }
        public static void SetGlobalColor(string name, Color c) { }
        public static void SetGlobalFloat(string name, float f) { }
        public bool isSupported => true;
    }

    public class Texture : Object
    {
        public virtual int width { get; set; }
        public virtual int height { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public FilterMode filterMode { get; set; }
        public int anisoLevel { get; set; }
    }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) : this(width, height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) : this(width, height) { }
        public static Texture2D whiteTexture => new(4, 4);
        public static Texture2D blackTexture => new(4, 4);
        public void SetPixel(int x, int y, Color c) { }
        public void SetPixels(Color[] colors) { }
        public void SetPixels32(Color32[] colors) { }
        public Color GetPixel(int x, int y) => Color.white;
        public Color GetPixelBilinear(float u, float v) => Color.white;
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public bool LoadImage(byte[] data) => true;
    }

    public class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { this.width = width; this.height = height; }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format) : this(width, height, depth) { }
        public bool Create() => true;
        public void Release() { }
        public static RenderTexture active { get; set; }
        public int antiAliasing { get; set; }
    }

    public class Sprite : Object
    {
        public Texture2D texture { get; private set; }
        public Rect rect { get; private set; }
        public Vector2 pivot { get; private set; }
        public float pixelsPerUnit { get; private set; } = 100f;
        public Bounds bounds => new(Vector3.zero, new Vector3(rect.width / pixelsPerUnit, rect.height / pixelsPerUnit, 0f));

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) =>
            new() { texture = texture, rect = rect, pivot = pivot };
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) =>
            new() { texture = texture, rect = rect, pivot = pivot, pixelsPerUnit = pixelsPerUnit };
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude) =>
            Create(texture, rect, pivot, pixelsPerUnit);
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType type) =>
            Create(texture, rect, pivot, pixelsPerUnit);
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType type, Vector4 border) =>
            Create(texture, rect, pivot, pixelsPerUnit);
    }

    public class Renderer : Component
    {
        Material _material;
        public bool enabled { get; set; } = true;
        public bool isVisible => enabled;
        public bool receiveShadows { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public int sortingOrder { get; set; }
        public int sortingLayerID { get; set; }
        public string sortingLayerName { get; set; } = "Default";
        public Rendering.LightProbeUsage lightProbeUsage { get; set; }
        public Rendering.ReflectionProbeUsage reflectionProbeUsage { get; set; }
        public bool allowOcclusionWhenDynamic { get; set; }

        public Material sharedMaterial
        {
            get => _material;
            set => _material = value;
        }

        public Material material
        {
            get => _material ??= new Material((Shader)null);
            set => _material = value;
        }

        public Material[] sharedMaterials
        {
            get => _material != null ? new[] { _material } : Array.Empty<Material>();
            set => _material = value != null && value.Length > 0 ? value[0] : null;
        }

        public Material[] materials
        {
            get => new[] { material };
            set => _material = value != null && value.Length > 0 ? value[0] : null;
        }

        public virtual Bounds bounds => new(transform.position, Vector3.zero);
        public Bounds localBounds => new(Vector3.zero, Vector3.zero);

        public void GetPropertyBlock(MaterialPropertyBlock block) { }
        public void SetPropertyBlock(MaterialPropertyBlock block) { }
        public void GetPropertyBlock(MaterialPropertyBlock block, int index) { }
        public void SetPropertyBlock(MaterialPropertyBlock block, int index) { }
    }

    public class MeshRenderer : Renderer
    {
        public override Bounds bounds
        {
            get
            {
                Mesh m = gameObject.GetComponent<MeshFilter>()?.sharedMesh;
                if (m == null) return new Bounds(transform.position, Vector3.zero);
                return TransformBounds(m.bounds, transform.localToWorldMatrix);
            }
        }

        internal static Bounds TransformBounds(Bounds local, Matrix4x4 m)
        {
            Vector3 c = m.MultiplyPoint3x4(local.center);
            Vector3 e = local.extents;
            Vector3 ax = m.MultiplyVector(new Vector3(e.x, 0f, 0f));
            Vector3 ay = m.MultiplyVector(new Vector3(0f, e.y, 0f));
            Vector3 az = m.MultiplyVector(new Vector3(0f, 0f, e.z));
            Vector3 w = new(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
            return new Bounds(c, w * 2f);
        }
    }

    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh { get; set; }
        public Transform rootBone { get; set; }
        public Transform[] bones { get; set; } = Array.Empty<Transform>();
        public bool updateWhenOffscreen { get; set; }
        public void BakeMesh(Mesh mesh) => BakeMesh(mesh, false);
        public void BakeMesh(Mesh mesh, bool useScale)
        {
            if (sharedMesh != null) mesh.vertices = sharedMesh.vertices;
        }
        public override Bounds bounds =>
            sharedMesh != null ? MeshRenderer.TransformBounds(sharedMesh.bounds, transform.localToWorldMatrix) : base.bounds;
    }

    public class LineRenderer : Renderer
    {
        Vector3[] _positions = Array.Empty<Vector3>();

        public int positionCount
        {
            get => _positions.Length;
            set
            {
                int n = Math.Max(0, value);
                Array.Resize(ref _positions, n);
            }
        }

        public bool useWorldSpace { get; set; } = true;
        public bool loop { get; set; }
        public float startWidth { get; set; } = 1f;
        public float endWidth { get; set; } = 1f;
        public float widthMultiplier { get; set; } = 1f;
        public AnimationCurve widthCurve { get; set; } = new();
        public Color startColor { get; set; } = Color.white;
        public Color endColor { get; set; } = Color.white;
        public Gradient colorGradient { get; set; } = new();
        public int numCapVertices { get; set; }
        public int numCornerVertices { get; set; }
        public LineTextureMode textureMode { get; set; }
        public LineAlignment alignment { get; set; }
        public bool generateLightingData { get; set; }

        public void SetPosition(int index, Vector3 position)
        {
            if (index >= 0 && index < _positions.Length) _positions[index] = position;
        }

        public Vector3 GetPosition(int index) => index >= 0 && index < _positions.Length ? _positions[index] : Vector3.zero;

        public void SetPositions(Vector3[] positions)
        {
            int n = Math.Min(positions.Length, _positions.Length);
            Array.Copy(positions, _positions, n);
        }

        public int GetPositions(Vector3[] positions)
        {
            int n = Math.Min(positions.Length, _positions.Length);
            Array.Copy(_positions, positions, n);
            return n;
        }

        public override Bounds bounds
        {
            get
            {
                if (_positions.Length == 0) return base.bounds;
                Vector3 p0 = useWorldSpace ? _positions[0] : transform.TransformPoint(_positions[0]);
                var b = new Bounds(p0, Vector3.zero);
                for (int i = 1; i < _positions.Length; i++)
                    b.Encapsulate(useWorldSpace ? _positions[i] : transform.TransformPoint(_positions[i]));
                return b;
            }
        }
    }

    public class TrailRenderer : Renderer
    {
        public float time { get; set; } = 5f;
        public float startWidth { get; set; } = 1f;
        public float endWidth { get; set; } = 1f;
        public float widthMultiplier { get; set; } = 1f;
        public AnimationCurve widthCurve { get; set; } = new();
        public Color startColor { get; set; } = Color.white;
        public Color endColor { get; set; } = Color.white;
        public Gradient colorGradient { get; set; } = new();
        public float minVertexDistance { get; set; } = 0.1f;
        public bool emitting { get; set; } = true;
        public bool autodestruct { get; set; }
        public int numCapVertices { get; set; }
        public int numCornerVertices { get; set; }
        public LineTextureMode textureMode { get; set; }
        public LineAlignment alignment { get; set; }
        public int positionCount => 0;
        public void Clear() { }
    }

    public class Light : Behaviour
    {
        public LightType type { get; set; } = LightType.Point;
        public Color color { get; set; } = Color.white;
        public float intensity { get; set; } = 1f;
        public float range { get; set; } = 10f;
        public float spotAngle { get; set; } = 30f;
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; } = 1f;
        public float shadowBias { get; set; }
        public float shadowNormalBias { get; set; }
        public LightRenderMode renderMode { get; set; }
        public int cullingMask { get; set; } = -1;
        public float bounceIntensity { get; set; } = 1f;
    }

}
