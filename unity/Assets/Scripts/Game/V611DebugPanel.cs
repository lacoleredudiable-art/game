using System.Collections.Generic;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// v6 implementation ladder controls. Deliberately plain: list-based pick-6,
    /// deterministic 1-1 smoke cast, and element paint cycle. Radial/polished UI is deferred.
    /// </summary>
    public sealed class V611DebugPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        SkillMotor _skills;
        RuneManager _runeManager;
        ElementSystemAssetCatalog _assets;
        PentagonInput _input;
        PentagonView _view;
        ManifestationDirector _manifestation;
        GameObject _panel;
        Text _status;
        Text _element;
        readonly List<int> _selected = new();
        readonly Dictionary<int, Text> _runeLabels = new();

        public void Configure(
            SkillMotor skills,
            RuneManager runeManager,
            ElementSystemAssetCatalog assets,
            PentagonInput input,
            PentagonView view,
            ManifestationDirector manifestation,
            Transform canvasRoot)
        {
            _skills = skills;
            _runeManager = runeManager;
            _assets = assets;
            _input = input;
            _view = view;
            _manifestation = manifestation;
            _selected.Clear();
            if (input?.Engine?.Loadout != null)
                _selected.AddRange(input.Engine.Loadout.RuneIds);

            Build(canvasRoot);
            Refresh();
        }

        void Build(Transform canvasRoot)
        {
            var toggle = CreateButton(canvasRoot, "V6", new Vector2(0.01f, 0.88f), new Vector2(0.09f, 0.97f));
            toggle.onClick.AddListener(Toggle);

            _panel = new GameObject("V611SimpleControls");
            _panel.transform.SetParent(canvasRoot, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.01f, 0.12f);
            panelRect.anchorMax = new Vector2(0.38f, 0.87f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.06f, 0.10f, 0.94f);

            var vertical = _panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 10, 10);
            vertical.spacing = 6f;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;

            Text heading = CreateLabel(_panel.transform, "V6.1.1 — BASİT DOĞRULAMA", 17);
            heading.alignment = TextAnchor.MiddleCenter;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var gridGo = new GameObject("RuneList");
            gridGo.transform.SetParent(_panel.transform, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(170f, 34f);
            grid.spacing = new Vector2(6f, 5f);
            gridGo.AddComponent<LayoutElement>().preferredHeight = 235f;

            for (int id = 1; id <= 12; id++)
            {
                int captured = id;
                Button button = CreateButton(gridGo.transform, string.Empty, Vector2.zero, Vector2.one);
                Text label = button.GetComponentInChildren<Text>();
                _runeLabels[id] = label;
                button.onClick.AddListener(() => ToggleRune(captured));
            }

            Button apply = CreateRowButton(_panel.transform, "SEÇİLİ 6'YI UYGULA");
            apply.onClick.AddListener(ApplyBuild);
            Button smoke = CreateRowButton(_panel.transform, "TEST 1-1 (F1)");
            smoke.onClick.AddListener(SmokeCast);
            Button element = CreateRowButton(_panel.transform, "ELEMENT DEĞİŞTİR (E)");
            element.onClick.AddListener(CycleElement);

            _element = CreateLabel(_panel.transform, string.Empty, 15);
            _element.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            _status = CreateLabel(_panel.transform, string.Empty, 14);
            _status.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

            _panel.SetActive(false);
            IsOpen = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.f1Key.wasPressedThisFrame)
                SmokeCast();
            if (keyboard.eKey.wasPressedThisFrame)
                CycleElement();
            if (keyboard.bKey.wasPressedThisFrame)
                Toggle();
        }

        void Toggle()
        {
            IsOpen = !IsOpen;
            if (_panel != null)
                _panel.SetActive(IsOpen);
        }

        void ToggleRune(int runeId)
        {
            int index = _selected.IndexOf(runeId);
            if (index >= 0)
                _selected.RemoveAt(index);
            else if (_selected.Count < RuneLoadout.SlotCount)
                _selected.Add(runeId);
            else
                SetStatus("Önce bir rünü çıkar; build tam 6 rün.");
            Refresh();
        }

        void ApplyBuild()
        {
            if (_selected.Count != RuneLoadout.SlotCount)
            {
                SetStatus($"6 rün gerekli; seçili={_selected.Count}.");
                return;
            }

            try
            {
                string error = "RuneManager yok.";
                RuneLoadout previous = _runeManager?.Current;
                if (_runeManager == null
                    || !_runeManager.TrySelect(_selected, null, out error))
                {
                    SetStatus("Build reddedildi: " + error);
                    return;
                }
                RuneLoadout loadout = _runeManager.Current;
                if (!_input.TrySetLoadout(loadout))
                {
                    if (previous != null)
                        _runeManager.TrySelect(
                            previous.RuneIds,
                            new List<int>(previous.PassiveRuneIds),
                            out _);
                    SetStatus("Çizim sürerken build değişmez.");
                    return;
                }
                _view.SetLoadout(loadout);
                SetStatus("Build uygulandı: [" + string.Join(",", loadout.RuneIds) + "]");
            }
            catch (System.Exception e)
            {
                SetStatus("Build reddedildi: " + e.Message);
            }
        }

        void SmokeCast()
        {
            bool accepted = _input != null && _input.TryDebugCastSkill(1, 1);
            SetStatus(accepted
                ? "1-1 aynı canlı cast yoluna gönderildi."
                : "1-1 için Saldırı rünü build'de olmalı.");
            if (accepted && IsOpen)
                Toggle();
        }

        void CycleElement()
        {
            ElementPaintNode? paint = _manifestation?.CycleElementPaint();
            if (paint.HasValue)
                _element.text = "Element boya: " + paint.Value.Name;
        }

        void Refresh()
        {
            foreach (var pair in _runeLabels)
            {
                int slot = _selected.IndexOf(pair.Key);
                string marker = slot >= 0 ? $"[{slot + 1}]" : "[ ]";
                string runeName = _assets?.FindRune(pair.Key)?.DisplayName
                    ?? _skills.RuneName(pair.Key);
                pair.Value.text = marker + " " + pair.Key + " " + runeName;
                pair.Value.color = slot >= 0 ? Color.cyan : Color.white;
            }

            ElementPaintNode? paint = _manifestation?.SelectedElementPaint;
            if (_element != null)
                _element.text = paint.HasValue ? "Element boya: " + paint.Value.Name : "Element boya: —";
            SetStatus($"Seçili {_selected.Count}/6 · B panel · F1 smoke · E element");
        }

        void SetStatus(string value)
        {
            if (_status != null)
                _status.text = value;
        }

        void OnDestroy()
        {
            IsOpen = false;
        }

        static Button CreateRowButton(Transform parent, string text)
        {
            Button button = CreateButton(parent, text, Vector2.zero, Vector2.one);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            return button;
        }

        static Button CreateButton(
            Transform parent,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(string.IsNullOrEmpty(text) ? "Button" : text);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = CreateLabel(go.transform, text, 14);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static Text CreateLabel(Transform parent, string text, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            return label;
        }
    }
}
