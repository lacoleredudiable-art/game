using System.Linq;
using Dovus.Core.Equipment;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.EditorTools
{
    /// <summary>
    /// Animasyon (b) elde silah sunumu için Play Mode yardımcıları — Unity_RunCommand
    /// snippet'lerinden çağrılır (bkz. anim-capture-howto.md). Yalnız normal oyun yollarını
    /// kullanır (ManifestationDirector.SetWeaponLoadout, HexagonInput.TryDebugCastSkill,
    /// BuildSelectScreen'in kendi Button'ı); System.Reflection yok, hasar/zamanlamaya dokunmaz.
    /// </summary>
    public static class AnimPreview
    {
        /// <summary>BUILD SEÇ ekranı açıksa "SAVAŞA BAŞLA" düğmesine basar. Zaten kapalıysa no-op.</summary>
        public static bool EnterFight()
        {
            if (!BuildSelectScreen.IsOpen)
                return true;

            var screen = Object.FindAnyObjectByType<BuildSelectScreen>(FindObjectsInactive.Include);
            if (screen == null)
                return false;

            Button start = screen.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.GetComponentInChildren<Text>()?.text == "SAVAŞA BAŞLA");
            if (start == null || !start.interactable)
                return false;

            start.onClick.Invoke();
            return !BuildSelectScreen.IsOpen;
        }

        /// <summary>
        /// Normal loadout/swap yolu: ManifestationDirector.SetWeaponLoadout. <paramref name="weaponKeyOrName"/>
        /// animations_key ("kilic") ya da görünen ad ("Kılıç") olabilir.
        /// </summary>
        public static bool Equip(string weaponKeyOrName)
        {
            var md = Object.FindAnyObjectByType<ManifestationDirector>();
            if (md == null)
                return false;

            EquipmentItem w = Find(md, weaponKeyOrName);
            if (w == null)
                return false;

            EquipmentItem second = md.EquippedWeapon != null && md.EquippedWeapon.Id != w.Id
                ? md.EquippedWeapon
                : md.AvailableWeapons.FirstOrDefault(x => x.Id != w.Id);
            md.SetWeaponLoadout(w, second);
            return true;
        }

        static EquipmentItem Find(ManifestationDirector md, string key)
        {
            foreach (EquipmentItem w in md.AvailableWeapons)
            {
                if (string.Equals(w.AnimationsKey, key, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(w.Name, key, System.StringComparison.OrdinalIgnoreCase))
                    return w;
            }
            return null;
        }

        /// <summary>Basit vuruş: V611DebugPanel'in de kullandığı debug cast yolu (verb=1, adj=1).</summary>
        public static bool Strike()
        {
            var input = Object.FindAnyObjectByType<HexagonInput>();
            return input != null && input.TryDebugCastSkill(1, 1);
        }

        public static void Shot(string path) => ScreenCapture.CaptureScreenshot(path);
    }
}
