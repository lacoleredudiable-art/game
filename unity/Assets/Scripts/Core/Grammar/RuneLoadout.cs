using System;
using System.Collections.Generic;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// v6.1.1 build yüzeyi: 12 ründen tekrarsız 6 seçim, bunların 0-2'si pasif yuva.
    /// Altıgen ekran noktaları bu listedeki slotlardır; rün id'si değildir.
    /// </summary>
    public sealed class RuneLoadout
    {
        public const int SlotCount = 6;
        public const int MaxPassiveSlots = 2;

        readonly int[] _runeIds;
        readonly HashSet<int> _passiveRuneIds;

        public RuneLoadout(
            IReadOnlyList<int> runeIds,
            IReadOnlyList<int>? passiveRuneIds = null)
        {
            if (runeIds == null)
                throw new ArgumentNullException(nameof(runeIds));
            if (runeIds.Count != SlotCount)
                throw new ArgumentException("Build tam 6 rün içermelidir.", nameof(runeIds));

            _runeIds = new int[SlotCount];
            var unique = new HashSet<int>();
            for (int i = 0; i < SlotCount; i++)
            {
                int id = runeIds[i];
                if (!RuneInfo.TryFromId(id, out _))
                    throw new ArgumentOutOfRangeException(nameof(runeIds), "Rün id 1..12 olmalıdır.");
                if (!unique.Add(id))
                    throw new ArgumentException("Build rünleri tekrarsız olmalıdır.", nameof(runeIds));
                _runeIds[i] = id;
            }

            _passiveRuneIds = new HashSet<int>();
            if (passiveRuneIds == null)
                return;
            if (passiveRuneIds.Count > MaxPassiveSlots)
                throw new ArgumentException("En fazla 2 pasif rün seçilebilir.", nameof(passiveRuneIds));

            for (int i = 0; i < passiveRuneIds.Count; i++)
            {
                int id = passiveRuneIds[i];
                if (!unique.Contains(id))
                    throw new ArgumentException("Pasif rün build içinde olmalıdır.", nameof(passiveRuneIds));
                _passiveRuneIds.Add(id);
            }
        }

        public static RuneLoadout Sequential { get; } =
            new RuneLoadout(new[] { 1, 2, 3, 4, 5, 6 });

        public IReadOnlyList<int> RuneIds => _runeIds;
        public IReadOnlyCollection<int> PassiveRuneIds => _passiveRuneIds;
        public int PassiveCount => _passiveRuneIds.Count;

        public int RuneIdAtSlot(int slot)
        {
            if (slot < 1 || slot > SlotCount)
                return 0;
            return _runeIds[slot - 1];
        }

        public bool TryResolveSlot(int slot, out Rune rune)
        {
            return RuneInfo.TryFromId(RuneIdAtSlot(slot), out rune);
        }

        public bool ContainsRune(int runeId)
        {
            for (int i = 0; i < _runeIds.Length; i++)
                if (_runeIds[i] == runeId)
                    return true;
            return false;
        }

        public bool IsPassive(int runeId) => _passiveRuneIds.Contains(runeId);
    }
}
