using System;
using System.Collections.Generic;
using System.Text;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Tuning;
using Dovus.Game.Diagnostics;

namespace Dovus.Game.Config
{
    /// <summary>
    /// O-17.8: açılışta tek log bloğu; her ayar için "ad = değer (kaynak: JSON / ayar dosyası / kod)". Ayar dosyasının
    /// bütün alanları ve dilimde JSON'dan gelen, dosyada olmayan ayarlar (<see cref="RuleEngineV4SliceTuningSources"/>).
    /// Android'de Unity bir log mesajını logcat'te 1024 baytta kestiği için blok UTF-8 bayt sınırıyla parçalara bölünür;
    /// her satır <see cref="Tag"/> taşır.
    /// </summary>
    public static class TuningSourceLog
    {
        public const string Tag = "[AYAR-KAYNAK]";
        public const string SourceJson = "JSON";
        public const string SourceFile = "ayar dosyası";
        public const string SourceCode = "kod";
        const int ChunkBytes = 960;

        /// <param name="sliceCatalog">Dilimde kural-motoru-v4.json kataloğu; bayrak kapalıyken null.</param>
        /// <param name="fileRead">Ayar dosyası bu açılışta okundu mu (release'te okunmaz).</param>
        public static List<string> BuildLines(TuningConfig config, RuleEngineV4Catalog sliceCatalog, bool fileRead)
        {
            IReadOnlyList<RuleEngineV4SliceTuningSources.Entry> json =
                sliceCatalog != null ? RuleEngineV4SliceTuningSources.Resolve(sliceCatalog) : null;
            var values = new List<string>();
            int nJson = 0, nFile = 0, nCode = 0;

            var tuning = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> kv in config.Leaves())
            {
                tuning[kv.Key] = kv.Value;
                values.Add(Line(kv.Key, kv.Value, config, json, ref nJson, ref nFile, ref nCode));
            }
            if (json != null)
            {
                Dictionary<string, object> scene = null;
                foreach (RuleEngineV4SliceTuningSources.Entry e in json)
                {
                    if (tuning.ContainsKey(e.Path))
                        continue;
                    if (scene == null)
                    {
                        scene = new Dictionary<string, object>(StringComparer.Ordinal);
                        foreach (KeyValuePair<string, object> kv in TuningFieldPaths.Leaves(config.Prototype, "GameTuning."))
                            scene[kv.Key] = kv.Value;
                    }
                    scene.TryGetValue(e.Path, out object legacy);
                    values.Add(Line(e.Path, legacy, config, json, ref nJson, ref nFile, ref nCode));
                }
            }

            var lines = new List<string>(values.Count + 1)
            {
                $"{Tag} açılış: {values.Count} ayar (JSON {nJson}, ayar dosyası {nFile}, kod {nCode}); " +
                $"dilim JSON'u {(sliceCatalog != null ? "açık" : "kapalı")}; dosya " +
                (fileRead ? $"okundu, panelden kaydedilmiş {config.PanelSavedFields.Count} alan ({TuningConfig.FilePath})" : "okunmadı"),
            };
            lines.AddRange(values);
            return lines;
        }

        public static void Write(TuningConfig config, RuleEngineV4Catalog sliceCatalog, bool fileRead)
        {
            List<string> lines = BuildLines(config, sliceCatalog, fileRead);
            var chunk = new StringBuilder();
            int chunkBytes = 0;
            foreach (string line in lines)
            {
                int lineBytes = Encoding.UTF8.GetByteCount(line) + 1;
                if (chunk.Length > 0 && chunkBytes + lineBytes > ChunkBytes)
                {
                    DebugConfig.DevLog(chunk.ToString());
                    chunk.Clear();
                    chunkBytes = 0;
                }
                if (chunk.Length > 0)
                    chunk.Append('\n');
                chunk.Append(line);
                chunkBytes += lineBytes;
            }
            if (chunk.Length > 0)
                DebugConfig.DevLog(chunk.ToString());
        }

        static string Line(
            string path,
            object tuningValue,
            TuningConfig config,
            IReadOnlyList<RuleEngineV4SliceTuningSources.Entry> json,
            ref int nJson,
            ref int nFile,
            ref int nCode)
        {
            if (RuleEngineV4SliceTuningSources.TryGet(json, path, out RuleEngineV4SliceTuningSources.Entry e))
            {
                if (e.HasJson)
                {
                    nJson++;
                    return $"{Tag} {path} = {TuningFieldPaths.Format(e.Value)} (kaynak: {SourceJson} {e.JsonKey})";
                }
                nCode++;
                object v = e.MissingFallsBackToTuning ? tuningValue : e.Value;
                return $"{Tag} {path} = {TuningFieldPaths.Format(v)} (kaynak: {SourceCode}; JSON'da {e.JsonKey} yok)";
            }
            if (config.IsFromFile(path))
            {
                nFile++;
                return $"{Tag} {path} = {TuningFieldPaths.Format(tuningValue)} (kaynak: {SourceFile})";
            }
            nCode++;
            return $"{Tag} {path} = {TuningFieldPaths.Format(tuningValue)} (kaynak: {SourceCode})";
        }
    }
}
