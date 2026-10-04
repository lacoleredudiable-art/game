using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// 0â€“2 pasif rÃ¼nÃ¼ altÄ±gendeki dÃ¼ÄŸmesinin Ã¼stÃ¼nde "PASÄ°F" rozetiyle gÃ¶sterir; pasif etkinken
    /// rozet kalan sÃ¼reyi taÅŸÄ±r. AyrÄ± panel yok (29 Eyl: telefonda gereksiz yer kaplÄ±yordu).
    /// </summary>
    public sealed class PassiveHud : MonoBehaviour
    {
        readonly Dictionary<int, float> _activeRemainSec = new();
        HexagonView _view;
        RuneManager _runes;
        RuneLoadout _loadout;

        public void Configure(HexagonView view)
        {
            _view = view;
        }

        public void BindRunes(RuneManager runes)
        {
            if (_runes != null)
                _runes.Changed -= OnLoadoutChanged;
            _runes = runes;
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
            _loadout = loadout;
            _activeRemainSec.Clear();
            Refresh();
        }

        public void Sync(IReadOnlyList<ActiveSlotPassive> active, double worldMs)
        {
            _activeRemainSec.Clear();
            if (active != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    float remain = Mathf.Max(0f, active[i].RemainingSec(worldMs));
                    if (!_activeRemainSec.TryGetValue(active[i].RuneId, out float prev) || remain > prev)
                        _activeRemainSec[active[i].RuneId] = remain;
                }
            }
            Refresh();
        }

        /// <summary>Aktif slot pasifi yokken rozetleri yuva seÃ§imine gÃ¶re yeniden Ã§izer.</summary>
        public void Refresh()
        {
            if (_view == null)
                return;
            for (int dot = 1; dot <= HexagonLayout.DotCount; dot++)
            {
                int runeId = _loadout != null ? _loadout.RuneIdAtSlot(dot) : dot;
                bool passive = _loadout != null && _loadout.IsPassive(runeId);
                float remain = passive && _activeRemainSec.TryGetValue(runeId, out float r) ? r : -1f;
                _view.SetPassiveBadge(dot, passive, remain);
            }
        }
    }
}
