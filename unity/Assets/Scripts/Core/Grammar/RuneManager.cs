using System;
using System.Collections.Generic;
using Dovus.Core.Equipment;

namespace Dovus.Core.Grammar
{
    /// <summary>Binding sıra adım 4: 12 katalog rününden 6 seçim ve 0-2 pasif yuva.</summary>
    public sealed class RuneManager
    {
        readonly SkillMotor _motor;

        public RuneManager(SkillMotor motor, RuneLoadout? initial = null)
        {
            _motor = motor ?? throw new ArgumentNullException(nameof(motor));
            Current = initial ?? motor.DefaultLoadout;
        }

        public RuneLoadout Current { get; private set; }
        public event Action<RuneLoadout>? Changed;

        public bool TrySelect(
            IReadOnlyList<int> runeIds,
            IReadOnlyList<int>? passiveRuneIds,
            out string error)
        {
            try
            {
                RuneLoadout next = _motor.CreateLoadout(runeIds, passiveRuneIds);
                Current = next;
                error = string.Empty;
                Changed?.Invoke(next);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public bool TrySelectMainClass(
            int mainClassId,
            IReadOnlyList<int>? passiveRuneIds,
            out string error)
        {
            try
            {
                if (!_motor.TryCreateMainClassLoadout(mainClassId, passiveRuneIds, out RuneLoadout next))
                {
                    error = $"Ana class bulunamadı: {mainClassId}";
                    return false;
                }
                Current = next;
                error = string.Empty;
                Changed?.Invoke(next);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public bool TrySetPassiveSlots(IReadOnlyList<int>? passiveRuneIds, out string error) =>
            TrySelect(Current.RuneIds, passiveRuneIds, out error);

        public IReadOnlyList<Skill> BuildSkills(
            SkillFactory factory,
            EquipmentItem? weapon = null,
            int elementPaintId = 0)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return factory.CreateForBuild(Current, weapon, elementPaintId);
        }
    }
}
