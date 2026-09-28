using System.Collections.Generic;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// v6 7b: savaş öncesi build ekranı. 12 çift yüzlü ründen tekrarsız 6'sı seçilir; seçim
    /// sırası altıgen slotudur (1 üst, saat yönü — <see cref="HexagonLayoutScreen.DotPx"/>).
    /// Açıkken dünya saati durur, altıgen/çubuk/orbit girdisi susar. Seçili altılı içinden
    /// 0-2 rün pasif yuva olarak işaretlenebilir.
    /// ui_rules.build_display "2_weapons": 10 silahtan 2'si (1 = başlangıç, 2 = swap yedeği).
    /// </summary>
    public sealed class BuildSelectScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        static readonly Vector2 ReferenceResolution = new(1600f, 900f);
        static readonly Color BackdropColor = new(0.02f, 0.04f, 0.07f, 0.96f);
        static readonly Color SidePanelColor = new(0.05f, 0.09f, 0.14f, 1f);
        static readonly Color CardColor = new(0.10f, 0.15f, 0.22f, 1f);
        static readonly Color CardSelectedColor = new(0.08f, 0.36f, 0.46f, 1f);
        static readonly Color SlotEmptyColor = new(0.14f, 0.19f, 0.26f, 1f);
        static readonly Color SlotFilledColor = new(0.10f, 0.55f, 0.66f, 1f);
        static readonly Color ButtonColor = new(0.18f, 0.24f, 0.32f, 1f);
        static readonly Color AccentColor = new(0.30f, 0.85f, 0.95f, 1f);
        static readonly Color MutedText = new(0.70f, 0.78f, 0.86f, 1f);

        SkillMotor _skills;
        RuneManager _runes;
        HexagonInput _input;
        HexagonView _view;
        GameClock _clock;
        ManifestationDirector _manifestation;
        Sprite _circle;
        readonly List<EquipmentItem> _weapons = new();
        readonly Dictionary<string, Image> _weaponChipImages = new();
        readonly Dictionary<string, Text> _weaponChipBadges = new();

        GameObject _screen;
        readonly List<RectTransform> _safeRects = new();
        Rect _appliedSafe;
        readonly List<int> _selected = new();
        readonly List<int> _passiveSelected = new();
        readonly Dictionary<int, Image> _cardImages = new();
        readonly Dictionary<int, GameObject> _cardBadges = new();
        readonly Dictionary<int, Text> _cardBadgeLabels = new();
        readonly Dictionary<int, Image> _passiveBadges = new();
        readonly Dictionary<int, Button> _passiveButtons = new();
        readonly Image[] _slotImages = new Image[RuneLoadout.SlotCount];
        readonly Text[] _slotLabels = new Text[RuneLoadout.SlotCount];
        Text _counter;
        Text _passiveCounter;
        Text _classLabel;
        Text _status;
        Button _startButton;
        Image _startImage;
        Text _startLabel;
        GameObject _closeButton;
        int _presetIndex = -1;
        bool _hasApplied;

        public void Configure(
            SkillMotor skills,
            RuneManager runes,
            HexagonInput input,
            HexagonView view,
            GameClock clock,
            ManifestationDirector manifestation,
            bool openNow)
        {
            _skills = skills;
            _runes = runes;
            _input = input;
            _view = view;
            _clock = clock;
            _manifestation = manifestation;
            _circle = CreateCircleSprite();

            BuildCanvas();
            _screen.SetActive(false);
            IsOpen = false;
            if (openNow)
                Open();
            else
                _hasApplied = true;
        }

        public void Open()
        {
            if (_screen == null || IsOpen)
                return;

            if (_input?.Engine != null && _input.Engine.State.Phase == SentencePhase.Building)
                _input.Engine.Abort();

            _selected.Clear();
            _passiveSelected.Clear();
            if (_runes?.Current != null)
            {
                _selected.AddRange(_runes.Current.RuneIds);
                _passiveSelected.AddRange(_runes.Current.PassiveRuneIds);
            }
            _presetIndex = FindMatchingClassIndex();

            _weapons.Clear();
            WeaponSwapState swap = _manifestation != null ? _manifestation.WeaponSwap : null;
            if (swap != null)
            {
                if (swap.Slot(0) != null)
                    _weapons.Add(swap.Slot(0));
                if (swap.Slot(1) != null)
                    _weapons.Add(swap.Slot(1));
            }

            IsOpen = true;
            _screen.SetActive(true);
            if (_clock != null)
                _clock.Paused = true;
            SetStatus(string.Empty);
            Refresh();
        }

        void Close()
        {
            IsOpen = false;
            if (_screen != null)
                _screen.SetActive(false);
            if (_clock != null)
                _clock.Paused = false;
        }

        void LateUpdate()
        {
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            if (safe != _appliedSafe)
                ApplySafeArea(safe);
        }

        void OnDestroy()
        {
            IsOpen = false;
            if (_clock != null)
                _clock.Paused = false;
        }

        // --- Seçim -------------------------------------------------------------------------

        void ToggleRune(int runeId)
        {
            int index = _selected.IndexOf(runeId);
            if (index >= 0)
            {
                _selected.RemoveAt(index);
                _passiveSelected.Remove(runeId);
            }
            else if (_selected.Count < RuneLoadout.SlotCount)
                _selected.Add(runeId);
            else
            {
                SetStatus("Build dolu — önce bir rünü çıkar.");
                return;
            }
            _presetIndex = FindMatchingClassIndex();
            SetStatus(string.Empty);
            Refresh();
        }

        void TogglePassive(int runeId)
        {
            if (!_selected.Contains(runeId))
            {
                SetStatus("Önce rünü build'e seç.");
                return;
            }
            int index = _passiveSelected.IndexOf(runeId);
            if (index >= 0)
                _passiveSelected.RemoveAt(index);
            else if (_passiveSelected.Count < RuneLoadout.MaxPassiveSlots)
                _passiveSelected.Add(runeId);
            else
            {
                SetStatus("2 pasif yuva dolu.");
                return;
            }
            SetStatus(string.Empty);
            Refresh();
        }

        void ToggleWeapon(EquipmentItem weapon)
        {
            int index = _weapons.FindIndex(w => w.Id == weapon.Id);
            if (index >= 0)
                _weapons.RemoveAt(index);
            else if (_weapons.Count < WeaponsCarried)
                _weapons.Add(weapon);
            else
            {
                SetStatus("2 silah dolu — önce birini çıkar.");
                return;
            }
            SetStatus(string.Empty);
            Refresh();
        }

        int WeaponsCarried => _manifestation?.WeaponSwap?.Rules.WeaponsCarried ?? 0;

        bool WeaponsReady => WeaponsCarried < 2 || _weapons.Count == WeaponsCarried;

        void RemoveSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selected.Count)
                return;
            int runeId = _selected[slotIndex];
            _selected.RemoveAt(slotIndex);
            _passiveSelected.Remove(runeId);
            _presetIndex = FindMatchingClassIndex();
            Refresh();
        }

        void ClearSelection()
        {
            _selected.Clear();
            _passiveSelected.Clear();
            _presetIndex = -1;
            SetStatus(string.Empty);
            Refresh();
        }

        void CyclePreset(int direction)
        {
            IReadOnlyList<MainClassNode> classes = _skills?.MainClasses;
            if (classes == null || classes.Count == 0)
            {
                SetStatus("Hazır class verisi yok.");
                return;
            }

            _presetIndex = _presetIndex < 0
                ? (direction > 0 ? 0 : classes.Count - 1)
                : (_presetIndex + direction + classes.Count) % classes.Count;
            _selected.Clear();
            _selected.AddRange(classes[_presetIndex].RuneIds);
            _passiveSelected.RemoveAll(id => !_selected.Contains(id));
            SetStatus(string.Empty);
            Refresh();
        }

        void ApplyAndStart()
        {
            if (_selected.Count != RuneLoadout.SlotCount)
            {
                SetStatus($"6 rün gerekli — seçili {_selected.Count}.");
                return;
            }
            if (!WeaponsReady)
            {
                SetStatus($"{WeaponsCarried} silah gerekli — seçili {_weapons.Count}.");
                return;
            }
            if (_runes == null || _input == null)
            {
                SetStatus("Rün yöneticisi bağlı değil.");
                return;
            }

            RuneLoadout previous = _runes.Current;
            if (!_runes.TrySelect(_selected, _passiveSelected, out string error))
            {
                SetStatus("Build reddedildi: " + error);
                return;
            }

            RuneLoadout loadout = _runes.Current;
            if (!_input.TrySetLoadout(loadout))
            {
                if (previous != null)
                    _runes.TrySelect(previous.RuneIds, new List<int>(previous.PassiveRuneIds), out _);
                SetStatus("Çizim sürerken build değişmez.");
                return;
            }

            _view?.SetLoadout(loadout);
            if (_manifestation != null && _weapons.Count > 0)
                _manifestation.SetWeaponLoadout(_weapons[0], _weapons.Count > 1 ? _weapons[1] : null);
            _hasApplied = true;
            string className = _presetIndex >= 0 ? _skills.MainClasses[_presetIndex].Name : "özel";
            string weaponNames = _weapons.Count > 0
                ? string.Join("+", _weapons.ConvertAll(w => w.Name))
                : "—";
            Debug.Log(
                $"[BuildSelect] build=[{string.Join(",", loadout.RuneIds)}] class={className} silah={weaponNames}");
            Close();
        }

        int FindMatchingClassIndex()
        {
            IReadOnlyList<MainClassNode> classes = _skills?.MainClasses;
            if (classes == null || _selected.Count != RuneLoadout.SlotCount)
                return -1;

            for (int i = 0; i < classes.Count; i++)
            {
                int[] ids = classes[i].RuneIds;
                if (ids.Length != _selected.Count)
                    continue;
                bool same = true;
                for (int k = 0; k < ids.Length && same; k++)
                    same = _selected.Contains(ids[k]);
                if (same)
                    return i;
            }
            return -1;
        }

        // --- Görünüm -----------------------------------------------------------------------

        void Refresh()
        {
            foreach (var pair in _cardImages)
            {
                int slot = _selected.IndexOf(pair.Key);
                bool chosen = slot >= 0;
                pair.Value.color = chosen ? CardSelectedColor : CardColor;
                _cardBadges[pair.Key].SetActive(chosen);
                if (chosen)
                    _cardBadgeLabels[pair.Key].text = (slot + 1).ToString();
                bool passive = _passiveSelected.Contains(pair.Key);
                _passiveBadges[pair.Key].color = passive ? AccentColor : SlotEmptyColor;
                _passiveButtons[pair.Key].interactable = chosen;
            }

            for (int i = 0; i < RuneLoadout.SlotCount; i++)
            {
                bool filled = i < _selected.Count;
                _slotImages[i].color = filled ? SlotFilledColor : SlotEmptyColor;
                _slotLabels[i].text = filled ? VerbFace(_selected[i]) : (i + 1).ToString();
                _slotLabels[i].color = filled ? Color.white : MutedText;
            }

            _counter.text = $"{_selected.Count}/6";
            _counter.color = _selected.Count == RuneLoadout.SlotCount ? AccentColor : MutedText;
            _passiveCounter.text = $"PASİF {_passiveSelected.Count}/2";
            _passiveCounter.color = _passiveSelected.Count > 0 ? AccentColor : MutedText;

            if (_presetIndex >= 0)
            {
                MainClassNode node = _skills.MainClasses[_presetIndex];
                _classLabel.text = $"{node.Name}\n<size=16>{node.Category} · {node.Feel}</size>";
            }
            else
            {
                _classLabel.text = _selected.Count == RuneLoadout.SlotCount
                    ? "Özel build\n<size=16>hazır class'larla eşleşmiyor</size>"
                    : "—\n<size=16>ya da ◀ ▶ ile hazır class seç</size>";
            }

            foreach (var pair in _weaponChipImages)
            {
                int slot = _weapons.FindIndex(w => w.Id == pair.Key);
                pair.Value.color = slot >= 0 ? CardSelectedColor : CardColor;
                _weaponChipBadges[pair.Key].text = slot switch
                {
                    0 => "1 · başlangıç",
                    1 => "2 · yedek",
                    _ => string.Empty
                };
            }

            bool ready = _selected.Count == RuneLoadout.SlotCount && WeaponsReady;
            _startButton.interactable = ready;
            _startImage.color = ready ? AccentColor : ButtonColor;
            _startLabel.color = ready ? new Color(0.02f, 0.08f, 0.12f, 1f) : MutedText;
            _startLabel.text = _hasApplied ? "UYGULA" : "SAVAŞA BAŞLA";
            _closeButton.SetActive(_hasApplied);
        }

        void SetStatus(string value)
        {
            if (_status != null)
                _status.text = value ?? string.Empty;
        }

        string VerbFace(int runeId)
        {
            if (_skills != null && _skills.TryGetRune(runeId, out RuneDefinition rune)
                && !string.IsNullOrEmpty(rune.VerbFace))
                return rune.VerbFace;
            return _skills != null ? _skills.RuneName(runeId) : runeId.ToString();
        }

        static Color CategoryColor(string category) => category switch
        {
            "hasar" => new Color(1f, 0.62f, 0.55f, 1f),
            "destek" => new Color(0.55f, 1f, 0.70f, 1f),
            "hareket" => new Color(0.60f, 0.85f, 1f, 1f),
            "savunma" => new Color(0.85f, 0.75f, 1f, 1f),
            _ => MutedText
        };

        // --- Kurulum -----------------------------------------------------------------------

        void BuildCanvas()
        {
            var canvasGo = new GameObject("BuildSelectCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Oyun HUD'unun (altıgen 50, his 200) üstünde, ayar panelinin (1000) altında.
            canvas.sortingOrder = 900;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform hudSafe = CreateRect("HudSafe", canvasGo.transform, Vector2.zero, Vector2.one);
            _safeRects.Add(hudSafe);
            Button open = CreateButton(hudSafe, "BUILD", 20, ButtonColor);
            var openRect = (RectTransform)open.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.sizeDelta = new Vector2(120f, 48f);
            openRect.anchoredPosition = new Vector2(-12f, -12f);
            open.onClick.AddListener(Open);

            _screen = new GameObject("BuildSelectScreen");
            _screen.transform.SetParent(canvasGo.transform, false);
            RectTransform screenRect = _screen.AddComponent<RectTransform>();
            Stretch(screenRect, Vector2.zero, Vector2.one);
            var backdrop = _screen.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform content = CreateRect("Content", _screen.transform, Vector2.zero, Vector2.one);
            _safeRects.Add(content);

            Text title = CreateText(content, "BUILD SEÇ", 40, Color.white, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, 0.02f, 0.90f, 0.50f, 0.98f);
            _counter = CreateText(content, "0/6", 40, MutedText, TextAnchor.MiddleRight);
            _counter.fontStyle = FontStyle.Bold;
            Place(_counter.rectTransform, 0.50f, 0.90f, 0.64f, 0.98f);
            _passiveCounter = CreateText(content, "PASİF 0/2", 18, MutedText, TextAnchor.MiddleRight);
            _passiveCounter.fontStyle = FontStyle.Bold;
            Place(_passiveCounter.rectTransform, 0.50f, 0.82f, 0.64f, 0.90f);
            Text subtitle = CreateText(
                content,
                "12 rünün 6'sını seç. Her rün hem fiil hem sıfat; skill = ilk çizdiğin (fiil) + ikinci (sıfat). "
                + "Seçim sırası altıgendeki yeri belirler. Karttaki P ile 0-2 pasif seç.",
                18,
                MutedText,
                TextAnchor.UpperLeft);
            Place(subtitle.rectTransform, 0.02f, 0.82f, 0.64f, 0.90f);

            bool hasWeapons = _manifestation != null
                && _manifestation.AvailableWeapons.Count > 0
                && WeaponsCarried >= 2;
            RectTransform grid = CreateRect("RuneGrid", content, Vector2.zero, Vector2.one);
            Place(grid, 0.015f, hasWeapons ? 0.19f : 0.02f, 0.645f, 0.82f);
            for (int id = 1; id <= 12; id++)
                BuildCard(grid, id);
            if (hasWeapons)
                BuildWeaponRow(content);

            BuildSidePanel(content);
            ApplySafeArea(HexagonLayoutScreen.SafeRectPx());
        }

        void BuildCard(RectTransform grid, int runeId)
        {
            int col = (runeId - 1) % 4;
            int row = (runeId - 1) / 4;
            string verb = VerbFace(runeId);
            string adjective = string.Empty;
            string category = string.Empty;
            string effect = string.Empty;
            if (_skills != null && _skills.TryGetRune(runeId, out RuneDefinition rune))
            {
                adjective = rune.AdjectiveFace;
                category = rune.Category;
                effect = rune.BaseEffect;
            }

            Button card = CreateButton(grid, string.Empty, 18, CardColor);
            var rect = (RectTransform)card.transform;
            rect.anchorMin = new Vector2(col / 4f, 1f - (row + 1) / 3f);
            rect.anchorMax = new Vector2((col + 1) / 4f, 1f - row / 3f);
            rect.offsetMin = new Vector2(6f, 6f);
            rect.offsetMax = new Vector2(-6f, -6f);
            int captured = runeId;
            card.onClick.AddListener(() => ToggleRune(captured));
            _cardImages[runeId] = (Image)card.targetGraphic;

            Text verbText = CreateText(rect, verb, 28, Color.white, TextAnchor.MiddleLeft);
            verbText.fontStyle = FontStyle.Bold;
            Place(verbText.rectTransform, 0.06f, 0.62f, 0.74f, 0.96f);
            Text adjectiveText = CreateText(rect, "sıfat: " + adjective, 18, MutedText, TextAnchor.MiddleLeft);
            Place(adjectiveText.rectTransform, 0.06f, 0.42f, 0.96f, 0.63f);
            Text categoryText = CreateText(
                rect, category.ToUpperInvariant(), 15, CategoryColor(category), TextAnchor.MiddleLeft);
            categoryText.fontStyle = FontStyle.Bold;
            Place(categoryText.rectTransform, 0.06f, 0.27f, 0.96f, 0.43f);
            Text effectText = CreateText(rect, effect, 14, MutedText, TextAnchor.UpperLeft);
            effectText.color = new Color(MutedText.r, MutedText.g, MutedText.b, 0.8f);
            Place(effectText.rectTransform, 0.06f, 0.03f, 0.96f, 0.28f);

            var badge = new GameObject("SlotBadge");
            badge.transform.SetParent(rect, false);
            var badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.sizeDelta = new Vector2(40f, 40f);
            badgeRect.anchoredPosition = new Vector2(-8f, -8f);
            var badgeImage = badge.AddComponent<Image>();
            badgeImage.sprite = _circle;
            badgeImage.color = AccentColor;
            badgeImage.raycastTarget = false;
            Text badgeLabel = CreateText(badgeRect, "1", 22, new Color(0.02f, 0.08f, 0.12f, 1f), TextAnchor.MiddleCenter);
            badgeLabel.fontStyle = FontStyle.Bold;
            _cardBadges[runeId] = badge;
            _cardBadgeLabels[runeId] = badgeLabel;
            badge.SetActive(false);

            Button passive = CreateButton(rect, "P", 16, SlotEmptyColor);
            var passiveRect = (RectTransform)passive.transform;
            passiveRect.anchorMin = passiveRect.anchorMax = new Vector2(1f, 0f);
            passiveRect.pivot = new Vector2(1f, 0f);
            passiveRect.sizeDelta = new Vector2(38f, 30f);
            passiveRect.anchoredPosition = new Vector2(-7f, 7f);
            passive.onClick.AddListener(() => TogglePassive(captured));
            _passiveBadges[runeId] = (Image)passive.targetGraphic;
            _passiveButtons[runeId] = passive;
        }

        void BuildWeaponRow(RectTransform content)
        {
            Text header = CreateText(
                content, $"SİLAHLAR — {WeaponsCarried} seç (savaşta swap)", 16, MutedText, TextAnchor.MiddleLeft);
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, 0.02f, 0.155f, 0.645f, 0.19f);

            RectTransform row = CreateRect("WeaponRow", content, Vector2.zero, Vector2.one);
            Place(row, 0.015f, 0.02f, 0.645f, 0.155f);
            IReadOnlyList<EquipmentItem> weapons = _manifestation.AvailableWeapons;
            int count = weapons.Count;
            for (int i = 0; i < count; i++)
            {
                EquipmentItem weapon = weapons[i];
                Button chip = CreateButton(row, string.Empty, 16, CardColor);
                var rect = (RectTransform)chip.transform;
                rect.anchorMin = new Vector2((float)i / count, 0f);
                rect.anchorMax = new Vector2((float)(i + 1) / count, 1f);
                rect.offsetMin = new Vector2(3f, 3f);
                rect.offsetMax = new Vector2(-3f, -3f);
                EquipmentItem captured = weapon;
                chip.onClick.AddListener(() => ToggleWeapon(captured));
                _weaponChipImages[weapon.Id] = (Image)chip.targetGraphic;

                Text name = CreateText(rect, weapon.Name, 18, Color.white, TextAnchor.MiddleCenter);
                name.fontStyle = FontStyle.Bold;
                Place(name.rectTransform, 0.04f, 0.50f, 0.96f, 0.95f);
                Text type = CreateText(rect, WeaponTypeLabel(weapon.Type), 13, MutedText, TextAnchor.MiddleCenter);
                Place(type.rectTransform, 0.04f, 0.28f, 0.96f, 0.52f);
                Text badge = CreateText(rect, string.Empty, 12, AccentColor, TextAnchor.MiddleCenter);
                badge.fontStyle = FontStyle.Bold;
                Place(badge.rectTransform, 0.02f, 0.03f, 0.98f, 0.28f);
                _weaponChipBadges[weapon.Id] = badge;
            }
        }

        static string WeaponTypeLabel(string type) => type switch
        {
            "melee" => "yakın",
            "medium" => "orta",
            "ranged" => "menzilli",
            _ => type ?? string.Empty
        };

        void BuildSidePanel(RectTransform content)
        {
            RectTransform side = CreateRect("SidePanel", content, Vector2.zero, Vector2.one);
            Place(side, 0.66f, 0.02f, 0.985f, 0.98f);
            var sideImage = side.gameObject.AddComponent<Image>();
            sideImage.color = SidePanelColor;

            Text header = CreateText(side, "ALTIGEN", 22, MutedText, TextAnchor.MiddleCenter);
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, 0.05f, 0.92f, 0.95f, 0.99f);

            RectTransform hexArea = CreateRect("HexArea", side, Vector2.zero, Vector2.one);
            Place(hexArea, 0.05f, 0.54f, 0.95f, 0.92f);
            RectTransform hex = CreateRect("Hex", hexArea, Vector2.zero, Vector2.one);
            var fitter = hex.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            const float ring = 0.36f;
            const float half = 0.15f;
            for (int i = 0; i < RuneLoadout.SlotCount; i++)
            {
                float rad = (90f - i * 60f) * Mathf.Deg2Rad;
                var c = new Vector2(0.5f + Mathf.Cos(rad) * ring, 0.5f + Mathf.Sin(rad) * ring);
                Button slot = CreateButton(hex, string.Empty, 16, SlotEmptyColor);
                var slotRect = (RectTransform)slot.transform;
                slotRect.anchorMin = c - new Vector2(half, half);
                slotRect.anchorMax = c + new Vector2(half, half);
                var slotImage = (Image)slot.targetGraphic;
                slotImage.sprite = _circle;
                int captured = i;
                slot.onClick.AddListener(() => RemoveSlot(captured));
                _slotImages[i] = slotImage;
                _slotLabels[i] = CreateText(slotRect, (i + 1).ToString(), 16, MutedText, TextAnchor.MiddleCenter);
                _slotLabels[i].fontStyle = FontStyle.Bold;
            }

            _classLabel = CreateText(side, string.Empty, 22, Color.white, TextAnchor.MiddleCenter);
            _classLabel.supportRichText = true;
            Place(_classLabel.rectTransform, 0.04f, 0.43f, 0.96f, 0.54f);

            Button prev = CreateButton(side, "◀", 26, ButtonColor);
            Place((RectTransform)prev.transform, 0.04f, 0.34f, 0.22f, 0.42f);
            prev.onClick.AddListener(() => CyclePreset(-1));
            Text presetHint = CreateText(side, "hazır class", 18, MutedText, TextAnchor.MiddleCenter);
            Place(presetHint.rectTransform, 0.22f, 0.34f, 0.78f, 0.42f);
            Button next = CreateButton(side, "▶", 26, ButtonColor);
            Place((RectTransform)next.transform, 0.78f, 0.34f, 0.96f, 0.42f);
            next.onClick.AddListener(() => CyclePreset(1));

            _status = CreateText(side, string.Empty, 16, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleCenter);
            Place(_status.rectTransform, 0.04f, 0.26f, 0.96f, 0.33f);

            Button clear = CreateButton(side, "TEMİZLE", 18, ButtonColor);
            Place((RectTransform)clear.transform, 0.04f, 0.12f, 0.34f, 0.25f);
            clear.onClick.AddListener(ClearSelection);

            _startButton = CreateButton(side, "SAVAŞA BAŞLA", 22, AccentColor);
            Place((RectTransform)_startButton.transform, 0.38f, 0.12f, 0.96f, 0.25f);
            _startButton.onClick.AddListener(ApplyAndStart);
            _startImage = (Image)_startButton.targetGraphic;
            _startLabel = _startButton.GetComponentInChildren<Text>();
            _startLabel.fontStyle = FontStyle.Bold;

            Button close = CreateButton(side, "✕", 22, ButtonColor);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(48f, 48f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);
            close.onClick.AddListener(Close);
            _closeButton = close.gameObject;
        }

        void ApplySafeArea(Rect safe)
        {
            _appliedSafe = safe;
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            var min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            var max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            foreach (RectTransform rect in _safeRects)
                Stretch(rect, min, max);
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, anchorMin, anchorMax);
            return rect;
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax) =>
            Stretch(rect, new Vector2(xMin, yMin), new Vector2(xMax, yMax));

        static Button CreateButton(Transform parent, string label, int fontSize, Color color)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Button" : label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, Vector2.zero, Vector2.one);
            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
                CreateText(rect, label, fontSize, Color.white, TextAnchor.MiddleCenter);
            return button;
        }

        static Text CreateText(Transform parent, string value, int fontSize, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, Vector2.zero, Vector2.one);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(8, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
