using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>Görselin kendi silahlarındaki (Silah_*) Vfx_Kabza / Vfx_Tip noktaları ve silah ekseni.</summary>
    public static class WeaponBladePoints
    {
        public const string OwnWeaponPrefix = "Silah_";
        public const string HiltPointName = "Vfx_Kabza";
        public const string TipPointName = "Vfx_Tip";

        /// <summary>
        /// Elde görünen kendi silahının (Silah_*) kabza ve uç noktaları; ikisi de yoksa false.
        /// </summary>
        public static bool TryActiveBladePoints(Transform root, out Transform hilt, out Transform tip)
        {
            hilt = null;
            tip = null;
            if (root == null)
                return false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(false))
            {
                if (!t.name.StartsWith(OwnWeaponPrefix, System.StringComparison.Ordinal))
                    continue;
                hilt = FindChildDeep(t, HiltPointName);
                tip = FindChildDeep(t, TipPointName);
                if (hilt != null && tip != null)
                    return true;
            }
            hilt = null;
            tip = null;
            return false;
        }

        /// <summary>
        /// Silah ekseni = Vfx_Kabza→Vfx_Tip dünya vektörü. Noktası olmayan kayıt prop'unda (Mixamo) yerel eksen.
        /// </summary>
        public static Vector3 WeaponAxis(Transform weapon, bool bow)
        {
            Transform hilt = FindChildDeep(weapon, HiltPointName);
            Transform tip = FindChildDeep(weapon, TipPointName);
            if (hilt != null && tip != null)
                return tip.position - hilt.position;
            // ff-4: Warrior_Sword mesh'inin uzunluk ekseni local Z; yay prop'u yerel +X.
            return bow ? weapon.right : weapon.forward;
        }

        static Transform FindChildDeep(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
            {
                if (c.name == name)
                    return c;
            }
            return null;
        }
    }
}
