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
    }
}
