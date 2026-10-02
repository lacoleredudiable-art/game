using UnityEngine;

namespace Dovus.Game
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
        public void Apply(string weaponKey)
        {
            weaponKey ??= string.Empty;
            if (string.Equals(_currentKey, weaponKey, System.StringComparison.Ordinal))
                return;
            string previousKey = _currentKey;
            _currentKey = weaponKey;

            if (!_registryLoaded)
            {
                _registry = Resources.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry");
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

        void ClearCurrent(string previousKey)
        {
            if (_rightInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(_rightInstance);
                else
                    DestroyImmediate(_rightInstance);
                _rightInstance = null;
            }

            if (_leftInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(_leftInstance);
                else
                    DestroyImmediate(_leftInstance);
                _leftInstance = null;
            }

            if (_animator != null && previousKey is "kilic" or "kalkan")
                SetSyntyAtlasHandItems(_animator, show: true);
        }

        Transform ResolveLeftAttachBone(string weaponKey)
        {
            Transform hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (weaponKey == "kilic" && WeaponGripProfile.IsMixamoRig(_animator))
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
            {
                if (isRight)
                    grip.ApplyRight(ref localPos, ref localRot, ref localScale);
                else
                    grip.ApplyLeft(ref localPos, ref localRot, ref localScale);
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            if (grip != null && WeaponGripProfile.IsMixamoRig(_animator))
                FitMixamoPropWorldSize(go, key, isRight);

            StripForProp(go);
            return go;
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
            Vector3 tip = blade.position + blade.up * EstimateBladeHalfLength(blade);
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
                return 0.35f;
            return Mathf.Max(b.extents.y, b.extents.z) * 0.85f;
        }

        static void FitMixamoPropWorldSize(GameObject go, string weaponKey, bool isRight)
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
                    "kilic" => 0.92f,
                    "kalkan" => 0.38f,
                    "cekic" => 1.12f,
                    "asa" => 1.65f,
                    "tilsim" => 0.12f,
                    "top" => 0.45f,
                    _ => 0f,
                };
            }

            return weaponKey switch
            {
                "kilic" => 0.65f,
                "kalkan" => 0.92f,
                "yay" => 1.05f,
                "kitap" => 0.24f,
                "kure" => 0.16f,
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
            return bounds.size.sqrMagnitude > 1e-8f;
        }

        /// <summary>Play doğrulama: eldeki prop dünya ölçüsü / lossyScale (Paladin vs Synty kıyas).</summary>
        public static void LogPropDiagnostics(Animator animator, string tag)
        {
            if (animator == null)
                return;
            LogHandProp(animator, HumanBodyBones.RightHand, "right", tag);
            LogHandProp(animator, HumanBodyBones.LeftHand, "left", tag);
        }

        static void LogHandProp(Animator animator, HumanBodyBones bone, string side, string tag)
        {
            Transform hand = animator.GetBoneTransform(bone);
            if (hand == null)
            {
                DebugConfig.DevLog($"[PropDiag] {tag} {side} hand=null");
                return;
            }

            if (hand.childCount == 0)
            {
                DebugConfig.DevLog($"[PropDiag] {tag} {side} (empty) handLossy={hand.lossyScale}");
                return;
            }

            for (int i = 0; i < hand.childCount; i++)
            {
                Transform child = hand.GetChild(i);
                if (!IsWeaponPropRoot(child.name))
                    continue;
                if (!TryRendererBounds(child.gameObject, out Bounds b))
                {
                    DebugConfig.DevLog($"[PropDiag] {tag} {side} {child.name} no-renderer handLossy={hand.lossyScale}");
                    continue;
                }

                DebugConfig.DevLog(
                    $"[PropDiag] {tag} {side} {child.name} bounds={b.size} lossyScale={child.lossyScale} "
                    + $"localPos={child.localPosition} localEuler={child.localEulerAngles}");
            }
        }

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
            "kalkan" => isRight ? BuildSwordFallback(0.55f) : BuildBigShield(),
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
        static GameObject BuildSwordFallback(float lengthM = 0.9f)
        {
            GameObject root = Root("Sword_Placeholder");
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0, lengthM * 0.42f), new Vector3(0.03f, 0.01f, lengthM * 0.78f), MetalMat());
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, -lengthM * 0.06f), new Vector3(0.015f, lengthM * 0.09f, 0.015f), WoodMat(),
                Quaternion.Euler(90f, 0f, 0f));
            return root;
        }

        static GameObject BuildStaffFallback()
        {
            const float len = 1.6f;
            GameObject root = Root("Staff_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * 0.42f, 0), new Vector3(0.025f, len * 0.46f, 0.025f), WoodMat());
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, len * 0.9f, 0), new Vector3(0.07f, 0.07f, 0.07f), OrbMat());
            return root;
        }

        static GameObject BuildBowFallback()
        {
            const float len = 1.0f;
            GameObject root = Root("Bow_Placeholder");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.025f, len * 0.5f, 0.025f), WoodMat());
            return root;
        }

        static GameObject BuildRoundShield() => BuildShield("RoundShield_Prop", 0.6f, 0.07f);
        static GameObject BuildBigShield() => BuildShield("BigShield_Prop", 0.9f, 0.09f);

        static GameObject BuildShield(string name, float diameterM, float thicknessM)
        {
            GameObject root = Root(name);
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(diameterM, thicknessM * 0.5f, diameterM), MetalMat(),
                Quaternion.Euler(0f, 0f, 90f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(thicknessM * 0.6f, 0f, 0f),
                new Vector3(diameterM * 0.18f, diameterM * 0.18f, diameterM * 0.18f), MetalMat());
            return root;
        }

        static GameObject BuildHammer()
        {
            const float len = 1.1f;
            GameObject root = Root("Hammer_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, len * 0.32f, 0), new Vector3(0.045f, len * 0.32f, 0.045f), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, len * 0.66f, 0), new Vector3(len * 0.26f, len * 0.16f, len * 0.26f), MetalMat());
            return root;
        }

        static GameObject BuildBook()
        {
            GameObject root = Root("Book_Prop");
            Prim(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.17f, 0.03f, 0.23f), WoodMat());
            Prim(root.transform, PrimitiveType.Cube, new Vector3(0, 0.008f, 0), new Vector3(0.15f, 0.02f, 0.20f), ClothMat());
            return root;
        }

        static GameObject BuildOrb()
        {
            GameObject root = Root("Orb_Prop");
            Prim(root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), OrbMat());
            return root;
        }

        static GameObject BuildTalisman()
        {
            GameObject root = Root("Talisman_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.09f, 0.01f, 0.09f), MetalMat(),
                Quaternion.Euler(90f, 0f, 0f));
            Prim(root.transform, PrimitiveType.Sphere, new Vector3(0, 0.07f, 0), new Vector3(0.025f, 0.025f, 0.025f), MetalMat());
            return root;
        }

        static GameObject BuildCannon()
        {
            GameObject root = Root("Cannon_Prop");
            Prim(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0, 0.2f), new Vector3(0.09f, 0.22f, 0.09f), MetalMat(),
                Quaternion.Euler(90f, 0f, 0f));
            return root;
        }

        // --- Malzemeler: gri/desatüre palet, URP Simple Lit (yoksa Standard'a düşer) -----------

        static Material MakeMat(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
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
        static Material OrbMat() => _orbMat ??= MakeMat(new Color(0.40f, 0.46f, 0.48f), new Color(0.12f, 0.30f, 0.33f) * 0.35f);
    }
}
