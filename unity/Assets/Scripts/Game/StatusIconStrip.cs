using System;
using System.Collections.Generic;
using Dovus.Core.Status;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>
    /// StatusBoard → frosted badge şeridi. Player (sol üst) veya boss (bar altı).
    /// Tüm StatusKind glifleri tanımlı; mockup dekoratif slot yok.
    /// </summary>
    public sealed class StatusIconStrip : MonoBehaviour
    {
        const int MaxSlots = 10;

        StatusBoard _board;
        PrototypeTuning _tuning;
        RectTransform _root;
        readonly List<Slot> _pool = new();
        readonly List<StatusKind> _scratch = new();

        struct Slot
        {
            public GameObject Go;
            public RectTransform Rect;
            public Image Bg;
            public Image Fill;
            public Text Glyph;
            public Text Mag;
            public StatusKind Kind;
            public bool Active;
        }

        public RectTransform Root => _root;

        public void Configure(
            StatusBoard board,
            PrototypeTuning tuning,
            Transform canvasRoot,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            string name)
        {
            _board = board;
            _tuning = tuning;

            var go = new GameObject(name);
            go.transform.SetParent(canvasRoot, false);
            if (canvasRoot != null)
                go.layer = canvasRoot.gameObject.layer;

            _root = go.AddComponent<RectTransform>();
            _root.anchorMin = anchorMin;
            _root.anchorMax = anchorMax;
            _root.pivot = pivot;
            _root.anchoredPosition = anchoredPos;
            _root.sizeDelta = new Vector2(
                PentagonLayoutScreen.DpToPixels(tuning.StatusIconSizeDp * MaxSlots + tuning.StatusIconGapDp * (MaxSlots - 1)),
                PentagonLayoutScreen.DpToPixels(tuning.StatusIconSizeDp));

            for (int i = 0; i < MaxSlots; i++)
                _pool.Add(CreateSlot(go.transform, i));
        }

        public void Rebind(StatusBoard board) => _board = board;

        public void SetAnchoredPosition(Vector2 pos)
        {
            if (_root != null)
                _root.anchoredPosition = pos;
        }

        Slot CreateSlot(Transform parent, int index)
        {
            float size = PentagonLayoutScreen.DpToPixels(_tuning.StatusIconSizeDp);
            float gap = PentagonLayoutScreen.DpToPixels(_tuning.StatusIconGapDp);

            var go = new GameObject("Status_" + index);
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(index * (size + gap), 0f);
            rect.sizeDelta = new Vector2(size, size);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.72f);
            bg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fill = fillGo.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.color = new Color(1f, 1f, 1f, 0.22f);
            fill.raycastTarget = false;

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(go.transform, false);
            var glyphRect = glyphGo.AddComponent<RectTransform>();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            var glyph = glyphGo.AddComponent<Text>();
            glyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (glyph.font == null)
                glyph.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            glyph.fontSize = Mathf.RoundToInt(size * 0.42f);
            glyph.fontStyle = FontStyle.Bold;
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.raycastTarget = false;
            glyph.color = Color.white;

            var magGo = new GameObject("Mag");
            magGo.transform.SetParent(go.transform, false);
            var magRect = magGo.AddComponent<RectTransform>();
            magRect.anchorMin = new Vector2(0f, 0f);
            magRect.anchorMax = new Vector2(1f, 0.4f);
            magRect.offsetMin = Vector2.zero;
            magRect.offsetMax = Vector2.zero;
            var mag = magGo.AddComponent<Text>();
            mag.font = glyph.font;
            mag.fontSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.28f));
            mag.alignment = TextAnchor.LowerCenter;
            mag.raycastTarget = false;
            mag.color = new Color(1f, 1f, 1f, 0.85f);
            mag.text = string.Empty;

            return new Slot
            {
                Go = go,
                Rect = rect,
                Bg = bg,
                Fill = fill,
                Glyph = glyph,
                Mag = mag,
                Kind = StatusKind.None,
                Active = false
            };
        }

        void LateUpdate()
        {
            if (_board == null || _root == null)
                return;

            _scratch.Clear();
            foreach (StatusKind k in _board.ActiveKinds)
            {
                if (k == StatusKind.None || k == StatusKind.Knockback)
                    continue;
                _scratch.Add(k);
            }

            _scratch.Sort(ComparePriority);
            int show = Math.Min(_scratch.Count, MaxSlots);

            float size = PentagonLayoutScreen.DpToPixels(_tuning.StatusIconSizeDp);
            float gap = PentagonLayoutScreen.DpToPixels(_tuning.StatusIconGapDp);

            for (int i = 0; i < MaxSlots; i++)
            {
                Slot slot = _pool[i];
                if (i >= show)
                {
                    if (slot.Active)
                    {
                        slot.Go.SetActive(false);
                        slot.Active = false;
                        slot.Kind = StatusKind.None;
                        _pool[i] = slot;
                    }
                    continue;
                }

                StatusKind kind = _scratch[i];
                if (!_board.TryGet(kind, out double rem, out float mag, out double total))
                    continue;

                if (!slot.Active || slot.Kind != kind)
                {
                    slot.Go.SetActive(true);
                    slot.Active = true;
                    slot.Kind = kind;
                    StatusVisual(kind, out Color accent, out string glyph);
                    slot.Bg.color = new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.78f);
                    slot.Fill.color = new Color(accent.r, accent.g, accent.b, 0.55f);
                    slot.Glyph.color = accent;
                    slot.Glyph.text = glyph;
                }

                float ratio = total > 1e-3 ? Mathf.Clamp01((float)(rem / total)) : 0f;
                slot.Fill.fillAmount = ratio;
                slot.Rect.anchoredPosition = new Vector2(i * (size + gap), 0f);

                if (kind == StatusKind.Shield && mag > 0.5f)
                    slot.Mag.text = Mathf.RoundToInt(mag).ToString();
                else
                    slot.Mag.text = string.Empty;

                _pool[i] = slot;
            }

            _root.sizeDelta = new Vector2(
                show > 0 ? show * size + (show - 1) * gap : 0f,
                size);
            _root.gameObject.SetActive(show > 0);
        }

        static int ComparePriority(StatusKind a, StatusKind b)
        {
            int pa = Priority(a);
            int pb = Priority(b);
            if (pa != pb)
                return pa.CompareTo(pb);
            return ((int)a).CompareTo((int)b);
        }

        static int Priority(StatusKind k)
        {
            if (StatusKindUtil.IsHardCc(k)) return 0;
            if (StatusKindUtil.IsSoftCc(k)) return 1;
            if (StatusKindUtil.IsDebuff(k)) return 2;
            if (StatusKindUtil.IsBuff(k)) return 3;
            return 4;
        }

        static void StatusVisual(StatusKind kind, out Color accent, out string glyph)
        {
            switch (kind)
            {
                case StatusKind.Stun:
                    accent = new Color(1f, 0.85f, 0.35f); glyph = "ST"; break;
                case StatusKind.Root:
                    accent = new Color(0.55f, 0.75f, 0.4f); glyph = "RT"; break;
                case StatusKind.Silence:
                    accent = new Color(0.7f, 0.55f, 0.95f); glyph = "SI"; break;
                case StatusKind.Fear:
                    accent = new Color(0.55f, 0.35f, 0.7f); glyph = "FE"; break;
                case StatusKind.Stasis:
                    accent = new Color(0.55f, 0.85f, 1f); glyph = "ZS"; break;
                case StatusKind.Slow:
                    accent = new Color(0.45f, 0.7f, 0.95f); glyph = "SL"; break;
                case StatusKind.Blind:
                    accent = new Color(0.75f, 0.75f, 0.8f); glyph = "BL"; break;
                case StatusKind.Disarm:
                    accent = new Color(0.9f, 0.55f, 0.45f); glyph = "DI"; break;
                case StatusKind.Taunt:
                    accent = new Color(0.95f, 0.55f, 0.35f); glyph = "TA"; break;
                case StatusKind.Burn:
                    accent = new Color(1f, 0.45f, 0.25f); glyph = "BR"; break;
                case StatusKind.Poison:
                    accent = new Color(0.55f, 0.9f, 0.35f); glyph = "PO"; break;
                case StatusKind.ArmorBreak:
                    accent = new Color(0.85f, 0.65f, 0.4f); glyph = "AB"; break;
                case StatusKind.GrievousWounds:
                    accent = new Color(0.9f, 0.3f, 0.35f); glyph = "GW"; break;
                case StatusKind.Weaken:
                    accent = new Color(0.7f, 0.5f, 0.55f); glyph = "WK"; break;
                case StatusKind.Shield:
                    accent = new Color(0.45f, 0.85f, 1f); glyph = "SH"; break;
                case StatusKind.Haste:
                    accent = new Color(0.55f, 0.95f, 0.75f); glyph = "HA"; break;
                case StatusKind.DamageReduction:
                    accent = new Color(0.5f, 0.7f, 0.95f); glyph = "DR"; break;
                case StatusKind.Regen:
                    accent = new Color(0.4f, 0.95f, 0.55f); glyph = "RG"; break;
                default:
                    accent = new Color(0.75f, 0.75f, 0.8f); glyph = "?"; break;
            }
        }
    }
}
