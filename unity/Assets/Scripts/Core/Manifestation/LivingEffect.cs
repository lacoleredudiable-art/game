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
    /// görünen silüet MorphLerp ile ona yaklaşır.
    /// SkillWorldPlanner planı varsa seyahat/menzil/bang rün yerine JSON’dan gelir.
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

        LivingTravelKind _travelKind = LivingTravelKind.LegacyVerb;
        float _speedMps;
        float _maxRangeM;
        float _bangRadiusM;
        float _lifetimeAddSec;
        bool _hasPlan;

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
        public bool HasSkillPlan => _hasPlan;
        public LivingTravelKind TravelKind => _travelKind;
        /// <summary>Plan bang yarıçapı; 0 ise caller tuning.ClosingBangRadiusM kullanır.</summary>
        public float BangRadiusM => _bangRadiusM;
        public float LifetimeAddSec => _lifetimeAddSec;

        public float TipX => _originX + _dirX * TipDistance;
        public float TipZ => _originZ + _dirZ * TipDistance;

        public float TipDistance => MathF.Min(_travel, MaxRange);

        public float MaxRange
        {
            get
            {
                if (_hasPlan && _maxRangeM > 0f)
                    return _maxRangeM;
                return _verb switch
                {
                    Rune.Aydinlik => _tuning.WaveMaxRadiusM,
                    Rune.Ates => _tuning.NeedleMaxRangeM,
                    Rune.Su => _tuning.SwarmMaxRadiusM,
                    _ => _tuning.WaveMaxRadiusM
                };
            }
        }

        /// <summary>SkillMotor + prezentasyon planı — silüet/seyahat/bang.</summary>
        public void ApplyPlan(in LivingEffectPlan plan)
        {
            if (Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
                return;
            if (!plan.HasPlan)
                return;

            _hasPlan = true;
            _travelKind = plan.TravelKind;
            _speedMps = plan.SpeedMps;
            _maxRangeM = plan.MaxRangeM;
            _bangRadiusM = plan.BangRadiusM;
            _lifetimeAddSec = plan.LifetimeAddSec;
            _target = plan.Silhouette.Clamped();
            if (_ageSec < 0.05f)
                _current = _target;
        }

        public void SetWords(IReadOnlyList<SentenceWord> words)
        {
            if (Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
                return;
            // Plan varken FromWords ara rünleri yanlış sıfat sayar — silüeti plan korur.
            if (_hasPlan)
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
                    float holdMax = _tuning.MaxHoldPastRangeSec + MathF.Max(0f, _lifetimeAddSec);
                    if (_travel >= MaxRange)
                    {
                        _holdPastRangeSec += dtSec;
                        if (Phase == LivingEffectPhase.Traveling
                            && _holdPastRangeSec >= holdMax)
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

        public void Abort()
        {
            if (Phase == LivingEffectPhase.Dead)
                return;
            _closing = null;
            _paidClosing = false;
            BeginFade();
        }

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

            if (_hasPlan && _travelKind == LivingTravelKind.ExpandingRadial)
            {
                float bx = bossX - _originX;
                float bz = bossZ - _originZ;
                float radial = MathF.Sqrt(bx * bx + bz * bz);
                return radial <= TipDistance + radiusM;
            }

            if (_verb == Rune.Aydinlik && !_hasPlan)
            {
                float bx = bossX - _originX;
                float bz = bossZ - _originZ;
                float radial = MathF.Sqrt(bx * bx + bz * bz);
                float ringGap = MathF.Abs(radial - TipDistance);
                if (ringGap > radiusM)
                    return false;

                if (_current.Focus > 0.35f)
                {
                    float along = bx * _dirX + bz * _dirZ;
                    if (along < 0f || along > TipDistance + radiusM)
                        return false;
                    float perp = MathF.Abs(bx * _dirZ - bz * _dirX);
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
            if (_hasPlan)
            {
                AdvancePlanned(dtSec);
                return;
            }

            if (_verb == Rune.Ates)
            {
                if (_ageSec < _tuning.NeedleWindupSec)
                    return;

                float dashSec = _tuning.NeedleDashSec > 0.016f ? _tuning.NeedleDashSec : 0.016f;
                float dashSpeed = _tuning.NeedleMaxRangeM / dashSec;
                dashSpeed *= 1f + _tuning.PierceSpeedBonus * _current.Pierce;
                _travel += dashSpeed * dtSec;
                return;
            }

            float speed = _verb switch
            {
                Rune.Aydinlik => _tuning.WaveSpeedMps,
                Rune.Su => _tuning.SwarmSpeedMps,
                _ => _tuning.WaveSpeedMps
            };
            speed *= 1f + _tuning.PierceSpeedBonus * _current.Pierce;
            _travel += speed * dtSec;
        }

        void AdvancePlanned(float dtSec)
        {
            switch (_travelKind)
            {
                case LivingTravelKind.Instant:
                    _travel = MaxRange;
                    return;
                case LivingTravelKind.Static:
                    {
                        float target = _bangRadiusM > 0f ? _bangRadiusM : MaxRange * 0.35f;
                        float spd = _speedMps > 0f ? _speedMps : _tuning.WaveSpeedMps;
                        if (_travel < target)
                            _travel += spd * dtSec;
                        else
                            _travel = MathF.Max(_travel, target);
                    }
                    return;
                case LivingTravelKind.ExpandingRadial:
                case LivingTravelKind.Linear:
                default:
                    {
                        float spd = _speedMps > 0f ? _speedMps : _tuning.WaveSpeedMps;
                        spd *= 1f + _tuning.PierceSpeedBonus * _current.Pierce;
                        _travel += spd * dtSec;
                    }
                    return;
            }
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
