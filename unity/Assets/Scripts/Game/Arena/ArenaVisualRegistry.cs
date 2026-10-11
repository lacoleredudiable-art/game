using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Arena
{
    /// <summary>
    /// Dilim arenası ortam modelleri: JSON model anahtarı (slice_arena_visuals) → FBX + isteğe bağlı kor katmanı
    /// (VFX_Kor, gövde materyalinin üstüne ikinci materyal). <c>Resources/Arena/</c> altında tek asset;
    /// OrtamAssetSetup yazar, RuleEngineV4SliceArenaVisualsBuilder okur.
    /// </summary>
    public sealed class ArenaVisualRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class ModelEntry
        {
            public string Key = string.Empty;
            public GameObject Model;
            public Material KorOverlay;
        }

        [SerializeField] List<ModelEntry> _models = new();
        [SerializeField] Material _floorGlow;
        [SerializeField] Material _hearthEmbers;

        public List<ModelEntry> Models => _models;

        public Material FloorGlow
        {
            get => _floorGlow;
            set => _floorGlow = value;
        }

        public Material HearthEmbers
        {
            get => _hearthEmbers;
            set => _hearthEmbers = value;
        }

        public ModelEntry Find(string key)
        {
            if (string.IsNullOrEmpty(key) || _models == null)
                return null;
            for (int i = 0; i < _models.Count; i++)
            {
                if (_models[i] != null && _models[i].Model != null
                    && string.Equals(_models[i].Key, key, StringComparison.Ordinal))
                    return _models[i];
            }
            return null;
        }
    }
}
