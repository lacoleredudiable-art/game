using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// O-5.11: boss gövdesi kamera ile karakter arasındayken boss modeli (kökün çizilen gövdesi ve <c>Visual</c>
    /// altındaki mesh'ler) saydam kopyalarla çizilir. Kopyalar URP/Lit (APK'da her varyantıyla paketli, AndroidBuilder
    /// RuntimeShaders): kaynak malzemenin ana dokusu ve rengi, alfa = opaklık. Arada kalmayınca asıl malzemeler döner.
    /// Geçiş süresi belgede yok: anlık.
    /// </summary>
    public sealed class CameraBossFade
    {
        readonly Renderer[] _renderers;
        readonly Material[][] _opaque;
        readonly Material[][] _faded;
        readonly float _opacity;
        bool _isFaded;

        public CameraBossFade(Transform bossRoot, float opacity)
            : this(bossRoot, bossRoot != null ? bossRoot.Find(CameraBossFadeDefaults.ModelChildName) : null, opacity, "boss", "O-5.11")
        {
        }

        /// <summary>
        /// Genel kurulum: kökün kendi çizilen gövdesi ve <paramref name="model"/> altındaki mesh'ler saydamlaşır.
        /// O-5.10 sütunları da bunu kullanır (kök = sütun, model = giydirilmiş görsel çocuk).
        /// </summary>
        public CameraBossFade(Transform root, Transform model, float opacity, string label, string rule)
        {
            _opacity = opacity;
            var renderers = new List<Renderer>();
            Shader lit = Shader.Find(CameraBossFadeDefaults.FadeShaderName);
            if (root == null || lit == null)
            {
                Debug.LogError($"[V4-FALLBACK] CameraBossFade: {label} kökü ya da '{CameraBossFadeDefaults.FadeShaderName}' " +
                               $"yok; {label} arada kalınca saydamlaşmaz ({rule})");
                _renderers = renderers.ToArray();
                _opaque = new Material[0][];
                _faded = new Material[0][];
                return;
            }

            Renderer own = root.GetComponent<Renderer>();
            if (own != null && own.enabled)
                renderers.Add(own);
            if (model != null)
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                {
                    if (r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer))
                        renderers.Add(r);
                }
            }

            _renderers = renderers.ToArray();
            _opaque = new Material[_renderers.Length][];
            _faded = new Material[_renderers.Length][];
            var made = new Dictionary<Material, Material>();
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material[] src = _renderers[i].sharedMaterials;
                _opaque[i] = src;
                _faded[i] = new Material[src.Length];
                for (int k = 0; k < src.Length; k++)
                {
                    if (src[k] == null)
                        continue;
                    if (!made.TryGetValue(src[k], out Material faded))
                    {
                        faded = Faded(src[k], lit, opacity);
                        made[src[k]] = faded;
                    }
                    _faded[i][k] = faded;
                }
            }
            if (_renderers.Length == 0)
                Debug.LogError($"[V4-FALLBACK] CameraBossFade: {label} modelinde çizilen renderer yok; {label} saydamlaşmaz ({rule})");
        }

        public bool IsFaded => _isFaded;

        /// <summary>Çizilen model opaklığı: arada kalınca kural değeri, değilse 1.</summary>
        public float Opacity => _isFaded ? _opacity : 1f;

        public void Apply(bool occluded)
        {
            if (occluded == _isFaded)
                return;
            _isFaded = occluded;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sharedMaterials = occluded ? _faded[i] : _opaque[i];
            }
        }

        static Material Faded(Material src, Shader lit, float opacity)
        {
            var m = new Material(lit) { name = src.name + CameraBossFadeDefaults.FadedNameSuffix };
            foreach (string prop in CameraBossFadeDefaults.SourceTextureProps)
            {
                if (src.HasProperty(prop) && src.GetTexture(prop) != null)
                {
                    m.SetTexture(CameraBossFadeDefaults.BaseMapProp, src.GetTexture(prop));
                    break;
                }
            }
            Color c = src.HasProperty(CameraBossFadeDefaults.BaseColorProp)
                ? src.GetColor(CameraBossFadeDefaults.BaseColorProp)
                : src.HasProperty(CameraBossFadeDefaults.LegacyColorProp) ? src.color : Color.white;
            c.a = opacity;
            m.SetColor(CameraBossFadeDefaults.BaseColorProp, c);
            m.SetFloat(CameraBossFadeDefaults.SurfaceProp, CameraBossFadeDefaults.SurfaceTransparent);
            m.SetFloat(CameraBossFadeDefaults.BlendProp, CameraBossFadeDefaults.BlendAlpha);
            m.SetFloat(CameraBossFadeDefaults.SrcBlendProp, (float)BlendMode.SrcAlpha);
            m.SetFloat(CameraBossFadeDefaults.DstBlendProp, (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat(CameraBossFadeDefaults.SrcBlendAlphaProp, (float)BlendMode.One);
            m.SetFloat(CameraBossFadeDefaults.DstBlendAlphaProp, (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat(CameraBossFadeDefaults.ZWriteProp, CameraBossFadeDefaults.Off);
            m.SetFloat(CameraBossFadeDefaults.AlphaClipProp, CameraBossFadeDefaults.Off);
            m.EnableKeyword(CameraBossFadeDefaults.TransparentKeyword);
            m.DisableKeyword(CameraBossFadeDefaults.AlphaTestKeyword);
            m.SetOverrideTag(CameraBossFadeDefaults.RenderTypeTag, CameraBossFadeDefaults.RenderTypeTransparent);
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }
    }
}
