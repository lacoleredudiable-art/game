using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Shared;
using Dovus.Game.Composition;
using Dovus.Game.Platform;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Oyuncu mana havuzu â€” PlayerVitals'a paralel (HP'ye dokunmaz).
    /// docs/element-sistemi.json global_rules.resource_system varsayÄ±lanlarÄ±.
    /// BaÄŸlama 2: Consume yetersizse 0'a kilitler. BaÄŸlama 3: CanAfford + EnforceResourceCost.
    /// </summary>
    public sealed class PlayerResource : MonoBehaviour
    {
        ResourceTracker _tracker;
        IClock _clock = UnityFrameClock.Default;

        public float Mana => _tracker != null ? _tracker.Mana : 0f;
        public float MaxMana => _tracker != null ? _tracker.MaxMana : 0f;
        public float Ratio => MaxMana > 0f ? Mathf.Clamp01(Mana / MaxMana) : 0f;

        /// <summary>JSON: max_mana 100 / regen_per_sec 8 / regen_delay_after_cast_sec 1.5.</summary>
        public void Bind(
            float maxMana = 100f,
            float regenPerSec = 8f,
            float regenDelayAfterCastSec = 1.5f)
        {
            _tracker = new ResourceTracker(maxMana, regenPerSec, regenDelayAfterCastSec);
        }

        /// <summary>DÃ¼nya saati â€” duraklatma/hit-stop'ta mana yenilenmez (fail-safe: baÄŸlanmazsa kare saati).</summary>
        public void BindClock(GameClock clock)
        {
            if (clock != null)
                _clock = clock.World;
        }

        /// <summary>CombatTuning.EnforceResourceCost kapÄ±sÄ± â€” tracker yoksa true (fail-open).</summary>
        public bool CanAfford(float cost) => _tracker == null || _tracker.CanAfford(cost);

        /// <summary>Cast maliyeti â€” engellemez; yetmezse 0. (BaÄŸlama 2 bang yolu.)</summary>
        public void Consume(float cost) => Consume(cost, false);

        /// <summary>BÃ¼yÃ¼ KitabÄ± deÄŸiÅŸtirme bonusu: freeCast ise mana dÃ¼ÅŸmez.</summary>
        public void Consume(float cost, bool freeCast) =>
            WeaponManaWaiver.Charge(_tracker, cost, freeCast);

        void Update()
        {
            if (_tracker == null)
                return;
            _tracker.Tick(_clock.DeltaSec);
        }
    }
}
