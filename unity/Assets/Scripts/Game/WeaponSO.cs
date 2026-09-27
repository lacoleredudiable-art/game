using Dovus.Core.Equipment;
using UnityEngine;

namespace Dovus.Game
{
    [CreateAssetMenu(menuName = "Dovus/Element System/Weapon", fileName = "Weapon")]
    public sealed class WeaponSO : ScriptableObject
    {
        [SerializeField] int _id;
        [SerializeField] string _displayName;
        [SerializeField] string _type;
        [SerializeField] float _damageMult = 1f;
        [SerializeField] float _castTimeMult = 1f;
        [SerializeField] float _poiseMult = 1f;
        [SerializeField] float _rangeMult = 1f;
        [SerializeField] int _mobilityMod;
        [SerializeField, TextArea] string _identityPassive;
        [SerializeField] int[] _compatibleVerbs = System.Array.Empty<int>();
        [SerializeField] string _animationsKey;

        public int Id => _id;
        public string DisplayName => _displayName;
        public string Type => _type;
        public float DamageMult => _damageMult;
        public float CastTimeMult => _castTimeMult;
        public float PoiseMult => _poiseMult;
        public float RangeMult => _rangeMult;
        public int MobilityMod => _mobilityMod;
        public string IdentityPassive => _identityPassive;
        public int[] CompatibleVerbs => _compatibleVerbs;
        public string AnimationsKey => _animationsKey;

        public void Import(EquipmentItem source)
        {
            _id = ParseNumericId(source?.Id);
            _displayName = source?.Name ?? string.Empty;
            _type = source?.Type ?? string.Empty;
            _damageMult = source?.DamageMult ?? 1f;
            _castTimeMult = source?.CastTimeMult ?? 1f;
            _poiseMult = source?.PoiseMult ?? 1f;
            _rangeMult = source?.RangeMult ?? 1f;
            _mobilityMod = source?.MobilityMod ?? 0;
            _identityPassive = source?.IdentityPassive ?? string.Empty;
            _compatibleVerbs = source?.CompatibleVerbs != null
                ? (int[])source.CompatibleVerbs.Clone()
                : System.Array.Empty<int>();
            _animationsKey = source?.AnimationsKey ?? string.Empty;
            name = $"Weapon_{_id:00}_{_displayName}";
        }

        public EquipmentItem ToEquipmentItem() =>
            new EquipmentItem(
                "weapon:" + _id,
                _displayName,
                EquipmentSlot.Weapon,
                string.Empty,
                _damageMult,
                _castTimeMult,
                _poiseMult,
                _rangeMult,
                _mobilityMod,
                _identityPassive,
                _compatibleVerbs != null
                    ? (int[])_compatibleVerbs.Clone()
                    : System.Array.Empty<int>(),
                _animationsKey,
                _type);

        static int ParseNumericId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;
            int colon = id.LastIndexOf(':');
            string value = colon >= 0 ? id.Substring(colon + 1) : id;
            return int.TryParse(value, out int parsed) ? parsed : 0;
        }
    }
}
