using System.Collections.Generic;
using System.IO;
using System.Text;
using Dovus.Game.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Kilic_VUR / Yay_VUR her karesinde <see cref="WeaponHandPropsView.TryMeasureGripAngles"/>:
    /// bilek-önkol açısı ve silah ekseninin hedef (karakter ileri) yönünden sapması. Evre: impact
    /// karesinden önce ön süre, impact vuruş, sonrası toparlanma. Yalnız raporlar; klibe dokunmaz.
    /// </summary>
    public static class K3GripAngleReport
    {
        const string OutPath = "../tools/verify-out/k3/tutus-acilari.md";

        static readonly (string Clip, string WeaponKey)[] Clips =
        {
            ("Kilic_VUR", "kilic"),
            ("Yay_VUR", "yay"),
        };

        [MenuItem("Dovus/k3/Tutuş açıları (VUR kareleri)")]
        public static string Run()
        {
            Dictionary<string, K3AssetSetup.ClipRow> rows = K3AssetSetup.ReadClipCsv();
            Dictionary<string, AnimationClip> clips = K3AssetSetup.LoadClips();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(K3AssetSetup.PrefabPath);
            UnityEngine.SceneManagement.Scene preview = EditorSceneManager.NewPreviewScene();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            var sb = new StringBuilder();
            sb.AppendLine($"Eşik: bilek-önkol < {WeaponHandPropsViewDefaults.GripWristMaxDeg}°, eksen sapması < {WeaponHandPropsViewDefaults.GripAxisMaxDeviationDeg}°");
            try
            {
                Animator anim = go.GetComponentInChildren<Animator>(true);
                foreach ((string clipName, string key) in Clips)
                {
                    K3AssetSetup.ClipRow row = rows[clipName];
                    sb.AppendLine();
                    sb.AppendLine($"### {clipName} ({key}, kare {row.FirstFrame}-{row.LastFrame}, impact {row.ImpactFrame})");
                    sb.AppendLine("| kare | evre | bilek-önkol ° | eksen sapması ° | eşik |");
                    sb.AppendLine("|---|---|---|---|---|");
                    PlayableGraph graph = PlayableGraph.Create("k3Grip");
                    var output = AnimationPlayableOutput.Create(graph, "k3", anim);
                    var playable = AnimationClipPlayable.Create(graph, clips[clipName]);
                    output.SetSourcePlayable(playable);
                    for (int frame = row.FirstFrame; frame <= row.LastFrame; frame++)
                    {
                        playable.SetTime((frame - row.FirstFrame) / K3AssetSetup.Fps);
                        graph.Evaluate();
                        string phase = frame < row.ImpactFrame ? "ön süre" : frame == row.ImpactFrame ? "vuruş" : "toparlanma";
                        if (!WeaponHandPropsView.TryMeasureGripAngles(anim, key, out WeaponHandPropsView.GripAngles a))
                        {
                            sb.AppendLine($"| {frame} | {phase} | — | — | silah yok |");
                            continue;
                        }
                        string flag = a.WristDeg >= WeaponHandPropsViewDefaults.GripWristMaxDeg ? "BİLEK " : string.Empty;
                        flag += a.AxisDeviationDeg >= WeaponHandPropsViewDefaults.GripAxisMaxDeviationDeg ? "EKSEN" : string.Empty;
                        sb.AppendLine($"| {frame} | {phase} | {a.WristDeg:0.0} | {a.AxisDeviationDeg:0.0} | {(flag.Length == 0 ? "uyar" : flag.Trim())} |");
                    }
                    graph.Destroy();
                }
                AppendGripTrials(sb, go, anim, rows, clips);
            }
            finally
            {
                Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(preview);
            }
            string text = sb.ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllText(OutPath, text);
            Debug.Log("[k3tutus]\n" + text);
            return text;
        }

        static readonly (string Grip, string WeaponKey, string Idle, string Vur)[] GripTrials =
        {
            (K3AssetSetup.GripRight, "kilic", "Loko_Kilic_Idle", "Kilic_VUR"),
            (K3AssetSetup.GripLeft, "yay", "Loko_Yay_Idle", "Yay_VUR"),
        };

        static readonly float[] TrialXDeg = { 0f, -90f, 90f };
        const int IdleSamples = 4;

        /// <summary>
        /// Grip'e ek X dönüşü denemesi (uygulanmaz): idle'da silah ekseninin 0° pozuna göre sapması
        /// (0° = Blender pozu), dikten açısı ve VUR impact karesinde hedef yönden sapma.
        /// </summary>
        static void AppendGripTrials(
            StringBuilder sb,
            GameObject go,
            Animator anim,
            Dictionary<string, K3AssetSetup.ClipRow> rows,
            Dictionary<string, AnimationClip> clips)
        {
            sb.AppendLine();
            sb.AppendLine("### Grip ek X dönüşü denemesi (Vfx_Kabza→Vfx_Tip, uygulanmaz)");
            sb.AppendLine("| tutuş | X ° | idle sapma (0° = Blender) ° | idle dikten ° | VUR impact hedef sapma ° |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach ((string gripName, string key, string idle, string vur) in GripTrials)
            {
                Transform grip = K3AssetSetup.FindDeep(go.transform, gripName);
                Transform weapon = K3AssetSetup.FindDeep(go.transform, WeaponHandPropsView.OwnWeaponName(key));
                if (grip == null || weapon == null)
                {
                    sb.AppendLine($"| {gripName} | — | — | — | silah yok |");
                    continue;
                }
                weapon.gameObject.SetActive(true);
                bool bow = key == "yay";
                Quaternion baseRot = grip.localRotation;
                K3AssetSetup.ClipRow idleRow = rows[idle];
                var reference = new Vector3[IdleSamples];
                foreach (float x in TrialXDeg)
                {
                    grip.localRotation = baseRot * Quaternion.Euler(x, 0f, 0f);
                    float devSum = 0f, vertSum = 0f;
                    for (int i = 0; i < IdleSamples; i++)
                    {
                        int frame = idleRow.FirstFrame + i * (idleRow.LastFrame - idleRow.FirstFrame) / IdleSamples;
                        Vector3 axis = SampleAxis(anim, clips[idle], (frame - idleRow.FirstFrame) / K3AssetSetup.Fps, weapon, bow);
                        if (x == 0f)
                            reference[i] = axis;
                        devSum += Vector3.Angle(axis, reference[i]);
                        float vert = Vector3.Angle(axis, Vector3.up);
                        vertSum += Mathf.Min(vert, 180f - vert);
                    }
                    K3AssetSetup.ClipRow vurRow = rows[vur];
                    Vector3 hit = SampleAxis(anim, clips[vur], (vurRow.ImpactFrame - vurRow.FirstFrame) / K3AssetSetup.Fps, weapon, bow);
                    Vector3 target = Vector3.ProjectOnPlane(anim.transform.forward, Vector3.up);
                    sb.AppendLine($"| {gripName} | {x:+0;-0;0} | {devSum / IdleSamples:0.0} | {vertSum / IdleSamples:0.0} | {Vector3.Angle(hit, target):0.0} |");
                }
                grip.localRotation = baseRot;
            }
        }

        static Vector3 SampleAxis(Animator anim, AnimationClip clip, float timeSec, Transform weapon, bool bow)
        {
            PlayableGraph graph = PlayableGraph.Create("k3GripTrial");
            try
            {
                var output = AnimationPlayableOutput.Create(graph, "k3", anim);
                var playable = AnimationClipPlayable.Create(graph, clip);
                output.SetSourcePlayable(playable);
                playable.SetTime(timeSec);
                graph.Evaluate();
                return WeaponHandPropsView.WeaponAxis(weapon, bow).normalized;
            }
            finally
            {
                graph.Destroy();
            }
        }
    }
}
