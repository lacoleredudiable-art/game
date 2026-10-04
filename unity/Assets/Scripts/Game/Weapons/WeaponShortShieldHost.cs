using Dovus.Core.Equipment;
using Dovus.Game.Actors;
using Dovus.Game.Platform;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Oyuncudaki kısa kalkan. Gelen hasar kapısı bunu
    /// <see cref="PlayerDodgeController.BlocksIncoming"/> sonrasında okur.
    /// Hasar formülüne girmez; puan burada tükenir.
    /// </summary>
    public sealed class WeaponShortShieldHost : MonoBehaviour
    {
        readonly WeaponShortShield _shield = new();
        GameClockHost _clock;

        public WeaponShortShield Shield => _shield;

        public void Bind(GameClockHost clock) => _clock = clock;

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
