using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using System;
using System.IO;
using UnityEngine;

namespace Dovus.Game.Config
{
    /// <summary>
    /// T10: ayar panelinin TEK eriÅŸim noktasÄ±. `Combat`/`Prototype` KOPYA deÄŸil â€” Bootstrap'in
    /// kurduÄŸu Ã‡ALIÅMA-ANI nesnelerine referansla baÄŸlanÄ±r (T9.1'deki desenin aynÄ±sÄ±: "referansla
    /// paylaÅŸÄ±ldÄ±ÄŸÄ± iÃ§in bir slider alanÄ± yazÄ±nca ilgili sistem bir sonraki karede gÃ¶rÃ¼r").
    /// Bu yÃ¼zden `CreateInstance` ile Ã¼retilir; hiÃ§bir `.asset` dosyasÄ± YOK â€” sahne/prefab gibi
    /// elle YAML kurulmaz (teknoloji-kararlari Â§3), ayar nesneleri de Ã¶yle.
    /// </summary>
    public sealed class TuningConfig : ScriptableObject
    {
        [Serializable]
        sealed class SaveData
        {
            public int version;
            public CombatTuning combat = new CombatTuning();
            public PrototypeTuning.PanelFields prototype = new PrototypeTuning.PanelFields();
        }

        public CombatTuning Combat { get; private set; }
        public PrototypeTuning Prototype { get; private set; }

        const string FileName = "tuning.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>O5: eski sÃ¼rÃ¼mlÃ¼ kaydÄ±n yedeÄŸi (atÄ±lan deÄŸerler kaybolmasÄ±n).</summary>
        public static string StaleBackupPath(int storedVersion) =>
            Path.Combine(Application.persistentDataPath, $"tuning.v{storedVersion}.bak.json");

        /// <summary>Son TryLoad eski kaydÄ± attÄ±ysa true (panel/test okur).</summary>
        public bool LastLoadDiscardedStale { get; private set; }

        public static TuningConfig Create(CombatTuning combat, PrototypeTuning prototype)
        {
            var config = CreateInstance<TuningConfig>();
            config.Combat = combat;
            config.Prototype = prototype;
            return config;
        }

        public string ToJson()
        {
            var data = new SaveData
            {
                version = TuningSchema.Version,
                combat = Combat,
                prototype = Prototype.ToPanelFields()
            };
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
        /// JSON'u YENÄ°, kopuk bir nesne aÄŸacÄ±na Ã§Ã¶zer (kimse ona referans tutmuyor, gÃ¼venli),
        /// sonra alan alan Ã‡ALIÅMA-ANI nesnelerinin iÃ§ine kopyalar. `JsonUtility.FromJsonOverwrite`
        /// KULLANILMADI: iÃ§ iÃ§e nesnelerin (Dodge/Sentence/...) kimliÄŸini koruyup korumadÄ±ÄŸÄ±
        /// belgelenmemiÅŸ â€” DodgeState/SentenceEngine gibi tÃ¼keticiler o alt nesnelerin referansÄ±nÄ±
        /// tuttuÄŸu iÃ§in kimlik kaymasÄ± sessiz bir "slider artÄ±k hiÃ§bir ÅŸeyi deÄŸiÅŸtirmiyor" hatasÄ±
        /// Ã¼retir. CopyFrom yolu bunu garantiler (Core Tuning sÄ±nÄ±flarÄ±ndaki desen).
        /// </summary>
        public bool TryLoad()
        {
            LastLoadDiscardedStale = false;
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

                // O5: eski sÃ¼rÃ¼m sessizce kazanmaz. Kod varsayÄ±lanlarÄ± kalÄ±r; eski dosya yedeklenir,
                // gÃ¼ncel sÃ¼rÃ¼mle yeniden yazÄ±lÄ±r. (BossDamageMigration'Ä±n v0 dÃ¼zeltmesi bunu kapsar.)
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
                        $"[T10] tuning.json sÃ¼rÃ¼m {data.version} < {TuningSchema.Version}: eski deÄŸerler uygulanmadÄ±, " +
                        $"kod varsayÄ±lanlarÄ± geÃ§erli (yedek: {StaleBackupPath(data.version)}).");
                    Save();
                    return false;
                }

                if (data.combat != null)
                    Combat.CopyFrom(data.combat);
                if (data.prototype != null)
                    Prototype.ApplyPanelFields(data.prototype);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[T10] Ayar yÃ¼klenemedi ({FilePath}): {e.Message}");
                return false;
            }
        }

        /// <summary>"SÄ±fÄ±rla": belgedeki spec varsayÄ±lanlarÄ±na dÃ¶ner (son kaydedilen JSON'a deÄŸil).</summary>
        public void ResetToDefaults()
        {
            Combat.ResetToDefaults();
            Prototype.ResetPanelFields();
        }

        public void ApplyPreset(TuningPreset preset) => TuningPresets.Apply(preset, Combat, Prototype);
    }
}
