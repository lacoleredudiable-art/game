using Dovus.Core.RuleEngineV4;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>kural-motoru-v4.json "slice_vfx" (efekt ayarı); bir kez okunur.</summary>
    public static class RuleVfxSettings
    {
        static RuleEngineV4SliceVfx _slice;

        public static RuleEngineV4SliceVfx Slice
        {
            get
            {
                if (_slice != null)
                    return _slice;
                TextAsset json = Resources.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource);
                _slice = json != null
                    ? RuleEngineV4SliceVfx.FromJson(json.text)
                    : new RuleEngineV4SliceVfx();
                return _slice;
            }
            set => _slice = value;
        }
    }
}
