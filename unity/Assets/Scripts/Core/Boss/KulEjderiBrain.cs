using System;
using System.Collections.Generic;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// Kül Ejderi karar durum makinesi (18-kul-ejderi.md): uyku → uyanış → bekleme → ön işaret → vuruş → açık kalma,
    /// sendeleme, faz geçişi, öfke ve ölüm. Saf C#: zaman <c>worldMs</c> parametresiyle gelir, kendi saati yoktur.
    /// Hareket/animasyon/hasar uygulamaz; yalnız <see cref="DrainEvents"/> olaylarını ve durumunu üretir.
    /// <see cref="Enabled"/> false iken <see cref="Tick"/> ve <see cref="Wake"/> hiçbir şey yapmaz
    /// (asama/1-dodge'daki "boss yerinde sabit" deneme düzeni bozulmaz).
    /// </summary>
    public sealed class KulEjderiBrain
    {
        readonly Random _rng;
        readonly List<KulEjderiEvent> _events = new List<KulEjderiEvent>();

        KulEjderiState _state = KulEjderiState.Sleeping;
        KulEjderiPhase _phase = KulEjderiPhase.Phase1;
        KulEjderiAttackSpec? _current;
        double _stateStartMs;
        double _stateEndMs;
        double _nextAttackMs;
        bool _selectionDeferred;
        double _phaseStartMs;
        double _nextAnnouncedMs;
        double _fightStartMs;
        double _resistantUntilMs = double.NegativeInfinity;
        double _gatedAttackLastMs = double.NegativeInfinity;
        double _lastLongStrikeMs = double.NegativeInfinity;
        KulEjderiAttackId? _lastAttack;
        float _hpRatio = 1f;
        bool _enraged;

        public KulEjderiBrain(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>Varsayılan false: yeni yapay zekâ açıkça açılmadıkça boss davranmaz.</summary>
        public bool Enabled { get; set; }

        /// <summary>Takım görevi sürüyor (KE-5.11, KE-6.12, KE-7.9): saldırı havuzu daralır, küçük tehdit durur.</summary>
        public bool TeamTaskActive { get; set; }

        public KulEjderiState State => _state;
        public KulEjderiPhase Phase => _phase;
        public KulEjderiAttackSpec? CurrentAttack => _current;
        public bool IsEnraged => _enraged;
        /// <summary>Şu anki durumun bitiş anı (açık kalma penceresinin sonu gibi sunum ihtiyaçları için).</summary>
        public double StateEndMs => _stateEndMs;
        /// <summary>Faz başlangıcı (duyurulu saldırı sayacı buradan işler).</summary>
        public double PhaseStartMs => _phaseStartMs;

        /// <summary>Açık kalma ya da sendeleme: boss'un vurma penceresi.</summary>
        public bool IsOpen => _state == KulEjderiState.Recovery || _state == KulEjderiState.Staggered;

        /// <summary>Uyanış, geçiş ve ölüm sinemalarında boss hasar almaz.</summary>
        public bool AcceptsDamage =>
            _state != KulEjderiState.Awakening && _state != KulEjderiState.Transition
            && _state != KulEjderiState.Dying && _state != KulEjderiState.Dead;

        public bool IsResistant(double worldMs) => worldMs < _resistantUntilMs;

        /// <summary>Ön işaret ilerlemesi 0-1 (ön işaret dışında 0).</summary>
        public float TelegraphProgress01(double worldMs)
        {
            if (_state != KulEjderiState.Telegraph || _current == null || _current.TelegraphMs <= 0)
                return 0f;
            double p = (worldMs - _stateStartMs) / _current.TelegraphMs;
            return (float)Math.Min(1.0, Math.Max(0.0, p));
        }

        /// <summary>Birikmiş olayları verir ve kuyruğu boşaltır.</summary>
        public void DrainEvents(List<KulEjderiEvent> into)
        {
            into.AddRange(_events);
            _events.Clear();
        }

        /// <summary>KE-5.2: oyuncu çizgiyi geçti ya da ejderhaya hasar verdi. true = uyanış başladı.</summary>
        public bool Wake(double worldMs)
        {
            if (!Enabled || _state != KulEjderiState.Sleeping)
                return false;
            _fightStartMs = worldMs;
            _state = KulEjderiState.Awakening;
            _stateStartMs = worldMs;
            _stateEndMs = worldMs + KulEjderiDefaults.AwakenMs;
            Emit(KulEjderiEventKind.Awakened, worldMs, null);
            return true;
        }

        /// <summary>Boss canının oranı değişti (1 = dolu, 0 = ölü). Faz geçişi ve ölüm buradan tetiklenir.</summary>
        public void OnHpRatio(float hpRatio, double worldMs)
        {
            _hpRatio = hpRatio;
            if (!Enabled)
                return;
            ApplyPendingHp(worldMs);
        }

        /// <summary>Kontrol fiiliyle kesme girişimi (§0). Başarılıysa saldırı iptal olur, boss sendeler.</summary>
        public KulEjderiInterruptResult TryInterrupt(double worldMs, bool fromRange)
        {
            if (!Enabled || _state != KulEjderiState.Telegraph || _current == null)
                return KulEjderiInterruptResult.NotTelegraphing;
            if (!_current.Interruptible)
                return KulEjderiInterruptResult.NotInterruptible;
            if (_current.InterruptNeedsRange && !fromRange)
                return KulEjderiInterruptResult.OutOfReach;
            if (IsResistant(worldMs))
                return KulEjderiInterruptResult.Resistant;

            KulEjderiAttackId id = _current.Id;
            _current = null;
            _resistantUntilMs = worldMs + KulEjderiDefaults.InterruptResistanceMs;
            Emit(KulEjderiEventKind.Interrupted, worldMs, id);
            EnterStagger(worldMs, KulEjderiDefaults.InterruptStaggerMs);
            return KulEjderiInterruptResult.Interrupted;
        }

        /// <summary>Denge çubuğu doldu (KE-8.1: 7 s) ya da görev başarısı (KE-5.12: 4 s). true = sendeleme başladı.</summary>
        public bool BeginStagger(double worldMs, int durationMs)
        {
            if (!Enabled)
                return false;
            switch (_state)
            {
                case KulEjderiState.Idle:
                case KulEjderiState.Telegraph:
                case KulEjderiState.Active:
                case KulEjderiState.Recovery:
                    CancelCurrent(worldMs);
                    EnterStagger(worldMs, durationMs);
                    return true;
                default:
                    return false;
            }
        }

        public void Tick(double worldMs)
        {
            if (!Enabled)
                return;
            CheckEnrage(worldMs);

            // Her adım sınır anını yeni durumun başı yapar; uzun karelerde birden çok durum atlanabilir.
            for (int guard = 0; guard < KulEjderiDefaults.MaxStepsPerTick; guard++)
            {
                if (!Step(worldMs))
                    return;
            }
        }

        bool Step(double worldMs)
        {
            switch (_state)
            {
                case KulEjderiState.Awakening:
                    if (worldMs < _stateEndMs)
                        return false;
                    BeginPhase(KulEjderiPhase.Phase1, _stateEndMs);
                    EnterIdle(_stateEndMs, KulEjderiDefaults.FirstAttackAfterAwakenMs);
                    return true;
                case KulEjderiState.Idle:
                    return StepIdle(worldMs);
                case KulEjderiState.Telegraph:
                    if (worldMs < _stateEndMs)
                        return false;
                    EnterActive(_stateEndMs);
                    return true;
                case KulEjderiState.Active:
                    if (worldMs < _stateEndMs)
                        return false;
                    EnterRecovery(_stateEndMs);
                    return true;
                case KulEjderiState.Recovery:
                    if (worldMs < _stateEndMs)
                        return false;
                    EnterIdle(_stateEndMs, NextGapMs());
                    return true;
                case KulEjderiState.Staggered:
                    if (worldMs < _stateEndMs)
                        return false;
                    Emit(KulEjderiEventKind.StaggerEnded, _stateEndMs, null);
                    EnterIdle(_stateEndMs, NextGapMs());
                    return true;
                case KulEjderiState.Transition:
                    if (worldMs < _stateEndMs)
                        return false;
                    CompleteTransition(_stateEndMs);
                    return true;
                case KulEjderiState.Dying:
                    if (worldMs < _stateEndMs)
                        return false;
                    _state = KulEjderiState.Dead;
                    _stateStartMs = _stateEndMs;
                    Emit(KulEjderiEventKind.Died, _stateEndMs, null);
                    return false;
                default:
                    return false;
            }
        }

        bool StepIdle(double worldMs)
        {
            if (worldMs < _nextAttackMs)
                return false;
            KulEjderiAttackSpec? next = SelectAttack(worldMs);
            if (next == null)
            {
                // Seçilecek saldırı yok (ör. faz 2 takım görevi): görev bitince hemen başlar.
                _selectionDeferred = true;
                return false;
            }

            double start = _selectionDeferred ? worldMs : _nextAttackMs;
            _selectionDeferred = false;
            EnterTelegraph(next, start);
            return true;
        }

        KulEjderiAttackSpec? SelectAttack(double worldMs)
        {
            if (!TeamTaskActive && worldMs >= _nextAnnouncedMs)
            {
                // D-7: dodge dolumu dolmadan ikinci uzun saldırı gelmez; duyurulu saldırı normal havuzdan kısa
                // bir saldırıyla beklenir (iptal edilmez, süre dolunca seçilir).
                KulEjderiAttackSpec announced = KulEjderiAttackTable.AnnouncedFor(_phase);
                if (!BlockedByDodgeRecharge(announced, worldMs))
                    return announced;
            }

            IReadOnlyList<KulEjderiAttackSpec> pool = KulEjderiAttackTable.NormalPool(_phase);
            var candidates = new List<KulEjderiAttackSpec>();
            double total = 0;
            foreach (KulEjderiAttackSpec spec in pool)
            {
                if (!AllowedDuringTeamTask(spec))
                    continue;
                // KE-4.2: aynı saldırı üst üste gelmez (görevde havuz tek saldırıya iner, o zaman tekrar serbest)
                if (!TeamTaskActive && _lastAttack.HasValue && spec.Id == _lastAttack.Value)
                    continue;
                if (spec.MinRepeatGapMs > 0 && worldMs - _gatedAttackLastMs < spec.MinRepeatGapMs)
                    continue;
                if (BlockedByDodgeRecharge(spec, worldMs))
                    continue;
                candidates.Add(spec);
                total += spec.Weight;
            }

            if (candidates.Count == 0)
                return null;
            double roll = _rng.NextDouble() * total;
            foreach (KulEjderiAttackSpec spec in candidates)
            {
                roll -= spec.Weight;
                if (roll < 0)
                    return spec;
            }

            return candidates[candidates.Count - 1];
        }

        /// <summary>D-7: spec uzunsa ve vuruşu son uzun vuruştan dodge dolumu dolmadan gelecekse true.</summary>
        bool BlockedByDodgeRecharge(KulEjderiAttackSpec spec, double worldMs) =>
            spec.IsLong && worldMs + spec.TelegraphMs - _lastLongStrikeMs < KulEjderiDefaults.DodgeRechargeMs;

        /// <summary>KE-5.11: faz 1'de yalnız Pençe Çakması; KE-6.12: faz 2'de hiç; KE-7.9: faz 3'te yalnız Pençe Süpürmesi.</summary>
        bool AllowedDuringTeamTask(KulEjderiAttackSpec spec)
        {
            if (!TeamTaskActive)
                return true;
            switch (_phase)
            {
                case KulEjderiPhase.Phase1:
                    return spec.Id == KulEjderiAttackId.ClawSlam;
                case KulEjderiPhase.Phase3:
                    return spec.Id == KulEjderiAttackId.ClawSweep;
                default:
                    return false;
            }
        }

        void EnterTelegraph(KulEjderiAttackSpec spec, double startMs)
        {
            _current = spec;
            _state = KulEjderiState.Telegraph;
            _stateStartMs = startMs;
            _stateEndMs = startMs + spec.TelegraphMs;
            if (spec.Announced)
                ScheduleNextAnnounced(startMs);
            Emit(KulEjderiEventKind.TelegraphStarted, startMs, spec.Id);
        }

        void EnterActive(double startMs)
        {
            KulEjderiAttackSpec spec = _current!;
            _state = KulEjderiState.Active;
            _stateStartMs = startMs;
            _stateEndMs = startMs + spec.ActiveMs;
            if (spec.MinRepeatGapMs > 0)
                _gatedAttackLastMs = startMs;
            if (spec.IsLong)
                _lastLongStrikeMs = startMs;
            Emit(KulEjderiEventKind.Struck, startMs, spec.Id);
        }

        void EnterRecovery(double startMs)
        {
            KulEjderiAttackSpec spec = _current!;
            _state = KulEjderiState.Recovery;
            _stateStartMs = startMs;
            double recovery = _enraged ? spec.RecoveryMs * KulEjderiDefaults.EnrageRecoveryFactor : spec.RecoveryMs;
            _stateEndMs = startMs + recovery;
            _lastAttack = spec.Id;
            Emit(KulEjderiEventKind.RecoveryStarted, startMs, spec.Id);
        }

        void EnterIdle(double startMs, int gapMs)
        {
            _current = null;
            _state = KulEjderiState.Idle;
            _stateStartMs = startMs;
            _nextAttackMs = startMs + gapMs;
            _selectionDeferred = false;
            ApplyPendingHp(startMs);
        }

        void EnterStagger(double startMs, int durationMs)
        {
            _current = null;
            _state = KulEjderiState.Staggered;
            _stateStartMs = startMs;
            _stateEndMs = startMs + durationMs;
            Emit(KulEjderiEventKind.StaggerStarted, startMs, null);
        }

        int NextGapMs() => _rng.Next(KulEjderiDefaults.AttackGapMinMs, KulEjderiDefaults.AttackGapMaxMs + 1);

        void BeginPhase(KulEjderiPhase phase, double startMs)
        {
            _phase = phase;
            _phaseStartMs = startMs;
            _nextAnnouncedMs = startMs + KulEjderiDefaults.FirstAnnouncedAfterPhaseStartMs;
        }

        void ScheduleNextAnnounced(double fromMs)
        {
            int span = KulEjderiDefaults.AnnouncedIntervalMaxMs - KulEjderiDefaults.AnnouncedIntervalMinMs;
            _nextAnnouncedMs = fromMs + KulEjderiDefaults.AnnouncedIntervalMinMs + _rng.Next(span + 1);
        }

        void CheckEnrage(double worldMs)
        {
            if (_enraged || _state == KulEjderiState.Sleeping || _state == KulEjderiState.Dying
                || _state == KulEjderiState.Dead)
                return;
            if (worldMs - _fightStartMs < KulEjderiDefaults.EnrageAtMs)
                return;
            _enraged = true;
            Emit(KulEjderiEventKind.Enraged, worldMs, null);
        }

        /// <summary>Saklı can oranına göre ölüm ya da bir sonraki faz geçişini başlatır (bir çağrıda en çok bir faz).</summary>
        void ApplyPendingHp(double worldMs)
        {
            switch (_state)
            {
                case KulEjderiState.Sleeping:
                case KulEjderiState.Awakening:
                case KulEjderiState.Transition:
                case KulEjderiState.Dying:
                case KulEjderiState.Dead:
                    return;
            }

            if (_hpRatio <= 0f)
            {
                CancelCurrent(worldMs);
                _state = KulEjderiState.Dying;
                _stateStartMs = worldMs;
                _stateEndMs = worldMs + KulEjderiDefaults.DyingMs;
                Emit(KulEjderiEventKind.Dying, worldMs, null);
                return;
            }

            if (_phase == KulEjderiPhase.Phase3 || KulEjderiPhaseRules.PhaseForRatio(_hpRatio) <= _phase)
                return;
            CancelCurrent(worldMs);
            _state = KulEjderiState.Transition;
            _stateStartMs = worldMs;
            _stateEndMs = worldMs + KulEjderiDefaults.TransitionMs;
            Emit(KulEjderiEventKind.TransitionStarted, worldMs, null);
        }

        void CompleteTransition(double endMs)
        {
            KulEjderiPhase next = _phase == KulEjderiPhase.Phase1 ? KulEjderiPhase.Phase2 : KulEjderiPhase.Phase3;
            BeginPhase(next, endMs);
            Emit(KulEjderiEventKind.PhaseChanged, endMs, null);
            _lastAttack = null;
            EnterIdle(endMs, NextGapMs());
        }

        /// <summary>Süren saldırı varsa iptal eder; bütün ön işaretler silinir (KE-6.1).</summary>
        void CancelCurrent(double worldMs)
        {
            if (_current == null)
                return;
            KulEjderiAttackId id = _current.Id;
            _current = null;
            Emit(KulEjderiEventKind.Cancelled, worldMs, id);
        }

        void Emit(KulEjderiEventKind kind, double worldMs, KulEjderiAttackId? attack) =>
            _events.Add(new KulEjderiEvent(kind, worldMs, _phase, attack));
    }
}
