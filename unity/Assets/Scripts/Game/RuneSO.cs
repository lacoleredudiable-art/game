using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    [CreateAssetMenu(menuName = "Dovus/Element System/Rune", fileName = "Rune")]
    public sealed class RuneSO : ScriptableObject
    {
        [SerializeField] int _id;
        [SerializeField] string _displayName;
        [SerializeField] string _verbFace;
        [SerializeField] string _adjectiveFace;
        [SerializeField] string _adjectivePrefix;
        [SerializeField] string _verbNoun;
        [SerializeField] string _family;
        [SerializeField] string _category;
        [SerializeField] string _targetMode;
        [SerializeField, TextArea] string _baseEffect;
        [SerializeField] float _passiveDurationDefault;

        public int Id => _id;
        public string DisplayName => _displayName;
        public string VerbFace => _verbFace;
        public string AdjectiveFace => _adjectiveFace;
        public string AdjectivePrefix => _adjectivePrefix;
        public string VerbNoun => _verbNoun;
        public string Family => _family;
        public string Category => _category;
        public string TargetMode => _targetMode;
        public string BaseEffect => _baseEffect;
        public float PassiveDurationDefault => _passiveDurationDefault;

        public void Import(in RuneDefinition source)
        {
            _id = source.Id;
            _displayName = source.Name;
            _verbFace = source.VerbFace;
            _adjectiveFace = source.AdjectiveFace;
            _adjectivePrefix = source.AdjectivePrefix;
            _verbNoun = source.VerbNoun;
            _family = source.Family;
            _category = source.Category;
            _targetMode = source.TargetMode;
            _baseEffect = source.BaseEffect;
            _passiveDurationDefault = source.PassiveDurationDefault;
            name = $"Rune_{_id:00}_{_displayName}";
        }
    }
}
