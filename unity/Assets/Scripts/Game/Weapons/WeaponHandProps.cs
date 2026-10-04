using Dovus.Game.Actors;
using Dovus.Game.DevTools;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Elde silah sunumu: <see cref="ActorVisual.SetWeapon"/> her değiştiğinde sağ/sol el kemiğine
    /// (<see cref="HumanBodyBones"/>) doğru prop'u takar. Quaternius FBX referansı
    /// <see cref="WeaponVisualRegistry.PropEntry"/>'den (binder doldurur); referans yoksa
    /// (prosedürel silahlarda hep, FBX'li silahlarda binder koşmadıysa) basit primitive
    /// placeholder kurulur — hata fırlatmaz. Sunum amaçlı: hasar/zamanlama buna dokunmaz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponHandProps : MonoBehaviour
    {
        [SerializeField] Animator _animator;

        WeaponVisualRegistry _registry;
        bool _registryLoaded;
        string _currentKey = "\u0000"; // ilk Apply her zaman çalışsın
        GameObject _rightInstance;
        GameObject _leftInstance;

        static Material _metalMat;
        static Material _woodMat;
        static Material _clothMat;
        static Material _orbMat;

        void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        /// <summary>weapons[].animations_key (örn. "kilic"). Idempotent: aynı anahtar no-op.</summary>
        public void Apply(string weaponKey) => Apply(weaponKey, force: false);

        public void ForceApply(string weaponKey)
        {
            _currentKey = "\u0000";
            Apply(weaponKey, force: true);
        }

        void Apply(string weaponKey, bool force)
        {
            weaponKey ??= string.Empty;
            if (!force && string.Equals(_currentKey, weaponKey, System.StringComparison.Ordinal))
                return;
            string previousKey = _currentKey;
            _currentKey = weaponKey;

            if (!_registryLoaded)
            {
                _registry = AssetLoader.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry", null);
                _registryLoaded = true;
            }

            ClearCurrent(previousKey);
            if (_animator == null)
                return;

            WeaponVisualRegistry.PropEntry entry = _registry != null ? _registry.FindProps(weaponKey) : null;
            Transform right = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform left = ResolveLeftAttachBone(weaponKey);

            _rightInstance = Spawn(weaponKey, entry, right, isRight: true);
            _leftInstance = Spawn(weaponKey, entry, left, isRight: false);
            SyncSyntyAtlasHandItems(weaponKey);
        }

        /// <summary>
        /// Takip edilen alanlar + el bone'larına doğrudan takılı her eski prop kökü (binder/force-
        /// refresh sonrası tutarsızlık, görsel değişimi) temizlenir. Play'de <see cref="Destroy"/>
        /// kare sonuna kadar ertelenir; aynı kare içinde (Play doğrulama) tekrar sorgulanırsa eski
        /// obje hâlâ hand bone'un çocuğu görünürdü (cw-2s bulgusu: silah değişince önceki prop
        /// ölçümlerde kalıyordu). Bu yüzden önce <see cref="Transform.SetParent"/>(null) + pasifleştir
        /// (anında hand bone sorgusundan çıkar, görünmez olur), gerçek <see cref="Destroy"/>/
        /// <see cref="DestroyImmediate"/> sonra gelsin fark etmez.
        /// </summary>
        void ClearCurrent(string previousKey)
        {
            DetachAndDestroy(ref _rightInstance);
            DetachAndDestroy(ref _leftInstance);

            if (_animator != null)
            {
                SweepPropRoots(_animator.GetBoneTransform(HumanBodyBones.RightHand));
                SweepPropRoots(_animator.GetBoneTransform(HumanBodyBones.LeftHand));
                // Forearm yalnız Mixamo'da ek takma noktası (bkz. ResolveLeftAttachBone); Synty'de
                // bu bone'un çocuğu gerçek "Hand_L" el kemiği — IsWeaponPropRoot adıyla ayırt
                // edemez, süpürülürse rig kırılır. Mixamo'da forearm çocuğu hep "mixamorig:*"
                // (ayrı korumalı) olduğundan güvenli.
                if (WeaponGripProfile.IsMixamoRig(_animator))
                    SweepPropRoots(_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
            }

            if (_animator != null && previousKey is "kilic" or "kalkan")
                SetSyntyAtlasHandItems(_animator, show: true);
        }

        static void DetachAndDestroy(ref GameObject go)
        {
            if (go == null)
                return;
            DestroyPropObject(go);
            go = null;
        }

        /// <summary>Bone'un doğrudan çocukları arasında kalan her prop kökünü (takip edilmeyenler
        /// dahil — örn. görsel yeniden kurulumundan sızan kalıntı) temizler.</summary>
        static void SweepPropRoots(Transform bone)
        {
            if (bone == null)
                return;
            for (int i = bone.childCount - 1; i >= 0; i--)
            {
                Transform child = bone.GetChild(i);
                if (IsWeaponPropRoot(child.name))
                    DestroyPropObject(child.gameObject);
            }
        }

        static void DestroyPropObject(GameObject go)
        {
            if (go == null)
                return;
            go.transform.SetParent(null, false);
            go.SetActive(false);
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        Transform ResolveLeftAttachBone(string weaponKey)
        {
            Transform hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (WeaponGripProfile.IsMixamoRig(_animator)
                && (weaponKey == "kilic" || weaponKey == "kalkan"))
            {
                Transform forearm = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                if (forearm != null)
                    return forearm;
            }

            return hand;
        }

        GameObject Spawn(string key, WeaponVisualRegistry.PropEntry entry, Transform hand, bool isRight)
        {
            if (hand == null)
                return null;

            GameObject prefab = isRight ? entry?.RightHandPrefab : entry?.LeftHandPrefab;
            GameObject go = prefab != null ? Instantiate(prefab) : BuildPrimitive(key, isRight);
            if (go == null)
                return null;

            go.transform.SetParent(hand, false);
            Vector3 localPos = Vector3.zero;
            Quaternion localRot = Quaternion.identity;
            Vector3 localScale = Vector3.one;
            WeaponGripProfile grip = _animator.GetComponentInParent<WeaponGripProfile>();
            if (entry != null)
            {
                localPos = isRight ? entry.RightLocalPosition : entry.LeftLocalPosition;
                localRot = Quaternion.Euler(isRight ? entry.RightLocalEulerAngles : entry.LeftLocalEulerAngles);
                localScale = isRight ? entry.RightLocalScale : entry.LeftLocalScale;
                if (grip != null && WeaponGripProfile.IsMixamoRig(_animator) && prefab != null)
                    localScale = Vector3.one;
            }

            if (grip != null && WeaponGripProfile.IsMixamoRig(_animator))
                grip.ApplyMixamoWeapon(key, isRight, ref localPos, ref localRot, ref localScale);

            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            if (grip != null && WeaponGripProfile.IsMixamoRig(_animator))
                FitMixamoPropWorldSize(go, key, isRight);
            else if (prefab != null)
                FitPropWorldSize(go, key, isRight);

            StripForProp(go);
            if (key == "kure" && !isRight)
                go.AddComponent<WeaponPropIdleMotion>();
            if (key == "asa" && isRight && WeaponGripProfile.IsMixamoRig(_animator))
                TryAddStaffHeadGlow(go);
            return go;
        }

        static void TryAddStaffHeadGlow(GameObject staffRoot)
        {
            if (!TryRendererBounds(staffRoot, out Bounds b))
                return;
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "StaffHeadGlow";
            Destroy(glow.GetComponent<Collider>());
            glow.transform.SetParent(staffRoot.transform, false);
            glow.transform.localPosition = new Vector3(0f, b.max.y - b.center.y + WeaponHandPropsDefaults.StaffGlowLiftM, 0f);
            glow.transform.localScale = Vector3.one * WeaponHandPropsDefaults.StaffGlowScale;
            var r = glow.GetComponent<Renderer>();
            r.sharedMaterial = OrbMat();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SyncSyntyAtlasHandItems(string weaponKey)
        {
            if (_animator == null || WeaponGripProfile.IsMixamoRig(_animator))
                return;
            bool hideAtlas = weaponKey is "kilic" or "kalkan";
            SetSyntyAtlasHandItems(_animator, show: !hideAtlas);
        }

        static void SetSyntyAtlasHandItems(Animator animator, bool show)
        {
            if (animator == null)
                return;
            foreach (Transform t in animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "SM_Item_Sword" || t.name == "SM_Item_Shield")
                    t.gameObject.SetActive(show);
            }
        }

        /// <summary>Mixamo kılıç: el + uç konumu ve dikeyden açı (derece) — feel doğrulama.</summary>
        public static void LogMixamoSwordAngle(Animator animator, string tag)
        {
            if (animator == null || !WeaponGripProfile.IsMixamoRig(animator))
                return;
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null)
                return;
            Transform blade = FindRightSwordProp(hand);
            if (blade == null)
            {
                DebugConfig.DevLog($"[SwordAngle] {tag} no prop");
                return;
            }

            Vector3 handPos = hand.position;
            // ff-4: Warrior_Sword mesh'inin uzunluk ekseni local Z (blade.forward) — bounds
            // ölçümü (mesh.vertices) grip ucu +Z'de, kabza 0'a yakın gösterdi. Önceki blade.up
            // (Y) varsayımı yanlış eksendi → ölçülen açı gerçek bıçak yönünü yansıtmıyordu.
            Vector3 tip = blade.position + blade.forward * EstimateBladeHalfLength(blade);
            Vector3 shaft = tip - handPos;
            float angleFromUp = shaft.sqrMagnitude > 1e-6f
                ? Vector3.Angle(Vector3.up, shaft)
                : 0f;
            DebugConfig.DevLog(
                $"[SwordAngle] {tag} hand={handPos} tip={tip} angleFromVertical={angleFromUp:0.0}°");
        }

        static Transform FindRightSwordProp(Transform hand)
        {
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform child = hand.GetChild(i);
                if (IsWeaponPropRoot(child.name))
                    return child;
            }

            return null;
        }

        static float EstimateBladeHalfLength(Transform propRoot)
        {
            if (!TryRendererBounds(propRoot.gameObject, out Bounds b))
                return WeaponHandPropsDefaults.BladeHalfLengthFallbackM;
            return Mathf.Max(b.extents.y, b.extents.z) * WeaponHandPropsDefaults.BladeHalfLengthBoundsMult;
        }

        static void FitMixamoPropWorldSize(GameObject go, string weaponKey, bool isRight) =>
            FitPropWorldSize(go, weaponKey, isRight);

        static void FitPropWorldSize(GameObject go, string weaponKey, bool isRight)
        {
            if (!TryRendererBounds(go, out Bounds bounds))
                return;
            float current = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float target = TargetMaxExtentM(weaponKey, isRight);
            if (current < 1e-4f || target <= 0f)
                return;
            float factor = target / current;
            go.transform.localScale *= factor;
        }

        static float TargetMaxExtentM(string weaponKey, bool isRight)
        {
            if (isRight)
            {
                return weaponKey switch
                {
                    "kilic" => WeaponHandPropsDefaults.RightSwordMaxExtentM,
                    "kalkan" => WeaponHandPropsDefaults.RightShieldMaxExtentM,
                    "cekic" => WeaponHandPropsDefaults.RightCekicMaxExtentM,
                    "asa" => WeaponHandPropsDefaults.RightStaffMaxExtentM,
                    "tilsim" => WeaponHandPropsDefaults.RightTilsimMaxExtentM,
                    "top" => WeaponHandPropsDefaults.RightTopMaxExtentM,
                    _ => 0f,
                };
            }

            return weaponKey switch
            {
                "kilic" => WeaponHandPropsDefaults.LeftSwordMaxExtentM,
                "kalkan" => WeaponHandPropsDefaults.LeftShieldMaxExtentM,
                "yay" => WeaponHandPropsDefaults.LeftBowMaxExtentM,
                "kitap" => WeaponHandPropsDefaults.LeftKitapMaxExtentM,
                "kure" => WeaponHandPropsDefaults.LeftKureMaxExtentM,
                _ => 0f,
            };
        }

        static bool TryRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0)
                return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                bounds.Encapsulate(rs[i].bounds);
            return bounds.size.sqrMagnitude > WeaponHandPropsDefaults.BoundsMinSqrMag;
        }

        public static bool IsWeaponPropRootName(string name) => IsWeaponPropRoot(name);

        static bool IsWeaponPropRoot(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (name.StartsWith("Finger", System.StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("IndexFinger", System.StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Thumb", System.StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        static void StripForProp(GameObject go)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Destroy(c);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // --- Prosedürel placeholder'lar (binder referansı yoksa) ------------------------------

        GameObject BuildPrimitive(string key, bool isRight) => key switch
        {
            "kilic" => isRight ? BuildSwordFallback() : BuildRoundShield(),
            "kalkan" => isRight ? BuildSwordFallback(WeaponHandPropsDefaults.ShieldRightSwordFallbackLengthM) : BuildBigShield(),
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
        static GameObject BuildSwordFallback(float lengthM = WeaponHandPropsDefaults.SwordFallbackDefaultLengthM)
        {
            GameObject root = Root("Sword_Placeholder");
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0, lengthM * WeaponHandPropsDefaults.SwordBladeCenterAlongMult), new Vector3(WeaponHandPropsDefaults.SwordBladeWidthM, WeaponHandPropsDefaults.SwordBladeThicknessM, lengthM * WeaponHandPropsDefaults.SwordBladeLengthMult), MetalMat());
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, -lengthM * WeaponHandPropsDefaults.SwordHiltCenterAlongMult), new Vector3(WeaponHandPropsDefaults.SwordHiltRadiusM, lengthM * WeaponHandPropsDefaults.SwordHiltHalfHeightMult, WeaponHandPropsDefaults.SwordHiltRadiusM), WoodMat(),
                Quaternion.Euler(90f, 0f, 0f));
            return root;
        }

        static GameObject BuildStaffFallback()
        {
            const float len = 1.6f;
            GameObject root = Root("Staff_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * WeaponHandPropsDefaults.StaffShaftCenterAlongMult, 0), new Vector3(WeaponHandPropsDefaults.StaffShaftRadiusM, len * WeaponHandPropsDefaults.StaffShaftHalfHeightMult, WeaponHandPropsDefaults.StaffShaftRadiusM), WoodMat());
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, len * WeaponHandPropsDefaults.StaffOrbCenterAlongMult, 0), new Vector3(WeaponHandPropsDefaults.StaffOrbDiameterM, WeaponHandPropsDefaults.StaffOrbDiameterM, WeaponHandPropsDefaults.StaffOrbDiameterM), OrbMat());
            return root;
        }

        static GameObject BuildBowFallback()
        {
            const float len = 1.0f;
            GameObject root = Root("Bow_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(WeaponHandPropsDefaults.BowShaftRadiusM, len * WeaponHandPropsDefaults.BowShaftHalfHeightMult, WeaponHandPropsDefaults.BowShaftRadiusM), WoodMat());
            return root;
        }

        static GameObject BuildRoundShield() => BuildShield("RoundShield_Prop", WeaponHandPropsDefaults.RoundShieldDiameterM, WeaponHandPropsDefaults.RoundShieldThicknessM);
        static GameObject BuildBigShield() => BuildShield("BigShield_Prop", WeaponHandPropsDefaults.BigShieldDiameterM, WeaponHandPropsDefaults.BigShieldThicknessM);

        static GameObject BuildShield(string name, float diameterM, float thicknessM)
        {
            GameObject root = Root(name);
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(diameterM, thicknessM * 0.5f, diameterM), MetalMat(),
                Quaternion.Euler(0f, 0f, 90f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(thicknessM * WeaponHandPropsDefaults.ShieldBossOffsetAlongThicknessMult, 0f, 0f),
                new Vector3(diameterM * WeaponHandPropsDefaults.ShieldBossDiameterMult, diameterM * WeaponHandPropsDefaults.ShieldBossDiameterMult, diameterM * WeaponHandPropsDefaults.ShieldBossDiameterMult), MetalMat());
            return root;
        }

        static GameObject BuildHammer()
        {
            const float len = 1.1f;
            GameObject root = Root("Hammer_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * WeaponHandPropsDefaults.HammerShaftCenterAlongMult, 0), new Vector3(WeaponHandPropsDefaults.HammerShaftRadiusM, len * WeaponHandPropsDefaults.HammerShaftCenterAlongMult, WeaponHandPropsDefaults.HammerShaftRadiusM), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, len * WeaponHandPropsDefaults.HammerHeadCenterAlongMult, 0), new Vector3(len * WeaponHandPropsDefaults.HammerHeadWidthMult, len * WeaponHandPropsDefaults.HammerHeadHeightMult, len * WeaponHandPropsDefaults.HammerHeadWidthMult), MetalMat());
            return root;
        }

        static GameObject BuildBook()
        {
            GameObject root = Root("Book_Prop");
            Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(WeaponHandPropsDefaults.BookCoverWidthM, WeaponHandPropsDefaults.BookCoverHeightM, WeaponHandPropsDefaults.BookCoverDepthM), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, WeaponHandPropsDefaults.BookPageLiftM, 0), new Vector3(WeaponHandPropsDefaults.BookPageWidthM, WeaponHandPropsDefaults.BookPageHeightM, WeaponHandPropsDefaults.BookPageDepthM), ClothMat());
            return root;
        }

        static GameObject BuildOrb()
        {
            GameObject root = Root("Orb_Prop");
            Prim(root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(WeaponHandPropsDefaults.OrbPropDiameterM, WeaponHandPropsDefaults.OrbPropDiameterM, WeaponHandPropsDefaults.OrbPropDiameterM), OrbMat());
            return root;
        }

        static GameObject BuildTalisman()
        {
            GameObject root = Root("Talisman_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(WeaponHandPropsDefaults.TalismanDiscRadiusM, WeaponHandPropsDefaults.TalismanDiscHeightM, WeaponHandPropsDefaults.TalismanDiscRadiusM), MetalMat(),
                Quaternion.Euler(90f, 0f, 0f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, WeaponHandPropsDefaults.TalismanCharmLiftM, 0), new Vector3(WeaponHandPropsDefaults.TalismanCharmDiameterM, WeaponHandPropsDefaults.TalismanCharmDiameterM, WeaponHandPropsDefaults.TalismanCharmDiameterM), MetalMat());
            return root;
        }

        static GameObject BuildCannon()
        {
            GameObject root = Root("Cannon_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, WeaponHandPropsDefaults.CannonBodyOffsetZM), new Vector3(WeaponHandPropsDefaults.CannonBodyRadiusM, WeaponHandPropsDefaults.CannonBodyHalfHeightM, WeaponHandPropsDefaults.CannonBodyRadiusM), MetalMat(),
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
        static Material OrbMat() => _orbMat ??= MakeMat(new Color(0.40f, 0.46f, 0.48f), new Color(0.12f, 0.30f, 0.33f) * WeaponHandPropsDefaults.OrbMatEmissionScale);
    }
}
