using System.Collections.Generic;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// Build ekranındaki element satırı (savaş ekranındaki ELEMENT çipinin yerine): 6 çip, 1 seçili.
    /// Seçim UYGULA'da <c>TrySetElementPaint</c> ile oynanan elemente yazılır.
    /// </summary>
    internal sealed class BuildSelectElementRow
    {
        public const float Height = 0.115f;
        const float ChipsHeight = 0.075f;
        static readonly Color SelectedColor = new(0.08f, 0.36f, 0.46f, 1f);

        readonly Dictionary<int, Image> _chips = new();
        readonly Color _cardColor;

        BuildSelectElementRow(Color cardColor) => _cardColor = cardColor;

        public int SelectedId { get; private set; } = -1;

        public static BuildSelectElementRow TryBuild(
            RectTransform content, SkillMotor skills, float bottom, Color card, Color muted, Color accent)
        {
            if (skills == null || skills.ElementPaints.Count == 0)
                return null;
            var row = new BuildSelectElementRow(card);
            Text header = BuildSelectHud.CreateText(content, "ELEMENT — 1 seç", 16, muted, TextAnchor.MiddleLeft);
            header.fontStyle = FontStyle.Bold;
            BuildSelectHud.Place(header.rectTransform, 0.02f, bottom + ChipsHeight, 0.645f, bottom + Height);

            RectTransform chips = BuildSelectHud.CreateRect("ElementRow", content, Vector2.zero, Vector2.one);
            BuildSelectHud.Place(chips, 0.015f, bottom, 0.645f, bottom + ChipsHeight);
            IReadOnlyList<ElementPaintNode> paints = skills.ElementPaints;
            for (int i = 0; i < paints.Count; i++)
            {
                ElementPaintNode paint = paints[i];
                Button chip = BuildSelectHud.CreateButton(chips, paint.Name, 16, card);
                chip.gameObject.name = "ElementChip" + paint.Id;
                var rect = (RectTransform)chip.transform;
                rect.anchorMin = new Vector2((float)i / paints.Count, 0f);
                rect.anchorMax = new Vector2((float)(i + 1) / paints.Count, 1f);
                rect.offsetMin = new Vector2(3f, 3f);
                rect.offsetMax = new Vector2(-3f, -3f);
                int id = paint.Id;
                chip.onClick.AddListener(() => row.Select(id));
                row._chips[id] = (Image)chip.targetGraphic;

                var swatch = new GameObject("Swatch");
                swatch.transform.SetParent(rect, false);
                var swatchRect = swatch.AddComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(0.05f, 0f);
                swatchRect.anchorMax = new Vector2(0.95f, 0.12f);
                swatchRect.offsetMin = swatchRect.offsetMax = Vector2.zero;
                var swatchImage = swatch.AddComponent<Image>();
                swatchImage.raycastTarget = false;
                swatchImage.color = ColorUtility.TryParseHtmlString(paint.ColorHex, out Color tint) ? tint : accent;
            }
            return row;
        }

        public void Reset(int currentId)
        {
            SelectedId = currentId;
            Refresh();
        }

        public void Select(int id)
        {
            SelectedId = id;
            Refresh();
        }

        public void Refresh()
        {
            foreach (var pair in _chips)
                pair.Value.color = pair.Key == SelectedId ? SelectedColor : _cardColor;
        }
    }
}
