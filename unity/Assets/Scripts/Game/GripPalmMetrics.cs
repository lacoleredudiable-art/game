using UnityEngine;

namespace Dovus.Game
{
    /// <summary>El kemiği ↔ prop Grip (origin) mesafesi (Play doğrulama).</summary>
    public static class GripPalmMetrics
    {
        const float PalmForwardM = 0.04f;

        public struct Sample
        {
            public string WeaponKey;
            public bool IsRight;
            public string ParentBoneName;
            public float GripToPalmM;
            public float GripToBoneM;
            public bool HasProp;
            public bool HasGripChild;
        }

        public static Sample Measure(Animator animator, string weaponKey, bool isRight)
        {
            var s = new Sample { WeaponKey = weaponKey, IsRight = isRight };
            if (animator == null)
                return s;

            HumanBodyBones bone = isRight ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            Transform hand = animator.GetBoneTransform(bone);
            if (hand == null)
                return s;

            Transform prop = FindPropOnHand(hand);
            if (prop == null)
                return s;

            s.HasProp = true;
            s.ParentBoneName = prop.parent != null ? prop.parent.name : "?";
            Transform grip = FindGripTransform(prop);
            s.HasGripChild = grip != null;
            Vector3 gripWorld = grip != null ? grip.position : prop.position;
            Vector3 palm = hand.position + hand.TransformDirection(EstimateFingerForwardLocal(hand)) * PalmForwardM;
            s.GripToPalmM = Vector3.Distance(gripWorld, palm);
            s.GripToBoneM = Vector3.Distance(gripWorld, hand.position);
            return s;
        }

        /// <summary>Registry + Grip pivot sonrası Grip düğümünü avuç merkezine kaydırır.</summary>
        public static void NudgePropGripToPalm(Transform hand, Transform propRoot)
        {
            if (hand == null || propRoot == null)
                return;
            Transform grip = FindGripTransform(propRoot);
            Vector3 gripWorld = grip != null ? grip.position : propRoot.position;
            Vector3 palm = hand.position + hand.TransformDirection(EstimateFingerForwardLocal(hand)) * PalmForwardM;
            Vector3 deltaWorld = palm - gripWorld;
            if (deltaWorld.sqrMagnitude < 1e-10f)
                return;
            propRoot.localPosition += hand.InverseTransformVector(deltaWorld);
        }

        public static void LogSample(Sample s)
        {
            if (!s.HasProp)
            {
                Debug.Log($"[GripPalm] {s.WeaponKey} {(s.IsRight ? "R" : "L")} prop=yok");
                return;
            }

            Debug.Log(
                $"[GripPalm] {s.WeaponKey} {(s.IsRight ? "R" : "L")} parent={s.ParentBoneName} "
                + $"grip→palm={s.GripToPalmM * 100f:F1}cm grip→bone={s.GripToBoneM * 100f:F1}cm gripChild={s.HasGripChild}");
        }

        public static Transform FindPropOnHandPublic(Transform hand) => FindPropOnHand(hand);

        static Transform FindPropOnHand(Transform hand)
        {
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform child = hand.GetChild(i);
                if (WeaponHandProps.IsWeaponPropRootName(child.name))
                    return child;
            }

            return null;
        }

        static Transform FindGripTransform(Transform root)
        {
            if (root.name.Equals("Grip", System.StringComparison.OrdinalIgnoreCase))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindGripTransform(root.GetChild(i));
                if (found != null)
                    return found;
            }

            return null;
        }

        static Vector3 EstimateFingerForwardLocal(Transform hand)
        {
            // Mixamo/Synty: parmak yoksa hand.forward + hand.up karışımı (spec yok, geometrik yedek).
            Vector3 f = hand.InverseTransformDirection(hand.forward).normalized;
            if (f.sqrMagnitude < 1e-6f)
                f = Vector3.forward;
            return f;
        }
    }
}
