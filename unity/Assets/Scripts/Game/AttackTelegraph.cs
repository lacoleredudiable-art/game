using System;
using UnityEngine;

namespace Dovus.Game
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
        float _duration = 0.7f;
        float _radius = 3.2f;
        float _length = 7f;
        float _width = 1.4f;
        float _arcHalf = 40f;
        float _age;
        bool _playing;
        bool _completed;
        Vector3 _origin;
        Vector3 _forward = Vector3.forward;

        Transform _outline;
        Transform _fill;
        Material _mat;
        Mesh _coneMesh;

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
            _duration = Mathf.Max(0.05f, durationSec);
            Dodgeable = dodgeable;
            _radius = Mathf.Max(0.2f, radiusM);
            _length = Mathf.Max(0.2f, lengthM);
            _width = Mathf.Max(0.2f, widthM);
            _arcHalf = Mathf.Clamp(arcHalfDeg, 5f, 170f);
            _age = 0f;
            Fill01 = 0f;
            _playing = true;
            _completed = false;
            EnsureVisuals();
            ApplyFill(0.02f);
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
                fill.z = Mathf.Max(0.05f, full.z * u);
            else
                fill = full * Mathf.Max(0.05f, u);
            _fill.localScale = fill;
            Paint(_outline, new Color(1f, 0.35f, 0.12f, 0.28f));
            Paint(_fill, Color.Lerp(new Color(1f, 0.72f, 0.2f, 0.45f), new Color(1f, 0.15f, 0.08f, 0.85f), u));
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
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i == seg - 1 ? 1 : i + 2;
            }
            mesh.vertices = verts;
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
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            float half = _arcHalf * Mathf.Deg2Rad;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)seg);
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (int i = 0; i < seg; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            _coneMesh.Clear();
            _coneMesh.vertices = verts;
            _coneMesh.triangles = tris;
            _coneMesh.RecalculateNormals();
            return _coneMesh;
        }

        void Paint(Transform body, Color color)
        {
            var rend = body.GetComponent<MeshRenderer>();
            if (rend == null || rend.sharedMaterial == null)
                return;
            Material mat = rend.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }

        static Material MakeMat()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return mat;
        }

        void HideSoon()
        {
            if (_fill != null)
                _fill.gameObject.SetActive(false);
            Destroy(gameObject, 0.18f);
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
