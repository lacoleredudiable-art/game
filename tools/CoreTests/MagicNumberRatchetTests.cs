using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// PLAN 2B.11b (A17): oynanış/ayar sayıları kodda gömülü durmaz → <c>*Defaults</c> sabit sınıfları, Core/Tuning, Game/Config (tuning/JSON).
/// Sayaç: Core/App/Game altında yorum ve string dışındaki önemsiz OLMAYAN sayı literal'leri. Muaf: önemsiz değerler (0–16 tamsayı, 0f/1f/2f/0.5f,
/// 10/60/90/100/180/255/360/1000, 1e-3…1e-6 epsilon'lar), const/enum/attribute/case satırları, dizi indeksleri, renkler, UI yerleşim satırları
/// ve ayar kaynağı dosyaları (<c>*Defaults.cs</c>, Core/Tuning, Game/Config, HudTheme, ProceduralChunkMesh, Editor/DevTools/Tests).
/// Ayrıntı ve ölçüm: docs/constants-report.md. Tavanlar yalnız AŞAĞI iner; yeni gömülü sayı eklemek yerine ilgili *Defaults sınıfına const ekle.
/// </summary>
[TestFixture]
public sealed class MagicNumberRatchetTests
{
    // Alan → tavan. 2B.11 öncesi toplam 1615; #114 sonrası 1499.
    static readonly Dictionary<string, int> Ceilings = new()
    {
        ["Core"] = 0,
        ["App"] = 0,
        ["Game/Skills"] = 0,
        ["Game/Vfx"] = 135,
        ["Game/Weapons"] = 127,
        ["Game/Arena"] = 121,
        ["Game/Casting"] = 109,
        ["Game/Actors"] = 84,
        ["Game/Hud"] = 77,
        ["Game/Audio"] = 72,
        ["Game/Boss"] = 69,
        ["Game/Cameras"] = 37,
        ["Game/Composition"] = 35,
        ["Game/Feel"] = 12,
        ["Game/Team"] = 0,
        ["Game/Platform"] = 4,
        ["Game/Data"] = 0,
    };

    static readonly HashSet<string> Trivial = BuildTrivial();

    static HashSet<string> BuildTrivial()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var values = Enumerable.Range(0, 17).Select(i => i.ToString()).Concat(new[]
        {
            "0", "1", "2", "0.5", "10", "100", "1000", "60", "90", "180", "360", "255", "0.0", "1.0", "2.0",
            "1e-4", "1e-3", "1e-5", "1e-6", "0.001", "0.0001", "0.00001", "1E-4", "1E-3", "1E-5", "1E-6",
        });
        foreach (string v in values)
        foreach (string s in new[] { "", "f", "F", "d", "D" })
        {
            bool smallInt = int.TryParse(v, out int n) && n >= 3 && n <= 16 && v.All(char.IsDigit);
            if (smallInt && s.Length > 0)
                continue;
            set.Add(v + s);
        }
        return set;
    }

    static readonly Regex Num = new(
        @"(?<![\w.])(\d+\.\d+(?:[eE][-+]?\d+)?[fFdDmM]?|\d+(?:[eE][-+]?\d+)[fFdD]?|\d+[fFdDmMuUlL]?)(?![\w.])", RegexOptions.Compiled);
    static readonly Regex CtxSkip = new(
        @"^\s*(\[|#|case\b|const\b|public const|internal const|private const|protected const)|\bconst\s+\w+\s+\w+\s*=", RegexOptions.Compiled);
    static readonly Regex EnumValue = new(@"^\s*\w+\s*=\s*-?\d+\s*,?\s*$", RegexOptions.Compiled);
    static readonly Regex Layout = new(
        @"\b(anchoredPosition|sizeDelta|anchorMin|anchorMax|offsetMin|offsetMax|fontSize|pivot|ReferenceResolution|RectOffset|padding|spacing|preferredWidth|preferredHeight|minWidth|minHeight|flexibleWidth|flexibleHeight|referencePixelsPerUnit|cellSize|SetSizeWithCurrentAnchors|characterSpacing|lineSpacing|sortingOrder|Place|CreateText|CreateButton|CreatePanel|CreateImage|SetAnchors|AddLabel|Label|Box|Button|GUILayout|GUI)\b",
        RegexOptions.Compiled);
    static readonly Regex ColorExpr = new(
        @"(new\s+Color(32)?\s*|\bColor(32)?\s+\w+\s*=\s*new\s*|\bColor(32)?\s*\.\s*\w+\s*\(|\bColor\s*\(|\bHSVToRGB\s*)\(([^()]|\([^()]*\))*\)", RegexOptions.Compiled);
    static readonly Regex RectExpr = new(@"new\s+Rect\s*\(([^()]|\([^()]*\))*\)", RegexOptions.Compiled);
    static readonly Regex Index = new(@"\[\s*\d+\s*\]", RegexOptions.Compiled);

    static string ScriptsRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "unity", "Assets", "Scripts"));

    static bool ExemptFile(string rel)
    {
        string p = "/" + rel;
        string b = Path.GetFileName(rel);
        return p.Contains("/Editor/") || p.Contains("/DevTools/") || p.Contains("/Tests/") || b.EndsWith("Defaults.cs", StringComparison.Ordinal)
               || p.Contains("/Core/Tuning/") || p.Contains("/Game/Config/") || b == "HudTheme.cs" || b == "ProceduralChunkMesh.cs";
    }

    static string Area(string rel)
    {
        string[] parts = rel.Split('/');
        if (parts[0] == "Game" && parts.Length > 2)
            return "Game/" + parts[1];
        return parts[0];
    }

    /// <summary>Yorum ve string/char literal'lerini boşlukla değiştirir (satır sonları korunur).</summary>
    internal static string StripCommentsAndStrings(string src)
    {
        var o = new StringBuilder(src.Length);
        int i = 0, n = src.Length;
        while (i < n)
        {
            char c = src[i];
            if (c == '/' && i + 1 < n && src[i + 1] == '/')
            {
                int j = src.IndexOf('\n', i);
                if (j < 0) j = n;
                o.Append(' ', j - i);
                i = j;
            }
            else if (c == '/' && i + 1 < n && src[i + 1] == '*')
            {
                int j = src.IndexOf("*/", i + 2, StringComparison.Ordinal);
                j = j < 0 ? n : j + 2;
                Blank(o, src, i, j);
                i = j;
            }
            else if (c == '"' || ((c == '@' || c == '$') && i + 1 < n && (src[i + 1] == '"' || src[i + 1] == '$' || src[i + 1] == '@')))
            {
                int j = i;
                bool verbatim = false;
                while (j < n && (src[j] == '@' || src[j] == '$'))
                {
                    verbatim |= src[j] == '@';
                    j++;
                }
                if (j >= n || src[j] != '"')
                {
                    o.Append(c);
                    i++;
                    continue;
                }
                int k = j + 1;
                while (k < n)
                {
                    if (src[k] == '\\' && !verbatim) { k += 2; continue; }
                    if (src[k] == '"')
                    {
                        if (verbatim && k + 1 < n && src[k + 1] == '"') { k += 2; continue; }
                        break;
                    }
                    k++;
                }
                int end = Math.Min(n, k + 1);
                Blank(o, src, i, end);
                i = end;
            }
            else if (c == '\'')
            {
                int k = i + 1;
                while (k < n && src[k] != '\'')
                    k += src[k] == '\\' ? 2 : 1;
                int end = Math.Min(n, k + 1);
                o.Append(' ', end - i);
                i = end;
            }
            else
            {
                o.Append(c);
                i++;
            }
        }
        return o.ToString();
    }

    static void Blank(StringBuilder o, string src, int from, int to)
    {
        for (int x = from; x < to; x++)
            o.Append(src[x] == '\n' ? '\n' : ' ');
    }

    static string BlankMatches(Regex re, string line) => re.Replace(line, m => new string(' ', m.Length));

    internal static Dictionary<string, List<string>> Scan()
    {
        string root = ScriptsRoot();
        var hits = new Dictionary<string, List<string>>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (ExemptFile(rel))
                continue;
            string[] lines = StripCommentsAndStrings(File.ReadAllText(file).Replace("\r\n", "\n")).Split('\n');
            for (int ln = 0; ln < lines.Length; ln++)
            {
                string line = lines[ln];
                if (CtxSkip.IsMatch(line) || EnumValue.IsMatch(line) || Layout.IsMatch(line))
                    continue;
                string l2 = BlankMatches(Index, BlankMatches(RectExpr, BlankMatches(ColorExpr, line)));
                foreach (Match m in Num.Matches(l2))
                {
                    if (Trivial.Contains(m.Groups[1].Value))
                        continue;
                    string area = Area(rel);
                    if (!hits.TryGetValue(area, out var list))
                        hits[area] = list = new List<string>();
                    list.Add($"{rel}:{ln + 1} {m.Groups[1].Value}");
                }
            }
        }
        return hits;
    }

    [Test]
    public void MagicNumbers_DoNotExceedRatchetCeiling()
    {
        var hits = Scan();
        var errors = new List<string>();
        foreach (var kv in hits)
        {
            int max = Ceilings.TryGetValue(kv.Key, out int c) ? c : 0;
            if (kv.Value.Count > max)
                errors.Add($"{kv.Key}: {kv.Value.Count} > tavan {max}. Örnekler: {string.Join(", ", kv.Value.Take(8))}");
        }
        Assert.That(errors, Is.Empty, () => "Gömülü oynanış sayısı arttı (ilgili *Defaults sınıfına const taşı):\n" + string.Join("\n", errors));
        TestContext.Out.WriteLine("MagicNumbers total=" + hits.Values.Sum(v => v.Count) + " "
                                  + string.Join(" ", hits.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value.Count)));
    }

    [Test]
    public void Ceilings_AreTight_SoTheRatchetOnlyMovesDown()
    {
        // Tavan gerçek sayıdan büyükse düşürülmeli (kazanım geri kaybolmasın).
        var hits = Scan();
        var loose = new List<string>();
        foreach (var kv in Ceilings)
        {
            int actual = hits.TryGetValue(kv.Key, out var l) ? l.Count : 0;
            if (kv.Value > actual)
                loose.Add($"{kv.Key}: tavan {kv.Value}, gerçek {actual} → tavanı {actual} yap");
        }
        Assert.That(loose, Is.Empty, () => string.Join("\n", loose));
    }

    [Test]
    public void Stripper_IgnoresNumbersInCommentsAndStrings()
    {
        string s = StripCommentsAndStrings("float a = 3.5f; // 7.25f\nvar t = \"9.75\"; /* 4.5f */ var c = '8';");
        Assert.That(s, Does.Contain("3.5f"));
        Assert.That(s, Does.Not.Contain("7.25f"));
        Assert.That(s, Does.Not.Contain("9.75"));
        Assert.That(s, Does.Not.Contain("4.5f"));
        Assert.That(s.Split('\n').Length, Is.EqualTo(2));
    }
}
