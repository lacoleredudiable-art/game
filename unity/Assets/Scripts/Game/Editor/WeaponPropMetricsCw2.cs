#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Dovus.Game;
using Dovus.Game.EditorTools;

namespace Dovus.Game.Editor
{
    /// <summary>cw-2 Play doğrulama: silah başına ≥45 kare bekleme, metrik dosyası.</summary>
    public static class WeaponPropMetricsCw2
    {
        public const string OutPath = @"C:\Users\lacol\_cleanup\cw2-metrics.txt";

        static readonly string[] Keys =
        {
            "kilic", "kalkan", "cekic", "yumruk", "yay", "asa", "kitap", "kure", "tilsim", "top",
        };

        static StringBuilder _sb;
        static int _weaponIndex;
        static int _waitFrames;
        static string _tag;
        static Transform _root;
        static Phase _phase;

        enum Phase
        {
            Warmup,
            Paladin,
            Synty,
        }

        public static void RunScheduled()
        {
            _sb = new StringBuilder();
            _phase = Phase.Warmup;
            _waitFrames = 0;
            _weaponIndex = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
                _waitFrames++;
                if (_phase == Phase.Warmup)
                {
                    if (_waitFrames < 90)
                        return;
                    if (!AnimPreview.EnterFight())
                        return;
                    BeginPass("Paladin", FindPlayer());
                    _phase = Phase.Paladin;
                    _waitFrames = 0;
                    return;
                }

                if (_waitFrames < 45)
                    return;

                if (_phase == Phase.Paladin)
                {
                    if (_weaponIndex >= Keys.Length)
                    {
                        var player = FindPlayer();
                        var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
                        if (player != null)
                            player.gameObject.SetActive(false);
                        if (ally != null)
                            ally.gameObject.SetActive(true);
                        BeginPass("Synty", ally != null ? ally.transform : null);
                        _phase = Phase.Synty;
                        _waitFrames = 0;
                        return;
                    }

                    EquipAndLog(_tag, _root, Keys[_weaponIndex]);
                    _weaponIndex++;
                    _waitFrames = 0;
                    return;
                }

                if (_phase == Phase.Synty)
                {
                    if (_weaponIndex >= Keys.Length)
                    {
                        var player = FindPlayer();
                        var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
                        if (player != null)
                            player.gameObject.SetActive(true);
                        if (ally != null)
                            ally.gameObject.SetActive(false);
                        File.WriteAllText(OutPath, _sb.ToString());
                        EditorApplication.update -= Tick;
                        Debug.Log("[WeaponPropMetricsCw2] wrote " + OutPath);
                        return;
                    }

                    EquipAndLog(_tag, _root, Keys[_weaponIndex]);
                    _weaponIndex++;
                    _waitFrames = 0;
                }
        }

        static void BeginPass(string tag, Transform root)
        {
            _tag = tag;
            _root = root;
            _weaponIndex = 0;
        }

        static void EquipAndLog(string tag, Transform root, string key)
        {
            if (root == null)
            {
                _sb.AppendLine(tag + " missing");
                return;
            }

            var visual = root.GetComponent<ActorVisual>() ?? root.GetComponentInChildren<ActorVisual>();
            if (tag == "Paladin")
                AnimPreview.Equip(key);
            visual?.SetWeapon(key, force: true);
            var anim = root.GetComponentInChildren<Animator>();
            if (anim == null)
                return;
            anim.Play("Locomotion", 0, 0f);
            anim.Update(0f);
            foreach (HumanBodyBones side in new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand })
            {
                Transform hand = anim.GetBoneTransform(side);
                string s = side == HumanBodyBones.RightHand ? "R" : "L";
                if (WeaponGripProfile.IsMixamoRig(anim) && (key == "kilic" || key == "kalkan") && s == "L")
                {
                    Transform forearm = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                    if (forearm != null)
                        hand = forearm;
                }

                Transform prop = FindProp(hand);
                if (prop == null)
                    continue;
                float gap = Vector3.Distance(hand.position, prop.position);
                if (!TryBounds(prop.gameObject, out Bounds b))
                    continue;
                bool vis = IsVisible(b);
                float angle = Vector3.Angle(Vector3.up, prop.up);
                _sb.AppendLine(
                    $"{tag}\t{key}\t{s}\t{prop.name}\tgap={gap:F3}\tangle={angle:F1}\tsize={b.size}\tvis={vis}");
            }
        }

        static Transform FindProp(Transform hand)
        {
            if (hand == null)
                return null;
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform child = hand.GetChild(i);
                if (WeaponHandProps.IsWeaponPropRootName(child.name))
                    return child;
            }

            return null;
        }

        static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0)
                return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                bounds.Encapsulate(rs[i].bounds);
            return true;
        }

        static bool IsVisible(Bounds b)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return false;
            Vector3 vp = cam.WorldToViewportPoint(b.center);
            return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        }

        static Transform FindPlayer()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.name == "Player" && t.gameObject.scene.isLoaded)
                    return t;
            }

            return null;
        }
    }
}
#endif
