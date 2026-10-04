using Dovus.Core.Element;
using Dovus.Core.Grammar;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    public sealed partial class BuildSelectHud
    {
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
                Sprite icon = filled ? RuneIconCatalog.Get(_selected[i]) : null;
                _slotIconImages[i].sprite = icon;
                _slotIconImages[i].enabled = icon != null;
                _slotLabels[i].text = filled && icon == null ? VerbFace(_selected[i]) : (filled ? string.Empty : (i + 1).ToString());
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
    }
}
