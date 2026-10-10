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

        public static bool TryOnSure(PhysicsCommand cmd, out OnSureCommand onSure)
        {
            if (cmd is OnSureCommand o)
            {
                onSure = o;
                return true;
            }
            onSure = null!;
            return false;
        }

        public static bool TryMenzile(PhysicsCommand cmd, out float rangeM)
        {
            if (cmd is MenzileYuruCommand walk)
            {
                rangeM = walk.RangeM;
                return true;
            }
            rangeM = 0f;
            return false;
        }

        public static bool TryDash(PhysicsCommand cmd, out KendiniTasiCommand dash)
        {
            if (cmd is KendiniTasiCommand d)
            {
                dash = d;
                return true;
            }
            dash = null!;
            return false;
        }

        public static bool TryPush(PhysicsCommand cmd, out float distanceM)
        {
            if (cmd is ItCommand push)
            {
                distanceM = push.DistanceM;
                return true;
            }
            distanceM = 0f;
            return false;
        }

        public static bool TryPoise(PhysicsCommand cmd, out float amount)
        {
            if (cmd is DengeVerCommand poise)
            {
                amount = poise.Amount;
                return true;
            }
            amount = 0f;
            return false;
        }

        public static bool TryGuard(PhysicsCommand cmd, out float durationSec, out float blockRatio)
        {
            if (cmd is DurusAcCommand guard)
            {
                durationSec = guard.DurationSec;
                blockRatio = guard.BlockRatio;
                return true;
            }
            durationSec = blockRatio = 0f;
            return false;
        }

        public static bool TryBounce(PhysicsCommand cmd, out SekCommand bounce)
        {
            if (cmd is SekCommand b)
            {
                bounce = b;
                return true;
            }
            bounce = null!;
            return false;
        }

        public static bool TryStructure(PhysicsCommand cmd, out float lifeSec)
        {
            if (cmd is YapiKurCommand structure)
            {
                lifeSec = structure.LifeSec;
                return true;
            }
            lifeSec = 0f;
            return false;
        }
    }
}
