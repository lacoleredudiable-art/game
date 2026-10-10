using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public sealed class RuleEngineV4CatalogAccess
    {
        RuleEngineV4Planner _planner;
        RuleEngineV4Catalog _catalog;

        public RuleEngineV4Catalog Catalog
        {
            get
            {
                EnsureLoaded();
                return _catalog;
            }
        }

        public RuleEngineV4Planner Planner
        {
            get
            {
                EnsureLoaded();
                return _planner;
            }
        }

        void EnsureLoaded()
        {
            if (_planner != null)
                return;
            TextAsset json = Resources.Load<TextAsset>("RuleEngineV4/kural-motoru-v4");
            if (json == null || string.IsNullOrWhiteSpace(json.text))
                throw new System.InvalidOperationException("RuleEngineV4/kural-motoru-v4.json yok.");
            _catalog = RuleEngineV4Catalog.FromJson(json.text);
            _planner = new RuleEngineV4Planner(_catalog);
        }
    }
}
