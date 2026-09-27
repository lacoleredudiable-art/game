using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    [CreateAssetMenu(menuName = "Dovus/Element System/Element", fileName = "Element")]
    public sealed class ElementSO : ScriptableObject
    {
        [SerializeField] int _id;
        [SerializeField] string _displayName;
        [SerializeField] string _namePrefix;
        [SerializeField] Color _color = Color.white;
        [SerializeField] string _colorHex;
        [SerializeField] string _vfx;

        public int Id => _id;
        public string DisplayName => _displayName;
        public string NamePrefix => _namePrefix;
        public Color Color => _color;
        public string ColorHex => _colorHex;
        public string Vfx => _vfx;

        public void Import(in ElementPaintNode source)
        {
            _id = source.Id;
            _displayName = source.Name;
            _namePrefix = source.NamePrefix;
            _colorHex = source.ColorHex;
            _vfx = source.Vfx;
            if (!ColorUtility.TryParseHtmlString(_colorHex, out _color))
                _color = Color.white;
            name = $"Element_{_id:00}_{_displayName}";
        }
    }
}
