#if UNITY_EDITOR
using System.Collections.Generic;
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>cw-2: 10 silah prop tutuş doğrulama — gerçek Play kareleri, log-only.</summary>
    public static class FeelCaptureCw2
    {
        static readonly string[] WeaponKeys =
        {
            "kilic", "kalkan", "cekic", "yumruk", "yay", "asa", "kitap", "kure", "tilsim", "top",
        };

        static readonly Dictionary<string, float> _syntyBaselineGap = new();

        public static void RunAll()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FeelCaptureCw2] Play modunda değil.");
                return;
            }

            if (!AnimPreview.EnterFight())
            {
                Debug.LogWarning("[FeelCaptureCw2] fight hazır değil; tekrar denenecek.");
                ScheduleFrames(30, RunAll);
                return;
            }

            ScheduleFrames(75, () => ScheduleWeaponPass(0, "Paladin", FindPlayerAnimator));
        }

        static void ScheduleWeaponPass(int index, string tag, System.Func<Animator> findAnim)
        {
            if (index >= WeaponKeys.Length)
            {
                if (tag == "Paladin")
                    ScheduleFrames(30, () => LogSyntyPass("after"));
                return;
            }

            string key = WeaponKeys[index];
            AnimPreview.Equip(key);
            ScheduleFrames(60, () =>
            {
                var anim = findAnim();
                if (anim != null)
                {
                    anim.Play("Locomotion", 0, 0f);
                    anim.Update(0f);
                }

                Camera cam = Camera.main;
                if (anim != null)
                    WeaponHandProps.LogWeaponPropVerification(anim, key, cam, tag);
                ScheduleWeaponPass(index + 1, tag, findAnim);
            });
        }

        static void LogSyntyPass(string label)
        {
            Transform player = FindPlayerRoot();
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            if (player != null)
                player.gameObject.SetActive(false);
            if (ally != null)
                ally.gameObject.SetActive(true);

            ScheduleFrames(20, () =>
            {
                for (int i = 0; i < WeaponKeys.Length; i++)
                {
                    int idx = i;
                    string key = WeaponKeys[idx];
                    AnimPreview.Equip(key);
                    ScheduleFrames(50 + idx * 5, () =>
                    {
                        var anim = FindAllyAnimator();
                        if (anim != null)
                        {
                            anim.Play("Locomotion", 0, 0f);
                            anim.Update(0f);
                            WeaponHandProps.LogWeaponPropVerification(anim, key, Camera.main, "Synty-" + label);
                            float gap = SamplePrimaryGap(anim, key);
                            string id = key + ":" + label;
                            if (label == "before")
                                _syntyBaselineGap[key] = gap;
                            else if (_syntyBaselineGap.TryGetValue(key, out float before))
                                Debug.Log($"[FeelCaptureCw2] Synty {key} gap before={before:F3} after={gap:F3} delta={(gap - before):F4}");
                        }

                        if (idx == WeaponKeys.Length - 1)
                        {
                            if (player != null)
                                player.gameObject.SetActive(true);
                            if (ally != null)
                                ally.gameObject.SetActive(false);
                            Debug.Log("[FeelCaptureCw2] complete");
                        }
                    });
                }
            });
        }

        static float SamplePrimaryGap(Animator anim, string weaponKey)
        {
            bool right = weaponKey is "kilic" or "kalkan" or "cekic" or "asa" or "tilsim" or "top";
            Transform hand = anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (hand == null)
                return -1f;
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform c = hand.GetChild(i);
                if (c.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                return Vector3.Distance(hand.position, c.position);
            }

            return 0f;
        }

        static Animator FindPlayerAnimator()
        {
            Transform t = FindPlayerRoot();
            return t != null ? t.GetComponentInChildren<Animator>() : null;
        }

        static Animator FindAllyAnimator()
        {
            var ally = Object.FindAnyObjectByType<AllyDummy>(FindObjectsInactive.Include);
            return ally != null ? ally.GetComponentInChildren<Animator>() : null;
        }

        static Transform FindPlayerRoot()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.name == "Player" && t.gameObject.scene.isLoaded)
                    return t;
            }

            return null;
        }

        static void ScheduleFrames(int frames, System.Action onDone)
        {
            int left = Mathf.Max(1, frames);
            void Tick()
            {
                left--;
                if (left > 0)
                    return;
                EditorApplication.update -= Tick;
                onDone();
            }

            EditorApplication.update += Tick;
        }
    }
}
#endif
