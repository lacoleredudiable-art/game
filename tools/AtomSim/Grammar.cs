using System.Text.Json;

namespace AtomSim;

/// <summary>
/// Skill(fiil, sıfat, silah) = Sıfat.kural(Silah.teslim(Fiil.atomlar)) → çakışma → etiket.
/// Sıfat kuralları atom TÜRÜNE yazılır; (fiil, sıfat) çiftine özel dal yoktur.
/// </summary>
sealed class Grammar
{
    readonly Data _d;

    public Grammar(Data d) => _d = d;

    public Plan Compose(int verb, int adjective, Weapon weapon)
    {
        var p = new Plan
        {
            Verb = verb,
            Adjective = adjective,
            Weapon = weapon.Id,
            VerbName = _d.VerbNames[verb],
            AdjectiveName = adjective > 0 ? _d.AdjectiveNames[adjective] : "",
            WeaponName = weapon.Name
        };
        BuildVerb(p);
        ApplyWeapon(p, weapon);
        if (adjective > 0) ApplyAdjective(p);
        ResolveConflicts(p);
        Labeler.Label(p);
        FindContradictions(p);
        p.Description = Describer.Describe(p);
        return p;
    }

    // ---------------------------------------------------------------- 1. fiil

    void BuildVerb(Plan p)
    {
        JsonElement va = _d.VerbAtoms(p.Verb);
        p.Body.Traits.Add(va.GetProperty("trait").GetString() ?? "");
        foreach (JsonElement e in va.GetProperty("effects").EnumerateArray())
        {
            var ef = new Effect
            {
                Atom = e.GetProperty("atom").GetString() ?? "",
                Stat = e.GetProperty("stat").GetString() ?? "",
                Target = e.GetProperty("target").GetString() ?? ""
            };
            double amount = 0;
            if (e.TryGetProperty("from", out JsonElement from)) amount = _d.VerbNum(p.Verb, from.GetString() ?? "");
            else if (e.TryGetProperty("value", out JsonElement value)) amount = value.GetDouble();
            if (e.TryGetProperty("sign", out JsonElement sign))
                amount = sign.GetString() == "zarar" ? -Math.Abs(amount) : Math.Abs(amount);
            ef.Amount = amount;
            if (e.TryGetProperty("duration_from", out JsonElement dur))
                ef.DurationSec = _d.VerbNum(p.Verb, dur.GetString() ?? "");
            p.Effects.Add(ef);
        }

        VerbHitbox hb = _d.Hitbox(p.Verb);
        p.Body.Shape = hb.Shape;
        p.Body.SizeM = hb.SizeA;
        p.Body.LifeSec = hb.Timed ? Math.Max(0.5, p.Effects.Max(x => x.DurationSec)) : 0.2;
        if (va.TryGetProperty("body", out JsonElement body) && body.TryGetProperty("permeability", out JsonElement perm))
            p.Body.Permeability = perm.GetString() ?? "normal";
        p.Trace.Add($"fiil: {p.VerbName} → {string.Join(", ", p.Effects)} | gövde {hb.Shape} {hb.SizeA}m");
    }

    // ---------------------------------------------------------------- 2. silah

    static readonly Dictionary<string, string> SelfMoveByPath = new()
    {
        ["temas"] = "kisa_hamle",
        ["durtme"] = "hamle",
        ["saplama"] = "uzun_hamle",
        ["yay"] = "yay_kayma",
        ["agir_yay"] = "agir_hamle",
        ["yere_vurus"] = "sicrayip_cakilma",
        ["balistik"] = "firlatilma",
        ["isin"] = "hat_kayma",
        ["yerlestirme"] = "isarete_isinlanma",
        ["govde"] = "kalkan_hucumu"
    };

    public static bool IsRangedPath(string path) => path is "balistik" or "isin" or "yerlestirme";

    /// <summary>Yolun nitel sınıfı: aynı sınıftaki silahlar yalnız sayıyla ayrışır.</summary>
    public static string PathClass(string path) => path switch
    {
        "temas" or "durtme" or "saplama" => "yakin_itis",
        "yay" or "agir_yay" => "yay",
        "yere_vurus" => "dikey",
        "balistik" => "ucan",
        "isin" => "hat",
        "yerlestirme" => "belirme",
        "govde" => "govde",
        _ => path
    };

    void ApplyWeapon(Plan p, Weapon w)
    {
        Body b = p.Body;
        b.Path = w.Path;
        p.Compatible = w.CompatibleVerbs.Contains(p.Verb);
        bool ranged = IsRangedPath(w.Path);
        b.ReachM = (ranged ? _d.Param("ranged_range_m") : _d.Param("melee_reach_m")) * w.RangeMult;

        bool movesSelf = p.Effects.Any(e => e.Stat == "kendini_tasi");
        b.BornAt = movesSelf ? "sende" : w.Path switch
        {
            "temas" => "dokunus",
            "durtme" => "onunde",
            "saplama" => "onunde_hat",
            "yay" or "agir_yay" => "onunde_yay",
            "yere_vurus" or "balistik" or "yerlestirme" => "hedef_noktada",
            "isin" => "hat",
            "govde" => "sende",
            _ => "onunde"
        };
        if (w.Path == "temas" && !movesSelf) b.MaxTargets = 1;

        switch (w.Path)
        {
            case "agir_yay":
                b.Unstoppable = true;
                break;
            case "yere_vurus":
                b.Vertical = true;
                break;
            case "balistik":
                b.SpeedMps = _d.Param("ballistic_speed_mps");
                break;
            case "isin":
                b.Contact = "tik";
                b.LifeSec = Math.Max(b.LifeSec, _d.Param("beam_channel_sec") * (p.Compatible ? 1.3 : 1.0));
                break;
            case "govde":
                b.Attached = true;
                b.LifeSec = Math.Max(b.LifeSec, 1.0);
                break;
        }

        foreach (Effect e in p.Effects)
        {
            if (e.Atom == "deger" && e.Stat == "can" && e.Amount < 0) e.Amount *= w.DamageMult;
            if (e.Atom == "konum" && e.Stat == "kendini_tasi")
            {
                e.Amount *= w.RangeMult;
                e.Modes.Add("yol:" + SelfMoveByPath[w.Path]);
            }
            if (e.Stat is "aktor_yarat" or "yansit") e.Modes.Add("silahla:" + PathClass(w.Path));
        }

        if (p.Compatible) ApplyWeaponIdentity(p, w);
        p.Trace.Add($"silah: {w.Name} yol={w.Path} menzil={b.ReachM:0.#}m doğar={b.BornAt}{(p.Compatible ? " +kimlik" : "")}");
    }

    /// <summary>weapon_skill_interaction tier C: yalnız uyumlu fiilde.</summary>
    static void ApplyWeaponIdentity(Plan p, Weapon w)
    {
        Body b = p.Body;
        bool harms = p.Effects.Any(e => e.Atom == "deger" && e.Target == "dusman");
        switch (w.Path)
        {
            case "temas": b.Traits.Add("seri_vurus_+10"); break;
            case "durtme": b.Traits.Add("arkadan_x1.5"); break;
            case "saplama":
                foreach (Effect e in p.Effects.Where(e => e.Atom == "deger" && e.Stat == "can" && e.Amount < 0))
                    e.Modes.Add("zirh_delen");
                break;
            case "yay": b.SizeM *= 1.2; break;
            case "agir_yay": b.Traits.Add("poise_x1.5"); break;
            case "yere_vurus":
                if (harms)
                    p.Effects.Add(new Effect { Atom = "hiz", Stat = "hareket", Target = "dusman", Amount = 0, DurationSec = 0.5, Modes = { "sersem" } });
                break;
            case "balistik": b.Traits.Add("sabitken_+50"); break;
            case "isin": break;
            case "yerlestirme":
                foreach (Effect e in p.Effects.Where(e => e.Atom == "deger" && e.Amount > 0)) e.Amount *= 1.2;
                break;
            case "govde": b.Traits.Add("blok_sonrasi_karsi_+20"); break;
        }
    }

    // ---------------------------------------------------------------- 3. sıfat

    void ApplyAdjective(Plan p)
    {
        int s = p.Adjective;
        Body b = p.Body;
        b.SizeM *= _d.AdjNum(s, "hitbox_scale_mult", 1);
        double dmg = _d.AdjNum(s, "damage_mult", 1);
        foreach (Effect e in p.Effects.Where(e => e.Atom == "deger")) e.Amount *= dmg;
        b.LifeSec += _d.AdjNum(s, "lifetime_add", 0);
        if (_d.AdjHas(s, "max_targets")) b.MaxTargets = (int)_d.AdjNum(s, "max_targets");

        string op = _d.AdjectiveOps[s];
        switch (op)
        {
            case "sikistir": Sikistir(p); break;
            case "aktar": Aktar(p); break;
            case "zincirle": Zincirle(p); break;
            case "sifirla": Sifirla(p); break;
            case "genislet": Genislet(p); break;
            case "esitle": Esitle(p); break;
            case "belirsizlestir": Belirsizlestir(p); break;
            case "yukselt": Yukselt(p); break;
            case "kilitle": Kilitle(p); break;
            case "ters_cevir": TersCevir(p); break;
            case "cogalt": Cogalt(p); break;
            case "surekli": Surekli(p); break;
            default: throw new InvalidOperationException("bilinmeyen op " + op);
        }
        p.Trace.Add($"sıfat: {p.AdjectiveName} ({op}) → {string.Join(", ", p.Effects)}");
    }

    static List<Effect> Snapshot(Plan p) => p.Effects.ToList();

    static Effect Add(Plan p, string atom, string stat, string target, double amount, double dur, params string[] modes)
    {
        var e = new Effect { Atom = atom, Stat = stat, Target = target, Amount = amount, DurationSec = dur };
        foreach (string m in modes) e.Modes.Add(m);
        p.Effects.Add(e);
        return e;
    }

    static bool HasEnemy(Plan p) => p.Effects.Any(e => e.Target == "dusman");

    void Sikistir(Plan p)
    {
        Body b = p.Body;
        double life = _d.Param("dense_life_mult");
        if (b.SpeedMps > 0) b.SpeedMps *= _d.Param("dense_speed_mult");
        b.LifeSec *= life;
        bool hitsEnemy = HasEnemy(p);
        if (hitsEnemy && b.Permeability != "kati") b.Permeability = "delici";
        if (!hitsEnemy) b.MaxTargets = 1;
        foreach (Effect e in Snapshot(p))
        {
            e.DurationSec *= life;
            switch (e.Atom)
            {
                case "deger" when e.Amount > 0 && e.Target != "dusman":
                    e.Modes.Add("tasma");
                    break;
                case "hiz":
                    if (e.Stat == "tempo") e.Amount = Math.Max(0.05, 1 - (1 - e.Amount) * _d.Param("dense_rate_mult"));
                    e.Modes.Add("sert");
                    break;
                case "konum":
                    e.Modes.Add(e.Stat == "kendini_tasi" ? "faz" : "sert");
                    break;
                case "varlik":
                    e.Modes.Add(e.Stat == "mermi_sil" ? "delici" : "guclu");
                    break;
                case "yon":
                    e.Amount *= _d.AdjNum(1, "damage_mult", 1);
                    e.Modes.Add("sert");
                    break;
            }
        }
    }

    void Aktar(Plan p)
    {
        Body b = p.Body;
        b.Pull = true;
        double steal = _d.AdjNum(p.Adjective, "lifesteal");
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _) when e.Target == "dusman":
                    Add(p, "deger", e.Stat, "kendin", Math.Abs(e.Amount) * steal, e.DurationSec, "aktarim");
                    break;
                case ("deger", _):
                    Add(p, "deger", e.Stat, "kendin", Math.Abs(e.Amount), e.DurationSec, "aktarim");
                    e.Stat = _d.EnemyStat(e.Stat);
                    e.Target = "dusman";
                    e.Amount = -Math.Abs(e.Amount);
                    e.Modes.Add("emme");
                    break;
                case ("hiz", "tempo"):
                    Add(p, "hiz", "tempo", "kendin", 1 + (1 - e.Amount), e.DurationSec, "aktarim");
                    break;
                case ("hiz", "hareket"):
                    Add(p, "konum", "cek", "dusman", 0, 0, "sana_dogru");
                    break;
                case ("konum", "kendini_tasi"):
                    e.Stat = "cek";
                    e.Target = "dusman";
                    e.Modes.Add("sana_dogru");
                    break;
                case ("konum", "it"):
                    e.Stat = "cek";
                    e.Modes.Add("merkeze");
                    break;
                case ("varlik", "durum_sil"):
                    e.Stat = "durum_aktar";
                    Add(p, "varlik", "durum_ekle", "dusman", e.Amount, 0, "aktarim");
                    break;
                case ("varlik", "aktor_yarat"):
                    e.Modes.Add("can_emen");
                    break;
                case ("yon", "yansit"):
                    e.Stat = "em";
                    e.Modes.Add("cana_cevir");
                    break;
            }
        }
        if (!p.Effects.Any(e => e.Stat == "mermi_sil"))
            Add(p, "varlik", "mermi_sil", "dusman_nesnesi", 0, 0, "yut");
        else
            foreach (Effect e in p.Effects.Where(e => e.Stat == "mermi_sil")) e.Modes.Add("yut");
    }

    void Zincirle(Plan p)
    {
        Body b = p.Body;
        b.Chain = (int)_d.AdjNum(p.Adjective, "bounce_targets");
        b.ChainMult = _d.AdjNum(p.Adjective, "bounce_damage_mult", 1);
        foreach (Effect e in Snapshot(p))
        {
            e.Modes.Add((e.Atom, e.Stat) switch
            {
                ("konum", "kendini_tasi") => "sekmeli",
                ("varlik", "aktor_yarat") => "ziplayan",
                ("deger", _) when e.Amount > 0 => "dosttan_dosta",
                ("varlik", "durum_sil") => "dosttan_dosta",
                _ => "seker"
            });
        }
    }

    void Sifirla(Plan p)
    {
        Body b = p.Body;
        b.Anchored = true;
        b.SpeedMps = 0;
        b.Traits.Add("cast_koklu");
        bool help = p.Effects.Any(e => e.Atom == "deger" && e.Amount > 0 && e.Target != "dusman");
        bool harm = p.Effects.Any(e => e.Atom == "deger" && e.Amount < 0);
        if (help) b.Contact = "tik";
        else if (harm) b.Contact = "giris";
        double add = _d.AdjNum(p.Adjective, "lifetime_add");
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _): e.Modes.Add(e.Amount < 0 ? "tuzak" : "totem"); break;
                case ("hiz", "tempo"): e.Amount = 0; e.Modes.Add("dondur"); break;
                case ("hiz", "hareket"): e.DurationSec += add; e.Modes.Add("uzun"); break;
                case ("konum", "kendini_tasi"):
                    e.Stat = "isaret_geri_don";
                    e.DurationSec = b.LifeSec;
                    e.Modes.Add("geri_donus");
                    break;
                case ("konum", "it"): e.Modes.Add(b.Contact == "giris" ? "tuzak" : "itme_alani"); break;
                case ("varlik", "aktor_yarat"): e.Modes.Add("taret"); break;
                case ("varlik", "durum_sil"): e.Modes.Add("arinma_alani"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("engel"); break;
                case ("yon", "yansit"): e.Modes.Add("ayna_yuzey"); break;
            }
        }
    }

    void Genislet(Plan p)
    {
        Body b = p.Body;
        b.Grows = true;
        b.Contact = "cephe";
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("hiz", _): e.Modes.Add("dalga"); break;
                case ("konum", "kendini_tasi"):
                    e.Modes.Add("inis_dalgasi");
                    Add(p, "konum", "it", "dusman", 0, 0, "dalga");
                    break;
                case ("konum", "it"): e.Modes.Add("dalga"); break;
                case ("varlik", "aktor_yarat"): e.Amount = _d.Param("spread_count"); e.Modes.Add("halka"); break;
                case ("yon", _): e.Modes.Add("aura"); break;
                default: e.Modes.Add("alan"); break;
            }
        }
    }

    void Esitle(Plan p)
    {
        Body b = p.Body;
        b.Link = true;
        b.Shape = "bag";
        b.ReachM = Math.Max(b.ReachM, _d.Param("link_len_m"));
        double root = _d.AdjNum(p.Adjective, "apply_root_sec");
        b.LifeSec = Math.Max(b.LifeSec, root);
        b.Contact = "tik";
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _) when e.Amount < 0: e.Modes.Add("bag_akisi"); break;
                case ("deger", _):
                    e.Modes.Add("can_bagi");
                    Add(p, "deger", "hasar_paylasimi", "dost", _d.Param("link_share"), b.LifeSec, "bag");
                    break;
                case ("hiz", "tempo"): e.Modes.Add("senkron"); break;
                case ("hiz", "hareket"): e.Modes.Add("tasma"); break;
                case ("konum", "kendini_tasi"): e.Stat = "yer_degistir"; e.Target = "dusman"; e.Modes.Add("iki_uc"); break;
                case ("konum", "it"): e.Stat = "cek"; e.Modes.Add("bag_boyunca"); break;
                case ("varlik", "aktor_yarat"): e.Modes.Add("bagli_muhafiz"); break;
                case ("varlik", "durum_sil"): e.Modes.Add("bag_bagisiklik"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("bag_hatti"); break;
                case ("yon", "yansit"): e.Stat = "yonlendir"; e.Target = "dusman"; e.Modes.Add("bag"); break;
            }
        }
        if (HasEnemy(p))
        {
            Add(p, "hiz", "hareket", "dusman", 0, root, "bag_ucu");
            Add(p, "hiz", "hareket", "dusman", 1 - _d.AdjNum(p.Adjective, "apply_slow"), b.LifeSec, "bag_ucu", "yavas");
        }
    }

    void Belirsizlestir(Plan p)
    {
        Body b = p.Body;
        b.Cloud = true;
        b.Shape = "bulut";
        b.Contact = "tik";
        foreach (Effect e in Snapshot(p))
        {
            e.Modes.Add((e.Atom, e.Stat) switch
            {
                ("hiz", "tempo") => "titrer",
                ("konum", "kendini_tasi") => "gorunmez_gecis",
                ("konum", "it") => "sis_patlamasi",
                ("varlik", "aktor_yarat") => "gorunmez",
                ("varlik", "mermi_sil") => "sis_perdesi",
                ("varlik", "durum_sil") => "gizli",
                ("yon", _) => "gizli",
                _ => "bulut_tik"
            });
        }
        Add(p, "gorunurluk", "gizlen", "dost", 0, b.LifeSec, "bulut_ici");
        Add(p, "gorunurluk", "kor", "dusman", _d.AdjNum(p.Adjective, "accuracy_debuff"), b.LifeSec, "bulut_ici");
    }

    void Yukselt(Plan p)
    {
        Body b = p.Body;
        b.Vertical = true;
        b.Ramp = true;
        bool rootsEnemy = p.Effects.Any(x => x.Atom == "hiz" && x.Stat == "hareket" && x.Target == "dusman" && x.Amount == 0);
        bool knockAdded = false;
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _) when e.Amount < 0:
                    e.Modes.Add("havaya_at");
                    if (!rootsEnemy && !knockAdded)
                    {
                        Add(p, "hiz", "hareket", "dusman", 0, _d.Param("knockup_sec"), "havada");
                        knockAdded = true;
                    }
                    break;
                case ("deger", _): e.Modes.Add("buyuyen"); break;
                case ("hiz", "tempo"):
                    e.Target = "kendin";
                    e.Amount = 1 + (1 - e.Amount);
                    e.Modes.Add("hizlanma");
                    break;
                case ("hiz", "hareket"):
                    if (!e.Modes.Contains("havada")) e.Modes.Add("havaya_at");
                    break;
                case ("konum", "kendini_tasi"): e.Modes.Add("sicrayip_cakil"); break;
                case ("konum", "it"): e.Modes.Add("yukari_firlat"); break;
                case ("varlik", "aktor_yarat"): e.Modes.Add("buyuyen"); break;
                case ("varlik", "durum_sil"): e.Modes.Add("guce_cevir"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("yukselen_perde"); break;
                case ("yon", _): e.Modes.Add("artan_oran"); break;
            }
        }
        Add(p, "deger", "hasar_buff", "kendin", _d.AdjNum(p.Adjective, "self_damage_buff"),
            _d.AdjNum(p.Adjective, "buff_duration_sec"), "yukselen");
    }

    void Kilitle(Plan p)
    {
        Body b = p.Body;
        b.Homing = true;
        b.Traits.Add("zirh_yoksay");
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", "can") when e.Amount < 0: e.Modes.Add("iskalamaz"); e.Modes.Add("zirh_yoksay"); break;
                case ("deger", _) when e.Amount < 0: e.Modes.Add("isaretli_an"); break;
                case ("deger", _): e.Modes.Add("koruyucu_tetik"); break;
                case ("hiz", _): e.Modes.Add("isaretli_an"); break;
                case ("konum", "kendini_tasi"): e.Stat = "hedefin_arkasina"; e.Modes.Add("isinlanma"); break;
                case ("konum", "it"): e.Modes.Add("tek_hedef"); break;
                case ("varlik", "aktor_yarat"): e.Modes.Add("suikastci"); break;
                case ("varlik", "durum_sil"): e.Amount = 99; e.Modes.Add("tumunu_sil"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("hedefli"); break;
                case ("yon", _): e.Amount = Math.Min(1, e.Amount * 2); e.Modes.Add("savusturma"); break;
            }
        }
    }

    void TersCevir(Plan p)
    {
        Body b = p.Body;
        b.Mirror = true;
        double ratio = _d.AdjNum(p.Adjective, "reflect_ratio");
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _) when e.Amount < 0: e.Modes.Add("kiskac"); break;
                case ("deger", _):
                    Add(p, "deger", _d.EnemyStat(e.Stat), "dusman", -Math.Abs(e.Amount) * ratio, e.DurationSec, "ters_kopya");
                    break;
                case ("hiz", "tempo"): e.Stat = "geri_sar"; e.Amount = _d.Param("rewind_sec"); e.Modes.Add("geri_sarma"); break;
                case ("hiz", "hareket"): e.Modes.Add("ters_kontrol"); break;
                case ("konum", "kendini_tasi"): e.Stat = "portal"; e.DurationSec = _d.Param("portal_life_sec"); e.Modes.Add("portal_cifti"); break;
                case ("konum", "it"): e.Modes.Add("iki_uctan"); break;
                case ("varlik", "aktor_yarat"): e.Modes.Add("ayna_klon"); break;
                case ("varlik", "durum_sil"): e.Stat = "iyi_durum_sil"; e.Target = "dusman"; e.Modes.Add("ters_hedef"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("geri_gonder"); break;
                case ("yon", _): e.Modes.Add("bolunen"); break;
            }
        }
        Add(p, "yon", "yansit", "kendin", ratio, _d.AdjNum(p.Adjective, "reflect_duration_sec"), "ayna_sifati");
    }

    void Cogalt(Plan p)
    {
        Body b = p.Body;
        b.Count += 1;
        b.CopyDelaySec = _d.AdjNum(p.Adjective, "duplicate_delay_sec");
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("hiz", "tempo"):
                    e.Modes.Add("iki_kez");
                    Add(p, "varlik", "onceki_skill_tekrar", "kendin", 1, 0, "yanki");
                    break;
                case ("konum", "kendini_tasi"):
                    e.Modes.Add("ardinda_kopya");
                    Add(p, "varlik", "yem_kopya", "kendin", 1, _d.Param("decoy_life_sec"), "dikkat_ceker");
                    break;
                case ("varlik", "aktor_yarat"): e.Stat = "klon"; e.Modes.Add("senin_kopyan"); break;
                default: e.Modes.Add("iki_kez"); break;
            }
        }
    }

    void Surekli(Plan p)
    {
        Body b = p.Body;
        double channel = _d.AdjNum(p.Adjective, "channel_sec");
        b.Continuous = true;
        b.LifeSec = Math.Max(b.LifeSec, channel);
        b.Contact = "tik";
        foreach (Effect e in Snapshot(p))
        {
            switch (e.Atom, e.Stat)
            {
                case ("deger", _): e.DurationSec = channel; e.Modes.Add("akis"); break;
                case ("hiz", "tempo"):
                    e.Modes.Add("zaman_alani");
                    e.DurationSec = channel;
                    Add(p, "hiz", "tempo", "dost", 1 + (1 - e.Amount), channel, "zaman_alani");
                    break;
                case ("hiz", "hareket"): e.Modes.Add("bataklik"); e.DurationSec = channel; break;
                case ("konum", "kendini_tasi"): e.DurationSec = channel; e.Modes.Add("suzulme"); break;
                case ("konum", "it"): e.Modes.Add("akinti"); break;
                case ("varlik", "aktor_yarat"): e.Amount = channel; e.Modes.Add("akis"); break;
                case ("varlik", "durum_sil"): e.Modes.Add("aura"); break;
                case ("varlik", "mermi_sil"): e.Modes.Add("surekli_perde"); break;
                case ("yon", _): e.DurationSec = channel; e.Modes.Add("surekli"); break;
            }
        }
    }

    // ---------------------------------------------------------------- 4. çakışma

    static void ResolveConflicts(Plan p)
    {
        Body b = p.Body;
        if (b.Anchored && b.Attached) { b.Attached = false; p.Conflicts.Add("çapalı > gövdeye bağlı: yerinde bırakıldı"); }
        if (b.Anchored && b.Homing) { b.Homing = false; p.Conflicts.Add("çapalı > güdüm: güdüm iptal"); }
        if (b.Permeability == "kati" && b.Traits.Contains("delici")) p.Conflicts.Add("katı > delici");
        if (b.Anchored && b.Path == "balistik") { b.Traits.Add("mayin"); p.Conflicts.Add("çapalı + balistik = indiği yerde mayın/engel"); }
        if (b.Anchored && b.Path == "isin") { b.Traits.Add("cit"); p.Conflicts.Add("çapalı + ışın = sabit hat (çit)"); }
        if (b.Cloud && b.Link) { b.Shape = "sis_koridoru"; p.Conflicts.Add("bulut + bağ = sis koridoru"); }
        if (b.Continuous && b.Path == "balistik") { b.Traits.Add("inen_akis_alani"); p.Conflicts.Add("sürekli + balistik = indiği yerde akan alan"); }
        if (b.Continuous && b.Path == "isin") p.Conflicts.Add("sürekli + ışın = uzun kanal");
    }

    // ---------------------------------------------------------------- D. çelişki

    static void FindContradictions(Plan p)
    {
        Body b = p.Body;
        foreach (Effect e in p.Effects)
        {
            if (e.Atom == "deger" && e.Amount > 0 && e.Target == "dusman") p.Contradictions.Add($"düşmana yarar ({e.Stat})");
            if (e.Atom == "deger" && e.Amount < 0 && e.Target is "dost" or "kendin") p.Contradictions.Add($"dosta zarar ({e.Stat})");
            if (e.Atom == "hiz" && e.Stat == "hareket" && e.Target != "dusman") p.Contradictions.Add("dosta kök/yavaşlatma");
            if (e.Atom == "hiz" && e.Stat == "tempo" && e.Amount < 1 && e.Target != "dusman") p.Contradictions.Add("dosta tempo düşürme");
            if (e.Atom == "gorunurluk" && e.Stat == "kor" && e.Target != "dusman") p.Contradictions.Add("dostu körleştirme");
        }
        if (b.Permeability == "kati" && b.Traits.Contains("delici")) p.Contradictions.Add("hem katı hem delici");
        if (b.Anchored && b.Homing) p.Contradictions.Add("hem çapalı hem güdümlü");
        if (b.Anchored && b.Attached) p.Contradictions.Add("hem çapalı hem gövdeye bağlı");
        if (p.Verb == 3 && !p.Effects.Any(e => e.Atom == "konum")) p.Contradictions.Add("Hareket fiili hiçbir şeyi taşımıyor");
        if (b.Link && p.Effects.All(e => e.Target == "kendin" && e.Stat != "aktor_yarat")) p.Contradictions.Add("bağın ikinci ucu yok");
    }
}
