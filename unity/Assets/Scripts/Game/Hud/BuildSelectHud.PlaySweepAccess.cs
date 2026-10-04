#if UNITY_EDITOR
using Dovus.Core.Equipment;
using System.Collections.Generic;

namespace Dovus.Game.Hud
{
    public sealed partial class BuildSelectHud
    {
        public List<int> SweepSelected => _selected;

        public List<int> SweepPassiveSelected => _passiveSelected;

        public List<EquipmentItem> SweepWeapons => _weapons;

        public void SweepApplyAndStart() => ApplyAndStart();

        public void SweepClose() => Close();
    }
}
#endif
