namespace Dovus.Core.Equipment
{
    public static class WeaponPassiveKinds
    {
        public static WeaponPassiveKind Parse(string id)
        {
            if (string.IsNullOrEmpty(id))
                return WeaponPassiveKind.None;
            return id switch
            {
                "sirt_vurusu" => WeaponPassiveKind.Backstab,
                "genis_yay" => WeaponPassiveKind.WideArc,
                "yere_cakma" => WeaponPassiveKind.GroundSlam,
                "karsi_saldiri" => WeaponPassiveKind.CounterStrike,
                "kosu_atisi" => WeaponPassiveKind.RunShot,
                "sabit_nisan" => WeaponPassiveKind.SteadyAim,
                "uzun_buyu" => WeaponPassiveKind.LongEnchant,
                "kutsal_etki" => WeaponPassiveKind.HolyEffect,
                "dolu_sayfa" => WeaponPassiveKind.FullPage,
                "capraz_ates" => WeaponPassiveKind.CrossFire,
                _ => WeaponPassiveKind.None
            };
        }
    }
}
