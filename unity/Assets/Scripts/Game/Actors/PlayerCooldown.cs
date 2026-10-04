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
    /// Kombo + global soğuma — PlayerResource'a paralel.
    /// docs/element-sistemi.json global_rules.cooldown_rules varsayılanları.
    /// Bağlama 4: EnforceCooldown kapısı; false iken hiç kullanılmaz (kozmetik radial kalır).
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

        /// <summary>CombatTuning.EnforceCooldown kapısı — tracker yoksa true (fail-open).</summary>
        public bool CanStart(string comboKey, double worldMs) =>
            _tracker == null || _tracker.CanStart(comboKey, worldMs);

        /// <summary>Fiil başlatma: yalnız GCD + eşzamanlı cast.</summary>
        public bool CanStartGlobalGate(double worldMs) =>
            _tracker == null || _tracker.CanStartGlobalGate(worldMs);

        /// <summary>
        /// Cast bang: GCD + kombo soğuması yazar. Eşzamanlı yuvayı hemen boşaltır
        /// (max_concurrent uçuş süresi Faz 6; bu turda kombo/GCD yeterli).
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
