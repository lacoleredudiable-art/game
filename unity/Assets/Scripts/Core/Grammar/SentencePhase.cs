using System.Collections.Generic;
using Dovus.Core.Input;
using Dovus.Core.Element;

namespace Dovus.Core.Grammar
{
    public enum SentencePhase
    {
        Idle,
        Building,
        Resolved,
        Aborted,

        /// <summary>
        /// §5 "Toparlanma girdi kilididir": kapanış ödendi, girdi kilitli. Kilit üç şeyle
        /// kesilir (düz vuruş, yeni fiil, dodge). Yalnızca State.Phase'in anlık değeri —
        /// CompletedSentence.Phase kapanışta Resolved kalır.
        /// </summary>
        Recovering
    }
}
