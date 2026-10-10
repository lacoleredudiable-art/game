using Dovus.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.4: tek mesh kartı + dissolve. Kılıç ATIL → atlas hücre F (kuyruk yayı),
    /// 0,25 s, atılış yolu boyunca. Atlas Resources'tan (4×2); yoksa prosedürel yer tutucu.
    /// </summary>
    public sealed class DragonSilhouetteCardView : MonoBehaviour
    {
        static int _liveCount;
        static bool _warnedMissingAtlas;

        MeshRenderer _mr;
        Material _mat;
        Texture2D _atlas;
        bool _ownsAtlas;
        float _life;
        float _age;
        bool _active;

        public static int LiveCount => _liveCount;

        public int PlayCount { get; private set; }
        public int LastAtlasCell { get; private set; } = -1;
        public float LastLifeSec { get; private set; }
        public bool IsVisible => _active;
        public Material CardMaterial => _mat;

        public void PlayAlongLine(Vector3 from, Vector3 to, in VfxColorRgb color, float lifeSec, int atlasCell)
        {
            if (_liveCount >= VfxPlanDefaults.EjderSiluetEkranMax && !_active)
                return;

            Ensure();
            Vector3 mid = (from + to) * 0.5f;
            mid.y += RuleVfxDefaults.SilhouetteLiftM;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                dir = transform.forward;
            dir.Normalize();
            transform.position = mid;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                * Quaternion.Euler(90f, 0f, 0f);
            float len = Mathf.Clamp(
                Vector3.Distance(from, to),
                RuleVfxDefaults.SilhouetteMinLenM,
                RuleVfxDefaults.SilhouetteWidthM);
            transform.localScale = new Vector3(len, RuleVfxDefaults.SilhouetteHeightM, 1f);

            _life = Mathf.Max(RuleVfxArtDefaults.MeshLifeMin, lifeSec);
            _age = 0f;
            if (!_active)
            {
                _active = true;
                _liveCount++;
            }

            ApplyAtlasCell(atlasCell);
            PlayCount++;
            LastAtlasCell = atlasCell;
            LastLifeSec = _life;
            Color c = new Color(
                color.R * RuleVfxArtDefaults.CoreHdrMult,
                color.G * RuleVfxArtDefaults.CoreHdrMult,
                color.B * RuleVfxArtDefaults.CoreHdrMult,
                1f);
            if (_mat.HasProperty("_CoreColor"))
                _mat.SetColor("_CoreColor", c);
            if (_mat.HasProperty("_Intensity"))
                _mat.SetFloat("_Intensity", color.Intensity);
            if (_mat.HasProperty("_Dissolve"))
                _mat.SetFloat("_Dissolve", 1f);
            _mr.enabled = true;
        }

        void Update()
        {
            if (!_active)
                return;
            _age += Time.deltaTime;
            float u = Mathf.Clamp01(_age / _life);
            float dissolve;
            if (_age < VfxPlanDefaults.EjderDissolveInSec)
                dissolve = 1f - Mathf.Clamp01(_age / VfxPlanDefaults.EjderDissolveInSec);
            else if (u > 1f - VfxPlanDefaults.EjderDissolveOutFrac)
            {
                float outU = (u - (1f - VfxPlanDefaults.EjderDissolveOutFrac))
                    / VfxPlanDefaults.EjderDissolveOutFrac;
                dissolve = outU;
            }
            else
                dissolve = 0f;

            if (_mat != null && _mat.HasProperty("_Dissolve"))
                _mat.SetFloat("_Dissolve", dissolve);

            if (_age >= _life)
                Release();
        }

        void Release()
        {
            if (!_active)
                return;
            _active = false;
            _liveCount = Mathf.Max(0, _liveCount - 1);
            if (_mr != null)
                _mr.enabled = false;
        }

        void OnDisable() => Release();

        void Ensure()
        {
            if (_mr != null)
                return;
            gameObject.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            Shader sh = Shader.Find(RuleVfxDefaults.KorShaderName)
                ?? Shader.Find(RuleVfxDefaults.ParticlesUnlit);
            _mat = new Material(sh);
            _atlas = Resources.Load<Texture2D>(RuleVfxDefaults.SilhouetteAtlasResource);
            _ownsAtlas = _atlas == null;
            if (_ownsAtlas)
            {
                if (!_warnedMissingAtlas)
                {
                    _warnedMissingAtlas = true;
                    Debug.LogWarning("[DragonSilhouette] Resources/" + RuleVfxDefaults.SilhouetteAtlasResource
                        + " yok; yer tutucu atlas kullanılıyor.");
                }
                _atlas = RuleVfxArtDefaults.BuildSilhouetteAtlas();
            }
            if (_mat.HasProperty("_MainTex"))
                _mat.SetTexture("_MainTex", _atlas);
            if (_mat.HasProperty("_DissolveTex"))
                _mat.SetTexture("_DissolveTex", _atlas);
            _mr.sharedMaterial = _mat;
            _mr.enabled = false;
        }

        /// <summary>Hücre 0 = sol üst; Unity UV'si alttan başladığı için satır ters çevrilir.</summary>
        void ApplyAtlasCell(int cell)
        {
            int cols = RuleVfxDefaults.SilhouetteAtlasCols;
            int rows = RuleVfxDefaults.SilhouetteAtlasRows;
            int idx = Mathf.Clamp(cell, 0, cols * rows - 1);
            int col = idx % cols;
            int row = idx / cols;
            if (_mat == null || !_mat.HasProperty("_MainTex"))
                return;
            // Doku ölçeği/ofseti = shader'daki _MainTex_ST (TRANSFORM_TEX).
            _mat.SetTextureScale("_MainTex", new Vector2(1f / cols, 1f / rows));
            _mat.SetTextureOffset("_MainTex", new Vector2(col / (float)cols, (rows - 1 - row) / (float)rows));
        }

        /// <summary>_MainTex_ST = (ölçek x, ölçek y, ofset x, ofset y).</summary>
        public Vector4 MainTexST
        {
            get
            {
                if (_mat == null || !_mat.HasProperty("_MainTex"))
                    return Vector4.zero;
                Vector2 s = _mat.GetTextureScale("_MainTex");
                Vector2 o = _mat.GetTextureOffset("_MainTex");
                return new Vector4(s.x, s.y, o.x, o.y);
            }
        }

        void OnDestroy()
        {
            Release();
            if (_mat != null)
                Destroy(_mat);
            if (_ownsAtlas && _atlas != null)
                Destroy(_atlas);
        }
    }
}
