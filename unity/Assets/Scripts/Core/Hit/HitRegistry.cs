using System.Collections.Generic;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Salıncak başına hedef başına tek vuruş. Hitbox birkaç karede hedefin içinde kalırsa hasar bir kez işler.
    /// </summary>
    public sealed class HitRegistry
    {
        readonly Dictionary<string, Dictionary<string, double>> _lastHitMs =
            new Dictionary<string, Dictionary<string, double>>();

        public int ActiveHitCount => _lastHitMs.Count;

        /// <summary>true = bu vuruş sayılır. Boş hitId süzülmez.</summary>
        public bool TryRegister(string hitId, string targetId, double nowMs, double rehitIntervalMs)
        {
            if (string.IsNullOrEmpty(hitId))
                return true;

            if (!_lastHitMs.TryGetValue(hitId, out Dictionary<string, double>? perTarget))
            {
                perTarget = new Dictionary<string, double>();
                _lastHitMs[hitId] = perTarget;
            }

            if (perTarget.TryGetValue(targetId, out double last))
            {
                if (rehitIntervalMs <= 0d || nowMs - last < rehitIntervalMs)
                    return false;
            }

            perTarget[targetId] = nowMs;
            return true;
        }

        /// <summary>Salıncak bitti: kayıtlar silinir (yeniden kullanılan kimlik temiz başlar).</summary>
        public void EndHit(string hitId)
        {
            if (!string.IsNullOrEmpty(hitId))
                _lastHitMs.Remove(hitId);
        }

        public void Clear() => _lastHitMs.Clear();
    }
}
