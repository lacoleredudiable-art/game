#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.EditorTools
{
    /// <summary>Mixamo/insanoid el eksenlerinden registry tutuşu (geometrik). spec'te yok.</summary>
    public static class WeaponGripAutoCalibrate
    {
        const string RegistryPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";

        enum GripKind
        {
            HaftedRight,
            BowLeft,
            ShieldLeft,
            PalmLeft,
            PalmRight,
            FistRight,
        }

        struct HandFrame
        {
            public Vector3 F;
            public Vector3 N;
            public Vector3 G;
            public bool UsedFingerBones;
        }

        [MenuItem("Dovus/Grip/Auto Calibrate")]
        public static void CalibrateFromMenu()
        {
            Run(out string report);
            Debug.Log(report);
        }

        public static bool Run(out string report)
        {
            var log = new StringBuilder();
            WeaponVisualRegistry registry = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryPath);
            if (registry == null)
            {
                report = "[GripAuto] registry yok: " + RegistryPath;
                Debug.LogError(report);
                return false;
            }

            GameObject prefab = Resources.Load<GameObject>("PlayerVisualOverride");
            if (prefab == null)
            {
                report = "[GripAuto] PlayerVisualOverride yok (Paladin bind?)";
                Debug.LogError(report);
                return false;
            }

            var root = Object.Instantiate(prefab);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Object.DestroyImmediate(root);
                report = "[GripAuto] Animator yok";
                Debug.LogError(report);
                return false;
            }

            animator.Update(0f);
            HandFrame right = BuildHandFrame(animator, true, log);
            HandFrame left = BuildHandFrame(animator, false, log);

            int count = 0;
            foreach (string key in WeaponFeelStore.WeaponKeys)
            {
                WeaponVisualRegistry.PropEntry entry = registry.FindProps(key);
                if (entry == null)
                    continue;
                ApplyWeapon(key, entry, right, left, animator, log);
                count++;
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(root);

            log.Insert(0, $"[GripAuto] {count} silah güncellendi.\n");
            report = log.ToString();
            return count > 0;
        }

        static void ApplyWeapon(
            string key,
            WeaponVisualRegistry.PropEntry entry,
            HandFrame right,
            HandFrame left,
            Animator animator,
            StringBuilder log)
        {
            switch (Classify(key))
            {
                case GripKind.HaftedRight:
                    WriteGrip(entry, right: true, right, PropForward(), PropUp(), right.G, -right.N, right.N * 0.02f + right.F * 0.04f);
                    break;
                case GripKind.BowLeft:
                    WriteGrip(entry, right: false, left, PropForward(), PropUp(), left.G, -left.N, left.N * 0.02f + left.F * 0.07f);
                    break;
                case GripKind.ShieldLeft:
                    Vector3 forearm = ForearmAxis(animator, false);
                    WriteGrip(entry, right: false, left, PropForward(), PropUp(), -left.N, forearm, left.N * -0.03f);
                    break;
                case GripKind.PalmLeft:
                    WriteGrip(entry, right: false, left, PropForward(), PropUp(), -left.N, -left.G, -left.N * 0.04f + left.F * 0.05f);
                    break;
                case GripKind.PalmRight:
                    Vector3 pos = right.N * -0.04f + right.F * 0.05f;
                    WriteGrip(entry, right: true, right, PropForward(), PropUp(), -right.N, -right.G, pos);
                    break;
                case GripKind.FistRight:
                    WriteGrip(entry, right: true, right, PropForward(), PropUp(), right.F, -right.N, Vector3.zero);
                    break;
            }

            log.AppendLine(
                $"{key}: R=({entry.RightLocalPosition.x:F3},{entry.RightLocalPosition.y:F3},{entry.RightLocalPosition.z:F3}) "
                + $"e({entry.RightLocalEulerAngles.x:F1},{entry.RightLocalEulerAngles.y:F1},{entry.RightLocalEulerAngles.z:F1}) "
                + $"L=({entry.LeftLocalPosition.x:F3},{entry.LeftLocalPosition.y:F3},{entry.LeftLocalPosition.z:F3}) "
                + $"e({entry.LeftLocalEulerAngles.x:F1},{entry.LeftLocalEulerAngles.y:F1},{entry.LeftLocalEulerAngles.z:F1})");
        }

        static GripKind Classify(string key) => key switch
        {
            "yay" => GripKind.BowLeft,
            "kalkan" => GripKind.ShieldLeft,
            "kitap" => GripKind.PalmLeft,
            "kure" or "tilsim" => GripKind.PalmRight,
            "yumruk" => GripKind.FistRight,
            _ => GripKind.HaftedRight,
        };

        static Vector3 PropForward() => Vector3.forward;
        static Vector3 PropUp() => Vector3.up;

        static void WriteGrip(
            WeaponVisualRegistry.PropEntry entry,
            bool right,
            HandFrame frame,
            Vector3 propForward,
            Vector3 propUp,
            Vector3 targetForward,
            Vector3 targetUp,
            Vector3 localPos)
        {
            Quaternion rot = AlignAxes(propForward, propUp, targetForward.normalized, targetUp.normalized);
            if (right)
            {
                entry.RightLocalPosition = localPos;
                entry.RightLocalEulerAngles = rot.eulerAngles;
                entry.RightLocalScale = Vector3.one;
            }
            else
            {
                entry.LeftLocalPosition = localPos;
                entry.LeftLocalEulerAngles = rot.eulerAngles;
                entry.LeftLocalScale = Vector3.one;
            }
        }

        static Quaternion AlignAxes(Vector3 srcForward, Vector3 srcUp, Vector3 dstForward, Vector3 dstUp)
        {
            Quaternion toFwd = Quaternion.FromToRotation(srcForward, dstForward);
            Vector3 rotatedUp = toFwd * srcUp;
            Vector3 dstUpProj = Vector3.ProjectOnPlane(dstUp, dstForward).normalized;
            Vector3 rotUpProj = Vector3.ProjectOnPlane(rotatedUp, dstForward).normalized;
            if (dstUpProj.sqrMagnitude < 1e-6f || rotUpProj.sqrMagnitude < 1e-6f)
                return toFwd;
            float angle = Vector3.SignedAngle(rotUpProj, dstUpProj, dstForward);
            return Quaternion.AngleAxis(angle, dstForward) * toFwd;
        }

        static HandFrame BuildHandFrame(Animator animator, bool right, StringBuilder log)
        {
            HumanBodyBones handBone = right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            HumanBodyBones midBone = right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal;
            HumanBodyBones thumbBone = right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal;
            Transform hand = animator.GetBoneTransform(handBone);
            Transform mid = animator.GetBoneTransform(midBone);
            Transform thumb = animator.GetBoneTransform(thumbBone);
            bool fingers = hand != null && mid != null && thumb != null;

            Vector3 f;
            Vector3 t;
            if (fingers)
            {
                f = hand.InverseTransformDirection(mid.position - hand.position).normalized;
                t = hand.InverseTransformDirection(thumb.position - hand.position).normalized;
            }
            else
            {
                log.AppendLine((right ? "Sağ" : "Sol") + " el: parmak kemiği yok — hand local eksen tahmini.");
                f = hand.forward.normalized;
                t = hand.right.normalized;
            }

            Vector3 n = Vector3.Cross(t, f).normalized;
            if (n.sqrMagnitude < 1e-6f)
                n = hand.up.normalized;
            Vector3 palm = -hand.up;
            if (Vector3.Dot(n, palm) < 0f)
                n = -n;
            Vector3 g = Vector3.Cross(f, n).normalized;
            return new HandFrame { F = f, N = n, G = g, UsedFingerBones = fingers };
        }

        static Vector3 ForearmAxis(Animator animator, bool right)
        {
            HumanBodyBones forearm = right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm;
            HumanBodyBones handBone = right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            Transform fore = animator.GetBoneTransform(forearm);
            Transform hand = animator.GetBoneTransform(handBone);
            if (fore == null || hand == null)
                return Vector3.up;
            return hand.InverseTransformDirection(hand.position - fore.position).normalized;
        }
    }
}
#endif
