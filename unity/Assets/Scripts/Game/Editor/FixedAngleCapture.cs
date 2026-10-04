#if UNITY_EDITOR
using Dovus.Core.Capture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Editor
{
    /// <summary>
    /// Batchmode: sabit kamera önayarlarından Prototype sahnesi PNG yakalama (PLAN 2B.16).
    /// Play'e girerken domain reload statik alanları sıfırlar; durum <see cref="SessionState"/>'te tutulur ve
    /// <see cref="InitializeOnLoadAttribute"/> ile kancalar yeniden kurulur.
    /// </summary>
    [InitializeOnLoad]
    public static class FixedAngleCapture
    {
        const string KeyPhase = "Dovus.FixedAngleCapture.Phase";
        const string KeyOutDir = "Dovus.FixedAngleCapture.OutDir";
        const string KeyAngles = "Dovus.FixedAngleCapture.Angles";
        const string KeyPlayEnteredAt = "Dovus.FixedAngleCapture.PlayEnteredAt";

        enum Phase
        {
            Idle,
            EnterPlay,
            WaitSettle,
            Done,
            Failed
        }

        sealed class CaptureRecord
        {
            public string Name;
            public string File;
            public long Bytes;
            public string Sha256;
        }

        static string _outDir;
        static string _anglesPath;
        static CaptureAnglePreset[] _presets;
        static bool _hooksInstalled;

        static Phase _phase
        {
            get => (Phase)SessionState.GetInt(KeyPhase, (int)Phase.Idle);
            set => SessionState.SetInt(KeyPhase, (int)value);
        }

        static double _playEnteredAt
        {
            get => SessionState.GetFloat(KeyPlayEnteredAt, 0f);
            set => SessionState.SetFloat(KeyPlayEnteredAt, (float)value);
        }

        static FixedAngleCapture()
        {
            Phase phase = _phase;
            if (phase == Phase.Idle)
                return;
            _outDir = SessionState.GetString(KeyOutDir, string.Empty);
            _anglesPath = SessionState.GetString(KeyAngles, string.Empty);
            if (!File.Exists(_anglesPath)
                || !CaptureAnglesSchema.TryParse(File.ReadAllText(_anglesPath), out _presets, out _))
            {
                _presets = Array.Empty<CaptureAnglePreset>();
            }
            InstallHooks();
        }

        /// <summary>Batchmode: -executeMethod Dovus.Game.Editor.FixedAngleCapture.CaptureFromCommandLine [-captureOut dir] [-anglesJson path]</summary>
        public static void CaptureFromCommandLine()
        {
            try
            {
                _outDir = ResolveOutDir();
                _anglesPath = ResolveAnglesPath();
                SessionState.SetString(KeyOutDir, _outDir);
                SessionState.SetString(KeyAngles, _anglesPath);
                if (!File.Exists(_anglesPath))
                {
                    Fail("angles.json bulunamadı: " + _anglesPath);
                    return;
                }

                if (!CaptureAnglesSchema.TryParse(File.ReadAllText(_anglesPath), out _presets, out string parseError))
                {
                    Fail(parseError);
                    return;
                }

                if (!File.Exists(FixedAngleCaptureDefaults.ScenePath))
                {
                    Fail("Sahne bulunamadı: " + FixedAngleCaptureDefaults.ScenePath);
                    return;
                }

                InstallHooks();
                EditorSceneManager.OpenScene(FixedAngleCaptureDefaults.ScenePath, OpenSceneMode.Single);
                _phase = Phase.EnterPlay;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }

        static void InstallHooks()
        {
            if (_hooksInstalled)
                return;
            _hooksInstalled = true;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (_phase == Phase.EnterPlay && !EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;

            if (_phase != Phase.WaitSettle || !EditorApplication.isPlaying)
                return;

            double elapsed = EditorApplication.timeSinceStartup - _playEnteredAt;
            if (elapsed < FixedAngleCaptureDefaults.BootstrapSettleSec)
                return;

            try
            {
                LookCapture.PrepareSession();
                var records = CaptureAllPresets(_presets, _outDir);
                WriteManifest(_outDir, records);
                LookCapture.EndSession();
                _phase = Phase.Done;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                return;
            }

            EditorApplication.isPlaying = false;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && _phase == Phase.EnterPlay)
            {
                _playEnteredAt = EditorApplication.timeSinceStartup;
                _phase = Phase.WaitSettle;
            }
            else if (change == PlayModeStateChange.EnteredEditMode && (_phase == Phase.Done || _phase == Phase.Failed))
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.update -= OnEditorUpdate;
                int code = _phase == Phase.Done ? 0 : 1;
                _phase = Phase.Idle;
                EditorApplication.Exit(code);
            }
        }

        static List<CaptureRecord> CaptureAllPresets(CaptureAnglePreset[] presets, string outDir)
        {
            Directory.CreateDirectory(outDir);
            var records = new List<CaptureRecord>(presets.Length);
            foreach (CaptureAnglePreset preset in presets)
            {
                string fileName = SanitizeFileName(preset.Name) + ".png";
                string path = Path.Combine(outDir, fileName);
                if (IsHudPreset(preset.Name) && Camera.main != null)
                {
                    CaptureUtil.CaptureCamera(Camera.main, path, preset.Width, preset.Height);
                }
                else
                {
                    Camera cam = CreatePresetCamera(preset);
                    try
                    {
                        CaptureUtil.CaptureCamera(cam, path, preset.Width, preset.Height);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(cam.gameObject);
                    }
                }

                var info = new FileInfo(path);
                records.Add(new CaptureRecord
                {
                    Name = preset.Name,
                    File = fileName,
                    Bytes = info.Exists ? info.Length : 0,
                    Sha256 = info.Exists ? CaptureUtil.Sha256(path) : string.Empty
                });
            }

            return records;
        }

        static Camera CreatePresetCamera(CaptureAnglePreset preset)
        {
            var go = new GameObject("FixedAngleCapture_" + preset.Name);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Camera.main != null
                ? Camera.main.backgroundColor
                : new Color(0.56f, 0.6f, 0.66f);
            cam.transform.position = new Vector3(preset.PosX, preset.PosY, preset.PosZ);
            cam.transform.rotation = Quaternion.Euler(preset.EulerX, preset.EulerY, preset.EulerZ);
            cam.fieldOfView = preset.Fov;
            cam.farClipPlane = 600f;
            var urpData = go.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
            return cam;
        }

        static bool IsHudPreset(string name) =>
            string.Equals(name, FixedAngleCaptureDefaults.HudAngleName, StringComparison.OrdinalIgnoreCase);

        static void WriteManifest(string outDir, List<CaptureRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"captures\": [");
            for (int i = 0; i < records.Count; i++)
            {
                CaptureRecord r = records[i];
                sb.Append("    { ");
                sb.Append("\"name\": ").Append(JsonString(r.Name)).Append(", ");
                sb.Append("\"file\": ").Append(JsonString(r.File)).Append(", ");
                sb.Append("\"bytes\": ").Append(r.Bytes).Append(", ");
                sb.Append("\"sha256\": ").Append(JsonString(r.Sha256));
                sb.Append(" }");
                if (i < records.Count - 1)
                    sb.Append(',');
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(outDir, "captures.json"), sb.ToString(), new UTF8Encoding(false));
        }

        static string JsonString(string value)
        {
            if (value == null)
                return "null";
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "capture";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return sb.ToString();
        }

        static string ResolveOutDir()
        {
            string fromArg = GetCommandLineArgValue("-captureOut");
            if (!string.IsNullOrWhiteSpace(fromArg))
                return Path.GetFullPath(fromArg);
            return Path.GetFullPath(Path.Combine(UnityProjectRoot(), "Temp", "captures"));
        }

        static string ResolveAnglesPath()
        {
            string fromArg = GetCommandLineArgValue("-anglesJson");
            if (!string.IsNullOrWhiteSpace(fromArg))
                return Path.GetFullPath(fromArg);
            return Path.GetFullPath(Path.Combine(RepoRoot(), FixedAngleCaptureDefaults.AnglesRepoRelative));
        }

        static string UnityProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static string RepoRoot() => Path.GetFullPath(Path.Combine(UnityProjectRoot(), ".."));

        static string GetCommandLineArgValue(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }

        static void Fail(string message)
        {
            Debug.LogError("[FixedAngleCapture] " + message);
            _phase = Phase.Failed;
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            else
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.update -= OnEditorUpdate;
                _phase = Phase.Idle;
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
