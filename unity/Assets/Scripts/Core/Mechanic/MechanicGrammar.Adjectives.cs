using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    public sealed partial class MechanicGrammar
    {
        void ApplyAdjective(MechanicPlan p)
        {
            int s = p.Adjective;
            MechanicBody b = p.Body;
            b.SizeM *= _r.AdjNum(s, "hitbox_scale_mult", 1);
            double dmg = _r.AdjNum(s, "damage_mult", 1);
            foreach (MechanicEffect e in p.Effects.Where(e => e.Atom == "deger")) e.Amount *= dmg;
            b.LifeSec += _r.AdjNum(s, "lifetime_add", 0);
            if (_r.AdjHas(s, "max_targets")) b.MaxTargets = (int)_r.AdjNum(s, "max_targets");

            string op = _r.AdjectiveOp(s);
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
                case "surekli": Continuous(p); break;
                default: throw new InvalidOperationException("mechanic_grammar: bilinmeyen op " + op);
            }
            p.Trace.Add($"sıfat: {p.AdjectiveName} ({op}) → {string.Join(", ", p.Effects)}");
        }

        static List<MechanicEffect> Snapshot(MechanicPlan p) => p.Effects.ToList();

        static MechanicEffect Add(MechanicPlan p, string atom, string stat, string target, double amount, double dur, params string[] modes)
        {
            var e = new MechanicEffect { Atom = atom, Stat = stat, Target = target, Amount = amount, DurationSec = dur };
            foreach (string m in modes) e.Modes.Add(m);
            p.Effects.Add(e);
            return e;
        }

        static bool HasEnemy(MechanicPlan p) => p.Effects.Any(e => e.Target == "dusman");

        void Sikistir(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            double life = _r.Param("dense_life_mult");
            if (b.SpeedMps > 0) b.SpeedMps *= _r.Param("dense_speed_mult");
            b.LifeSec *= life;
            bool hitsEnemy = HasEnemy(p);
            if (hitsEnemy && b.Permeability != "kati") b.Permeability = "delici";
            if (!hitsEnemy) b.MaxTargets = 1;
            foreach (MechanicEffect e in Snapshot(p))
            {
                e.DurationSec *= life;
                switch (e.Atom)
                {
                    case "deger":
                        if (e.Amount > 0 && e.Target != "dusman") e.Modes.Add("tasar");
                        break;
                    case "hiz":
                        if (e.Stat == "tempo") e.Amount = Math.Max(MechanicDefaults.MinDenseRateFloor, 1 - (1 - e.Amount) * _r.Param("dense_rate_mult"));
                        e.Modes.Add("sert");
                        break;
                    case "konum":
                        e.Modes.Add(e.Stat == "kendini_tasi" ? "faz" : "sert");
                        break;
                    case "varlik":
                        e.Modes.Add(e.Stat == "mermi_sil" ? "delici" : "guclu");
                        break;
                    case "yon":
                        e.Amount *= _r.AdjNum(p.Adjective, "damage_mult", 1);
                        e.Modes.Add("sert");
                        break;
                }
            }
        }

        void Aktar(MechanicPlan p)
        {
            // Çekme yalnız saldıran ya da gerçekten çeken gövdede. Şifa, kalkan, arınma
            // ve tempo girdabı boss'u oyuncuya yapıştırmaz. Düşman canı döngüden önce okunur;
            // yararın eksiye dönmesi çekme sayılmaz.
            bool offensive = p.Effects.Any(e => e.Stat == "can" && e.Target == "dusman" && e.Amount < 0);
            double steal = _r.AdjNum(p.Adjective, "lifesteal");
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("deger", _) when e.Target == "dusman":
                        Add(p, "deger", e.Stat, "kendin", Math.Abs(e.Amount) * steal, e.DurationSec, "aktarim");
                        break;
                    case ("deger", _):
                        Add(p, "deger", e.Stat, "kendin", Math.Abs(e.Amount), e.DurationSec, "aktarim");
                        e.Stat = _r.EnemyStat(e.Stat);
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
                        // Yansıtma durur. Emme ayrıca cana çevirir; ikisi birden.
                        Add(p, "deger", "em", "kendin", Math.Abs(e.Amount), e.DurationSec, "cana_cevir");
                        break;
                }
            }
            p.Body.Pull = offensive || p.Effects.Any(e => e.Stat == "cek");
            List<MechanicEffect> erasers = p.Effects.Where(e => e.Stat == "mermi_sil").ToList();
            if (erasers.Count == 0) Add(p, "varlik", "mermi_sil", "dusman_nesnesi", 0, 0, "yut");
            else foreach (MechanicEffect e in erasers) e.Modes.Add("yut");
        }

        void Zincirle(MechanicPlan p)
        {
            p.Body.Chain = (int)_r.AdjNum(p.Adjective, "bounce_targets");
            p.Body.ChainMult = _r.AdjNum(p.Adjective, "bounce_damage_mult", 1);
            foreach (MechanicEffect e in Snapshot(p))
            {
                string mode;
                if (e.Atom == "konum" && e.Stat == "kendini_tasi") mode = "sekmeli";
                else if (e.Atom == "varlik" && e.Stat == "aktor_yarat") mode = "ziplayan";
                else if ((e.Atom == "deger" && e.Amount > 0) || e.Stat == "durum_sil") mode = "dosttan_dosta";
                else mode = "seker";
                e.Modes.Add(mode);
            }
        }

        void Sifirla(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            b.Anchored = true;
            b.SpeedMps = 0;
            b.Traits.Add("cast_koklu");
            bool help = p.Effects.Any(e => e.Atom == "deger" && e.Amount > 0 && e.Target != "dusman");
            bool harm = p.Effects.Any(e => e.Atom == "deger" && e.Amount < 0);
            if (help) b.Contact = "tik";
            else if (harm) b.Contact = "giris";
            double add = _r.AdjNum(p.Adjective, "lifetime_add");
            foreach (MechanicEffect e in Snapshot(p))
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

        void Genislet(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            b.Grows = true;
            b.Contact = "cephe";
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("hiz", _): e.Modes.Add("dalga"); break;
                    case ("konum", "kendini_tasi"):
                        e.Modes.Add("inis_dalgasi");
                        Add(p, "konum", "it", "dusman", 0, 0, "dalga");
                        break;
                    case ("konum", "it"): e.Modes.Add("dalga"); break;
                    case ("varlik", "aktor_yarat"): e.Amount = _r.Param("spread_count"); e.Modes.Add("halka"); break;
                    case ("yon", _): e.Modes.Add("aura"); break;
                    default: e.Modes.Add("alan"); break;
                }
            }
        }

        void Esitle(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            b.Link = true;
            b.Shape = "bag";
            b.ReachM = Math.Max(b.ReachM, _r.Param("link_len_m"));
            double root = _r.AdjNum(p.Adjective, "apply_root_sec");
            b.LifeSec = Math.Max(b.LifeSec, root);
            b.Contact = "tik";
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("deger", _) when e.Amount < 0: e.Modes.Add("bag_akisi"); break;
                    case ("deger", _):
                        e.Modes.Add("can_bagi");
                        Add(p, "deger", "hasar_paylasimi", "dost", _r.Param("link_share"), b.LifeSec, "bag");
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
                Add(p, "hiz", "hareket", "dusman", 1 - _r.AdjNum(p.Adjective, "apply_slow"), b.LifeSec, "bag_ucu", "yavas");
            }
        }

        void Belirsizlestir(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            b.Cloud = true;
            b.Shape = "bulut";
            b.Contact = "tik";
            foreach (MechanicEffect e in Snapshot(p))
            {
                string mode;
                switch (e.Atom, e.Stat)
                {
                    case ("hiz", "tempo"): mode = "titrer"; break;
                    case ("konum", "kendini_tasi"): mode = "gorunmez_gecis"; break;
                    case ("konum", "it"): mode = "sis_patlamasi"; break;
                    case ("varlik", "aktor_yarat"): mode = "gorunmez"; break;
                    case ("varlik", "mermi_sil"): mode = "sis_perdesi"; break;
                    case ("varlik", "durum_sil"): mode = "gizli"; break;
                    case ("yon", _): mode = "gizli"; break;
                    default: mode = "bulut_tik"; break;
                }
                e.Modes.Add(mode);
            }
            Add(p, "gorunurluk", "gizlen", "dost", 0, b.LifeSec, "bulut_ici");
            Add(p, "gorunurluk", "kor", "dusman", _r.AdjNum(p.Adjective, "accuracy_debuff"), b.LifeSec, "bulut_ici");
        }

        void Yukselt(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            b.Vertical = true;
            b.Ramp = true;
            bool rootsEnemy = p.Effects.Any(x => x.Atom == "hiz" && x.Stat == "hareket" && x.Target == "dusman" && x.Amount == 0);
            bool knockAdded = false;
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("deger", _) when e.Amount < 0:
                        e.Modes.Add("havaya_at");
                        if (!rootsEnemy && !knockAdded)
                        {
                            Add(p, "hiz", "hareket", "dusman", 0, _r.Param("knockup_sec"), "havada");
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
            Add(p, "deger", "hasar_buff", "kendin", _r.AdjNum(p.Adjective, "self_damage_buff"),
                _r.AdjNum(p.Adjective, "buff_duration_sec"), "yukselen");
        }

        void Kilitle(MechanicPlan p)
        {
            p.Body.Homing = true;
            p.Body.Traits.Add("zirh_yoksay");
            foreach (MechanicEffect e in Snapshot(p))
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

        void TersCevir(MechanicPlan p)
        {
            p.Body.Mirror = true;
            double ratio = _r.AdjNum(p.Adjective, "reflect_ratio");
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("deger", _) when e.Amount < 0: e.Modes.Add("kiskac"); break;
                    case ("deger", _):
                        Add(p, "deger", _r.EnemyStat(e.Stat), "dusman", -Math.Abs(e.Amount) * ratio, e.DurationSec, "ters_kopya");
                        break;
                    case ("hiz", "tempo"): e.Stat = "geri_sar"; e.Amount = _r.Param("rewind_sec"); e.Modes.Add("geri_sarma"); break;
                    case ("hiz", "hareket"): e.Modes.Add("ters_kontrol"); break;
                    case ("konum", "kendini_tasi"): e.Stat = "portal"; e.DurationSec = _r.Param("portal_life_sec"); e.Modes.Add("portal_cifti"); break;
                    case ("konum", "it"): e.Modes.Add("iki_uctan"); break;
                    case ("varlik", "aktor_yarat"): e.Modes.Add("ayna_klon"); break;
                    case ("varlik", "durum_sil"): e.Stat = "iyi_durum_sil"; e.Target = "dusman"; e.Modes.Add("ters_hedef"); break;
                    case ("varlik", "mermi_sil"): e.Modes.Add("geri_gonder"); break;
                    case ("yon", _): e.Modes.Add("bolunen"); break;
                }
            }
            Add(p, "yon", "yansit", "kendin", ratio, _r.AdjNum(p.Adjective, "reflect_duration_sec"), "ayna_sifati");
        }

        void Cogalt(MechanicPlan p)
        {
            p.Body.Count += 1;
            p.Body.CopyDelaySec = _r.AdjNum(p.Adjective, "duplicate_delay_sec");
            foreach (MechanicEffect e in Snapshot(p))
            {
                switch (e.Atom, e.Stat)
                {
                    case ("hiz", "tempo"):
                        e.Modes.Add("iki_kez");
                        Add(p, "varlik", "onceki_skill_tekrar", "kendin", 1, 0, "yanki");
                        break;
                    case ("konum", "kendini_tasi"):
                        e.Modes.Add("ardinda_kopya");
                        Add(p, "varlik", "yem_kopya", "kendin", 1, _r.Param("decoy_life_sec"), "dikkat_ceker");
                        break;
                    case ("varlik", "aktor_yarat"): e.Stat = "klon"; e.Modes.Add("senin_kopyan"); break;
                    default: e.Modes.Add("iki_kez"); break;
                }
            }
        }

        /// <summary>
        /// JSON skill bayrağı decoy_aggro (D17, kullanıcı onaylı): yem/klon diye anlatılan skill
        /// başladığın yerde 3-11 gibi dikkat çeken bir yem bırakır. Sıfat kuralı zaten yem verdiyse dokunmaz.
        /// </summary>
        void ApplySkillDecoy(MechanicPlan p)
        {
            if (p.Adjective <= 0 || !_r.SkillLeavesDecoy(p.Verb, p.Adjective) || p.Find("yem_kopya") != null)
                return;
            Add(p, "varlik", "yem_kopya", "kendin", 1, _r.Param("decoy_life_sec"), "dikkat_ceker");
        }

        void Continuous(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            double channel = _r.AdjNum(p.Adjective, "channel_sec");
            b.Continuous = true;
            b.LifeSec = Math.Max(b.LifeSec, channel);
            b.Contact = "tik";
            foreach (MechanicEffect e in Snapshot(p))
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
    }
}
