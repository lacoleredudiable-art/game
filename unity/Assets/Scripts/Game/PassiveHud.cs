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
        readonly int[] _selectedRuneIds = new int[2];
        readonly bool[] _occupied = new bool[2];
        PrototypeTuning _tuning;
        RuneManager _runes;
        SkillMotor _skills;

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

        public void BindRunes(RuneManager runes, SkillMotor skills)
        {
            if (_runes != null)
                _runes.Changed -= OnLoadoutChanged;
            _runes = runes;
            _skills = skills;
            if (_runes != null)
            {
                _runes.Changed += OnLoadoutChanged;
                OnLoadoutChanged(_runes.Current);
            }
        }

        void OnDestroy()
        {
            if (_runes != null)
                _runes.Changed -= OnLoadoutChanged;
        }

        void OnLoadoutChanged(RuneLoadout loadout)
        {
            _selectedRuneIds[0] = 0;
            _selectedRuneIds[1] = 0;
            if (loadout != null)
            {
                int passiveSlot = 0;
                for (int i = 0; i < loadout.RuneIds.Count && passiveSlot < 2; i++)
                {
                    int runeId = loadout.RuneIds[i];
                    if (loadout.IsPassive(runeId))
                        _selectedRuneIds[passiveSlot++] = runeId;
                }
            }
            RefreshLoadoutSlots();
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
                RefreshLoadoutSlots();
                return;
            }

            RefreshLoadoutSlots();
            int nextFree = SelectedCount();
            int count = Mathf.Min(2 - nextFree, active.Count);
            for (int i = 0; i < count; i++)
            {
                ActivePassive a = active[i];
                PassiveNode n = a.Node;
                float remain = Mathf.Max(0f, n.DurationSec - (float)((worldMs - a.SinceMs) / 1000.0));
                SetSlot(nextFree + i, 0, DisplayName(n.Id), remain);
            }
        }

        public void Sync(IReadOnlyList<ActiveSlotPassive> active, double worldMs)
        {
            if (active == null || active.Count == 0)
            {
                RefreshLoadoutSlots();
                return;
            }

            RefreshLoadoutSlots();
            _occupied[0] = false;
            _occupied[1] = false;
            // Önce mevcut build'in pasiflerini sabit yuvasına koy; build değişiminden kalan
            // eski etkiler yalnız boş kalan yuvayı geçici olarak kullanır.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    ActiveSlotPassive passive = active[i];
                    int selectedSlot = SlotForRune(passive.RuneId);
                    bool currentLoadout = selectedSlot >= 0;
                    if ((pass == 0) != currentLoadout)
                        continue;
                    int slot = currentLoadout && !_occupied[selectedSlot]
                        ? selectedSlot
                        : FirstUnoccupied(_occupied);
                    if (slot < 0)
                        continue;
                    _occupied[slot] = true;
                    SetSlot(
                        slot,
                        passive.RuneId,
                        string.IsNullOrEmpty(passive.Name) ? passive.RuneId.ToString() : passive.Name,
                        passive.RemainingSec(worldMs));
                }
            }
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
            _selectedRuneIds[0] = 0;
            _selectedRuneIds[1] = 0;
            for (int i = 0; i < 2; i++)
                ClearSlot(i);
            UpdateHeader();
        }

        void ClearSlot(int i)
        {
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
            _slotTime[index].text = remain >= 0f ? remain.ToString("0") + "s" : "HAZIR";
        }

        void RefreshLoadoutSlots()
        {
            for (int i = 0; i < 2; i++)
            {
                int runeId = _selectedRuneIds[i];
                if (runeId <= 0)
                {
                    ClearSlot(i);
                    continue;
                }
                string name = _skills != null ? _skills.RuneName(runeId) : runeId.ToString();
                SetSlot(i, runeId, name, -1f);
            }
            UpdateHeader();
        }

        int SlotForRune(int runeId)
        {
            for (int i = 0; i < _selectedRuneIds.Length; i++)
                if (_selectedRuneIds[i] == runeId)
                    return i;
            return -1;
        }

        int SelectedCount()
        {
            int count = 0;
            for (int i = 0; i < _selectedRuneIds.Length; i++)
                if (_selectedRuneIds[i] > 0)
                    count++;
            return count;
        }

        static int FirstUnoccupied(bool[] occupied)
        {
            for (int i = 0; i < occupied.Length; i++)
                if (!occupied[i])
                    return i;
            return -1;
        }

        void UpdateHeader()
        {
            if (_header != null)
                _header.text = "PASİF YUVALARI  " + SelectedCount() + "/2";
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
