using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Assets;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>Dilim: gramer bellekten; tek dış JSON kural-motoru-v4.json.</summary>
    public static class SliceCombatBootstrap
    {
        static string _json;

        public static string GrammarJson
        {
            get
            {
                if (_json != null)
                    return _json;
                TextAsset v4 = AssetLoader.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource, null);
                if (v4 == null || string.IsNullOrWhiteSpace(v4.text))
                {
                    _json = string.Empty;
                    return _json;
                }
                _json = SliceGrammarDefaults.BuildGrammarJson(v4.text);
                return _json;
            }
        }

        public static bool TryLoad(
            out SkillMotor motor,
            out EquipmentCatalog equipment,
            out AnimationDatabase animations)
        {
            motor = null;
            equipment = null;
            animations = new AnimationDatabase();
            if (!SliceGrammarLoader.TryLoad(out motor))
                return false;
            equipment = EquipmentCatalog.FromJson(GrammarJson);
            return true;
        }
    }
}
