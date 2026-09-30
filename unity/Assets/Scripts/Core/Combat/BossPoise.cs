using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Boss poise barı. Vuruşlar düşürür; 0 olunca kısa sersemlik, sonra tavanına döner.
    /// Zaman parametre olarak geçer.
    /// </summary>
    public sealed class BossPoise
    {
        public BossPoise(float max)
        {
            Max = max > 0f ? max : 1f;
            Current = Max;
        }

        public float Max { get; }
        public float Current { get; private set; }
        public float RemainingSec { get; private set; }
        public bool IsStaggered => RemainingSec > 0f;
        public float Ratio => Max > 0f ? Current / Max : 1f;

        /// <summary>true = bu vuruş poise'i kırdı ve sersemlik başladı.</summary>
        public bool ApplyHit(float poiseDamage, float staggerDurationSec)
        {
            if (IsStaggered || poiseDamage <= 0f)
                return false;

            Current = Math.Max(0f, Current - poiseDamage);
            if (Current > 0f)
                return false;

            RemainingSec = staggerDurationSec > 0f ? staggerDurationSec : 0f;
            return RemainingSec > 0f;
        }

        public void Tick(float dtSec)
        {
            if (RemainingSec <= 0f || dtSec <= 0f)
                return;

            RemainingSec = Math.Max(0f, RemainingSec - dtSec);
            if (RemainingSec <= 0f)
                Current = Max;
        }

        public void Reset()
        {
            Current = Max;
            RemainingSec = 0f;
        }
    }
}
