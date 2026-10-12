using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dovus.Game.Config
{
    /// <summary>
    /// T10: ayar panelinin TEK erişim noktası. `Combat`/`Prototype` KOPYA değil — Bootstrap'in
    /// kurduğu ÇALIŞMA-ANI nesnelerine referansla bağlanır (T9.1'deki desenin aynısı: "referansla
    /// paylaşıldığı için bir slider alanı yazınca ilgili sistem bir sonraki karede görür").
    /// Bu yüzden `CreateInstance` ile üretilir; hiçbir `.asset` dosyası YOK — sahne/prefab gibi
    /// elle YAML kurulmaz (teknoloji-kararlari §3), ayar nesneleri de öyle.
    /// O-17.8: dosyadaki bir değer yalnız panelden KAYDET'e basılarak yazılmışsa geçerlidir; dosya hangi alanların
    /// panelden kaydedildiğini <c>panelSaved</c> listesinde tutar, işaretsiz alanlar kod/JSON değerinde kalır.
    /// </summary>
    public sealed class TuningConfig : ScriptableObject
    {
        [Serializable]
        sealed class SaveData
        {
            public int version;
            /// <summary>Panelden KAYDET ile yazılmış alan yolları ("combat.Dodge.IframeMs", "prototype.LookAheadM").</summary>
            public List<string> panelSaved = new List<string>();
            public CombatTuning combat = new CombatTuning();
            public GameTuning.PanelFields prototype = new GameTuning.PanelFields();
        }

        public const string CombatPrefix = "combat.";
        public const string PrototypePrefix = "prototype.";

        public CombatTuning Combat { get; private set; }
        public GameTuning Prototype { get; private set; }

        const string FileName = "tuning.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>O5: eski sürümlü kaydın yedeği (atılan değerler kaybolmasın).</summary>
        public static string StaleBackupPath(int storedVersion) =>
            Path.Combine(Application.persistentDataPath, $"tuning.v{storedVersion}.bak.json");

        /// <summary>Son TryLoad eski kaydı attıysa true (panel/test okur).</summary>
        public bool LastLoadDiscardedStale { get; private set; }

        /// <summary>Son TryLoad dosyayı okudu mu (yoksa, bozuksa ya da eski sürümse false).</summary>
        public bool LastLoadReadFile { get; private set; }

        readonly HashSet<string> _panelSaved = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _pendingPanelEdits = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _appliedFromFile = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Panelden KAYDET ile kaydedilmiş alan yolları (dosyaya bu yazılır).</summary>
        public IReadOnlyCollection<string> PanelSavedFields => _panelSaved;

        /// <summary>Panelde değiştirilip henüz KAYDET'e basılmamış alan yolları.</summary>
        public IReadOnlyCollection<string> PendingPanelEdits => _pendingPanelEdits;

        public static TuningConfig Create(CombatTuning combat, GameTuning prototype)
        {
            var config = CreateInstance<TuningConfig>();
            config.Combat = combat;
            config.Prototype = prototype;
            return config;
        }

        /// <summary>Dosyadaki bütün alanlar ve çalışma anındaki değerleri ("combat.*", "prototype.*").</summary>
        public List<KeyValuePair<string, object>> Leaves()
        {
            List<KeyValuePair<string, object>> leaves = TuningFieldPaths.Leaves(Combat, CombatPrefix);
            leaves.AddRange(TuningFieldPaths.Leaves(Prototype.ToPanelFields(), PrototypePrefix));
            return leaves;
        }

        /// <summary>Alanın değeri bu açılışta ayar dosyasından (panelden kaydedilmiş) mı geldi.</summary>
        public bool IsFromFile(string path) => _appliedFromFile.Contains(path);

        public string ToJson()
        {
            var saved = new List<string>(_panelSaved);
            saved.Sort(StringComparer.Ordinal);
            var data = new SaveData
            {
                version = TuningSchema.Version,
                panelSaved = saved,
                combat = Combat,
                prototype = Prototype.ToPanelFields()
            };
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Panel bir değeri değiştirirken çağırır: değişen alanlar "KAYDET bekliyor" diye not edilir. Dosyaya yazılmaz.
        /// </summary>
        public void RecordPanelEdit(Action edit)
        {
            var before = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> kv in Leaves())
                before[kv.Key] = kv.Value;
            edit();
            foreach (KeyValuePair<string, object> kv in Leaves())
            {
                if (!before.TryGetValue(kv.Key, out object old) || !Equals(old, kv.Value))
                    _pendingPanelEdits.Add(kv.Key);
            }
        }

        /// <summary>Panelin KAYDET düğmesi: bekleyen değişiklikler panelden kaydedilmiş sayılır ve dosyaya yazılır.</summary>
        public bool SaveFromPanel()
        {
            _panelSaved.UnionWith(_pendingPanelEdits);
            _pendingPanelEdits.Clear();
            return Write();
        }

        bool Write()
        {
            try
            {
                File.WriteAllText(FilePath, ToJson());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[T10] Ayar kaydedilemedi ({FilePath}): {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// JSON'u YENİ, kopuk bir nesne ağacına çözer (kimse ona referans tutmuyor, güvenli),
        /// sonra yalnız panelden kaydedilmiş alanları ÇALIŞMA-ANI nesnelerinin içine kopyalar.
        /// `JsonUtility.FromJsonOverwrite` KULLANILMADI: iç içe nesnelerin (Dodge/Sentence/...) kimliğini koruyup
        /// korumadığı belgelenmemiş — DodgeState/SentenceEngine gibi tüketiciler o alt nesnelerin referansını
        /// tuttuğu için kimlik kayması sessiz bir "slider artık hiçbir şeyi değiştirmiyor" hatası
        /// üretir. Alan alan kopya (TuningFieldPaths) yalnız yaprak alanı yazar.
        /// </summary>
        public bool TryLoad()
        {
            LastLoadDiscardedStale = false;
            LastLoadReadFile = false;
            _panelSaved.Clear();
            _pendingPanelEdits.Clear();
            _appliedFromFile.Clear();
            try
            {
                if (!File.Exists(FilePath))
                    return false;

                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                    return false;

                // O5: eski sürüm sessizce kazanmaz. Kod varsayılanları kalır; eski dosya yedeklenir,
                // güncel sürümle yeniden yazılır. (BossDamageMigration'ın v0 düzeltmesi bunu kapsar.)
                // Yeniden yazılan dosyada panelden kaydedilmiş alan yoktur: otomatik kayıt kullanıcı değeri sayılmaz.
                if (TuningSchema.Decide(data.version) == TuningSchema.LoadDecision.DiscardStale)
                {
                    LastLoadDiscardedStale = true;
                    try
                    {
                        File.Copy(FilePath, StaleBackupPath(data.version), true);
                    }
                    catch (Exception copyError)
                    {
                        Debug.LogWarning($"[T10] Eski ayar yedeklenemedi: {copyError.Message}");
                    }
                    Debug.LogWarning(
                        $"[T10] tuning.json sürüm {data.version} < {TuningSchema.Version}: eski değerler uygulanmadı, " +
                        $"kod varsayılanları geçerli (yedek: {StaleBackupPath(data.version)}).");
                    Write();
                    return false;
                }

                LastLoadReadFile = true;
                GameTuning.PanelFields livePanel = Prototype.ToPanelFields();
                if (data.panelSaved != null)
                {
                    foreach (string path in data.panelSaved)
                    {
                        if (string.IsNullOrEmpty(path))
                            continue;
                        _panelSaved.Add(path);
                        if (CopyFromFile(path, data, livePanel))
                            _appliedFromFile.Add(path);
                        else
                            Debug.LogWarning($"[T10] tuning.json panelden kaydedilmiş alan '{path}' bu sürümde yok; uygulanmadı.");
                    }
                }
                Prototype.ApplyPanelFields(livePanel);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[T10] Ayar yüklenemedi ({FilePath}): {e.Message}");
                return false;
            }
        }

        bool CopyFromFile(string path, SaveData data, GameTuning.PanelFields livePanel)
        {
            if (path.StartsWith(CombatPrefix, StringComparison.Ordinal))
                return TuningFieldPaths.TryCopy(path.Substring(CombatPrefix.Length), data.combat, Combat);
            if (path.StartsWith(PrototypePrefix, StringComparison.Ordinal))
                return TuningFieldPaths.TryCopy(path.Substring(PrototypePrefix.Length), data.prototype, livePanel);
            return false;
        }

        /// <summary>
        /// "Sıfırla": belgedeki spec varsayılanlarına döner (son kaydedilen JSON'a değil). Panelden kaydedilmiş
        /// işaretler de silinir; dosyaya KAYDET'le yazılır.
        /// </summary>
        public void ResetToDefaults()
        {
            Combat.ResetToDefaults();
            Prototype.ResetPanelFields();
            _panelSaved.Clear();
            _pendingPanelEdits.Clear();
        }

        public void ApplyPreset(TuningPreset preset) => TuningPresets.Apply(preset, Combat, Prototype);
    }
}
