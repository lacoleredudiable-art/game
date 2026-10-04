using Dovus.Core.Equipment;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.DevTools;
using Dovus.Game.Skills;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    public sealed partial class BuildSelectHud : MonoBehaviour
    {
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
                HudDefaults.BuildSelectSubtitleFontSize,
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

            Sprite runeIcon = RuneIconCatalog.Get(runeId);
            if (runeIcon != null)
            {
                Image icon = CreateIcon(rect, "RuneIcon", runeIcon);
                Place(icon.rectTransform, 0.035f, 0.53f, 0.26f, 0.96f);
            }

            Text verbText = CreateText(rect, verb, 28, Color.white, TextAnchor.MiddleLeft);
            verbText.fontStyle = FontStyle.Bold;
            Place(verbText.rectTransform, 0.27f, 0.62f, 0.78f, 0.96f);
            Text adjectiveText = CreateText(rect, "sıfat: " + adjective, 18, MutedText, TextAnchor.MiddleLeft);
            Place(adjectiveText.rectTransform, 0.27f, 0.42f, 0.96f, 0.63f);
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

                Sprite weaponIcon = WeaponIconCatalog.Get(weapon);
                if (weaponIcon != null)
                {
                    Image icon = CreateIcon(rect, "WeaponIcon", weaponIcon);
                    Place(icon.rectTransform, 0.26f, 0.48f, 0.74f, 0.96f);
                }
                Text name = CreateText(rect, weapon.Name, 18, Color.white, TextAnchor.MiddleCenter);
                name.fontStyle = FontStyle.Bold;
                Place(name.rectTransform, 0.04f, 0.31f, 0.96f, 0.52f);
                Text type = CreateText(rect, WeaponTypeLabel(weapon.Type), 13, MutedText, TextAnchor.MiddleCenter);
                Place(type.rectTransform, 0.04f, 0.18f, 0.96f, 0.33f);
                Text badge = CreateText(rect, string.Empty, 12, AccentColor, TextAnchor.MiddleCenter);
                badge.fontStyle = FontStyle.Bold;
                Place(badge.rectTransform, 0.02f, 0.01f, 0.98f, 0.19f);
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
                Image slotIcon = CreateIcon(slotRect, "RuneIcon", null);
                Place(slotIcon.rectTransform, 0.13f, 0.13f, 0.87f, 0.87f);
                _slotIconImages[i] = slotIcon;
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
            text.font = HudTheme.LegacyFont;
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

        static Image CreateIcon(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
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
