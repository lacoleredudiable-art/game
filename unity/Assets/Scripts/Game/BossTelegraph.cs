using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// YERE ÇAKMA telegrafı: hazırlık pozu + büyüyen yer diski + yükselen ses (§11).
    /// Renk yalnızca §10 kırmızı-turuncu.
    ///
    /// Disk boss transform'unun ÇOCUĞU DEĞİL (T8.1): bossun (1.7, 1.3, 1.7) ölçeği ve
    /// hazırlık squash'ı diski çarpıyor, 5.4 m'lik etki yarıçapı ekranda ~7.5–9.2 m
    /// görünüyordu — telegraf hacim hakkında yalan söylüyordu. Mesh de Quad (kare) değil
    /// artık Cylinder: yerde yatan gerçek bir daire, yarıçapı doğrudan okunuyor.
    /// </summary>
    public sealed class BossTelegraph : MonoBehaviour
    {
        const float DiscHeightY = 0.03f;
        const float DiscThicknessScale = 0.02f;

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
            Hide();
        }

        /// <summary>Windup: p 0→1. Disk §11'in gerçek etki yarıçapını (5.4 m) gösterir.</summary>
        public void SetProgress(float progress01)
        {
            float p = Mathf.Clamp01(progress01);
            DrawDisc(
                _boss.RadiusM * Mathf.Max(0.12f, p),
                Color.Lerp(_colors.TelegraphWarm, _colors.TelegraphHot, p),
                0.35f + 0.5f * p);

            // Hazırlık: yukarı gerilip çakmaya hazır — silüet, sayı değil.
            ApplyPose(1f - _colors.TelegraphSquash * p, 1f + _colors.TelegraphStretch * p);

            if (_tone != null)
            {
                if (!_tone.isPlaying)
                    _tone.Play();
                _tone.pitch = Mathf.Lerp(_colors.TelegraphTonePitchMin, _colors.TelegraphTonePitchMax, p);
                _tone.volume = Mathf.Lerp(_colors.TelegraphToneVolumeMin, _colors.TelegraphToneVolumeMax, p);
            }
        }

        /// <summary>
        /// Çakma anı. Gerilmiş poz vururken kalmamalı — aşağı squash (T8.1); eskiden aktif
        /// pencere boyunca boss hâlâ "hazırlanıyor" pozundaydı.
        /// </summary>
        public void Slam()
        {
            DrawDisc(_boss.RadiusM, _colors.TelegraphHot, 0.9f);
            ApplySlamPose(1f);
            if (_tone != null && _tone.isPlaying)
                _tone.Stop();
        }

        /// <summary>Toparlanma: t 1→0. Poz tabana döner, disk söner.</summary>
        public void Recover(float t01)
        {
            float t = Mathf.Clamp01(t01);
            if (t <= 0.02f)
            {
                Hide();
                return;
            }

            DrawDisc(_boss.RadiusM, _colors.TelegraphHot, 0.5f * t);
            ApplySlamPose(t);
        }

        public void Hide()
        {
            if (_disc != null && _disc.gameObject.activeSelf)
                _disc.gameObject.SetActive(false);
            ApplyPose(1f, 1f);
            if (_tone != null && _tone.isPlaying)
                _tone.Stop();
        }

        void OnDestroy()
        {
            if (_disc != null)
                Destroy(_disc.gameObject);
            if (_discMat != null)
                Destroy(_discMat);
            if (_clip != null)
                Destroy(_clip);
        }

        void DrawDisc(float radiusM, Color color, float alpha)
        {
            if (_disc == null || _bossXform == null)
                return;

            if (!_disc.gameObject.activeSelf)
                _disc.gameObject.SetActive(true);

            _disc.position = new Vector3(_bossXform.position.x, DiscHeightY, _bossXform.position.z);
            _disc.localScale = new Vector3(radiusM * 2f, DiscThicknessScale, radiusM * 2f);
            Color c = color;
            c.a = alpha;
            SetMatColor(_discMat, c);
        }

        void ApplyPose(float xz, float y)
        {
            if (_bossXform == null)
                return;

            _bossXform.localScale = new Vector3(_baseScale.x * xz, _baseScale.y * y, _baseScale.z * xz);
        }

        void ApplySlamPose(float t)
        {
            float squash = _colors.TelegraphSlamSquash * t;
            ApplyPose(1f + squash * 0.5f, 1f - squash);
        }

        void BuildDisc()
        {
            var go = new GameObject("SlamDisc");
            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Cylinder);
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            _discMat = MakeDiscMat();
            rend.sharedMaterial = _discMat;
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
                data[i] = 0.35f * Mathf.Sin(2f * Mathf.PI * hz * t);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
