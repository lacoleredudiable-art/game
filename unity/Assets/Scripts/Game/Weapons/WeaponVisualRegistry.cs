using Dovus.Game.Actors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Elde silah + arketip override controller kaydı. <c>Resources/Animation/</c> altında
    /// tek asset; MixamoArchetypeBind yazar, <see cref="ActorView"/> okur. Mixamo override
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

        [SerializeField] List<ArchetypeEntry> _archetypes = new();
        [SerializeField] List<PropEntry> _props = new();

        public List<ArchetypeEntry> Archetypes => _archetypes;
        public List<PropEntry> Props => _props;

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
