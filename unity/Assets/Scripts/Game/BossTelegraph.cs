using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// YERE ÇAKMA telegrafı: hazırlık pozu + büyüyen yer göstergesi + yükselen ses (§11).
    /// Renk yalnızca §10 kırmızı-turuncu.
    /// </summary>
    public sealed class BossTelegraph : MonoBehaviour
    {
        PrototypeTuning _colors;
        BossTuning _boss;
        Transform _bossXform;
        Transform _disc;
        Material _discMat;
        AudioSource _tone;
        AudioClip _clip;
        Vector3 _baseScale;

        public void Bind(PrototypeTuning colors, BossTuning boss, Transform bossXform)
        {
            _colors = colors;
            _boss = boss;
            _bossXform = bossXform;
            _baseScale = bossXform.localScale;
            BuildDisc();
            BuildTone();
            SetProgress(0f, show: false);
        }

        public void SetProgress(float progress01, bool show)
        {
            float p = Mathf.Clamp01(progress01);
            if (_disc != null)
                _disc.gameObject.SetActive(show);

            if (!show)
            {
                if (_bossXform != null)
                    _bossXform.localScale = _baseScale;
                if (_tone != null && _tone.isPlaying)
                    _tone.Stop();
                return;
            }

            float radius = _boss.RadiusM * Mathf.Max(0.12f, p);
            _disc.position = new Vector3(_bossXform.position.x, 0.03f, _bossXform.position.z);
            _disc.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            Color hot = _colors.TelegraphHot;
            Color warm = _colors.TelegraphWarm;
            Color c = Color.Lerp(warm, hot, p);
            c.a = 0.35f + 0.5f * p;
            SetMatColor(_discMat, c);

            // Hazırlık: yukarı gerilip çakmaya hazır — squash/stretch, sayı değil.
            float stretch = 1f + 0.28f * p;
            float squash = 1f - 0.18f * p;
            _bossXform.localScale = new Vector3(
                _baseScale.x * squash,
                _baseScale.y * stretch,
                _baseScale.z * squash);

            if (_tone != null)
            {
                if (!_tone.isPlaying)
                    _tone.Play();
                _tone.pitch = Mathf.Lerp(0.55f, 1.8f, p);
                _tone.volume = 0.12f + 0.28f * p;
            }
        }

        public void SlamFlash()
        {
            if (_disc == null)
                return;
            _disc.gameObject.SetActive(true);
            SetMatColor(_discMat, new Color(_colors.TelegraphHot.r, _colors.TelegraphHot.g, _colors.TelegraphHot.b, 0.9f));
            _disc.localScale = new Vector3(_boss.RadiusM * 2.1f, _boss.RadiusM * 2.1f, 1f);
            if (_tone != null)
                _tone.Stop();
        }

        public void Hide()
        {
            SetProgress(0f, show: false);
        }

        void OnDestroy()
        {
            if (_discMat != null)
                Destroy(_discMat);
            if (_clip != null)
                Destroy(_clip);
        }

        void BuildDisc()
        {
            var go = new GameObject("SlamDisc");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Quad);
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _discMat = MakeDiscMat();
            rend.sharedMaterial = _discMat;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _disc = go.transform;
        }

        void BuildTone()
        {
            _tone = gameObject.AddComponent<AudioSource>();
            _tone.playOnAwake = false;
            _tone.loop = true;
            _tone.spatialBlend = 0.35f;
            _clip = MakeTone(220);
            _tone.clip = _clip;
        }

        Material MakeDiscMat()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            SetMatColor(mat, _colors.TelegraphWarm);
            return mat;
        }

        static void SetMatColor(Material mat, Color c)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
        }

        static AudioClip MakeTone(int hz)
        {
            const int sampleRate = 44100;
            int samples = sampleRate;
            var clip = AudioClip.Create("TelegraphTone", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 0.35f;
                data[i] = env * Mathf.Sin(2f * Mathf.PI * hz * t);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
