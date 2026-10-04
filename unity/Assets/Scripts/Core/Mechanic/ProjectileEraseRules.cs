using System;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// MechanicPlan → EraseSpec (saf). mermi_sil etkisinin moduna göre: yut → emme, engel → çapalı disk,
    /// geri_gonder → yansıtma, delici → hat, sis_perdesi → perde, hedefli → tek tek, yukselen_perde →
    /// büyüyen disk, surekli_perde → oyuncuyu izleyen disk, bag_hatti → bağ şeridi, diğerleri → silme.
    /// Skill kimliği okunmaz; yalnız etki modu ve gövde (AGENTS kural 6).
    /// </summary>
    public static class ProjectileEraseRules
    {
        public static bool Erases(MechanicPlan plan) =>
            plan != null && plan.Effects.Any(e => e.Stat == "mermi_sil");

        public static EraseSpec For(MechanicPlan plan, MechanicRules rules)
        {
            MechanicEffect e = plan?.Effects.FirstOrDefault(x => x.Stat == "mermi_sil");
            if (e == null)
                return default;
            MechanicBody b = plan.Body;
            float radius = (float)Math.Max(ProjectileEraseRulesDefaults.MinEraseRadiusM, b.SizeM);
            EraseShape shape = EraseShape.Disk;
            EraseMode mode = EraseMode.Delete;
            float grow = 1f, rate = 0f, lifesteal = 0f, reflectMult = 0f, width = 0f, length = 0f;
            bool anchored = false;

            if (e.Has("yut"))
            {
                mode = EraseMode.Absorb;
                lifesteal = (float)(rules?.AdjNum(plan.Adjective, "lifesteal", 0) ?? 0);
            }
            else if (e.Has("engel"))
                anchored = true;
            else if (e.Has("geri_gonder"))
            {
                mode = EraseMode.Reflect;
                reflectMult = (float)Param(rules, "projectile_reflect_mult", 1.0);
            }
            else if (e.Has("delici"))
            {
                shape = EraseShape.Line;
                width = (float)Param(rules, "erase_line_width_m", 1.0);
                length = (float)Math.Max(ProjectileEraseRulesDefaults.MinBeamLengthM, b.ReachM);
            }
            else if (e.Has("sis_perdesi"))
                mode = EraseMode.Shroud;
            else if (e.Has("hedefli"))
            {
                mode = EraseMode.Targeted;
                rate = (float)Param(rules, "targeted_erase_per_sec", 2.0);
            }
            else if (e.Has("yukselen_perde"))
                grow = (float)Math.Max(1.0, Param(rules, "ramp_max", ProjectileEraseRulesDefaults.RampMaxMult));
            else if (e.Has("surekli_perde"))
                shape = EraseShape.Follow;
            else if (e.Has("bag_hatti"))
            {
                shape = EraseShape.Segment;
                width = (float)Math.Max(ProjectileEraseRulesDefaults.MinEraseWidthM, b.SizeM * ProjectileEraseRulesDefaults.MinBeamLengthM);
            }

            bool twice = e.Has("iki_kez");
            return new EraseSpec(
                shape, mode, radius, grow, rate,
                b.Chain, twice, anchored, lifesteal, reflectMult, width, length,
                twice ? Math.Max(0.0, b.CopyDelaySec) : 0.0);
        }

        static double Param(MechanicRules rules, string key, double fallback)
        {
            if (rules == null)
                return fallback;
            double v = rules.Param(key);
            return v > 0 ? v : fallback;
        }
    }
}
