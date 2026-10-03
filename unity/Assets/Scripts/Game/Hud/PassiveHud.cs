using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Game.Casting;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Hud
{
    /// <summary>
    /// 0–2 pasif rünü altıgendeki düğmesinin üstünde "PASİF" rozetiyle gösterir; pasif etkinken
    /// rozet kalan süreyi taşır. Ayrı panel yok (29 Eyl: telefonda gereksiz yer kaplıyordu).
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

        /// <summary>Aktif slot pasifi yokken rozetleri yuva seçimine göre yeniden çizer.</summary>
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
