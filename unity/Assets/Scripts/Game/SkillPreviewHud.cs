using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// Binding sıra 7a: salt-okunur preview. Seçim/cast üretmez; canlı SentenceEngine ve
    /// SkillFactory sonucunu gösterir.
    /// </summary>
    public sealed class SkillPreviewHud : MonoBehaviour
    {
        SentenceEngine _engine;
        SkillMotor _motor;
        SkillFactory _factory;
        EquipmentItem _weapon;
        ManifestationDirector _manifestation;
        Text _title;
        Text _detail;
        string _lastSignature = string.Empty;

        public void Configure(
            SentenceEngine engine,
            SkillMotor motor,
            SkillFactory factory,
            EquipmentItem weapon,
            ManifestationDirector manifestation,
            Transform canvasRoot)
        {
            _engine = engine;
            _motor = motor;
            _factory = factory;
            _weapon = weapon;
            _manifestation = manifestation;

            var root = new GameObject("SkillPreviewReadOnly");
            root.transform.SetParent(canvasRoot, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.24f, 0.48f);
            rect.anchorMax = new Vector2(0.76f, 0.58f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.08f, 0.12f, 0.72f);
            bg.raycastTarget = false;

            _title = CreateText(root.transform, "Title", new Vector2(0f, 0.46f), Vector2.one, 20);
            _detail = CreateText(root.transform, "Detail", Vector2.zero, new Vector2(1f, 0.46f), 14);
            SetPreview("İlk rünü seç", "fiil + sıfat · 2-rün skill", Color.cyan);
        }

        void LateUpdate()
        {
            if (_engine == null || _motor == null || _factory == null)
                return;

            SentenceState state = _engine.State;
            int elementId = _manifestation?.SelectedElementPaint?.Id ?? 0;
            string signature = state.Phase + ":" + state.Words.Count + ":" + elementId;
            for (int i = 0; i < state.Words.Count; i++)
                signature += ":" + (int)state.Words[i].Rune;
            if (state.Words.Count == 0 && _manifestation?.LastFactorySkill != null)
                signature += ":last:" + _manifestation.LastFactorySkill.Id;
            if (signature == _lastSignature)
                return;
            _lastSignature = signature;

            if (state.Phase == SentencePhase.Building && state.Words.Count == 1)
            {
                int verbId = (int)state.Words[0].Rune;
                SkillResolution preview = _motor.Resolve(new[] { verbId });
                SetPreview(preview.VerbName, "ikinci rün: sıfat seç", Color.cyan);
                return;
            }

            Skill skill = null;
            if (state.Words.Count == 2)
            {
                try
                {
                    skill = _factory.CreateFromWords(state.Words, _weapon, elementId);
                }
                catch
                {
                    skill = null;
                }
            }
            skill ??= _manifestation?.LastFactorySkill;
            if (skill == null)
            {
                SetPreview("İlk rünü seç", "fiil + sıfat · 2-rün skill", Color.cyan);
                return;
            }

            Color color = skill.Weapon.Compatible ? Color.green : Color.yellow;
            string compatibility = skill.Weapon.Compatible ? "uyumlu" : "uyumsuz ×0.8 / cast ×1.2";
            string prose = !string.IsNullOrEmpty(skill.Resolution.SkillJob)
                ? skill.Resolution.SkillJob
                : skill.Resolution.ProseFeel;
            SetPreview(skill.DisplayName, compatibility + " · " + prose, color);
        }

        void SetPreview(string title, string detail, Color color)
        {
            if (_title == null || _detail == null)
                return;
            _title.text = title ?? string.Empty;
            _detail.text = detail ?? string.Empty;
            _title.color = color;
            _detail.color = new Color(color.r, color.g, color.b, 0.9f);
        }

        static Text CreateText(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(8f, 2f);
            rect.offsetMax = new Vector2(-8f, -2f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
