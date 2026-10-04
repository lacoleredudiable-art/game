using Dovus.Game.Vfx;
using System;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Boss
{
    public enum TelegraphShape
    {
        Circle,
        Cone,
        Line
    }

    /// <summary>
    /// Yere çizilen uyarı: daire, koni ya da şerit. Süre dolunca <see cref="Completed"/>.
    /// Dodgeable false ise i-frame bu vuruşu yutmaz (çağıran hasar kapısına iletir).
    /// </summary>
    public sealed class AttackTelegraph : MonoBehaviour
    {
        const float HeightY = 0.04f;

        TelegraphShape _shape = TelegraphShape.Circle;
        float _duration = AttackTelegraphDefaults.DefaultDurationSec;
        float _radius = AttackTelegraphDefaults.DefaultRadiusM;
        float _length = AttackTelegraphDefaults.DefaultLengthM;
        float _width = AttackTelegraphDefaults.DefaultWidthM;
        float _arcHalf = AttackTelegraphDefaults.DefaultArcHalfDeg;
        float _age;
        bool _playing;
        bool _completed;
        Vector3 _origin;
        Vector3 _forward = Vector3.forward;

        Transform _outline;
        Transform _fill;
        Material _mat;
        Mesh _coneMesh;
        static Texture2D _glowTex;

        public bool Dodgeable { get; private set; } = true;
        public float Fill01 { get; private set; }
        public event Action<AttackTelegraph> Completed;

        public void Begin(
            Vector3 origin,
            Vector3 forward,
            TelegraphShape shape,
            float durationSec,
            bool dodgeable,
            float radiusM,
            float lengthM,
            float widthM,
            float arcHalfDeg)
        {
            _origin = origin;
            _origin.y = HeightY;
            forward.y = 0f;
            _forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            _shape = shape;
            _duration = Mathf.Max(AttackTelegraphDefaults.MinDurationSec, durationSec);
            Dodgeable = dodgeable;
            _radius = Mathf.Max(AttackTelegraphDefaults.MinShapeDimensionM, radiusM);
            _length = Mathf.Max(AttackTelegraphDefaults.MinShapeDimensionM, lengthM);
            _width = Mathf.Max(AttackTelegraphDefaults.MinShapeDimensionM, widthM);
            _arcHalf = Mathf.Clamp(arcHalfDeg, AttackTelegraphDefaults.ArcHalfMinDeg, AttackTelegraphDefaults.ArcHalfMaxDeg);
            _age = 0f;
            Fill01 = 0f;
            _playing = true;
            _completed = false;
            EnsureVisuals();
            ApplyFill(AttackTelegraphDefaults.InitialFillScale);
        }

        void Update()
        {
            if (!_playing)
                return;
            _age += Time.deltaTime;
            float u = Mathf.Clamp01(_age / _duration);
            Fill01 = u;
            ApplyFill(u);
            if (u < 1f || _completed)
                return;
            _completed = true;
            _playing = false;
            Completed?.Invoke(this);
            HideSoon();
        }

        void EnsureVisuals()
        {
            if (_mat == null)
                _mat = MakeMat();
            if (_outline == null)
                _outline = MakeBody("TelegraphOutline");
            if (_fill == null)
                _fill = MakeBody("TelegraphFill");
        }

        Transform MakeBody(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = _mat;
            filter.sharedMesh = _shape == TelegraphShape.Cone ? ConeMesh() : ShapeMesh();
            return go.transform;
        }

        void ApplyFill(float u)
        {
            if (_outline == null || _fill == null)
                return;
            Quaternion rot = Quaternion.LookRotation(_forward, Vector3.up);
            _outline.SetPositionAndRotation(_origin, rot);
            _fill.SetPositionAndRotation(_origin, rot);
            Vector3 full = FullScale();
            _outline.localScale = full;
            Vector3 fill = full;
            if (_shape == TelegraphShape.Line)
                fill.z = Mathf.Max(AttackTelegraphDefaults.FillProgressMin, full.z * u);
            else
                fill = full * Mathf.Max(AttackTelegraphDefaults.FillProgressMin, u);
            _fill.localScale = fill;
            // ff-4: "büyük solid turuncu disk çok yüksek sesliydi" — dolum alfası 0.85'e kadar
            // çıkıyordu. Dolum/çevre artık ~0.25-0.35 tavanlı, yarıçap kenarında yumuşak/parlak
            // ince bir "rim" (bkz. GlowTexture) ile okunabilir kalır.
            Paint(_outline, new Color(1f, 0.35f, 0.12f, 0.26f));
            Paint(_fill, Color.Lerp(new Color(1f, 0.72f, 0.2f, 0.22f), new Color(1f, 0.15f, 0.08f, 0.34f), u));
        }

        Vector3 FullScale()
        {
            switch (_shape)
            {
                case TelegraphShape.Line:
                    return new Vector3(_width, 1f, _length);
                case TelegraphShape.Cone:
                    return new Vector3(_radius, 1f, _radius);
                default:
                    return new Vector3(_radius * 2f, 1f, _radius * 2f);
            }
        }

        Mesh ShapeMesh()
        {
            if (_shape == TelegraphShape.Line)
                return QuadMesh();
            return DiscMesh();
        }

        static Mesh DiscMesh()
        {
            const int seg = 28;
            var mesh = new Mesh { name = "TelegraphDisc" };
            var verts = new Vector3[seg + 1];
            var uvs = new Vector2[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f); // merkez: GlowTexture'da d=0
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(a);
                float sz = Mathf.Sin(a);
                verts[i + 1] = new Vector3(cx * 0.5f, 0f, sz * 0.5f);
                // Kenar (d=1): GlowTexture'ın merkeze göre 0.5 uzaklığı — parlak ince rim orada.
                uvs[i + 1] = new Vector2(cx * 0.5f + 0.5f, sz * 0.5f + 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i == seg - 1 ? 1 : i + 2;
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh QuadMesh()
        {
            var mesh = new Mesh { name = "TelegraphLine" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 1f),
                new Vector3(-0.5f, 0f, 1f)
            };
            // Şerit radyal değil — GlowTexture'ın düz merkez bölgesini (d=0, taban alfa) örnekler.
            mesh.uv = new[]
            {
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        Mesh ConeMesh()
        {
            if (_coneMesh == null)
                _coneMesh = new Mesh { name = "TelegraphCone" };
            const int seg = 16;
            var verts = new Vector3[seg + 2];
            var uvs = new Vector2[seg + 2];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            float half = _arcHalf * Mathf.Deg2Rad;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)seg);
                float x = Mathf.Sin(a);
                float z = Mathf.Cos(a);
                verts[i + 1] = new Vector3(x, 0f, z);
                // Birim yarıçap köşeleri: GlowTexture'da d=1 (rim).
                uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
            }
            for (int i = 0; i < seg; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            _coneMesh.Clear();
            _coneMesh.vertices = verts;
            _coneMesh.uv = uvs;
            _coneMesh.triangles = tris;
            _coneMesh.RecalculateNormals();
            return _coneMesh;
        }

        void Paint(Transform body, Color color)
        {
            var rend = body.GetComponent<MeshRenderer>();
            if (rend == null || rend.sharedMaterial == null)
                return;
            // O11: her karede renk değişir; rend.material telegraf başına 2 örnek sızdırıyordu → property block.
            SharedTint.Paint(rend, color);
        }

        static Material MakeMat()
        {
            Shader shader = AssetLoader.FindShader("Sprites/Default", null);
            if (shader == null)
                shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null);
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            mat.mainTexture = GlowTexture();
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", GlowTexture());
            return mat;
        }

        /// <summary>
        /// ff-4: dolgu alfası tavanlandı ama tek düz renk hâlâ "büyük solid disk" gibi okunur —
        /// kenara yakın parlak ince bir rim + merkeze ve dış kenara doğru yumuşak sönme (64×64,
        /// URP güvenli — Shader.Find yok, mevcut Sprites/Default/Unlit materyaline uygulanır).
        /// </summary>
        static Texture2D GlowTexture()
        {
            if (_glowTex != null)
                return _glowTex;
            const int size = 64;
            const float half = size * 0.5f;
            _glowTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "TelegraphGlow"
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a;
                if (d <= AttackTelegraphDefaults.GlowAlphaDistanceInner)
                    a = AttackTelegraphDefaults.GlowAlphaFloor;
                else if (d <= AttackTelegraphDefaults.GlowAlphaDistanceMid)
                    a = Mathf.Lerp(AttackTelegraphDefaults.GlowAlphaFloor, 1f, (d - AttackTelegraphDefaults.GlowAlphaDistanceInner) / AttackTelegraphDefaults.GlowAlphaMidSpan);
                else
                    a = Mathf.Lerp(1f, AttackTelegraphDefaults.GlowAlphaTail, Mathf.Clamp01((d - AttackTelegraphDefaults.GlowAlphaDistanceMid) / AttackTelegraphDefaults.GlowAlphaTailSpan));
                if (d > 1f)
                    a = 0f;
                _glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            _glowTex.Apply(false, true);
            return _glowTex;
        }

        void HideSoon()
        {
            if (_fill != null)
                _fill.gameObject.SetActive(false);
            Destroy(gameObject, AttackTelegraphDefaults.FadeDestroyDelaySec);
        }

        void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
            if (_coneMesh != null)
                Destroy(_coneMesh);
        }
    }
}
