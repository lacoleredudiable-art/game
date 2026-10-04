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
    public sealed partial class MechanicGrammar
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
            p.Body.LifeSec = hb.Timed ? Math.Max(0.5, longest) : MechanicDefaults.MinTimedBodyLifeSec;
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
                    b.LifeSec = Math.Max(b.LifeSec, _r.Param("beam_channel_sec") * (p.Compatible ? MechanicDefaults.BeamChannelLifeMult : 1.0));
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

    }
}
