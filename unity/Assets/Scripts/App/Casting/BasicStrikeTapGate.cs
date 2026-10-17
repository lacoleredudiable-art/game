using System;
using Dovus.Core.Shared;

namespace Dovus.App.Casting
{
    /// <summary>Y24: klip (prefire + recovery) sürerken tek slotluk düz vuruş kuyruğu.</summary>
    public sealed class BasicStrikeTapGate
    {
        double _clipUntilMs = -1;
        bool _queued;

        public bool HasQueuedTap => _queued;

        public double ClipUntilMs => _clipUntilMs;

        public bool ClipActive(double nowMs) => nowMs < _clipUntilMs;

        /// <summary>Klip bitene kadar gelen dokunuşları yutar; en fazla bir sonraki vuruş sıraya alınır.</summary>
        public bool TryAcceptTap(double nowMs, float clipSec)
        {
            if (ClipActive(nowMs))
            {
                _queued = true;
                return false;
            }

            BeginClip(nowMs, clipSec);
            return true;
        }

        public void BeginClip(double nowMs, float clipSec)
        {
            long clipMs = (long)System.Math.Round(clipSec * Units.SecToMs, System.MidpointRounding.AwayFromZero);
            _clipUntilMs = nowMs + clipMs;
        }

        /// <summary>Klip bittiği ilk tick'te sıradaki dokunuşu tüketir.</summary>
        public bool Tick(double nowMs, Action onQueuedStrike)
        {
            if (!_queued || ClipActive(nowMs))
                return false;
            _queued = false;
            onQueuedStrike?.Invoke();
            return true;
        }
    }
}
