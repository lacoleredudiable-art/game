using System;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Düz vuruş zinciri durum makinesi (A5, O-4.14): hangi kesişin sırada olduğunu tutar.
    /// Dokunuşun ne zaman kabul edileceğini (klip sürerken sıraya alma, Y24) <see cref="ActionArbiter"/> + tampon yapar;
    /// bu sınıf yalnız kabul edilen vuruşun adımını ve faz sürelerini üretir.
    /// </summary>
    public sealed class BasicStrikeCombo
    {
        StrikeComboDef _def;
        int _nextStep;
        int _lastStartMs;
        bool _hasStarted;

        public BasicStrikeCombo(StrikeComboDef def)
        {
            _def = def ?? throw new ArgumentNullException(nameof(def));
        }

        /// <summary>13 §9 [Ö] weave: skill ya da silah değişimi başlayınca düz vuruş sayacı sıfırlanır.</summary>
        public bool WeaveResets { get; set; } = true;

        /// <summary>Sırada olan kesişin indeksi (0 tabanlı).</summary>
        public int NextStep => _nextStep;

        /// <summary>Silah değişince zincir yeni silahın tanımına geçer ve sıfırlanır.</summary>
        public void SetDef(StrikeComboDef def)
        {
            _def = def ?? throw new ArgumentNullException(nameof(def));
            Reset();
        }

        public void Reset()
        {
            _nextStep = 0;
            _hasStarted = false;
        }

        /// <summary>Vuruş başlıyor: adımı seç, zinciri ilerlet, faz sürelerini ver (hazırlık = vuruş karesine kadar).</summary>
        public ActionPlan Begin(int nowMs)
        {
            if (!_hasStarted || nowMs - _lastStartMs > _def.ResetMs)
                _nextStep = 0;

            StrikeStep s = _def.Step(_nextStep % _def.StepCount);
            _nextStep = (_nextStep + 1) % _def.StepCount;
            _lastStartMs = nowMs;
            _hasStarted = true;
            return new ActionPlan(ActionKind.BasicStrike, 0, s.HitMs, 0, s.DurationMs - s.HitMs);
        }

        /// <summary>Arbiter'ın <c>OnStarted</c>'ına bağlanır. Düz vuruşun kendi başlangıcı zinciri sıfırlamaz.</summary>
        public void NotifyStarted(ActionPlan plan)
        {
            if (WeaveResets && (plan.Kind == ActionKind.Skill || plan.Kind == ActionKind.WeaponSwap))
                Reset();
        }
    }
}
