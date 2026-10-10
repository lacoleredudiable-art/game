namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Game katmanının Türkçe komut sınıf adlarına dokunmadan parametre okuması.</summary>
    public static class RuleEngineV4CommandAccess
    {
        public static bool TryMeleeRange(PhysicsCommand cmd, out float rangeM)
        {
            if (cmd is YakinVurusCommand melee)
            {
                rangeM = melee.RangeM;
                return true;
            }
            rangeM = 0f;
            return false;
        }

        public static bool TryMissile(PhysicsCommand cmd, out float rangeM, out float speedMps)
        {
            if (cmd is MermiFirlatCommand missile)
            {
                rangeM = missile.RangeM;
                speedMps = missile.SpeedMps;
                return true;
            }
            rangeM = speedMps = 0f;
            return false;
        }

        public static bool TryArea(PhysicsCommand cmd, out float radiusM)
        {
            if (cmd is AlanAcCommand area)
            {
                radiusM = area.RadiusM;
                return true;
            }
            radiusM = 0f;
            return false;
        }

        public static bool TryDamage(PhysicsCommand cmd, out float amount, out float powerMult)
        {
            if (cmd is HasarVerCommand dmg)
            {
                amount = dmg.Amount;
                powerMult = dmg.PowerMult;
                return true;
            }
            amount = powerMult = 0f;
            return false;
        }

        public static bool TryHeal(PhysicsCommand cmd, out float amount, out float powerMult)
        {
            if (cmd is SifaVerCommand heal)
            {
                amount = heal.Amount;
                powerMult = heal.PowerMult;
                return true;
            }
            amount = powerMult = 0f;
            return false;
        }

        public static bool TryMark(PhysicsCommand cmd, out float lifeSec)
        {
            if (cmd is IsaretKoyCommand mark)
            {
                lifeSec = mark.LifeSec;
                return true;
            }
            lifeSec = 0f;
            return false;
        }
    }
}
