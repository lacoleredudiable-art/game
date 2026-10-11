using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    public sealed partial class WeaponHandPropsView
    {
        static Material _metalMat;
        static Material _woodMat;
        static Material _clothMat;
        static Material _orbMat;

        // --- Prosedürel placeholder'lar (binder referansı yoksa) ------------------------------

        GameObject BuildPrimitive(string key, bool isRight) => key switch
        {
            "kilic" => isRight ? BuildSwordFallback() : BuildRoundShield(),
            "kalkan" => isRight ? BuildSwordFallback(WeaponHandPropsViewDefaults.ShieldRightSwordFallbackLengthM) : BuildBigShield(),
            "cekic" => isRight ? BuildHammer() : null,
            "yay" => isRight ? null : BuildBowFallback(),
            "asa" => isRight ? BuildStaffFallback() : null,
            "kitap" => isRight ? null : BuildBook(),
            "kure" => isRight ? null : BuildOrb(),
            "tilsim" => isRight ? BuildTalisman() : null,
            "top" => isRight ? BuildCannon() : null,
            _ => null, // "yumruk" ve bilinmeyen anahtarlar: çıplak el.
        };

        static GameObject Root(string name)
        {
            var go = new GameObject(name);
            return go;
        }

        static GameObject Prim(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat, Quaternion? localRot = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Warrior_Sword referansı yoksa: basit düz bıçak, hilt'siz, ~0,9 m (ölçek ile ayarlanır).</summary>
        static GameObject BuildSwordFallback(float lengthM = WeaponHandPropsViewDefaults.SwordFallbackDefaultLengthM)
        {
            GameObject root = Root("Sword_Placeholder");
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0, lengthM * WeaponHandPropsViewDefaults.SwordBladeCenterAlongMult), new Vector3(WeaponHandPropsViewDefaults.SwordBladeWidthM, WeaponHandPropsViewDefaults.SwordBladeThicknessM, lengthM * WeaponHandPropsViewDefaults.SwordBladeLengthMult), MetalMat());
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, -lengthM * WeaponHandPropsViewDefaults.SwordHiltCenterAlongMult), new Vector3(WeaponHandPropsViewDefaults.SwordHiltRadiusM, lengthM * WeaponHandPropsViewDefaults.SwordHiltHalfHeightMult, WeaponHandPropsViewDefaults.SwordHiltRadiusM), WoodMat(),
                Quaternion.Euler(90f, 0f, 0f));
            return root;
        }

        static GameObject BuildStaffFallback()
        {
            const float len = 1.6f;
            GameObject root = Root("Staff_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * WeaponHandPropsViewDefaults.StaffShaftCenterAlongMult, 0), new Vector3(WeaponHandPropsViewDefaults.StaffShaftRadiusM, len * WeaponHandPropsViewDefaults.StaffShaftHalfHeightMult, WeaponHandPropsViewDefaults.StaffShaftRadiusM), WoodMat());
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, len * WeaponHandPropsViewDefaults.StaffOrbCenterAlongMult, 0), new Vector3(WeaponHandPropsViewDefaults.StaffOrbDiameterM, WeaponHandPropsViewDefaults.StaffOrbDiameterM, WeaponHandPropsViewDefaults.StaffOrbDiameterM), OrbMat());
            return root;
        }

        static GameObject BuildBowFallback()
        {
            const float len = 1.0f;
            GameObject root = Root("Bow_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(WeaponHandPropsViewDefaults.BowShaftRadiusM, len * WeaponHandPropsViewDefaults.BowShaftHalfHeightMult, WeaponHandPropsViewDefaults.BowShaftRadiusM), WoodMat());
            return root;
        }

        static GameObject BuildRoundShield() => BuildShield("RoundShield_Prop", WeaponHandPropsViewDefaults.RoundShieldDiameterM, WeaponHandPropsViewDefaults.RoundShieldThicknessM);
        static GameObject BuildBigShield() => BuildShield("BigShield_Prop", WeaponHandPropsViewDefaults.BigShieldDiameterM, WeaponHandPropsViewDefaults.BigShieldThicknessM);

        static GameObject BuildShield(string name, float diameterM, float thicknessM)
        {
            GameObject root = Root(name);
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(diameterM, thicknessM * 0.5f, diameterM), MetalMat(),
                Quaternion.Euler(0f, 0f, 90f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(thicknessM * WeaponHandPropsViewDefaults.ShieldBossOffsetAlongThicknessMult, 0f, 0f),
                new Vector3(diameterM * WeaponHandPropsViewDefaults.ShieldBossDiameterMult, diameterM * WeaponHandPropsViewDefaults.ShieldBossDiameterMult, diameterM * WeaponHandPropsViewDefaults.ShieldBossDiameterMult), MetalMat());
            return root;
        }

        static GameObject BuildHammer()
        {
            const float len = 1.1f;
            GameObject root = Root("Hammer_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * WeaponHandPropsViewDefaults.HammerShaftCenterAlongMult, 0), new Vector3(WeaponHandPropsViewDefaults.HammerShaftRadiusM, len * WeaponHandPropsViewDefaults.HammerShaftCenterAlongMult, WeaponHandPropsViewDefaults.HammerShaftRadiusM), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, len * WeaponHandPropsViewDefaults.HammerHeadCenterAlongMult, 0), new Vector3(len * WeaponHandPropsViewDefaults.HammerHeadWidthMult, len * WeaponHandPropsViewDefaults.HammerHeadHeightMult, len * WeaponHandPropsViewDefaults.HammerHeadWidthMult), MetalMat());
            return root;
        }

        static GameObject BuildBook()
        {
            GameObject root = Root("Book_Prop");
            Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(WeaponHandPropsViewDefaults.BookCoverWidthM, WeaponHandPropsViewDefaults.BookCoverHeightM, WeaponHandPropsViewDefaults.BookCoverDepthM), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, WeaponHandPropsViewDefaults.BookPageLiftM, 0), new Vector3(WeaponHandPropsViewDefaults.BookPageWidthM, WeaponHandPropsViewDefaults.BookPageHeightM, WeaponHandPropsViewDefaults.BookPageDepthM), ClothMat());
            return root;
        }

        static GameObject BuildOrb()
        {
            GameObject root = Root("Orb_Prop");
            Prim(root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(WeaponHandPropsViewDefaults.OrbPropDiameterM, WeaponHandPropsViewDefaults.OrbPropDiameterM, WeaponHandPropsViewDefaults.OrbPropDiameterM), OrbMat());
            return root;
        }

        static GameObject BuildTalisman()
        {
            GameObject root = Root("Talisman_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(WeaponHandPropsViewDefaults.TalismanDiscRadiusM, WeaponHandPropsViewDefaults.TalismanDiscHeightM, WeaponHandPropsViewDefaults.TalismanDiscRadiusM), MetalMat(),
                Quaternion.Euler(90f, 0f, 0f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, WeaponHandPropsViewDefaults.TalismanCharmLiftM, 0), new Vector3(WeaponHandPropsViewDefaults.TalismanCharmDiameterM, WeaponHandPropsViewDefaults.TalismanCharmDiameterM, WeaponHandPropsViewDefaults.TalismanCharmDiameterM), MetalMat());
            return root;
        }

        static GameObject BuildCannon()
        {
            GameObject root = Root("Cannon_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, WeaponHandPropsViewDefaults.CannonBodyOffsetZM), new Vector3(WeaponHandPropsViewDefaults.CannonBodyRadiusM, WeaponHandPropsViewDefaults.CannonBodyHalfHeightM, WeaponHandPropsViewDefaults.CannonBodyRadiusM), MetalMat(),
                Quaternion.Euler(90f, 0f, 0f));
            return root;
        }

        // --- Malzemeler: gri/desatüre palet, URP Simple Lit (yoksa Standard'a düşer) -----------

        static Material MakeMat(Color color, Color? emission = null)
        {
            Shader shader = AssetLoader.FindShader("Universal Render Pipeline/Simple Lit", null)
                ?? AssetLoader.FindShader("Universal Render Pipeline/Lit", null)
                ?? AssetLoader.FindShader("Standard", null);
            var mat = new Material(shader) { color = color };
            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        static Material MetalMat() => _metalMat ??= MakeMat(new Color(0.45f, 0.45f, 0.46f));
        static Material WoodMat() => _woodMat ??= MakeMat(new Color(0.22f, 0.18f, 0.15f));
        static Material ClothMat() => _clothMat ??= MakeMat(new Color(0.58f, 0.55f, 0.50f));
        static Material OrbMat() => _orbMat ??= MakeMat(new Color(0.40f, 0.46f, 0.48f), new Color(0.12f, 0.30f, 0.33f) * WeaponHandPropsViewDefaults.OrbMatEmissionScale);
    }
}
