using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// El prop ofseti: <see cref="WeaponVisualRegistry"/> değerlerine eklenir (Mixamo vs Synty el ekseni).
    /// Görsel prefab kökünde; <see cref="WeaponHandProps"/> animator üzerinden okur.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponGripProfile : MonoBehaviour
    {
        [SerializeField] GripOffset _right = GripOffset.Identity;
        [SerializeField] GripOffset _left = GripOffset.Identity;

        [System.Serializable]
        public struct GripOffset
        {
            public Vector3 LocalPosition;
            public Vector3 LocalEulerAngles;
            public Vector3 LocalScale;

            public static GripOffset Identity => new GripOffset
            {
                LocalPosition = Vector3.zero,
                LocalEulerAngles = Vector3.zero,
                LocalScale = Vector3.one,
            };
        }

        public void ApplyRight(ref Vector3 localPos, ref Quaternion localRot, ref Vector3 localScale)
        {
            Apply(_right, ref localPos, ref localRot, ref localScale);
        }

        public void ApplyLeft(ref Vector3 localPos, ref Quaternion localRot, ref Vector3 localScale)
        {
            Apply(_left, ref localPos, ref localRot, ref localScale);
        }

        static void Apply(GripOffset o, ref Vector3 localPos, ref Quaternion localRot, ref Vector3 localScale)
        {
            localPos += o.LocalPosition;
            localRot *= Quaternion.Euler(o.LocalEulerAngles);
            localScale = new Vector3(
                localScale.x * o.LocalScale.x,
                localScale.y * o.LocalScale.y,
                localScale.z * o.LocalScale.z);
        }

        /// <summary>Mixamo humanoid (mixamorig) — Synty registry ofsetleri yetmez.</summary>
        public static bool IsMixamoRig(Animator animator)
        {
            if (animator == null)
                return false;
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null && hand.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return animator.transform.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Paladin / Mixamo oyuncu görseli için önerilen kılıç tutuşu (editor binder yazır).</summary>
        public static GripOffset DefaultMixamoRightSword() => new GripOffset
        {
            LocalPosition = new Vector3(0.03f, 0.02f, 0.05f),
            LocalEulerAngles = new Vector3(45f, 210f, 0f),
            LocalScale = Vector3.one,
        };

        public static GripOffset DefaultMixamoLeftShield() => new GripOffset
        {
            LocalPosition = new Vector3(0.04f, 0.16f, 0.02f),
            // Quaternius Shield_Heater dekor yüzü +Z; ön cepheye (~sol-ön) bakacak şekilde forearm eksenine paralel.
            LocalEulerAngles = new Vector3(-8f, 210f, 88f),
            LocalScale = Vector3.one,
        };

#if UNITY_EDITOR
        public void SetMixamoDefaults()
        {
            _right = DefaultMixamoRightSword();
            _left = DefaultMixamoLeftShield();
        }
#endif
    }
}
