using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Vfx;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class LivingEffectView
    {
        void DrawBasicStrike(EffectSilhouette s, float alpha)
        {
            _ = s;
            _ = alpha;
            _line.positionCount = 0;
            _lineBaseWidth = _colors.Visuals.EffectLineWidthDefaultM;

            if (_needle == null)
                return;

            Vector3 dir = new Vector3(_logic.DirX, 0f, _logic.DirZ);
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;

            float dist = Mathf.Min(_logic.TipDistance, _tuning.BasicStrikeRangeM);
            float y = _colors.Visuals.EffectBasicStrikeHeightM;
            Vector3 origin = new Vector3(_logic.OriginX, y, _logic.OriginZ);
            // Uç, kısa menzilin ortasına yakın — "tek vuruşluk jab", uçan iğne değil.
            Vector3 tip = origin + dir * Mathf.Max(LivingEffectViewDefaults.TipMinDistM, dist * LivingEffectViewDefaults.TipDistAlongMult);

            _needle.gameObject.SetActive(true);
            _needle.position = tip;
            _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float thick = _colors.Visuals.EffectBasicStrikeThickM;
            float len = _colors.Visuals.EffectBasicStrikeLenM;
            if (_logic.Phase == LivingEffectPhase.Banging)
                len *= 1f + LivingEffectViewDefaults.BangLenWobbleMult * Mathf.Sin(_logic.BangAgeSec * LivingEffectViewDefaults.BangLenWobbleFreqHz);
            _needle.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        void DrawWave(Vector3 origin, Vector3 dir, float radius, EffectSilhouette s, float y)
        {
            if (_logic.Verb != Rune.Toprak && s.Focus < _colors.Visuals.EffectShowMinFocus && _logic.Verb != Rune.Hava && _logic.Verb != Rune.Karanlik)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.Visuals.EffectLineWidthDefaultM;
                return;
            }

            // İĞNE fiilinde ana gövde iğne; dalga çizgisi yok
            if (_logic.Verb == Rune.Ates && s.Spread < _colors.Visuals.EffectIgneShowMinSpread)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.Visuals.EffectLineWidthDefaultM;
                return;
            }

            // SÜRÜ: cephe çizgisi yok — dağınık bulut blobs ile okunur.
            if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && s.Focus < _colors.Visuals.EffectFocusSwarmAlongLineMin)
            {
                _line.positionCount = 0;
                _lineBaseWidth = _colors.Visuals.EffectLineWidthDefaultM;
                return;
            }

            float focus = s.Focus;
            float baseWidth = _colors.Visuals.EffectLineWidthDefaultM;
            if (focus < _colors.Visuals.EffectFocusRingMax)
            {
                // Halka
                _line.loop = true;
                _line.positionCount = RingSegments;
                for (int i = 0; i < RingSegments; i++)
                {
                    float a = (i / (float)RingSegments) * Mathf.PI * 2f;
                    Vector3 p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    p.y = y;
                    _line.SetPosition(i, p);
                }
            }
            else if (focus < _colors.Visuals.EffectFocusArcMax)
            {
                // Yaya daralma — boss yönüne doğru koridor
                float halfArc = Mathf.Lerp(Mathf.PI, LivingEffectViewDefaults.FocusRingMinArcRad, (focus - _colors.Visuals.EffectFocusRingMax) / (_colors.Visuals.EffectFocusArcMax - _colors.Visuals.EffectFocusRingMax));
                float facing = Mathf.Atan2(dir.x, dir.z);
                int segs = LivingEffectViewDefaults.FocusRingSegmentCount;
                _line.loop = false;
                _line.positionCount = segs;
                for (int i = 0; i < segs; i++)
                {
                    float t = i / (segs - 1f);
                    float a = facing - halfArc + t * (halfArc * 2f);
                    Vector3 p = origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
                    p.y = y;
                    _line.SetPosition(i, p);
                }
            }
            else
            {
                // Tek hat (fay hattı)
                _line.loop = false;
                _line.positionCount = 2;
                Vector3 tip = origin + dir * radius;
                tip.y = y;
                _line.SetPosition(0, origin);
                _line.SetPosition(1, tip);
                baseWidth = Mathf.Lerp(_colors.Visuals.EffectLineWidthWideM, _colors.Visuals.EffectLineWidthNarrowM, s.Pierce);
            }

            if (_logic.Verb == Rune.Toprak)
            {
                baseWidth = Mathf.Lerp(_colors.Visuals.EffectSarsintiWidthWideM, _colors.Visuals.EffectSarsintiWidthNarrowM, focus);
                // Kütle: geniş halka daha kalın okunur.
                baseWidth *= Mathf.Lerp(1f, _colors.Visuals.EffectSarsintiMassWidthMul, 1f - focus);
            }

            // Taban her karede burada baştan hesaplanır (birikmez) — PulseBang bunun üstüne
            // çarpar, DrawWave'in kendisi asla çarpımı miras almaz.
            _lineBaseWidth = baseWidth;
            _line.widthMultiplier = baseWidth;
        }

        void DrawNeedle(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s, float travel01)
        {
            bool show = _logic.Verb == Rune.Ates || _logic.Verb == Rune.Aydinlik
                || s.Pierce > _colors.Visuals.EffectPierceNeedleShowMin;
            if (!show || _needle == null)
            {
                if (_needle != null) _needle.gameObject.SetActive(false);
                HideNeedleGhosts();
                return;
            }

            _needle.gameObject.SetActive(true);
            float thick = Mathf.Lerp(_colors.Visuals.EffectNeedleThickWideM, _colors.Visuals.EffectNeedleThickNarrowM, s.Pierce);
            float len = _colors.Visuals.EffectNeedleLenBaseM + _colors.Visuals.EffectNeedleLenPerPierceM * s.Pierce;

            if (_logic.Verb == Rune.Ates || _logic.Verb == Rune.Aydinlik)
            {
                DrawIgneZenitsu(origin, dir, dist, thick, len, travel01);
                return;
            }

            // Sıfat olarak iğne: uca oturur (5-1 hattı vb.)
            HideNeedleGhosts();
            Vector3 tip = origin + dir * dist;
            _needle.position = tip;
            if (dir.sqrMagnitude > 1e-4f)
                _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            _needle.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        /// <summary>
        /// Özet §6 Zenitsu küçük hâli: gerilme → 2–3 kare gidiş → donmuş varış.
        /// </summary>
        void DrawIgneZenitsu(
            Vector3 origin,
            Vector3 dir,
            float dist,
            float thick,
            float len,
            float travel01)
        {
            float windup = Mathf.Max(SkillsTimeDefaults.MinPositiveSec, _tuning.NeedleWindupSec);
            float age = _logic.AgeSec;
            bool arrived = travel01 >= LivingEffectViewDefaults.NeedleArrivedTravelThreshold || _logic.Travel >= _logic.MaxRange - LivingEffectViewDefaults.NeedleArrivedRangeMarginM;

            if (age < windup)
            {
                // Gerilme: kökte uzar, yerinde — henüz fırlamadı.
                HideNeedleGhosts();
                float t = age / windup;
                float stretch = Mathf.Lerp(1f, _colors.Visuals.EffectNeedleWindupLenMul, t);
                _needle.position = origin + dir * (len * LivingEffectViewDefaults.NeedleWindupAlongMult * t);
                if (dir.sqrMagnitude > 1e-4f)
                    _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                float thin = thick * Mathf.Lerp(1f, LivingEffectViewDefaults.NeedleWindupThinMult, t);
                _needle.localScale = new Vector3(thin, len * 0.5f * stretch, thin);
                return;
            }

            if (!arrived)
            {
                // Gidiş: uç TipDistance'ta; 2 hayalet smear (afterimage — az mesh).
                Vector3 tip = origin + dir * dist;
                _needle.position = tip;
                if (dir.sqrMagnitude > 1e-4f)
                    _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                _needle.localScale = new Vector3(thick * LivingEffectViewDefaults.NeedleHoldThickMult, len * LivingEffectViewDefaults.NeedleHoldLenMult, thick * LivingEffectViewDefaults.NeedleHoldThickMult);

                for (int i = 0; i < _needleGhosts.Length; i++)
                {
                    float back = (i + 1) / (_needleGhosts.Length + 1f);
                    Vector3 gp = Vector3.Lerp(origin, tip, 1f - back * LivingEffectViewDefaults.NeedleGhostLerpMult);
                    _needleGhosts[i].gameObject.SetActive(true);
                    _needleGhosts[i].position = gp;
                    _needleGhosts[i].rotation = _needle.rotation;
                    float gScale = 1f - back * LivingEffectViewDefaults.NeedleGhostScaleMult;
                    _needleGhosts[i].localScale = new Vector3(
                        thick * LivingEffectViewDefaults.NeedleGhostThickMult * gScale,
                        len * LivingEffectViewDefaults.NeedleGhostLenMult * gScale,
                        thick * LivingEffectViewDefaults.NeedleGhostThickMult * gScale);
                }

                return;
            }

            // Varış: donmuş poz — kısa, sert.
            HideNeedleGhosts();
            Vector3 end = origin + dir * dist;
            _needle.position = end;
            if (dir.sqrMagnitude > 1e-4f)
                _needle.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            float holdLen = len * _colors.Visuals.EffectNeedleArrivalLenMul;
            _needle.localScale = new Vector3(thick * LivingEffectViewDefaults.NeedleHoldThickPulseMult, holdLen * 0.5f, thick * LivingEffectViewDefaults.NeedleHoldThickPulseMult);
        }

        void DrawSwarm(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            int count;
            if (_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik)
            {
                // Fiil SÜRÜ: her zaman dağınık bulut — sıfır yayılmada bile birkaç gövde.
                float minB = _colors.Visuals.EffectSwarmMinBlobs;
                count = Mathf.RoundToInt(Mathf.Lerp(minB, _blobs.Length, Mathf.Clamp01(s.Spread)));
            }
            else
            {
                count = Mathf.RoundToInt(s.Spread * _tuning.MaxSwarmBlobs);
            }

            count = Mathf.Clamp(count, 0, _blobs.Length);
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 1e-4f)
                right = Vector3.right;

            float stagger = Mathf.Max(0f, _tuning.SwarmStaggerSec);

            for (int i = 0; i < _blobs.Length; i++)
            {
                if (i >= count)
                {
                    _blobs[i].gameObject.SetActive(false);
                    continue;
                }

                // Kademeli varış: gövdeler aynı anda değil sırayla görünür.
                float appearAt = i * stagger;
                if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && _logic.AgeSec < appearAt)
                {
                    _blobs[i].gameObject.SetActive(false);
                    continue;
                }

                _blobs[i].gameObject.SetActive(true);
                float localAge = Mathf.Max(0f, _logic.AgeSec - appearAt);
                float reach = dist;
                if ((_logic.Verb == Rune.Hava || _logic.Verb == Rune.Karanlik) && stagger > 1e-4f)
                {
                    // Her gövde kendi gecikmesiyle uca yetişir — cephe değil bulut.
                    float catchUp = Mathf.Clamp01(localAge / (stagger * count + LivingEffectViewDefaults.BlobStaggerPaddingSec));
                    reach = dist * Mathf.Lerp(LivingEffectViewDefaults.BlobReachStartMult, 1f, catchUp);
                }

                float u = (i + 1) / (count + 1f);
                float along = reach * u;
                float jitter = _colors.Visuals.EffectSwarmJitterM;
                // Düzensiz ofset (sabit hash) — düzenli halka değil.
                float jx = Pseudo(i, 1) * jitter * (0.5f + s.Spread);
                float jz = Pseudo(i, 2) * jitter * (0.5f + s.Spread);
                float side = (i % 2 == 0 ? 1f : -1f) * (LivingEffectViewDefaults.BlobSideBaseMult + (1f - s.Focus) * LivingEffectViewDefaults.BlobSideFocusSpreadMult)
                             * (LivingEffectViewDefaults.BlobSideSpreadMult + s.Spread);

                Vector3 p;
                if (s.Focus > _colors.Visuals.EffectFocusSwarmAlongLineMin || _logic.Verb == Rune.Ates)
                {
                    p = origin + dir * along + right * (side * (1f - s.Focus * LivingEffectViewDefaults.BlobJitterFocusDampMult) + jx * LivingEffectViewDefaults.BlobJitterSideMult)
                        + dir * jz * LivingEffectViewDefaults.BlobJitterAlongMult;
                }
                else
                {
                    // Dağınık bulut: açı + yarıçap jitter
                    float a = u * Mathf.PI * 2f + Pseudo(i, 3) * LivingEffectViewDefaults.BlobOrbitPseudoMult + localAge * LivingEffectViewDefaults.BlobOrbitAgeMult;
                    float r = reach * (LivingEffectViewDefaults.BlobOrbitReachInnerMult + LivingEffectViewDefaults.BlobOrbitReachOuterMult * u) + jz;
                    p = origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r
                        + right * jx * 0.5f;
                }

                p.y = origin.y + LivingEffectViewDefaults.BlobLiftBaseM + LivingEffectViewDefaults.BlobLiftSpreadMult * s.Lift * Mathf.Abs(Mathf.Sin(localAge * LivingEffectViewDefaults.BlobLiftWobbleFreqHz + i));
                _blobs[i].position = p;
                float sc = _colors.Visuals.EffectBlobScaleBaseM + _colors.Visuals.EffectBlobScalePerSpreadM * s.Spread;
                sc *= LivingEffectViewDefaults.BlobScaleBaseMult + LivingEffectViewDefaults.BlobScaleJitterMult * (0.5f + 0.5f * Pseudo(i, 4));
                // Yatay wisp — eski “top sürü” silüetini kırar.
                _blobs[i].localScale = new Vector3(sc * LivingEffectViewDefaults.BlobMeshXZMult, sc * LivingEffectViewDefaults.BlobMeshYMult, sc * LivingEffectViewDefaults.BlobMeshXZMult);
            }
        }

        void HideSwarm()
        {
            if (_blobs == null) return;
            for (int i = 0; i < _blobs.Length; i++)
                _blobs[i].gameObject.SetActive(false);
        }

        void HideNeedleGhosts()
        {
            if (_needleGhosts == null) return;
            for (int i = 0; i < _needleGhosts.Length; i++)
                _needleGhosts[i].gameObject.SetActive(false);
        }

        void PulseBang(Vector3 origin, Vector3 dir, float dist, EffectSilhouette s)
        {
            _ = s;
            float pulse = 1f + LivingEffectViewDefaults.BangPulseAmpMult * Mathf.Sin(_logic.BangAgeSec * LivingEffectViewDefaults.BangPulseFreqHz);
            _line.widthMultiplier = _lineBaseWidth * pulse;
            if (_needle != null && _needle.gameObject.activeSelf)
                _needle.localScale *= 1f + LivingEffectViewDefaults.BangNeedlePulseMult * pulse;

            if (_bangPs != null && _logic.BangAgeSec < LivingEffectViewDefaults.BangPsReplayWindowSec && !_bangPs.isPlaying)
            {
                Vector3 tip = origin + (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward) * dist;
                tip.y = origin.y + LivingEffectViewDefaults.BangTipLiftM;
                _bangPs.transform.position = tip;
                var main = _bangPs.main;
                main.startColor = _hasSkillTint ? _skillLine : (_colors != null ? _colors.Visuals.InkCyan : Color.cyan);
                _bangPs.Play();
            }
        }

        /// <summary>[-1,1] sabit gürültü — Random değil, morph sırasında zıplamaz.</summary>
        static float Pseudo(int i, int salt)
        {
            float x = Mathf.Sin(i * LivingEffectViewDefaults.PseudoHashMultI + salt * LivingEffectViewDefaults.PseudoHashMultSalt) * LivingEffectViewDefaults.PseudoHashScale;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        static Material MakeMat(Color c)
        {
            var shader = FindTransparentUnlitShader();
            var mat = new Material(shader);
            ConfigureTransparentFallback(mat);
            SetMatColor(mat, c);
            return mat;
        }

    }
}
