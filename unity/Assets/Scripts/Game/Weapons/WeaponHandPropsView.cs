using Dovus.Game.Actors;
using Dovus.Game.Diagnostics;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Elde silah sunumu: <see cref="ActorView.SetWeapon"/> her değiştiğinde sağ/sol el kemiğine
    /// (<see cref="HumanBodyBones"/>) doğru prop'u takar. Quaternius FBX referansı
    /// <see cref="WeaponVisualRegistry.PropEntry"/>'den (binder doldurur); referans yoksa
    /// (prosedürel silahlarda hep, FBX'li silahlarda binder koşmadıysa) basit primitive
    /// placeholder kurulur — hata fırlatmaz. Sunum amaçlı: hasar/zamanlama buna dokunmaz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class WeaponHandPropsView : MonoBehaviour
    {
        const string OwnWeaponPrefix = "Silah_";

        [SerializeField] Animator _animator;

        WeaponVisualRegistry _registry;
        bool _registryLoaded;
        string _currentKey = "\u0000"; // ilk Apply her zaman çalışsın
        GameObject _rightInstance;
        GameObject _leftInstance;

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
            Transform own = ShowOwnWeapon(weaponKey);

            _rightInstance = IsUnder(own, right) ? null : Spawn(weaponKey, entry, right, isRight: true);
            _leftInstance = IsUnder(own, _animator.GetBoneTransform(HumanBodyBones.LeftHand)) || !HasLeftHandProp(weaponKey)
                ? null
                : Spawn(weaponKey, entry, left, isRight: false);
            SyncSyntyAtlasHandItems(weaponKey);
        }

        /// <summary>Kılıç tek elli: sol elde kalkan yok (kalkan yalnız "kalkan" silahında).</summary>
        static bool HasLeftHandProp(string weaponKey) => weaponKey != "kilic";

        /// <summary>
        /// Görselin kendi silahları (k3: Grip_R / Socket_L altında Silah_*): anahtarın silahı görünür,
        /// diğerleri gizlenir. Mesh'i olmayan yer tutucu sahiplik saymaz; o silahta kayıt prop'u takılır.
        /// </summary>
        Transform ShowOwnWeapon(string weaponKey)
        {
            string wanted = OwnWeaponName(weaponKey);
            Transform shown = null;
            foreach (Transform t in _animator.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(OwnWeaponPrefix, System.StringComparison.Ordinal))
                    continue;
                bool match = t.name == wanted && t.GetComponentInChildren<Renderer>(true) != null;
                t.gameObject.SetActive(match);
                if (match)
                    shown = t;
            }
            return shown;
        }

        /// <summary>weapons[].animations_key → görselin silah objesi adı ("kilic" → "Silah_Kilic").</summary>
        public static string OwnWeaponName(string weaponKey) =>
            string.IsNullOrEmpty(weaponKey)
                ? string.Empty
                : OwnWeaponPrefix + char.ToUpperInvariant(weaponKey[0]) + weaponKey.Substring(1);

        static bool IsUnder(Transform t, Transform bone) => t != null && bone != null && t.IsChildOf(bone);

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
                if (WeaponGripView.IsMixamoRig(_animator))
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
            if (WeaponGripView.IsMixamoRig(_animator) && weaponKey == "kalkan")
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
            WeaponGripView grip = _animator.GetComponentInParent<WeaponGripView>();
            if (entry != null)
            {
                localPos = isRight ? entry.RightLocalPosition : entry.LeftLocalPosition;
                localRot = Quaternion.Euler(isRight ? entry.RightLocalEulerAngles : entry.LeftLocalEulerAngles);
                localScale = isRight ? entry.RightLocalScale : entry.LeftLocalScale;
                if (grip != null && WeaponGripView.IsMixamoRig(_animator) && prefab != null)
                    localScale = Vector3.one;
            }

            if (grip != null && WeaponGripView.IsMixamoRig(_animator))
                grip.ApplyMixamoWeapon(key, isRight, ref localPos, ref localRot, ref localScale);

            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            if (grip != null && WeaponGripView.IsMixamoRig(_animator))
                FitMixamoPropWorldSize(go, key, isRight);
            else if (prefab != null)
                FitPropWorldSize(go, key, isRight);

            StripForProp(go);
            if (key == "kure" && !isRight)
                go.AddComponent<WeaponPropIdleView>();
            if (key == "asa" && isRight && WeaponGripView.IsMixamoRig(_animator))
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
            glow.transform.localPosition = new Vector3(0f, b.max.y - b.center.y + WeaponHandPropsViewDefaults.StaffGlowLiftM, 0f);
            glow.transform.localScale = Vector3.one * WeaponHandPropsViewDefaults.StaffGlowScale;
            var r = glow.GetComponent<Renderer>();
            r.sharedMaterial = OrbMat();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SyncSyntyAtlasHandItems(string weaponKey)
        {
            if (_animator == null || WeaponGripView.IsMixamoRig(_animator))
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

        /// <summary>Bilek-önkol açısı ve silah ekseninin hedef yönden sapması (derece).</summary>
        public readonly struct GripAngles
        {
            public readonly float WristDeg;
            public readonly float AxisDeviationDeg;

            public GripAngles(float wristDeg, float axisDeviationDeg)
            {
                WristDeg = wristDeg;
                AxisDeviationDeg = axisDeviationDeg;
            }

            public bool WithinLimits =>
                WristDeg < WeaponHandPropsViewDefaults.GripWristMaxDeg
                && AxisDeviationDeg < WeaponHandPropsViewDefaults.GripAxisMaxDeviationDeg;
        }

        /// <summary>
        /// Kılıç (sağ el) / Yay (sol el) tutuş ölçümü. Bilek: önkol (dirsek→bilek) ile el (bilek→orta
        /// parmak kökü ya da el kemiği ekseni) arası. Silah ekseni: Kılıç Vfx_Kabza→Vfx_Tip; Yay ok ekseni (yay yerel +X,
        /// kiriş tarafından kabzaya). Hedef yön: karakterin yatay ileri yönü.
        /// </summary>
        public static bool TryMeasureGripAngles(Animator animator, string weaponKey, out GripAngles angles)
        {
            angles = default;
            if (animator == null)
                return false;
            bool bow = weaponKey == "yay";
            Transform forearm = animator.GetBoneTransform(bow ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            Transform hand = animator.GetBoneTransform(bow ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (forearm == null || hand == null)
                return false;
            Transform weapon = FindOwnWeapon(animator, weaponKey) ?? (bow ? null : FindRightSwordProp(hand));
            if (weapon == null)
                return false;

            // Parmak kemiği eşlenmemişse (k3: yumruk blend shape) el yönü kemiğin +Y'si (Mixamo: parmaklara).
            Transform finger = animator.GetBoneTransform(bow ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            Vector3 handDir = finger != null ? finger.position - hand.position : hand.up;
            float wrist = Vector3.Angle(hand.position - forearm.position, handDir);
            Vector3 target = Vector3.ProjectOnPlane(animator.transform.forward, Vector3.up);
            angles = new GripAngles(wrist, Vector3.Angle(WeaponAxis(weapon, bow), target));
            return true;
        }

        /// <summary>Mixamo kılıç/yay: bilek-önkol açısı ve eksen sapması — feel doğrulama.</summary>
        public static void LogMixamoWeaponAngle(Animator animator, string weaponKey, string tag)
        {
            if (animator == null || !WeaponGripView.IsMixamoRig(animator))
                return;
            if (!TryMeasureGripAngles(animator, weaponKey, out GripAngles a))
            {
                DebugConfig.DevLog($"[GripAngle] {tag} {weaponKey} no prop");
                return;
            }
            DebugConfig.DevLog(
                $"[GripAngle] {tag} {weaponKey} wrist={a.WristDeg:0.0}° axisDev={a.AxisDeviationDeg:0.0}° ok={a.WithinLimits}");
        }

        static Transform FindOwnWeapon(Animator animator, string weaponKey)
        {
            string wanted = OwnWeaponName(weaponKey);
            foreach (Transform t in animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == wanted)
                    return t;
            }
            return null;
        }

        static Vector3 WeaponAxis(Transform weapon, bool bow)
        {
            if (bow)
                return weapon.right;
            Transform hilt = FindChildDeep(weapon, "Vfx_Kabza");
            Transform tip = FindChildDeep(weapon, "Vfx_Tip");
            if (hilt != null && tip != null)
                return tip.position - hilt.position;
            // ff-4: Warrior_Sword mesh'inin uzunluk ekseni local Z.
            return weapon.forward;
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
                    "kilic" => WeaponHandPropsViewDefaults.RightSwordMaxExtentM,
                    "kalkan" => WeaponHandPropsViewDefaults.RightShieldMaxExtentM,
                    "cekic" => WeaponHandPropsViewDefaults.RightCekicMaxExtentM,
                    "asa" => WeaponHandPropsViewDefaults.RightStaffMaxExtentM,
                    "tilsim" => WeaponHandPropsViewDefaults.RightTilsimMaxExtentM,
                    "top" => WeaponHandPropsViewDefaults.RightTopMaxExtentM,
                    _ => 0f,
                };
            }

            return weaponKey switch
            {
                "kalkan" => WeaponHandPropsViewDefaults.LeftShieldMaxExtentM,
                "yay" => WeaponHandPropsViewDefaults.LeftBowMaxExtentM,
                "kitap" => WeaponHandPropsViewDefaults.LeftKitapMaxExtentM,
                "kure" => WeaponHandPropsViewDefaults.LeftKureMaxExtentM,
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
            return bounds.size.sqrMagnitude > WeaponHandPropsViewDefaults.BoundsMinSqrMag;
        }

        public static bool IsWeaponPropRootName(string name) => IsWeaponPropRoot(name);

        static bool IsWeaponPropRoot(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            // Görselin kendi tutuş soketleri (k3 Grip_R / Socket_L) prop değil; silinirse silahı da gider.
            if (name.StartsWith("Grip_", System.StringComparison.Ordinal)
                || name.StartsWith("Socket_", System.StringComparison.Ordinal))
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
    }
}
