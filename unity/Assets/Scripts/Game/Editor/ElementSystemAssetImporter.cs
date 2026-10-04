using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>Canonical JSON → kalıcı 12 RuneSO / 10 WeaponSO / 6 ElementSO.</summary>
    public static class ElementSystemAssetImporter
    {
        const string Root = "Assets/Generated/ElementSystem";

        [MenuItem("Dovus/Import Element System v6.1.1 Assets")]
        public static void Import()
        {
            AssetCatalog.ResetStandaloneForEditor();
            ElementSystemDesign design = ElementSystemJsonLoader.LoadRequired();
            EnsureFolder("Assets", "Generated");
            EnsureFolder("Assets/Generated", "ElementSystem");
            EnsureFolder(Root, "Runes");
            EnsureFolder(Root, "Weapons");
            EnsureFolder(Root, "Elements");

            foreach (RuneDefinition source in design.SkillMotor.RuneDefinitions)
            {
                RuneSO asset = LoadOrCreate<RuneSO>($"{Root}/Runes/Rune_{source.Id:00}.asset");
                asset.Import(source);
                EditorUtility.SetDirty(asset);
            }

            foreach (EquipmentItem source in design.Equipment.Items)
            {
                if (source.Slot != EquipmentSlot.Weapon)
                    continue;
                int id = ParseWeaponId(source.Id);
                WeaponSO asset = LoadOrCreate<WeaponSO>($"{Root}/Weapons/Weapon_{id:00}.asset");
                asset.Import(source);
                EditorUtility.SetDirty(asset);
            }

            foreach (ElementPaintNode source in design.SkillMotor.ElementPaints)
            {
                ElementSO asset = LoadOrCreate<ElementSO>($"{Root}/Elements/Element_{source.Id:00}.asset");
                asset.Import(source);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JSONLoader] v6.1.1 SO import complete: 12 RuneSO / 10 WeaponSO / 6 ElementSO.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        static int ParseWeaponId(string value)
        {
            int colon = value != null ? value.LastIndexOf(':') : -1;
            string text = colon >= 0 ? value.Substring(colon + 1) : value;
            return int.TryParse(text, out int id) ? id : 0;
        }
    }
}
