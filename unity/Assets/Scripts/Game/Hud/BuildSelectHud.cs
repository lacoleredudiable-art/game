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
    /// <summary>
    /// v6 7b: savaş öncesi build ekranı. 12 çift yüzlü ründen tekrarsız 6'sı seçilir; seçim
    /// sırası altıgen slotudur (1 üst, saat yönü — <see cref="HexagonLayoutScreen.DotPx"/>).
    /// Açıkken dünya saati durur, altıgen/çubuk/orbit girdisi susar. Seçili altılı içinden
    /// 0-2 rün pasif yuva olarak işaretlenebilir.
    /// ui_rules.build_display "2_weapons": 10 silahtan 2'si (1 = başlangıç, 2 = swap yedeği).
    /// </summary>
    public sealed partial class BuildSelectHud : MonoBehaviour
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
        HexagonInputController _input;
        HexagonView _view;
        GameClockHost _clock;
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
        readonly Image[] _slotIconImages = new Image[RuneLoadout.SlotCount];
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
            HexagonInputController input,
            HexagonView view,
            GameClockHost clock,
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
            DebugConfig.DevLog(
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
    }
}
