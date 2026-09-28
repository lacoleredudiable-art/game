using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// 0–2 pasifin kalıcı savaş yuvası. Boş yuvalar da görünür; aktif olduğunda rün ikonu,
    /// adı ve kalan süresi gelir. Böylece build seçimi savaşta kaybolmaz.
    /// </summary>
    public sealed class PassiveHud : MonoBehaviour
    {
        RectTransform _root;
        Text _header;
        readonly Image[] _slotBg = new Image[2];
        readonly Image[] _slotIcon = new Image[2];
        readonly Text[] _slotName = new Text[2];
        readonly Text[] _slotTime = new Text[2];
        readonly int[] _activeRuneIds = new int[2];
        PrototypeTuning _tuning;

        public void Configure(Transform canvasRoot, PrototypeTuning tuning)
        {
            _tuning = tuning ?? new PrototypeTuning();
            HudTheme theme = HudTheme.Current;
            var go = new GameObject("PassiveHud");
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            float width = HexagonLayoutScreen.DpToPixels(_tuning.PassiveHudWidthDp);
            float slotH = HexagonLayoutScreen.DpToPixels(_tuning.PassiveSlotHeightDp);
            float gap = HexagonLayoutScreen.DpToPixels(_tuning.PassiveSlotGapDp);
            float headerH = HexagonLayoutScreen.DpToPixels(theme.PassiveTitleDp + theme.PassiveHeaderExtraDp);
            _root.sizeDelta = new Vector2(width, headerH + slotH * 2f + gap);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = theme.PanelSoftColor;
            bg.raycastTarget = false;
            var outline = bgGo.AddComponent<Outline>();
            outline.effectColor = theme.PanelEdgeColor;
            outline.effectDistance = new Vector2(1f, -1f);

            _header = CreateText(go.transform, "Header", theme.PassiveTitleDp, theme.ControlCaptionColor);
            _header.alignment = TextAnchor.MiddleLeft;
            _header.rectTransform.anchorMin = new Vector2(0f, 1f);
            _header.rectTransform.anchorMax = new Vector2(1f, 1f);
            _header.rectTransform.pivot = new Vector2(0.5f, 1f);
            _header.rectTransform.sizeDelta = new Vector2(0f, headerH);
            float panelInset = HexagonLayoutScreen.DpToPixels(theme.PassivePanelInsetDp);
            _header.rectTransform.offsetMin = new Vector2(panelInset, -headerH);
            _header.rectTransform.offsetMax = new Vector2(-panelInset, 0f);

            for (int i = 0; i < 2; i++)
                BuildSlot(go.transform, i, width, slotH, headerH + i * (slotH + gap));
            ClearSlots();
        }

        public void BindBelowPlayer(VitalsHud vitals)
        {
            if (_root == null || vitals == null)
                return;
            float left = HexagonLayoutScreen.SafeLeftInsetPx()
                + HexagonLayoutScreen.DpToPixels(_tuning.VitalsMarginDp);
            float y = vitals.PlayerStackBottomCanvasY
                - HexagonLayoutScreen.DpToPixels(_tuning.PassiveHudGapBelowVitalsDp);
            _root.anchoredPosition = new Vector2(left, y);
        }

        /// <summary>Aktif pasif listesini yeniler; boşsa gizler.</summary>
        public void Sync(IReadOnlyList<ActivePassive> active, double worldMs)
        {
            if (active == null || active.Count == 0)
            {
                ClearSlots();
                return;
            }

            int count = Mathf.Min(2, active.Count);
            for (int i = 0; i < count; i++)
            {
                ActivePassive a = active[i];
                PassiveNode n = a.Node;
                float remain = Mathf.Max(0f, n.DurationSec - (float)((worldMs - a.SinceMs) / 1000.0));
                SetSlot(i, 0, DisplayName(n.Id), remain);
            }
            for (int i = count; i < 2; i++)
                ClearSlot(i);
            _header.text = "PASİF YUVALARI  " + count + "/2";
        }

        public void Sync(IReadOnlyList<ActiveSlotPassive> active, double worldMs)
        {
            if (active == null || active.Count == 0)
            {
                ClearSlots();
                return;
            }

            int count = Mathf.Min(2, active.Count);
            for (int i = 0; i < count; i++)
            {
                ActiveSlotPassive passive = active[i];
                SetSlot(
                    i,
                    passive.RuneId,
                    string.IsNullOrEmpty(passive.Name) ? passive.RuneId.ToString() : passive.Name,
                    passive.RemainingSec(worldMs));
            }
            for (int i = count; i < 2; i++)
                ClearSlot(i);
            _header.text = "PASİF YUVALARI  " + count + "/2";
        }

        static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "?";
            return id.Replace('_', ' ');
        }

        void BuildSlot(Transform parent, int index, float width, float height, float top)
        {
            HudTheme theme = HudTheme.Current;
            var go = new GameObject("PassiveSlot" + (index + 1));
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(width, height);
            _slotBg[index] = go.AddComponent<Image>();
            _slotBg[index].color = theme.PanelColor;
            _slotBg[index].raycastTarget = false;

            var iconGo = new GameObject("RuneIcon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            float inset = HexagonLayoutScreen.DpToPixels(theme.PassiveIconInsetDp);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(inset, 0f);
            iconRt.sizeDelta = new Vector2(height - inset * 2f, height - inset * 2f);
            _slotIcon[index] = iconGo.AddComponent<Image>();
            _slotIcon[index].preserveAspect = true;
            _slotIcon[index].raycastTarget = false;

            _slotName[index] = CreateText(go.transform, "Name", theme.PassiveBodyDp, theme.PrimaryTextColor);
            RectTransform nameRt = _slotName[index].rectTransform;
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(height, 0f);
            nameRt.offsetMax = new Vector2(-HexagonLayoutScreen.DpToPixels(theme.PassiveNameEndPadDp), 0f);
            _slotName[index].alignment = TextAnchor.MiddleLeft;
            _slotName[index].fontStyle = FontStyle.Bold;

            _slotTime[index] = CreateText(go.transform, "Time", theme.PassiveBodyDp, theme.SkillNeutralColor);
            RectTransform timeRt = _slotTime[index].rectTransform;
            timeRt.anchorMin = new Vector2(1f, 0f);
            timeRt.anchorMax = Vector2.one;
            timeRt.pivot = new Vector2(1f, 0.5f);
            timeRt.sizeDelta = new Vector2(HexagonLayoutScreen.DpToPixels(theme.PassiveTimerWidthDp), 0f);
            _slotTime[index].alignment = TextAnchor.MiddleCenter;
            _slotTime[index].fontStyle = FontStyle.Bold;
        }

        void ClearSlots()
        {
            if (_header != null)
                _header.text = "PASİF YUVALARI  0/2";
            for (int i = 0; i < 2; i++)
                ClearSlot(i);
        }

        void ClearSlot(int i)
        {
            _activeRuneIds[i] = 0;
            if (_slotIcon[i] != null)
            {
                _slotIcon[i].sprite = null;
                _slotIcon[i].enabled = false;
            }
            if (_slotName[i] != null)
            {
                _slotName[i].text = "BOŞ YUVA";
                _slotName[i].color = HudTheme.Current.EmptySlotTextColor;
            }
            if (_slotTime[i] != null)
                _slotTime[i].text = "—";
        }

        void SetSlot(int index, int runeId, string name, float remain)
        {
            Sprite icon = runeId > 0 ? RuneIconCatalog.Get(runeId) : null;
            _slotIcon[index].sprite = icon;
            _slotIcon[index].enabled = icon != null;
            _slotName[index].text = name;
            _slotName[index].color = HudTheme.Current.PrimaryTextColor;
            _slotTime[index].text = remain.ToString("0") + "s";
            int identity = runeId > 0 ? runeId : -(index + 1);
            if (icon != null && _activeRuneIds[index] != identity)
                UiJuice.PunchScale(_slotIcon[index].rectTransform, HudTheme.Current.ReadyPopScale, HudTheme.Current.JuiceSec);
            _activeRuneIds[index] = identity;
        }

        static Text CreateText(Transform parent, string name, float sizeDp, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.font = HudTheme.LegacyFont;
            text.fontSize = Mathf.RoundToInt(HexagonLayoutScreen.DpToPixels(sizeDp));
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
