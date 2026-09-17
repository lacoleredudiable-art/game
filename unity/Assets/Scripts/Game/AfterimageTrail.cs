using System.Collections.Generic;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>Dodge hayalet kopyaları — FeelTuning.AfterimageCount / AfterimageLifeMs (§8).</summary>
    public sealed class AfterimageTrail : MonoBehaviour
    {
        struct Ghost
        {
            public Transform Xform;
            public Renderer Rend;
            public float DieAtUnscaled;
        }

        readonly List<Ghost> _live = new();
        readonly Queue<Ghost> _pool = new();

        // Her kare her hayalet için yeni blok tahsis etmek 7×60 ≈ 420 alloc/sn ediyordu (T8.1).
        // Alan başlatıcısı OLAMAZ: statik kurucu MonoBehaviour ctor'undan tetiklenirse Unity
        // MaterialPropertyBlock yaratmaya izin vermiyor. Tembel kurulum.
        static MaterialPropertyBlock _block;

        FeelTuning _feel;
        PrototypeTuning _colors;
        Material _mat;
        float _alpha = 0.55f;
        int _countOverride = -1;

        /// <summary>Ulti afterimage_count — &lt;0 ise FeelTuning.AfterimageCount.</summary>
        public int CountOverride
        {
            get => _countOverride;
            set => _countOverride = value;
        }

        public int Count
        {
            get
            {
                if (_countOverride >= 0)
                    return _countOverride;
                return _feel != null ? _feel.AfterimageCount : 0;
            }
        }

        public void Bind(FeelTuning feel, PrototypeTuning colors)
        {
            _feel = feel;
            _colors = colors;
            _alpha = colors.AfterimageAlpha;
            _mat = MakeMat(colors.PlayerColor, _alpha);
        }

        public void Emit(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            int cap = Count;
            if (_feel == null || cap <= 0)
                return;

            while (_live.Count >= cap)
                Retire(0);

            Ghost g = _pool.Count > 0 ? _pool.Dequeue() : CreateGhost();
            g.Xform.position = position;
            g.Xform.rotation = rotation;
            g.Xform.localScale = scale;
            g.Xform.gameObject.SetActive(true);
            g.DieAtUnscaled = Time.unscaledTime + _feel.AfterimageLifeMs / 1000f;
            SetAlpha(g.Rend, _alpha);
            _live.Add(g);
        }

        public void Clear()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
                Retire(i);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            float life = _feel != null ? _feel.AfterimageLifeMs / 1000f : 0.32f;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Ghost g = _live[i];
                float left = g.DieAtUnscaled - now;
                if (left <= 0f)
                {
                    Retire(i);
                    continue;
                }

                SetAlpha(g.Rend, _alpha * Mathf.Clamp01(left / life));
            }
        }

        void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
        }

        Ghost CreateGhost()
        {
            var go = new GameObject("Afterimage");
            go.transform.SetParent(transform.parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Capsule);
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Ghost { Xform = go.transform, Rend = rend };
        }

        void Retire(int index)
        {
            Ghost g = _live[index];
            _live.RemoveAt(index);
            g.Xform.gameObject.SetActive(false);
            _pool.Enqueue(g);
        }

        static void SetAlpha(Renderer rend, float a)
        {
            Material mat = rend != null ? rend.sharedMaterial : null;
            if (mat == null)
                return;

            bool baseColor = mat.HasProperty("_BaseColor");
            Color c = baseColor ? mat.GetColor("_BaseColor") : mat.color;
            c.a = a;
            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            _block.SetColor(baseColor ? "_BaseColor" : "_Color", c);
            rend.SetPropertyBlock(_block);
        }

        static Material MakeMat(Color color, float alpha)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            color.a = alpha;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
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
    }
}
