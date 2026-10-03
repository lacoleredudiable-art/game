using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// El prop ofseti: yalnız <see cref="WeaponVisualRegistry.PropEntry"/> (Mixamo/Synty ayrımı yok).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponGripProfile : MonoBehaviour
    {
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

        public static bool TryGetRegistryGrip(WeaponVisualRegistry.PropEntry entry, bool isRight, out GripOffset offset)
        {
            offset = GripOffset.Identity;
            if (entry == null)
                return false;
            offset = new GripOffset
            {
                LocalPosition = isRight ? entry.RightLocalPosition : entry.LeftLocalPosition,
                LocalEulerAngles = isRight ? entry.RightLocalEulerAngles : entry.LeftLocalEulerAngles,
                LocalScale = isRight ? entry.RightLocalScale : entry.LeftLocalScale,
            };
            if (offset.LocalScale.sqrMagnitude < 0.0001f)
                offset.LocalScale = Vector3.one;
            return true;
        }

        public static void ApplyRegistry(
            WeaponVisualRegistry.PropEntry entry,
            bool isRight,
            ref Vector3 localPos,
            ref Quaternion localRot,
            ref Vector3 localScale)
        {
            if (!TryGetRegistryGrip(entry, isRight, out GripOffset o))
                return;
            Apply(o, ref localPos, ref localRot, ref localScale);
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

        /// <summary>Mixamo humanoid (mixamorig) — sol kalkan forearm takması vb.</summary>
        public static bool IsMixamoRig(Animator animator)
        {
            if (animator == null)
                return false;
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null && hand.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return animator.transform.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

#if UNITY_EDITOR
        public void SetMixamoDefaults()
        {
            // Eski binder çağrısı; tutuş artık WeaponVisualRegistry'de kalibre edilir.
        }
#endif
    }
}
