using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Game.DevTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Weapons
{
    public sealed class WeaponLoadoutCycle
    {
        readonly IWeaponLoadoutCycleHost _host;
        readonly List<EquipmentItem> _cycleWeapons = new();
        int _cycleWeaponIndex = -1;
        int _elementPaintIndex;

        public WeaponLoadoutCycle(IWeaponLoadoutCycleHost host) => _host = host;

        public IReadOnlyList<EquipmentItem> CycleWeapons => _cycleWeapons;

        public int ElementPaintIndex => _elementPaintIndex;

        public void ResetElementPaintIndex() => _elementPaintIndex = 0;

        public void ConfigureWeaponCycle(IReadOnlyList<EquipmentItem> weapons)
        {
            _cycleWeapons.Clear();
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    EquipmentItem weapon = weapons[i];
                    if (weapon != null && weapon.Slot == EquipmentSlot.Weapon)
                        _cycleWeapons.Add(weapon);
                }
            }

            _cycleWeaponIndex = -1;
            for (int i = 0; i < _cycleWeapons.Count; i++)
            {
                if (_host.EquippedWeapon != null
                    && string.Equals(_cycleWeapons[i].Id, _host.EquippedWeapon.Id, StringComparison.Ordinal))
                {
                    _cycleWeaponIndex = i;
                    break;
                }
            }
        }

        public EquipmentItem CycleEquippedWeapon()
        {
            if (_cycleWeapons.Count == 0)
                return _host.EquippedWeapon;

            _cycleWeaponIndex = (_cycleWeaponIndex + 1) % _cycleWeapons.Count;
            _host.EquippedWeapon = _cycleWeapons[_cycleWeaponIndex];
            _host.RefreshDefenderArmor();
            _host.LastFactorySkill = null;
            _host.WeaponSwap?.ReplaceActive(_host.EquippedWeapon);

            string routeType = SkillExecutorRouter.IsRangedWeapon(_host.EquippedWeapon)
                ? "ranged"
                : "melee";
            string numericId = _host.EquippedWeapon.Id;
            int colon = numericId.LastIndexOf(':');
            if (colon >= 0 && colon + 1 < numericId.Length)
                numericId = numericId.Substring(colon + 1);
            DebugConfig.DevLog(
                $"[WeaponCycle] id={numericId} name={_host.EquippedWeapon.Name} "
                + $"type={routeType} canonicalType={_host.EquippedWeapon.Type}");
            return _host.EquippedWeapon;
        }

        public ElementPaintNode? CycleElementPaint()
        {
            if (_host.Skills == null || _host.Skills.ElementPaints.Count == 0)
                return null;
            _elementPaintIndex = (_elementPaintIndex + 1) % _host.Skills.ElementPaints.Count;
            ElementPaintNode paint = _host.Skills.ElementPaints[_elementPaintIndex];
            _host.Readout?.NoteSkill("Element: " + paint.Name, "isim/VFX boya katmanı", Color.cyan);
            DebugConfig.DevLog($"[ElementSystem] element paint={paint.Id}:{paint.Name} ({paint.Vfx})");
            return paint;
        }

        public bool TrySetElementPaint(int elementId)
        {
            if (_host.Skills == null)
                return false;
            for (int i = 0; i < _host.Skills.ElementPaints.Count; i++)
            {
                if (_host.Skills.ElementPaints[i].Id != elementId)
                    continue;
                _elementPaintIndex = i;
                ElementPaintNode paint = _host.Skills.ElementPaints[i];
                _host.RaiseElementPaintChanged(paint);
                _host.Readout?.NoteSkill("Element: " + paint.Name, "isim/VFX boya katmanı", Color.cyan);
                DebugConfig.DevLog($"[ElementSystem] element paint={paint.Id}:{paint.Name} ({paint.Vfx})");
                return true;
            }
            return false;
        }

        public WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill)
        {
            if (_host.EquipmentBonus == null || _host.EquippedWeapon == null || skill.IsEmpty)
                return WeaponSkillCompatibility.Neutral;
            return _host.SkillFactory != null
                ? _host.SkillFactory.EvaluateWeapon(skill, _host.EquippedWeapon)
                : WeaponSkillCompatibility.Neutral;
        }

        public void SyncCycleIndex()
        {
            _cycleWeaponIndex = -1;
            if (_host.EquippedWeapon == null)
                return;
            for (int i = 0; i < _cycleWeapons.Count; i++)
            {
                if (string.Equals(_cycleWeapons[i].Id, _host.EquippedWeapon.Id, StringComparison.Ordinal))
                {
                    _cycleWeaponIndex = i;
                    return;
                }
            }
        }
    }
}
