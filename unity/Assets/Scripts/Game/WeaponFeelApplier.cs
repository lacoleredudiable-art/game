using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Kayıtlı silah hissi → tutuş prop'ları ve düz vuruş bang süresi. Ayar sahnesi + kayıt dosyası.
    /// </summary>
    public sealed class WeaponFeelApplier : MonoBehaviour
    {
        ManifestationDirector _director;
        CombatTuning _combat;
        ActorVisual _visual;
        string _lastKey = "\u0000";
        float _baseBangSec = 0.22f;

        public void Bind(ManifestationDirector director, CombatTuning combat, Transform player)
        {
            _director = director;
            _combat = combat;
            _visual = player != null ? player.GetComponent<ActorVisual>() : null;
            if (_combat?.Manifestation != null)
                _baseBangSec = _combat.Manifestation.BasicStrikeBangSec;
            WeaponFeelStore.EnsureLoaded();
        }

        void LateUpdate()
        {
            if (_director == null)
                return;
            var weapon = _director.EquippedWeapon;
            string key = weapon?.AnimationsKey ?? string.Empty;
            if (string.Equals(key, _lastKey, System.StringComparison.Ordinal))
                return;
            ApplyWeapon(key);
        }

        public void ApplyWeapon(string animationsKey)
        {
            animationsKey ??= string.Empty;
            _lastKey = animationsKey;
            if (_combat?.Manifestation != null)
            {
                WeaponFeelStore.WeaponEntry entry = WeaponFeelStore.Get(animationsKey);
                _combat.Manifestation.BasicStrikeBangSec = Mathf.Max(0.05f, entry.hitBangSec);
            }

            _visual?.SetWeapon(animationsKey, force: true);
            var props = _visual != null ? _visual.GetComponentInChildren<WeaponHandProps>() : null;
            props?.ForceApply(animationsKey);
        }

        public void RefreshFromStore(string animationsKey)
        {
            _lastKey = "\u0000";
            ApplyWeapon(animationsKey);
        }
    }
}
