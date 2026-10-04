using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// Skill(fiil, sıfat, silah) = Sıfat.kural(Silah.teslim(Fiil.atomlar)) → çakışma → etiket.
    /// Sıfat kuralları atom TÜRÜNE yazılır; (fiil, sıfat) çiftine özel dal yoktur (AGENTS kural 6).
    /// Kurallar element-sistemi.json mechanic_grammar'dan, sayılar verb_base/adjective_mods'tan.
    /// </summary>
    public sealed class MechanicGrammar
    {
        readonly MechanicRules _r;

        public MechanicGrammar(MechanicRules rules) => _r = rules ?? throw new ArgumentNullException(nameof(rules));

        public MechanicRules Rules => _r;

        public MechanicPlan Compose(int verb, int adjective, int weaponId)
        {
            MechanicWeapon w = _r.Weapon(weaponId) ?? _r.Weapons[0];
            return Compose(verb, adjective, w);
        }

        public MechanicPlan Compose(int verb, int adjective, MechanicWeapon weapon)
        {
            var p = new MechanicPlan
            {
                Verb = verb,
                Adjective = adjective,
                Weapon = weapon.Id,
                VerbName = _r.VerbName(verb),
                AdjectiveName = adjective > 0 ? _r.AdjectiveName(adjective) : string.Empty,
                WeaponName = weapon.Name
            };
            BuildVerb(p);
            ApplyWeapon(p, weapon);
            if (adjective > 0) ApplyAdjective(p);
            ApplySkillDecoy(p);
            ResolveConflicts(p);
            MechanicLabeler.Label(p);
            FindContradictions(p);
            p.Description = MechanicDescriber.Describe(p);
            return p;
        }

        // ---------------------------------------------------------------- 1. fiil (hitbox NE)

        void BuildVerb(MechanicPlan p)
        {
            JsonValue va = _r.VerbAtoms(p.Verb);
            p.Body.Traits.Add(va["trait"].AsString());
            foreach (JsonValue e in va["effects"].AsArray())
            {
                var ef = new MechanicEffect
                {
                    Atom = e["atom"].AsString(),
                    Stat = e["stat"].AsString(),
                    Target = e["target"].AsString()
                };
                double amount = 0;
                if (e.Has("from")) amount = _r.VerbNum(p.Verb, e["from"].AsString());
                else if (e.Has("value")) amount = e["value"].AsDouble();
                if (e.Has("sign"))
                    amount = e["sign"].AsString() == "zarar" ? -Math.Abs(amount) : Math.Abs(amount);
                ef.Amount = amount;
                if (e.Has("duration_from")) ef.DurationSec = _r.VerbNum(p.Verb, e["duration_from"].AsString());
                p.Effects.Add(ef);
            }

            MechanicHitbox hb = _r.Hitbox(p.Verb);
            p.Body.Shape = hb.Shape;
            p.Body.SizeM = hb.SizeA;
            double longest = p.Effects.Count > 0 ? p.Effects.Max(x => x.DurationSec) : 0;
            p.Body.LifeSec = hb.Timed ? Math.Max(0.5, longest) : 0.2;
            string perm = va["body"]["permeability"].AsString();
            if (perm.Length > 0) p.Body.Permeability = perm;
            p.Trace.Add($"fiil: {p.VerbName} → {string.Join(", ", p.Effects)} | gövde {hb.Shape} {hb.SizeA}m");
        }

        // ---------------------------------------------------------------- 2. silah (teslim)

        static readonly Dictionary<string, string> SelfMoveByPath = new Dictionary<string, string>
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
            ["govde"] = "kalkan_hucumu",
            ["ok"] = "ok_kayma",
            ["sayfa"] = "sayfa_kayma",
            ["kure"] = "kureye_isinlanma"
        };

        public static bool IsRangedPath(string path) =>
            path == "balistik" || path == "isin" || path == "yerlestirme"
            || path == "ok" || path == "sayfa" || path == "kure";

        /// <summary>Yolun nitel sınıfı: aynı sınıftaki silahlar yalnız sayıyla ayrışır.</summary>
        public static string PathClass(string path)
        {
            switch (path)
            {
                case "temas":
                case "durtme":
                case "saplama": return "yakin_itis";
                case "yay":
                case "agir_yay": return "yay";
                case "yere_vurus": return "dikey";
                case "balistik": return "ucan";
                case "isin": return "hat";
                case "yerlestirme": return "belirme";
                case "govde": return "govde";
                case "ok": return "ok";
                case "sayfa": return "sayfa";
                case "kure": return "kure";
                default: return path;
            }
        }

        static string BornAtFor(string path)
        {
            switch (path)
            {
                case "temas": return "dokunus";
                case "saplama": return "onunde_hat";
                case "yay":
                case "agir_yay": return "onunde_yay";
                case "yere_vurus":
                case "balistik":
                case "yerlestirme": return "hedef_noktada";
                case "isin": return "hat";
                case "govde": return "sende";
                case "ok":
                case "sayfa":
                case "kure": return "hedef_noktada";
                default: return "onunde";
            }
        }

        void ApplyWeapon(MechanicPlan p, MechanicWeapon w)
        {
            MechanicBody b = p.Body;
            b.Path = w.Path;
            p.Compatible = w.CompatibleVerbs.Contains(p.Verb);
            bool ranged = IsRangedPath(w.Path);
            b.ReachM = (ranged ? _r.Param("ranged_range_m") : _r.Param("melee_reach_m")) * w.RangeMult;

            bool movesSelf = p.Effects.Any(e => e.Stat == "kendini_tasi");
            b.BornAt = movesSelf ? "sende" : BornAtFor(w.Path);
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
                    b.SpeedMps = _r.Param("ballistic_speed_mps");
                    break;
                case "isin":
                    b.Contact = "tik";
                    b.LifeSec = Math.Max(b.LifeSec, _r.Param("beam_channel_sec") * (p.Compatible ? 1.3 : 1.0));
                    break;
                case "govde":
                    b.Attached = true;
                    b.LifeSec = Math.Max(b.LifeSec, 1.0);
                    break;
                case "ok":
                case "sayfa":
                    b.SpeedMps = _r.Param("ballistic_speed_mps");
                    break;
            }

            foreach (MechanicEffect e in p.Effects)
            {
                if (e.Atom == "deger" && e.Stat == "can" && e.Amount < 0) e.Amount *= w.DamageMult;
                if (e.Atom == "konum" && e.Stat == "kendini_tasi")
                {
                    e.Amount *= w.RangeMult;
                    e.Modes.Add("yol:" + SelfMoveByPath[w.Path]);
                }
                if (e.Stat == "aktor_yarat" || e.Stat == "yansit") e.Modes.Add("silahla:" + PathClass(w.Path));
                // Kendine yansıtma gövdenin doğduğu yerde yaşar (mechanic_grammar.self_effect_location).
                if (e.Stat == "yansit" && e.Target == "kendin" && b.BornAt != "sende")
                {
                    e.Target = b.BornAt == "dokunus" ? "dost" : "alan";
                    e.Modes.Add(b.BornAt == "dokunus" ? "dokunulana" : "dunyada");
                }
            }

            if (p.Compatible)
            {
                ApplyWeaponIdentity(p, w);
            }
            else
            {
                double mult = _r.IncompatibleEffectMult;
                foreach (MechanicEffect e in p.Effects.Where(e => e.Atom == "deger")) e.Amount *= mult;
                b.CastTimeMult = _r.IncompatibleCastTimeMult;
                b.Traits.Add("uyumsuz");
            }
            p.Trace.Add($"silah: {w.Name} yol={w.Path} menzil={b.ReachM:0.#}m doğar={b.BornAt}{(p.Compatible ? " +kimlik" : " uyumsuz")}");
        }

        /// <summary>weapon_skill_interaction tier C: yalnız uyumlu fiilde.</summary>
        void ApplyWeaponIdentity(MechanicPlan p, MechanicWeapon w)
        {
            MechanicBody b = p.Body;
            bool harms = p.Effects.Any(e => e.Atom == "deger" && e.Target == "dusman");
            switch (w.Path)
            {
                // Sırt çarpanı sirt_vurusu pasifindedir. İkinci bir arkadan etiketi yazılmaz.
                case "temas":
                case "durtme":
                    break;
                case "ok": b.Traits.Add("kosu_kritigi"); break;
                case "sayfa": b.Traits.Add("dolu_sayfa"); break;
                case "kure": b.Traits.Add("capraz_ates"); break;
                case "saplama":
                    foreach (MechanicEffect e in p.Effects.Where(e => e.Atom == "deger" && e.Stat == "can" && e.Amount < 0))
                        e.Modes.Add("zirh_delen");
                    break;
                case "yay": b.SizeM *= 1.2; break;
                case "agir_yay": b.Traits.Add("poise_x1.5"); break;
                case "yere_vurus":
                    if (harms)
                    {
                        double stun = _r.WeaponPassiveNum(w.Id, "stun_sec", 0);
                        if (stun > 0)
                            Add(p, "hiz", "hareket", "dusman", 0, stun, "sersem");
                    }
                    break;
                case "balistik": b.Traits.Add("sabitken_+50"); break;
                case "yerlestirme":
                    double power = _r.WeaponPassiveNum(w.Id, "power_mult", 1);
                    foreach (MechanicEffect e in p.Effects.Where(e => e.Atom == "deger" && e.Amount > 0))
                        e.Amount *= power;
                    break;
                case "govde": b.Traits.Add("blok_sonrasi_karsi_+20"); break;
            }
        }

        // ---------------------------------------------------------------- 3. sıfat (hitbox NASIL)

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
                case "surekli": Surekli(p); break;
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
                        if (e.Stat == "tempo") e.Amount = Math.Max(0.05, 1 - (1 - e.Amount) * _r.Param("dense_rate_mult"));
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

        void Surekli(MechanicPlan p)
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

        // ---------------------------------------------------------------- 4. çakışma

        static void ResolveConflicts(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            if (b.Anchored && b.Attached) { b.Attached = false; p.Conflicts.Add("çapalı > gövdeye bağlı: yerinde bırakıldı"); }
            if (b.Anchored && b.Homing) { b.Homing = false; p.Conflicts.Add("çapalı > güdüm: güdüm iptal"); }
            if (b.Anchored && b.Path == "balistik") { b.Traits.Add("mayin"); p.Conflicts.Add("çapalı + balistik = indiği yerde mayın/engel"); }
            if (b.Anchored && b.Path == "isin") { b.Traits.Add("cit"); p.Conflicts.Add("çapalı + ışın = sabit hat (çit)"); }
            if (b.Cloud && b.Link) { b.Shape = "sis_koridoru"; p.Conflicts.Add("bulut + bağ = sis koridoru"); }
            if (b.Continuous && b.Path == "balistik") { b.Traits.Add("inen_akis_alani"); p.Conflicts.Add("sürekli + balistik = indiği yerde akan alan"); }
            if (b.Continuous && b.Path == "isin") p.Conflicts.Add("sürekli + ışın = uzun kanal");
        }

        // ---------------------------------------------------------------- D. çelişki

        static void FindContradictions(MechanicPlan p)
        {
            MechanicBody b = p.Body;
            foreach (MechanicEffect e in p.Effects)
            {
                bool friendlyTarget = e.Target == "dost" || e.Target == "kendin";
                if (e.Atom == "deger" && e.Amount > 0 && e.Target == "dusman") p.Contradictions.Add($"düşmana yarar ({e.Stat})");
                if (e.Atom == "deger" && e.Amount < 0 && friendlyTarget) p.Contradictions.Add($"dosta zarar ({e.Stat})");
                if (e.Atom == "hiz" && e.Stat == "hareket" && e.Target != "dusman") p.Contradictions.Add("dosta kök/yavaşlatma");
                if (e.Atom == "hiz" && e.Stat == "tempo" && e.Amount < 1 && e.Target != "dusman") p.Contradictions.Add("dosta tempo düşürme");
                if (e.Atom == "gorunurluk" && e.Stat == "kor" && e.Target != "dusman") p.Contradictions.Add("dostu körleştirme");
            }
            if (b.Anchored && b.Homing) p.Contradictions.Add("hem çapalı hem güdümlü");
            if (b.Anchored && b.Attached) p.Contradictions.Add("hem çapalı hem gövdeye bağlı");
            if (b.Traits.Contains("tasiyici") && !p.Effects.Any(e => e.Atom == "konum")) p.Contradictions.Add("taşıyıcı fiil hiçbir şeyi taşımıyor");
            if (b.Link && p.Effects.All(e => e.Target == "kendin" && e.Stat != "aktor_yarat")) p.Contradictions.Add("bağın ikinci ucu yok");
        }
    }
}
