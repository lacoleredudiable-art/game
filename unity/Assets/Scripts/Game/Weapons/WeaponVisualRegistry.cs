using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Elde silah + arketip override controller kaydı. <c>Resources/Animation/</c> altında
    /// tek asset; MixamoArchetypeBind ve K3AssetSetup yazar, <see cref="ActorView"/> okur. Mixamo override
    /// alanları bu PC dışında boş (gitignored) — eksikse temel controller/pozsuz silah kullanılır,
    /// hata fırlatmaz. <see cref="Props"/>: silah başına el prop'u (Quaternius FBX referansı ya da
    /// boş — <see cref="WeaponHandPropsView"/> boşsa primitive placeholder kurar). WeaponKey =
    /// weapons[].animations_key (<see cref="WeaponArchetypeMap"/> girdileriyle birebir).
    /// </summary>
    public sealed class WeaponVisualRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class ArchetypeEntry
        {
            public string ArchetypeKey = string.Empty;
            public AnimatorOverrideController Override;
        }

        [Serializable]
        public sealed class PropEntry
        {
            public string WeaponKey = string.Empty;
            public GameObject RightHandPrefab;
            public Vector3 RightLocalPosition;
            public Vector3 RightLocalEulerAngles;
            public Vector3 RightLocalScale = Vector3.one;
            public GameObject LeftHandPrefab;
            public Vector3 LeftLocalPosition;
            public Vector3 LeftLocalEulerAngles;
            public Vector3 LeftLocalScale = Vector3.one;
        }

        /// <summary>
        /// Silah başına yürüme seti (k3): override, iskeletin temel controller'ındaki <c>Loko_*</c> kliplerini
        /// o silahın <c>Loko_&lt;Silah&gt;_*</c> setiyle değiştirir. K3AssetSetup üretir; dosyalar repoda.
        /// </summary>
        [Serializable]
        public sealed class WeaponLocomotionEntry
        {
            public string WeaponKey = string.Empty;
            public AnimatorOverrideController Override;
        }

        [SerializeField] List<ArchetypeEntry> _archetypes = new();
        [SerializeField] List<PropEntry> _props = new();
        [SerializeField] List<WeaponLocomotionEntry> _weaponLocomotion = new();

        public List<ArchetypeEntry> Archetypes => _archetypes;
        public List<PropEntry> Props => _props;
        public List<WeaponLocomotionEntry> WeaponLocomotion => _weaponLocomotion;

        /// <summary>
        /// Silah değişiminde Animator'a verilecek controller. İskeletin silah yürüme seti varsa (k3) o silahınki;
        /// iskelet sete sahip ama silahın kaydı yoksa v4'te [V4-FALLBACK] ve temel controller. Sete sahip olmayan
        /// iskelette arketip override'ı (yalnız aynı temel controller'ınsa), o da yoksa temel controller.
        /// </summary>
        public RuntimeAnimatorController ResolveController(
            string weaponKey, string archetypeKey, RuntimeAnimatorController baseController)
        {
            if (HasWeaponLocomotion(baseController))
            {
                AnimatorOverrideController loco = FindWeaponLocomotion(weaponKey, baseController);
                if (loco != null)
                    return loco;
                Debug.LogError($"[V4-FALLBACK] silah yürüme klibi: '{weaponKey}' için '{baseController.name}' kaydı yok " +
                               "(WeaponVisualRegistry; Dovus/k3/Silah yürüme setleri); temel controller'ın seti kalır");
                return baseController;
            }
            RuntimeAnimatorController ctrl = FindOverride(archetypeKey);
            // Override yalnız kendi temel controller'ında anlamlı; başka iskeletin controller'ını ezmez.
            bool foreign = ctrl is AnimatorOverrideController ovr && ovr.runtimeAnimatorController != baseController;
            return ctrl != null && !foreign ? ctrl : baseController;
        }

        /// <summary>Bu temel controller'ın en az bir silah yürüme seti kaydı varsa true (iskelet silah başına set kullanır).</summary>
        public bool HasWeaponLocomotion(RuntimeAnimatorController baseController)
        {
            if (baseController == null || _weaponLocomotion == null)
                return false;
            for (int i = 0; i < _weaponLocomotion.Count; i++)
            {
                if (BaseOf(_weaponLocomotion[i]) == baseController)
                    return true;
            }
            return false;
        }

        public AnimatorOverrideController FindWeaponLocomotion(string weaponKey, RuntimeAnimatorController baseController)
        {
            if (string.IsNullOrEmpty(weaponKey) || baseController == null || _weaponLocomotion == null)
                return null;
            for (int i = 0; i < _weaponLocomotion.Count; i++)
            {
                if (BaseOf(_weaponLocomotion[i]) == baseController
                    && string.Equals(_weaponLocomotion[i].WeaponKey, weaponKey, StringComparison.Ordinal))
                    return _weaponLocomotion[i].Override;
            }
            return null;
        }

        static RuntimeAnimatorController BaseOf(WeaponLocomotionEntry e) =>
            e != null && e.Override != null ? e.Override.runtimeAnimatorController : null;

        public RuntimeAnimatorController FindOverride(string archetypeKey)
        {
            if (string.IsNullOrEmpty(archetypeKey) || _archetypes == null)
                return null;
            for (int i = 0; i < _archetypes.Count; i++)
            {
                if (_archetypes[i] != null
                    && string.Equals(_archetypes[i].ArchetypeKey, archetypeKey, StringComparison.Ordinal)
                    && _archetypes[i].Override != null)
                    return _archetypes[i].Override;
            }
            return null;
        }

        public PropEntry FindProps(string weaponKey)
        {
            if (string.IsNullOrEmpty(weaponKey) || _props == null)
                return null;
            for (int i = 0; i < _props.Count; i++)
            {
                if (_props[i] != null && string.Equals(_props[i].WeaponKey, weaponKey, StringComparison.Ordinal))
                    return _props[i];
            }
            return null;
        }
    }
}
