namespace Dovus.Core.Equipment
{
    public enum WeaponPassiveKind
    {
        None,
        SirtVurusu,
        GenisYay,
        YereCakma,
        KarsiSaldiri,
        KosuAtisi,
        SabitNisan,
        UzunBuyu,
        KutsalEtki,
        DoluSayfa,
        CaprazAtes
    }

    public static class WeaponPassiveKinds
    {
        public static WeaponPassiveKind Parse(string id)
        {
            if (string.IsNullOrEmpty(id))
                return WeaponPassiveKind.None;
            return id switch
            {
                "sirt_vurusu" => WeaponPassiveKind.SirtVurusu,
                "genis_yay" => WeaponPassiveKind.GenisYay,
                "yere_cakma" => WeaponPassiveKind.YereCakma,
                "karsi_saldiri" => WeaponPassiveKind.KarsiSaldiri,
                "kosu_atisi" => WeaponPassiveKind.KosuAtisi,
                "sabit_nisan" => WeaponPassiveKind.SabitNisan,
                "uzun_buyu" => WeaponPassiveKind.UzunBuyu,
                "kutsal_etki" => WeaponPassiveKind.KutsalEtki,
                "dolu_sayfa" => WeaponPassiveKind.DoluSayfa,
                "capraz_ates" => WeaponPassiveKind.CaprazAtes,
                _ => WeaponPassiveKind.None
            };
        }
    }
}
