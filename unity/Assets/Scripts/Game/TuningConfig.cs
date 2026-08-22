using System;
using System.IO;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T10: ayar panelinin TEK erişim noktası. `Combat`/`Prototype` KOPYA değil — Bootstrap'in
    /// kurduğu ÇALIŞMA-ANI nesnelerine referansla bağlanır (T9.1'deki desenin aynısı: "referansla
    /// paylaşıldığı için bir slider alanı yazınca ilgili sistem bir sonraki karede görür").
    /// Bu yüzden `CreateInstance` ile üretilir; hiçbir `.asset` dosyası YOK — sahne/prefab gibi
    /// elle YAML kurulmaz (teknoloji-kararlari §3), ayar nesneleri de öyle.
    /// </summary>
    public sealed class TuningConfig : ScriptableObject
    {
        [Serializable]
        sealed class SaveData
        {
            public CombatTuning combat = new CombatTuning();
            public PrototypeTuning.PanelFields prototype = new PrototypeTuning.PanelFields();
        }

        public CombatTuning Combat { get; private set; }
        public PrototypeTuning Prototype { get; private set; }

        const string FileName = "tuning.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static TuningConfig Create(CombatTuning combat, PrototypeTuning prototype)
        {
            var config = CreateInstance<TuningConfig>();
            config.Combat = combat;
            config.Prototype = prototype;
            return config;
        }

        public string ToJson()
        {
            var data = new SaveData { combat = Combat, prototype = Prototype.ToPanelFields() };
            return JsonUtility.ToJson(data, true);
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, ToJson());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[T10] Ayar kaydedilemedi ({FilePath}): {e.Message}");
            }
        }

        /// <summary>
        /// JSON'u YENİ, kopuk bir nesne ağacına çözer (kimse ona referans tutmuyor, güvenli),
        /// sonra alan alan ÇALIŞMA-ANI nesnelerinin içine kopyalar. `JsonUtility.FromJsonOverwrite`
        /// KULLANILMADI: iç içe nesnelerin (Dodge/Sentence/...) kimliğini koruyup korumadığı
        /// belgelenmemiş — DodgeState/SentenceEngine gibi tüketiciler o alt nesnelerin referansını
        /// tuttuğu için kimlik kayması sessiz bir "slider artık hiçbir şeyi değiştirmiyor" hatası
        /// üretir. CopyFrom yolu bunu garantiler (Core Tuning sınıflarındaki desen).
        /// </summary>
        public bool TryLoad()
        {
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

                if (data.combat != null)
                    Combat.CopyFrom(data.combat);
                if (data.prototype != null)
                    Prototype.ApplyPanelFields(data.prototype);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[T10] Ayar yüklenemedi ({FilePath}): {e.Message}");
                return false;
            }
        }

        /// <summary>"Sıfırla": belgedeki spec varsayılanlarına döner (son kaydedilen JSON'a değil).</summary>
        public void ResetToDefaults()
        {
            Combat.ResetToDefaults();
            Prototype.ResetPanelFields();
        }

        public void ApplyPreset(TuningPreset preset) => TuningPresets.Apply(preset, Combat, Prototype);
    }
}
