using Dovus.Core.Equipment;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Oyuncudaki kısa kalkan. Gelen hasar kapısı bunu
    /// <see cref="PlayerDodgeRig.BlocksIncoming"/> sonrasında okur.
    /// Hasar formülüne girmez; puan burada tükenir.
    /// </summary>
    public sealed class WeaponShortShieldHost : MonoBehaviour
    {
        readonly WeaponShortShield _shield = new();
        GameClock _clock;

        public WeaponShortShield Shield => _shield;

        public void Bind(GameClock clock) => _clock = clock;

        public double NowMs => _clock != null ? _clock.Director.WorldTimeMs : 0;

        public void Grant(float points, double nowMs, float durationSec) =>
            _shield.Grant(points, nowMs, durationSec);

        public static float Absorb(Component host, float incoming, double nowMs)
        {
            if (host == null || incoming <= 0f)
                return incoming;
            WeaponShortShieldHost found = host.GetComponent<WeaponShortShieldHost>();
            return found != null ? found._shield.Absorb(incoming, nowMs) : incoming;
        }
    }
}
