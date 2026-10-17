using System;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Tek oyuncu için "şu an ne çalışıyor, gelen girdi ne olur" hakemi: faz hesabı + iptal tablosu + girdi tamponu.
    /// Hasar, mana, CD, animasyon bilmez; yalnız karar verir. Zaman ms olarak parametredir.
    /// </summary>
    public sealed class ActionArbiter
    {
        readonly CancelRuleTable _table;
        readonly InputBuffer _buffer;
        readonly Func<ActionKind, int, int, ActionPlan> _resolve;
        ActionPlan _running;
        int _startMs;
        bool _hasRunning;

        public ActionArbiter(CancelRuleTable table, InputBuffer buffer, Func<ActionKind, int, int, ActionPlan> resolver)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _resolve = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>Eylem başlayınca çağrılır (tampondan gelen dahil). Düz vuruş sayacı sıfırlama (weave) buraya bağlanır.</summary>
        public Action<ActionPlan, int>? OnStarted { get; set; }

        public InputBuffer Buffer => _buffer;

        public bool IsRunning(int nowMs) => PhaseAt(nowMs) != ActionPhase.None;

        public ActionPlan Running => _running;

        public ActionPhase PhaseAt(int nowMs) =>
            _hasRunning ? _running.PhaseAt(nowMs - _startMs) : ActionPhase.None;

        /// <summary>Yeni girdi. Boştaysa başlar; çalışan varsa tablo karar verir.</summary>
        public ArbiterResult Request(ActionKind kind, int token, int nowMs)
        {
            _buffer.Expire(nowMs);
            ActionPhase phase = PhaseAt(nowMs);
            CancelDecision d = _table.Evaluate(phase != ActionPhase.None, _running.Kind, phase, kind);

            if (d.Verdict == CancelVerdict.Deny)
            {
                if (d.BufferMs > 0)
                {
                    _buffer.Push(kind, token, nowMs, d.BufferMs);
                    return new ArbiterResult(ArbiterOutcome.Buffered, default, default, d, false);
                }

                return new ArbiterResult(ArbiterOutcome.Rejected, default, default, d, false);
            }

            return Begin(kind, token, nowMs, d, false);
        }

        /// <summary>Her karede çağrılır: süresi dolanı atar, fazı ilerletir, kuralın izin verdiği tampon girdisini başlatır.</summary>
        public ArbiterResult Tick(int nowMs)
        {
            _buffer.Expire(nowMs);
            ActionPhase phase = PhaseAt(nowMs);
            if (phase == ActionPhase.None)
                _hasRunning = false;

            // Tamponu öncelik sırasıyla dene (küçük sayı önce).
            int bestIdx = -1;
            CancelDecision bestDecision = default;
            for (int i = 0; i < _buffer.Entries.Count; i++)
            {
                InputBuffer.Entry e = _buffer.Entries[i];
                CancelDecision d = _table.Evaluate(phase != ActionPhase.None, _running.Kind, phase, e.Kind);
                if (d.Verdict == CancelVerdict.Deny)
                    continue;
                if (bestIdx < 0 || _table.PriorityOf(e.Kind) < _table.PriorityOf(_buffer.Entries[bestIdx].Kind))
                {
                    bestIdx = i;
                    bestDecision = d;
                }
            }

            if (bestIdx < 0)
                return new ArbiterResult(ArbiterOutcome.None, default, default, default, false);

            InputBuffer.Entry pick = _buffer.Entries[bestIdx];
            _buffer.Remove(pick.Kind);
            return Begin(pick.Kind, pick.Token, nowMs, bestDecision, true);
        }

        /// <summary>Sert CC / ölüm: çalışan eylem ve tampon düşer.</summary>
        public void Interrupt()
        {
            _hasRunning = false;
            _buffer.Clear();
        }

        ArbiterResult Begin(ActionKind kind, int token, int nowMs, CancelDecision d, bool fromBuffer)
        {
            bool cut = d.Verdict == CancelVerdict.Cancel;
            ActionKind cutKind = _running.Kind;
            ActionPlan plan = _resolve(kind, token, nowMs);
            _running = plan;
            _startMs = nowMs;
            _hasRunning = true;
            OnStarted?.Invoke(plan, nowMs);
            return new ArbiterResult(cut ? ArbiterOutcome.Cancelled : ArbiterOutcome.Started, plan, cutKind, d, fromBuffer);
        }
    }
}
