using System.Globalization;
using System.Text;
using AtomSim;

string root = FindRepoRoot();
var data = Data.Load(
    Path.Combine(root, "docs", "element-sistemi.json"),
    Path.Combine(root, "docs", "atom-grammar-taslak.json"));
var grammar = new Grammar(data);

var baselines = new Dictionary<(int v, int w), Plan>();
var plans = new List<Plan>();
foreach (Weapon w in data.Weapons)
    for (int v = 1; v <= 12; v++)
    {
        baselines[(v, w.Id)] = grammar.Compose(v, 0, w);
        for (int s = 1; s <= 12; s++) plans.Add(grammar.Compose(v, s, w));
    }

// A: sıfat nitel imzayı değiştirmeli; A2: sıfat en az bir ETKİ atomunu değiştirmeli (yalnız gövde değil)
static string EffectSig(Plan p) => string.Join(" ; ", p.Effects.Select(e => e.Signature()).OrderBy(x => x, StringComparer.Ordinal));
var failA = plans.Where(p => p.QualSignature() == baselines[(p.Verb, p.Weapon)].QualSignature()).ToList();
var failA2 = plans.Where(p => EffectSig(p) == EffectSig(baselines[(p.Verb, p.Weapon)])).ToList();

// B: aynı silahta aynı nitel imza
var collisions = plans.GroupBy(p => (p.Weapon, Sig: p.QualSignature()))
    .Where(g => g.Count() > 1).ToList();

// B2: silahtan bağımsız davranış çekirdeği (yol hariç) aynı olan skill çiftleri
static string CoreSig(Plan p) => p.QualSignature().Replace("yol=" + p.Body.Path, "");
var coreCollisions = plans.Where(p => p.Weapon == 4).GroupBy(CoreSig).Where(g => g.Count() > 1).ToList();

// C: her skill 10 silahta kaç farklı imza (tam yol) / kaç farklı yol sınıfı imzası
var perSkill = plans.GroupBy(p => (p.Verb, p.Adjective)).Select(g => new
{
    g.Key.Verb,
    g.Key.Adjective,
    Full = g.Select(p => p.QualSignature()).Distinct().Count(),
    Class = g.Select(p => p.QualSignature().Replace("yol=" + p.Body.Path, "yol=" + Grammar.PathClass(p.Body.Path))).Distinct().Count(),
    Core = g.Select(CoreSig).Distinct().Count()
}).ToList();

// C2: iki silah kaç skill'de yol adı dışında birebir aynı davranıyor
var pairSame = new List<(string a, string b, int n)>();
foreach (Weapon wa in data.Weapons)
    foreach (Weapon wb in data.Weapons.Where(x => x.Id > wa.Id))
    {
        int n = 0;
        for (int v = 1; v <= 12; v++)
            for (int s = 1; s <= 12; s++)
                if (CoreSig(plans.First(p => p.Verb == v && p.Adjective == s && p.Weapon == wa.Id))
                    == CoreSig(plans.First(p => p.Verb == v && p.Adjective == s && p.Weapon == wb.Id)))
                    n++;
        pairSame.Add((wa.Name, wb.Name, n));
    }

var contradictions = plans.Where(p => p.Contradictions.Count > 0).ToList();
var plain = plans.Where(p => p.Labels.All(Labeler.Generic.Contains)).ToList();

string outDir = Path.Combine(root, "tools", "AtomSim", "out");
Directory.CreateDirectory(outDir);
WriteCsv(Path.Combine(outDir, "skills.csv"), plans);
WriteReport(Path.Combine(outDir, "rapor.md"));

Console.WriteLine($"plan: {plans.Count} (+{baselines.Count} sıfatsız taban)");
Console.WriteLine($"A  sıfat nitel imzayı değiştirmiyor: {failA.Count}");
Console.WriteLine($"A2 sıfat hiçbir etki atomunu değiştirmiyor (yalnız gövde): {failA2.Count}");
Console.WriteLine($"B  aynı silahta aynı imza grubu: {collisions.Count}");
Console.WriteLine($"B2 Kılıç'ta yol hariç aynı çekirdek grubu: {coreCollisions.Count}");
Console.WriteLine($"C  10 silahta ort. farklı imza: {perSkill.Average(x => x.Full):0.00} (yol sınıfıyla {perSkill.Average(x => x.Class):0.00}, yol adı hariç davranış {perSkill.Average(x => x.Core):0.00})");
foreach (var x in pairSame.OrderByDescending(x => x.n).Take(5))
    Console.WriteLine($"   {x.a} = {x.b}: {x.n}/144 skill'de aynı davranış");
Console.WriteLine($"D  çelişkili plan: {contradictions.Count}");
Console.WriteLine($"   özel etiketsiz (yalnız genel desen) plan: {plain.Count}");
Console.WriteLine($"rapor: {Path.Combine(outDir, "rapor.md")}");

void WriteCsv(string path, List<Plan> all)
{
    var sb = new StringBuilder();
    sb.AppendLine("skill;fiil;sifat;silah;uyumlu;etiketler;aciklama;catisma;celiski;imza");
    foreach (Plan p in all)
        sb.AppendLine(string.Join(";", new[]
        {
            p.SkillId, p.VerbName, p.AdjectiveName, p.WeaponName, p.Compatible ? "evet" : "hayir",
            string.Join(", ", p.Labels), p.Description, string.Join(" / ", p.Conflicts),
            string.Join(" / ", p.Contradictions), p.QualSignature()
        }.Select(Csv)));
    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
}

static string Csv(string s) => s.Contains(';') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

void WriteReport(string path)
{
    var sb = new StringBuilder();
    sb.AppendLine("# AtomSim raporu — 1440 skill");
    sb.AppendLine();
    sb.AppendLine("Kurallar: `docs/atom-grammar-taslak.json` (TASLAK). Sayılar: `docs/element-sistemi.json`.");
    sb.AppendLine();
    sb.AppendLine("## Özet");
    sb.AppendLine();
    sb.AppendLine($"- Plan: {plans.Count} (12 fiil × 12 sıfat × 10 silah) + {baselines.Count} sıfatsız taban");
    sb.AppendLine($"- A — sıfat nitel imzayı değiştirmiyor: **{failA.Count}**");
    sb.AppendLine($"- A2 — sıfat hiçbir etki atomunu değiştirmiyor (yalnız gövde): **{failA2.Count}**");
    sb.AppendLine($"- B — aynı silahta aynı imzayı veren grup: **{collisions.Count}**");
    sb.AppendLine($"- B2 — Kılıç'ta yol hariç aynı çekirdek: **{coreCollisions.Count}**");
    sb.AppendLine($"- C — bir skill 10 silahta ort. **{perSkill.Average(x => x.Full):0.00}** farklı imza; yol sınıfına indirgeyince **{perSkill.Average(x => x.Class):0.00}** (7 sınıf: yakın itiş, yay, dikey, uçan, hat, belirme, gövde)");
    sb.AppendLine($"- C (dürüst) — yol adı imzadan çıkarılınca bir skill 10 silahta ort. **{perSkill.Average(x => x.Core):0.00}** farklı davranış");
    sb.AppendLine($"- D — çelişkili plan: **{contradictions.Count}**");
    sb.AppendLine();
    sb.AppendLine("### Silah çiftleri: yol adı dışında aynı davranan skill sayısı (144 üzerinden)");
    sb.AppendLine();
    foreach (var x in pairSame.OrderByDescending(x => x.n).Take(10))
        sb.AppendLine($"- {x.a} = {x.b}: {x.n}");
    sb.AppendLine();
    sb.AppendLine($"- Özel mekanik etiketi olmayan (yalnız Delici/Seken/Dalga gibi genel desen): **{plain.Count}**");
    sb.AppendLine();

    sb.AppendLine("## Etiket sıklığı (1440 içinde)");
    sb.AppendLine();
    sb.AppendLine("| Etiket | Adet |");
    sb.AppendLine("|---|---|");
    foreach (var g in plans.SelectMany(p => p.Labels).GroupBy(x => x).OrderByDescending(g => g.Count()))
        sb.AppendLine($"| {g.Key} | {g.Count()} |");
    sb.AppendLine();

    foreach (int wid in new[] { 4, 7 })
    {
        Weapon w = data.Weapons.First(x => x.Id == wid);
        sb.AppendLine($"## 144 skill — {w.Name} ({w.Path})");
        sb.AppendLine();
        for (int v = 1; v <= 12; v++)
        {
            Plan b0 = baselines[(v, wid)];
            sb.AppendLine($"### {v} {data.VerbNames[v]}");
            sb.AppendLine();
            sb.AppendLine($"- *sıfatsız*: {b0.Description}");
            foreach (Plan p in plans.Where(p => p.Weapon == wid && p.Verb == v).OrderBy(p => p.Adjective))
            {
                string flag = p.Contradictions.Count > 0 ? " ⚠ " + string.Join(", ", p.Contradictions) : "";
                sb.AppendLine($"- **{p.SkillId} {p.AdjectiveName} {p.VerbName}** — _{string.Join(", ", p.Labels)}_ — {p.Description}{flag}");
            }
            sb.AppendLine();
        }
    }

    sb.AppendLine("## Aynı skill, 10 silah");
    sb.AppendLine();
    foreach ((int v, int s) in new[] { (3, 10), (4, 4), (12, 12), (2, 9), (1, 3), (11, 11) })
    {
        sb.AppendLine($"### {v}-{s} {data.AdjectiveNames[s]} {data.VerbNames[v]}");
        sb.AppendLine();
        foreach (Plan p in plans.Where(p => p.Verb == v && p.Adjective == s))
            sb.AppendLine($"- {p.WeaponName}{(p.Compatible ? " ✓" : "")}: _{string.Join(", ", p.Labels)}_ — {p.Description}");
        sb.AppendLine();
    }

    sb.AppendLine("## A — sıfatın nitel etkisi olmayanlar");
    sb.AppendLine();
    foreach (Plan p in failA) sb.AppendLine($"- {p.SkillId} {p.AdjectiveName} {p.VerbName} / {p.WeaponName}");
    sb.AppendLine();
    sb.AppendLine("## A2 — sıfat yalnız gövdeyi değiştiriyor (skill id, silah fark etmeksizin)");
    sb.AppendLine();
    foreach (var g in failA2.GroupBy(p => p.SkillId))
        sb.AppendLine($"- {g.Key} {g.First().AdjectiveName} {g.First().VerbName}: {g.Count()} silahta");
    sb.AppendLine();
    sb.AppendLine("## B2 — Kılıç'ta yol hariç aynı davranış çekirdeği");
    sb.AppendLine();
    foreach (var g in coreCollisions)
        sb.AppendLine("- " + string.Join(" = ", g.Select(p => $"{p.SkillId} {p.AdjectiveName} {p.VerbName}")));
    sb.AppendLine();
    sb.AppendLine("## C — silahla en az ayrışan skill'ler (yol sınıfı imzası)");
    sb.AppendLine();
    foreach (var x in perSkill.OrderBy(x => x.Core).Take(15))
        sb.AppendLine($"- {x.Verb}-{x.Adjective} {data.AdjectiveNames[x.Adjective]} {data.VerbNames[x.Verb]}: {x.Core} davranış / {x.Class} sınıf / {x.Full} tam");
    sb.AppendLine();
    sb.AppendLine("## D — çelişkiler");
    sb.AppendLine();
    foreach (var g in contradictions.GroupBy(p => p.SkillId))
        sb.AppendLine($"- {g.Key} {g.First().AdjectiveName} {g.First().VerbName} ({g.Count()} silah): {string.Join(", ", g.SelectMany(p => p.Contradictions).Distinct())}");
    sb.AppendLine();
    sb.AppendLine("## Özel etiketsiz planlar (skill id → silah sayısı)");
    sb.AppendLine();
    foreach (var g in plain.GroupBy(p => p.SkillId).OrderBy(g => g.Key))
        sb.AppendLine($"- {g.Key} {g.First().AdjectiveName} {g.First().VerbName}: {g.Count()} silah — _{string.Join(", ", g.First().Labels)}_");

    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
}

static string FindRepoRoot()
{
    string? dir = Directory.GetCurrentDirectory();
    while (dir != null && !File.Exists(Path.Combine(dir, "docs", "element-sistemi.json")))
        dir = Path.GetDirectoryName(dir);
    return dir ?? throw new InvalidOperationException("docs/element-sistemi.json bulunamadı");
}
