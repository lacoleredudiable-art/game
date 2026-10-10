using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4CatalogLoader
    {
        static RuleEngineV4Planner _planner;
        static RuleEngineV4Catalog _catalog;

        public static RuleEngineV4Catalog Catalog
        {
            get
            {
                EnsureLoaded();
                return _catalog;
            }
        }

        public static RuleEngineV4Planner Planner
        {
            get
            {
                EnsureLoaded();
                return _planner;
            }
        }

        static void EnsureLoaded()
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
