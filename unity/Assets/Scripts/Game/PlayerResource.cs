using Dovus.Core.Combat;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Oyuncu mana havuzu — PlayerVitals'a paralel (HP'ye dokunmaz).
    /// docs/element-sistemi.json global_rules.resource_system varsayılanları.
    /// Bağlama 2: yetersiz mana cast'i engellemez (Consume 0'a kilitler).
    /// </summary>
    public sealed class PlayerResource : MonoBehaviour
    {
        ResourceTracker _tracker;

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

        /// <summary>Cast maliyeti — engellemez; yetmezse 0.</summary>
        public void Consume(float cost) => _tracker?.Consume(cost);

        void Update()
        {
            if (_tracker == null)
                return;
            _tracker.Tick(Time.deltaTime);
        }
    }
}
