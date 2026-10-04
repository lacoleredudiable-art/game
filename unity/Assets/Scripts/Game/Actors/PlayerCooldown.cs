using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Kombo + global soÄŸuma â€” PlayerResource'a paralel.
    /// docs/element-sistemi.json global_rules.cooldown_rules varsayÄ±lanlarÄ±.
    /// BaÄŸlama 4: EnforceCooldown kapÄ±sÄ±; false iken hiÃ§ kullanÄ±lmaz (kozmetik radial kalÄ±r).
    /// </summary>
    public sealed class PlayerCooldown : MonoBehaviour
    {
        CooldownTracker _tracker;

        public float GlobalCooldownSec => _tracker != null ? _tracker.GlobalCooldownSec : 0f;

        /// <summary>JSON cooldown_rules: global_cooldown_sec 0.3 / max_concurrent_casts 1.</summary>
        public void Bind(float globalCooldownSec = 0.3f, int maxConcurrentCasts = 1)
        {
            _tracker = new CooldownTracker(globalCooldownSec, maxConcurrentCasts);
        }

        /// <summary>CombatTuning.EnforceCooldown kapÄ±sÄ± â€” tracker yoksa true (fail-open).</summary>
        public bool CanStart(string comboKey, double worldMs) =>
            _tracker == null || _tracker.CanStart(comboKey, worldMs);

        /// <summary>Fiil baÅŸlatma: yalnÄ±z GCD + eÅŸzamanlÄ± cast.</summary>
        public bool CanStartGlobalGate(double worldMs) =>
            _tracker == null || _tracker.CanStartGlobalGate(worldMs);

        /// <summary>
        /// Cast bang: GCD + kombo soÄŸumasÄ± yazar. EÅŸzamanlÄ± yuvayÄ± hemen boÅŸaltÄ±r
        /// (max_concurrent uÃ§uÅŸ sÃ¼resi Faz 6; bu turda kombo/GCD yeterli).
        /// </summary>
        public bool TryBeginCast(string comboKey, float comboCooldownSec, double worldMs)
        {
            if (_tracker == null)
                return false;
            if (!_tracker.TryStart(comboKey, comboCooldownSec, worldMs))
                return false;
            _tracker.CompleteCast();
            return true;
        }

        public float ComboRemainingSec(string comboKey, double worldMs) =>
            _tracker == null ? 0f : _tracker.ComboRemainingSec(comboKey, worldMs);

        public float GlobalRemainingSec(double worldMs) =>
            _tracker == null ? 0f : _tracker.GlobalRemainingSec(worldMs);
    }
}
