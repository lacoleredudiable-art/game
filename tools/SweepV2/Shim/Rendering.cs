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

    public class Camera : Behaviour
    {
        static readonly List<Camera> s_cameras = new();

        internal static void Register(Camera c) => s_cameras.Add(c);

        public static Camera main
        {
            get
            {
                foreach (Camera c in s_cameras)
                {
                    if (c == null || !c.isActiveAndEnabled) continue;
                    if (c.gameObject.CompareTag("MainCamera")) return c;
                }
                return null;
            }
        }

        public static Camera current => main;
        public static Camera[] allCameras => s_cameras.Where(c => c != null && c.isActiveAndEnabled).ToArray();
        public static int allCamerasCount => allCameras.Length;

        public float fieldOfView { get; set; } = 60f;
        public float nearClipPlane { get; set; } = 0.3f;
        public float farClipPlane { get; set; } = 1000f;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; } = 5f;
        public float depth { get; set; }
        public int cullingMask { get; set; } = -1;
        public CameraClearFlags clearFlags { get; set; } = CameraClearFlags.Skybox;
        public Color backgroundColor { get; set; }
        public bool allowHDR { get; set; }
        public bool allowMSAA { get; set; }
        public RenderTexture targetTexture { get; set; }
        public Rect rect { get; set; } = new(0, 0, 1, 1);
        public float aspect => (float)Screen.width / Screen.height;
        public int pixelWidth => Screen.width;
        public int pixelHeight => Screen.height;
        public bool useOcclusionCulling { get; set; }
        public Rect pixelRect => new(0, 0, Screen.width, Screen.height);

        public Vector3 WorldToScreenPoint(Vector3 position)
        {
            Vector3 v = WorldToViewportPoint(position);
            return new Vector3(v.x * Screen.width, v.y * Screen.height, v.z);
        }

        public Vector3 WorldToViewportPoint(Vector3 position)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            if (local.z <= 0.0001f) return new Vector3(0.5f, 0.5f, local.z);
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float y = local.y / (local.z * tan);
            float x = local.x / (local.z * tan * aspect);
            return new Vector3(x * 0.5f + 0.5f, y * 0.5f + 0.5f, local.z);
        }

        public Vector3 ScreenToWorldPoint(Vector3 position) => transform.position + transform.forward * position.z;
        public Vector3 ViewportToWorldPoint(Vector3 position) => transform.position + transform.forward * position.z;
        public Vector3 ScreenToViewportPoint(Vector3 position) => new(position.x / Screen.width, position.y / Screen.height, position.z);
        public Vector3 ViewportToScreenPoint(Vector3 position) => new(position.x * Screen.width, position.y * Screen.height, position.z);

        public Ray ScreenPointToRay(Vector3 pos)
        {
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float x = (pos.x / Screen.width * 2f - 1f) * tan * aspect;
            float y = (pos.y / Screen.height * 2f - 1f) * tan;
            Vector3 dir = transform.TransformDirection(new Vector3(x, y, 1f));
            return new Ray(transform.position, dir);
        }

        public Ray ViewportPointToRay(Vector3 pos) =>
            ScreenPointToRay(new Vector3(pos.x * Screen.width, pos.y * Screen.height, pos.z));

        public void Render() { }
        public void ResetAspect() { }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public float planeDistance { get; set; } = 100f;
        public int sortingOrder { get; set; }
        public bool overrideSorting { get; set; }
        public bool pixelPerfect { get; set; }
        public float scaleFactor { get; set; } = 1f;
        public float referencePixelsPerUnit { get; set; } = 100f;
        public string sortingLayerName { get; set; } = "Default";
        public Canvas rootCanvas => this;
        public bool isRootCanvas => true;
        public static void ForceUpdateCanvases() { }
    }

    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; } = 1f;
        public bool blocksRaycasts { get; set; } = true;
        public bool interactable { get; set; } = true;
        public bool ignoreParentGroups { get; set; }
    }

    public class CanvasRenderer : Component
    {
        public void SetAlpha(float a) { }
        public float GetAlpha() => 1f;
        public bool cullTransparentMesh { get; set; }
    }

    public class RectTransform : Transform
    {
        public enum Edge { Left, Right, Top, Bottom }
        public enum Axis { Horizontal, Vertical }

        public Vector2 anchorMin { get; set; } = new(0.5f, 0.5f);
        public Vector2 anchorMax { get; set; } = new(0.5f, 0.5f);
        public Vector2 pivot { get; set; } = new(0.5f, 0.5f);
        public Vector2 sizeDelta { get; set; } = new(100f, 100f);

        public Vector2 anchoredPosition
        {
            get => new(localPosition.x, localPosition.y);
            set => localPosition = new Vector3(value.x, value.y, localPosition.z);
        }

        public Vector3 anchoredPosition3D
        {
            get => localPosition;
            set => localPosition = value;
        }

        public Vector2 offsetMin
        {
            get => anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            set
            {
                Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }

        public Vector2 offsetMax
        {
            get => anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            set
            {
                Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }

        public Rect rect => new(-pivot.x * sizeDelta.x, -pivot.y * sizeDelta.y, sizeDelta.x, sizeDelta.y);

        public void SetSizeWithCurrentAnchors(Axis axis, float size)
        {
            Vector2 s = sizeDelta;
            if (axis == Axis.Horizontal) s.x = size; else s.y = size;
            sizeDelta = s;
        }

        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }

        public void GetWorldCorners(Vector3[] fourCornersArray)
        {
            Rect r = rect;
            fourCornersArray[0] = TransformPoint(new Vector3(r.xMin, r.yMin));
            fourCornersArray[1] = TransformPoint(new Vector3(r.xMin, r.yMax));
            fourCornersArray[2] = TransformPoint(new Vector3(r.xMax, r.yMax));
            fourCornersArray[3] = TransformPoint(new Vector3(r.xMax, r.yMin));
        }

        public void GetLocalCorners(Vector3[] fourCornersArray)
        {
            Rect r = rect;
            fourCornersArray[0] = new Vector3(r.xMin, r.yMin);
            fourCornersArray[1] = new Vector3(r.xMin, r.yMax);
            fourCornersArray[2] = new Vector3(r.xMax, r.yMax);
            fourCornersArray[3] = new Vector3(r.xMax, r.yMin);
        }

        public void ForceUpdateRectTransforms() { }
    }

    public static class RectTransformUtility
    {
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint) => false;
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) => false;
        public static Vector2 WorldToScreenPoint(Camera cam, Vector3 worldPoint) =>
            cam != null ? (Vector2)cam.WorldToScreenPoint(worldPoint) : new Vector2(worldPoint.x, worldPoint.y);
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = screenPoint;
            return true;
        }
        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = screenPoint;
            return true;
        }
    }

    [Serializable]
    public class Gradient
    {
        public GradientColorKey[] colorKeys { get; set; } = { new(Color.white, 0f), new(Color.white, 1f) };
        public GradientAlphaKey[] alphaKeys { get; set; } = { new(1f, 0f), new(1f, 1f) };
        public GradientMode mode { get; set; }

        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            this.colorKeys = colorKeys;
            this.alphaKeys = alphaKeys;
        }

        public Color Evaluate(float time)
        {
            Color c = Color.white;
            if (colorKeys != null && colorKeys.Length > 0) c = colorKeys[0].color;
            if (colorKeys != null)
            {
                for (int i = 1; i < colorKeys.Length; i++)
                {
                    if (time <= colorKeys[i].time)
                    {
                        GradientColorKey a = colorKeys[i - 1], b = colorKeys[i];
                        float t = Mathf.InverseLerp(a.time, b.time, time);
                        c = Color.Lerp(a.color, b.color, t);
                        break;
                    }
                    c = colorKeys[i].color;
                }
            }
            return c;
        }
    }

    public struct GradientColorKey
    {
        public Color color;
        public float time;
        public GradientColorKey(Color col, float time) { color = col; this.time = time; }
    }

    public struct GradientAlphaKey
    {
        public float alpha;
        public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }

    public struct Keyframe
    {
        public float time, value, inTangent, outTangent;
        public Keyframe(float time, float value) { this.time = time; this.value = value; inTangent = 0f; outTangent = 0f; }
        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            this.time = time; this.value = value; this.inTangent = inTangent; this.outTangent = outTangent;
        }
    }

    [Serializable]
    public class AnimationCurve
    {
        public Keyframe[] keys { get; set; } = Array.Empty<Keyframe>();
        public AnimationCurve() { }
        public AnimationCurve(params Keyframe[] keys) { this.keys = keys ?? Array.Empty<Keyframe>(); }
        public int length => keys.Length;
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd) =>
            new(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd));
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd) =>
            new(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd));
        public static AnimationCurve Constant(float timeStart, float timeEnd, float value) =>
            new(new Keyframe(timeStart, value), new Keyframe(timeEnd, value));
        public int AddKey(float time, float value)
        {
            var list = keys.ToList();
            list.Add(new Keyframe(time, value));
            keys = list.OrderBy(k => k.time).ToArray();
            return keys.Length - 1;
        }
        public float Evaluate(float time)
        {
            if (keys.Length == 0) return 0f;
            if (time <= keys[0].time) return keys[0].value;
            for (int i = 1; i < keys.Length; i++)
            {
                if (time <= keys[i].time)
                    return Mathf.Lerp(keys[i - 1].value, keys[i].value, Mathf.InverseLerp(keys[i - 1].time, keys[i].time, time));
            }
            return keys[keys.Length - 1].value;
        }
    }

    public class GUISkin : Object
    {
        public GUIStyle box { get; set; } = new();
        public GUIStyle label { get; set; } = new();
        public GUIStyle button { get; set; } = new();
        public GUIStyle toggle { get; set; } = new();
        public GUIStyle textField { get; set; } = new();
        public GUIStyle window { get; set; } = new();
    }

    public class GUIStyleState
    {
        public Color textColor { get; set; }
        public Texture2D background { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public GUIStyleState normal { get; set; } = new();
        public GUIStyleState hover { get; set; } = new();
        public GUIStyleState active { get; set; } = new();
        public TextAnchor alignment { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public RectOffset padding { get; set; } = new();
        public RectOffset margin { get; set; } = new();
        public Font font { get; set; }
        public float fixedHeight { get; set; }
        public float fixedWidth { get; set; }
        public bool stretchWidth { get; set; }
        public Vector2 CalcSize(GUIContent content) => new(100f, 20f);
    }

    public class GUIContent
    {
        public string text;
        public GUIContent() { }
        public GUIContent(string text) { this.text = text; }
    }

    public class GUILayoutOption { }

    public static class GUI
    {
        public static GUISkin skin { get; set; } = new();
        public static Color color { get; set; } = Color.white;
        public static Color backgroundColor { get; set; } = Color.white;
        public static Color contentColor { get; set; } = Color.white;
        public static bool enabled { get; set; } = true;
        public static int depth { get; set; }
        public static Matrix4x4 matrix { get; set; } = Matrix4x4.identity;
        public static void Box(Rect r, string text) { }
        public static void Box(Rect r, string text, GUIStyle style) { }
        public static void Box(Rect r, GUIContent c) { }
        public static void Label(Rect r, string text) { }
        public static void Label(Rect r, string text, GUIStyle style) { }
        public static void Label(Rect r, GUIContent c, GUIStyle style) { }
        public static bool Button(Rect r, string text) => false;
        public static bool Button(Rect r, string text, GUIStyle style) => false;
        public static bool Toggle(Rect r, bool value, string text) => value;
        public static bool Toggle(Rect r, bool value, string text, GUIStyle style) => value;
        public static float HorizontalSlider(Rect r, float value, float left, float right) => value;
        public static string TextField(Rect r, string text) => text;
        public static void DrawTexture(Rect r, Texture t) { }
    }

    public static class GUILayout
    {
        public static void BeginArea(Rect r) { }
        public static void BeginArea(Rect r, GUIStyle style) { }
        public static void BeginArea(Rect r, string text, GUIStyle style) { }
        public static void EndArea() { }
        public static void BeginHorizontal(params GUILayoutOption[] o) { }
        public static void BeginHorizontal(GUIStyle s, params GUILayoutOption[] o) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] o) { }
        public static void BeginVertical(GUIStyle s, params GUILayoutOption[] o) { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 p, params GUILayoutOption[] o) => p;
        public static void EndScrollView() { }
        public static void Label(string text, params GUILayoutOption[] o) { }
        public static void Label(string text, GUIStyle s, params GUILayoutOption[] o) { }
        public static bool Button(string text, params GUILayoutOption[] o) => false;
        public static bool Button(string text, GUIStyle s, params GUILayoutOption[] o) => false;
        public static bool Toggle(bool value, string text, params GUILayoutOption[] o) => value;
        public static bool Toggle(bool value, string text, GUIStyle s, params GUILayoutOption[] o) => value;
        public static string TextField(string text, params GUILayoutOption[] o) => text;
        public static string TextField(string text, GUIStyle s, params GUILayoutOption[] o) => text;
        public static float HorizontalSlider(float value, float l, float r, params GUILayoutOption[] o) => value;
        public static void Space(float px) { }
        public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float w) => new();
        public static GUILayoutOption Height(float h) => new();
        public static GUILayoutOption MinWidth(float w) => new();
        public static GUILayoutOption MaxWidth(float w) => new();
        public static GUILayoutOption ExpandWidth(bool e) => new();
    }

    // ---------------------------------------------------------------- ses

    public class AudioClip : Object
    {
        public float length { get; private set; }
        public int samples { get; private set; }
        public int channels { get; private set; } = 1;
        public int frequency { get; private set; } = 44100;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) =>
            new() { name = name, samples = lengthSamples, channels = channels, frequency = frequency, length = (float)lengthSamples / frequency };
        public bool SetData(float[] data, int offsetSamples) => true;
        public bool GetData(float[] data, int offsetSamples) => true;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; } = 1f;
        public float pitch { get; set; } = 1f;
        public bool loop { get; set; }
        public bool playOnAwake { get; set; } = true;
        public float spatialBlend { get; set; }
        public bool mute { get; set; }
        public float time { get; set; }
        public int priority { get; set; } = 128;
        public float minDistance { get; set; } = 1f;
        public float maxDistance { get; set; } = 500f;
        public float dopplerLevel { get; set; } = 1f;
        public bool isPlaying => false;
        public void Play() { }
        public void Play(ulong delay) { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) { }
    }

    public class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
    }

    public class AudioLowPassFilter : Behaviour
    {
        public float cutoffFrequency { get; set; } = 5000f;
    }
}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
}
