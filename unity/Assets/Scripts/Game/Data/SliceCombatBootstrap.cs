using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Data
{
    /// <summary>Dilim: cümle grameri + silahlar; element-sistemi.json kullanılmaz.</summary>
    public static class SliceCombatBootstrap
    {
        static string _json;

        public static string GrammarJson
        {
            get
            {
                if (_json != null)
                    return _json;
                TextAsset asset = AssetLoader.Load<TextAsset>(SliceSentenceGrammarLoader.ResourcePath, null);
                _json = asset != null ? asset.text : string.Empty;
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
            string json = GrammarJson;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            motor = SkillMotor.FromJson(json);
            if (!motor.IsV61)
                return false;
            equipment = EquipmentCatalog.FromJson(json);
            return true;
        }
    }
}
