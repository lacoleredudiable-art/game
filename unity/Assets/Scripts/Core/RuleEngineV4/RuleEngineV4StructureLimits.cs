namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4StructureLimits
    {
        int _global;
        readonly int[] _perPlayer = new int[8];

        public bool TryPlace(int playerSlot)
        {
            if (_global >= RuleEngineV4WorldPhysicsDefaults.StructureMaxGlobal)
                return false;
            int slot = playerSlot < 0 || playerSlot >= _perPlayer.Length ? 0 : playerSlot;
            if (_perPlayer[slot] >= RuleEngineV4WorldPhysicsDefaults.StructureMaxPerPlayer)
                return false;
            _perPlayer[slot]++;
            _global++;
            return true;
        }

        public void Release(int playerSlot)
        {
            int slot = playerSlot < 0 || playerSlot >= _perPlayer.Length ? 0 : playerSlot;
            if (_perPlayer[slot] > 0)
                _perPlayer[slot]--;
            if (_global > 0)
                _global--;
        }
    }
}
