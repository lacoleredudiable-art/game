using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// efekt-motoru §3.4: tek kart + dissolve. Kılıç ATIL → atlas hücre F (kuyruk yayı),
    /// 0,25 s, atılış yolu boyunca. Kart ortak efekt atlasından (siluet bölgesi 4×2) ve ortak
    /// VFX_Kor batch'inden çizilir (<see cref="EjderVfxBatchHost"/>); kendi materyali yok.
    /// </summary>
    public sealed class DragonSilhouetteCardView : MonoBehaviour, IEjderVfxQuadSource
    {
        static int _liveCount;

        Vector3 _center;
        Vector3 _halfRight;
        Vector3 _halfUp;
        Color _color;
        float _dissolve;
        float _life;
        float _fadeIn;
        float _fadeOut;
        float _age;
        bool _active;
        EjderVfxBatchHost _batch;

        public static int LiveCount => _liveCount;

        public int PlayCount { get; private set; }
        public int LastAtlasCell { get; private set; } = -1;
        public float LastLifeSec { get; private set; }
        public bool IsVisible => _active;
        /// <summary>Ortak batch materyali (siluet, harf, halka aynı).</summary>
        public Material CardMaterial => _batch != null ? _batch.SharedMaterial : null;
        /// <summary>Son oynatılan hücrenin ortak atlastaki UV'si.</summary>
        public RuleEngineV4UvRect CellUv { get; private set; }

        /// <summary>Son oynatmanın belirme / sönme süresi (sn).</summary>
        public float LastFadeInSec { get; private set; }
        public float LastFadeOutSec { get; private set; }
        /// <summary>0 = tam görünür, 1 = tamamen erimiş.</summary>
        public float Dissolve => _dissolve;
        /// <summary>Kart dörtgeninin dünya köşeleri (ekran kaplama ölçümü).</summary>
        public void GetCorners(Vector3[] corners4)
        {
            corners4[0] = _center - _halfRight - _halfUp;
            corners4[1] = _center + _halfRight - _halfUp;
            corners4[2] = _center + _halfRight + _halfUp;
            corners4[3] = _center - _halfRight + _halfUp;
        }

        /// <summary>
        /// fadeInSec / fadeOutSec ≤ 0 ise varsayılan (EjderDissolveInSec, ömrün EjderDissolveOutFrac'ı).
        /// Ekranda en çok <see cref="VfxPlanDefaults.EjderSiluetEkranMax"/> kart; dolu ise yenisi doğmaz.
        /// </summary>
        public void PlayAlongLine(
            Vector3 from,
            Vector3 to,
            in VfxColorRgb color,
            float lifeSec,
            int atlasCell,
            float fadeInSec = 0f,
            float fadeOutSec = 0f)
        {
            if (_liveCount >= VfxPlanDefaults.EjderSiluetEkranMax && !_active)
                return;

            Vector3 mid = (from + to) * 0.5f;
            mid.y += RuleVfxDefaults.SilhouetteLiftM;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < RuleVfxDefaults.PathSampleEpsSq)
                dir = transform.forward;
            dir.Normalize();
            // Yatay kart: yerel X yola dik, yerel Y yol boyunca (eski Quad + Euler(90,0,0) ile aynı).
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float len = Mathf.Clamp(
                Vector3.Distance(from, to),
                RuleVfxDefaults.SilhouetteMinLenM,
                RuleVfxDefaults.SilhouetteWidthM);
            _center = mid;
            _halfRight = rot * Vector3.right * (len * 0.5f);
            _halfUp = rot * Vector3.up * (RuleVfxDefaults.SilhouetteHeightM * 0.5f);

            _life = Mathf.Max(RuleVfxArtDefaults.MeshLifeMin, lifeSec);
            _fadeIn = fadeInSec > 0f ? fadeInSec : VfxPlanDefaults.EjderDissolveInSec;
            _fadeOut = fadeOutSec > 0f ? fadeOutSec : _life * VfxPlanDefaults.EjderDissolveOutFrac;
            float fadeSum = _fadeIn + _fadeOut;
            if (fadeSum > _life)
            {
                _fadeIn *= _life / fadeSum;
                _fadeOut *= _life / fadeSum;
            }
            LastFadeInSec = _fadeIn;
            LastFadeOutSec = _fadeOut;
            _age = 0f;
            if (!_active)
            {
                _active = true;
                _liveCount++;
            }

            CellUv = RuleVfxSettings.Slice.Atlas.SilhouetteUv(atlasCell);
            PlayCount++;
            LastAtlasCell = atlasCell;
            LastLifeSec = _life;
            float k = RuleVfxArtDefaults.CoreHdrMult * color.Intensity;
            _color = new Color(color.R * k, color.G * k, color.B * k, 1f);
            _dissolve = 1f;
        }

        void Update()
        {
            if (!_active)
                return;
            _age += Time.deltaTime;
            float fadeOutStart = _life - _fadeOut;
            if (_age < _fadeIn)
                _dissolve = 1f - Mathf.Clamp01(_age / _fadeIn);
            else if (_age > fadeOutStart)
                _dissolve = Mathf.Clamp01((_age - fadeOutStart) / Mathf.Max(_fadeOut, RuleVfxArtDefaults.MeshLifeMin));
            else
                _dissolve = 0f;

            if (_age >= _life)
                Release();
        }

        public void EmitQuads(EjderVfxBatchHost batch)
        {
            if (!_active)
                return;
            batch.AddQuad(_center, _halfRight, _halfUp, CellUv, _color, _dissolve);
        }

        public void Bind(EjderVfxBatchHost batch)
        {
            if (_batch != null)
                _batch.Unregister(this);
            _batch = batch;
            if (_batch != null)
                _batch.Register(this);
        }

        void Release()
        {
            if (!_active)
                return;
            _active = false;
            _liveCount = Mathf.Max(0, _liveCount - 1);
        }

        void OnDisable() => Release();

        void OnDestroy()
        {
            Release();
            if (_batch != null)
                _batch.Unregister(this);
        }
    }
}
