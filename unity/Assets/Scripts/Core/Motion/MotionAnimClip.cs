using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Kalıp anahtarının Animator state/trigger karşılığı. Silah klibi yalnız tablodan gelir.</summary>
    public readonly struct MotionAnimClip
    {
        public MotionAnimClip(string key, string state, string trigger, bool fallback)
        {
            Key = key ?? string.Empty;
            State = state ?? string.Empty;
            Trigger = trigger ?? string.Empty;
            Fallback = fallback;
        }

        public string Key { get; }
        public string State { get; }
        public string Trigger { get; }
        public bool Fallback { get; }
    }
}
