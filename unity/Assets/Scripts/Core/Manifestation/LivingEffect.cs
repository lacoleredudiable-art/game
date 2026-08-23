using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;

namespace Dovus.Core.Manifestation
{
    public enum LivingEffectPhase
    {
        Traveling,
        Fading,
        AwaitingClosing,
        Banging,
        Dead
    }

    /// <summary>
    /// Tek bir yaşayan etki — Unity bilmez. Sıfat hedef silüeti anında günceller;
    /// görünen silüet MorphLerp ile ona yaklaşır (5→5-1 toplama gözle okunur).
    /// </summary>
    public sealed class LivingEffect
    {
        readonly ManifestationTuning _tuning;
        readonly Rune _verb;
        readonly float _originX;
        readonly float _originZ;
        readonly float _dirX;
        readonly float _dirZ;

        EffectSilhouette _target;
        EffectSilhouette _current;
        float _ageSec;
        float _travel;
        float _fade;
        float _bangAge;
        float _holdPastRangeSec;
        bool _paidClosing;
        ClosingHit? _closing;

        public LivingEffect(
            Rune verb,
            float originX,
            float originZ,
            float dirX,
            float dirZ,
            IReadOnlyList<SentenceWord> words,
            ManifestationTuning? tuning = null)
        {
            _tuning = tuning ?? new ManifestationTuning();
            _verb = verb;
            _originX = originX;
            _originZ = originZ;

            float len = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 1e-5f)
            {
                _dirX = 0f;
                _dirZ = 1f;
            }
            else
            {
                _dirX = dirX / len;
                _dirZ = dirZ / len;
            }

            _target = SilhouetteBuilder.FromWords(words, _tuning);
            _current = _target;
            Phase = LivingEffectPhase.Traveling;
        }

        public Rune Verb => _verb;
        public LivingEffectPhase Phase { get; private set; }
        public EffectSilhouette Current => _current;
        public EffectSilhouette Target => _target;
        public float AgeSec => _ageSec;
        public float Travel => _travel;
        public float OriginX => _originX;
        public float OriginZ => _originZ;
        public float DirX => _dirX;
        public float DirZ => _dirZ;
        public float FadeT => _fade;
        public float BangAgeSec => _bangAge;
        public ClosingHit? Closing => _closing;
        public bool PaidClosing => _paidClosing;
        public bool IsAlive => Phase is not LivingEffectPhase.Dead;

        public float TipX => _originX + _dirX * TipDistance;
        public float TipZ => _originZ + _dirZ * TipDistance;

        public float TipDistance => _verb switch
        {
            Rune.Sarsinti => MathF.Min(_travel, _tuning.WaveMaxRadiusM),
            Rune.Igne => MathF.Min(_travel, _tuning.NeedleMaxRangeM),
            Rune.Suru => MathF.Min(_travel, _tuning.SwarmMaxRadiusM),
            _ => MathF.Min(_travel, _tuning.WaveMaxRadiusM)
        };

        public float MaxRange => _verb switch
        {
            Rune.Sarsinti => _tuning.WaveMaxRadiusM,
            Rune.Igne => _tuning.NeedleMaxRangeM,
            Rune.Suru => _tuning.SwarmMaxRadiusM,
            _ => _tuning.WaveMaxRadiusM
        };

        public void SetWords(IReadOnlyList<SentenceWord> words)
        {
            if (Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
                return;
            _target = SilhouetteBuilder.FromWords(words, _tuning);
        }

        public void Tick(float dtSec)
        {
            if (dtSec <= 0f || Phase == LivingEffectPhase.Dead)
                return;

            _ageSec += dtSec;
            Morph(dtSec);

            switch (Phase)
            {
                case LivingEffectPhase.Traveling:
                case LivingEffectPhase.AwaitingClosing:
                    AdvanceTravel(dtSec);
                    // Cümle Building ya da AwaitingClosing iken etki ölmez — menzil sonunda
                    // bekler/sürer (§5/T2: kapanışın ödülü dünyada görünür kalmalı). Sönme
                    // yalnızca Abort'ta ve kapanış patladıktan sonra (Banging→Dead) olur.
                    if (_travel >= MaxRange)
                    {
                        _holdPastRangeSec += dtSec;
                        // Güvenlik payı: cümle hiç kapanmazsa (beklenmedik durum) sonsuza
                        // asılı kalmasın. Normal akışta motor her cümleyi kapatır, buraya
                        // hiç değmez.
                        if (Phase == LivingEffectPhase.Traveling
                            && _holdPastRangeSec >= _tuning.MaxHoldPastRangeSec)
                            BeginFade();
                    }
                    else
                    {
                        _holdPastRangeSec = 0f;
                    }
                    break;
                case LivingEffectPhase.Fading:
                    _fade += dtSec / _tuning.FadeDurationSec;
                    if (_fade >= 1f)
                        Phase = LivingEffectPhase.Dead;
                    break;
                case LivingEffectPhase.Banging:
                    _bangAge += dtSec;
                    if (_bangAge >= _tuning.BangDurationSec)
                        Phase = LivingEffectPhase.Dead;
                    break;
            }
        }

        /// <summary>Dodge / vurulma — kapanış yok, sön.</summary>
        public void Abort()
        {
            if (Phase == LivingEffectPhase.Dead)
                return;
            _closing = null;
            _paidClosing = false;
            BeginFade();
        }

        /// <summary>Cümle çözüldü — kapanış sessizlikten sonra çağrılacak.</summary>
        public void ArmClosing(ClosingHit closing)
        {
            if (Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
                return;
            _closing = closing;
            Phase = LivingEffectPhase.AwaitingClosing;
        }

        public void FireClosingBang()
        {
            if (!_closing.HasValue || Phase == LivingEffectPhase.Dead)
                return;
            _paidClosing = true;
            _bangAge = 0f;
            Phase = LivingEffectPhase.Banging;
        }

        public bool OverlapsBoss(float bossX, float bossZ, float radiusM)
        {
            float dx = TipX - bossX;
            float dz = TipZ - bossZ;
            // SARSINTI halka/hat: mesafe halka yarıçapına yakınsa isabet
            if (_verb == Rune.Sarsinti)
            {
                float bx = bossX - _originX;
                float bz = bossZ - _originZ;
                float radial = MathF.Sqrt(bx * bx + bz * bz);
                float ringGap = MathF.Abs(radial - TipDistance);
                if (ringGap > radiusM)
                    return false;

                // Odak yüksekse yalnızca hat koridoru
                if (_current.Focus > 0.35f)
                {
                    float along = bx * _dirX + bz * _dirZ;
                    if (along < 0f || along > TipDistance + radiusM)
                        return false;
                    float perp = MathF.Abs(bx * -_dirZ + bz * _dirX);
                    float halfWidth = Lerp(
                        _tuning.WaveCorridorHalfWidthWideM,
                        _tuning.WaveCorridorHalfWidthNarrowM,
                        _current.Focus) + radiusM;
                    return perp <= halfWidth;
                }

                return true;
            }

            float d = MathF.Sqrt(dx * dx + dz * dz);
            return d <= radiusM + _tuning.TravelHitRadiusM;
        }

        void AdvanceTravel(float dtSec)
        {
            // T14 İĞNE: Zenitsu küçük hâli — gerilmede yol alma, sonra 2–3 karelik gidiş,
            // menzilde sert duruş (TipDistance zaten MaxRange'de kesilir).
            if (_verb == Rune.Igne)
            {
                if (_ageSec < _tuning.NeedleWindupSec)
                    return;

                float dashSec = _tuning.NeedleDashSec > 0.016f ? _tuning.NeedleDashSec : 0.016f;
                float dashSpeed = _tuning.NeedleMaxRangeM / dashSec;
                // Pierce hâlâ tempoyu keskinleştirir (silüet, hasar değil).
                dashSpeed *= 1f + _tuning.PierceSpeedBonus * _current.Pierce;
                _travel += dashSpeed * dtSec;
                return;
            }

            float speed = _verb switch
            {
                Rune.Sarsinti => _tuning.WaveSpeedMps,
                Rune.Suru => _tuning.SwarmSpeedMps,
                _ => _tuning.WaveSpeedMps
            };
            // Pierce hızlandırır (daha derin atılış hissi) — sayısal hasar değil silüet tempo
            speed *= 1f + _tuning.PierceSpeedBonus * _current.Pierce;
            _travel += speed * dtSec;
        }

        void Morph(float dtSec)
        {
            float t = 1f - MathF.Exp(-_tuning.MorphLerpPerSec * dtSec);
            _current = EffectSilhouette.Lerp(_current, _target, t).Clamped();
        }

        void BeginFade()
        {
            if (Phase == LivingEffectPhase.Fading || Phase == LivingEffectPhase.Dead)
                return;
            Phase = LivingEffectPhase.Fading;
            _fade = 0f;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
