using System;
using System.Collections.Generic;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Veri odaklı iptal tablosu (13 §9). Karar tek yerde: <see cref="Evaluate"/>.
    /// Satır yoksa reddedilir (sessizce izin verilmez). Satırlar <see cref="CancelRuleDefaults"/>'tan gelir.
    /// </summary>
    public sealed class CancelRuleTable
    {
        readonly CancelRule[] _rules;
        readonly int[] _priority;

        /// <param name="rules">İlk eşleşen satır kazanır.</param>
        /// <param name="priority">
        /// Tampondaki girdiler arasında hangisi önce denenir: küçük sayı önce (13 §9: dodge &gt; silah değişimi &gt;
        /// düz vuruş sıfırlama). Dizi <see cref="ActionKind"/> sırasındadır.
        /// </param>
        public CancelRuleTable(IReadOnlyList<CancelRule> rules, IReadOnlyList<int> priority)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (priority == null || priority.Count < KindCount)
                throw new ArgumentException("priority her ActionKind için bir sayı ister", nameof(priority));

            _rules = new CancelRule[rules.Count];
            for (int i = 0; i < _rules.Length; i++) _rules[i] = rules[i];
            _priority = new int[KindCount];
            for (int i = 0; i < KindCount; i++) _priority[i] = priority[i];
        }

        const int KindCount = 4;

        public IReadOnlyList<CancelRule> Rules => _rules;

        public int PriorityOf(ActionKind kind) => _priority[(int)kind];

        public CancelDecision Evaluate(bool running, ActionKind runningKind, ActionPhase phase, ActionKind incoming)
        {
            if (!running || phase == ActionPhase.None)
                return new CancelDecision(CancelVerdict.Start, default, false);

            for (int i = 0; i < _rules.Length; i++)
            {
                if (_rules[i].Matches(runningKind, phase, incoming))
                    return new CancelDecision(_rules[i].Cancels ? CancelVerdict.Cancel : CancelVerdict.Deny, _rules[i], true);
            }

            return new CancelDecision(CancelVerdict.Deny, default, false);
        }
    }
}
