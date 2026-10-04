using System;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    public sealed partial class MechanicGrammar
    {
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
