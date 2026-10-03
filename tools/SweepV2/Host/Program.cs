using Dovus.Core.Equipment;
using Dovus.Game.Editor;
using Dovus.Game.Skills;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SweepV2
{
    /// <summary>
    /// Sweep v2 başsız koşucu. Oyun + PlaySweep.cs değişmeden, UnityEngine yerine Shim ile koşar.
    ///   dotnet run --project tools/SweepV2 -c Release -- --all
    ///   dotnet run --project tools/SweepV2 -c Release -- --weapon Kılıç --case 1-11
    /// </summary>
    public static class Program
    {
        const float FrameSec = 1f / 60f;

        public static int Main(string[] args)
        {
            Options o;
            try
            {
                o = Options.Parse(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(Options.Usage);
                return 2;
            }
            if (o.Help)
            {
                Console.WriteLine(Options.Usage);
                return 0;
            }

            // Play sweep tr-TR editörde koştu (notlarda "0,16 sn"); çıktı ve metin karşılaştırmaları aynı kalsın.
            var culture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;

            string root = RepoRoot();
            string outDir = Path.GetFullPath(o.OutDir ?? Path.Combine(root, "tools", "SweepV2", "out"));
            Directory.CreateDirectory(outDir);

            Application.dataPath = Path.Combine(root, "unity", "Assets");
            Application.persistentDataPath = Path.Combine(Path.GetTempPath(), "dovus-sweep-v2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Application.persistentDataPath);
            Resources.Root = Path.Combine(root, "unity", "Assets", "Resources");

            int errors = 0;
            var errorSamples = new List<string>();
            Debug.Sink = (msg, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errors++;
                    if (errorSamples.Count < 20) errorSamples.Add($"[{type}] {msg}");
                    if (o.Verbose) Console.Error.WriteLine($"[{type}] {msg}");
                }
                else if (o.Verbose)
                {
                    Console.WriteLine($"[{type}] {msg}");
                }
            };

            var wall = Stopwatch.StartNew();
            RuntimeHelpers.RunClassConstructor(typeof(PlaySweep).TypeHandle);
            SceneSetup.Build(root);
            EditorApplication.EnterPlay();

            // Unity sahneyi yükleyip ilk Start/Update'leri koşturduktan sonra sweep başlar.
            for (int i = 0; i < 5; i++) World.Step(FrameSec);

            var md = UnityEngine.Object.FindAnyObjectByType<ManifestationDirector>();
            if (md == null)
            {
                Console.Error.WriteLine("ManifestationDirector kurulmadı. İlk hatalar:");
                foreach (string e in errorSamples) Console.Error.WriteLine("  " + e);
                return 3;
            }

            List<string> weaponNames = md.AvailableWeapons.Where(w => w != null && !string.IsNullOrEmpty(w.Name))
                .Select(w => w.Name).ToList();
            List<string> weapons;
            try
            {
                weapons = o.Weapons.Count == 0 ? weaponNames : o.Weapons.Select(w => ResolveWeapon(w, weaponNames)).ToList();
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                return 2;
            }

            var cases = new List<PlaySweepCase>();
            foreach (string w in weapons)
                cases.AddRange(PlaySweep.AllCombos(w, o.SecondWeapon ?? "", o.StartDistM));
            if (o.Cases.Count > 0)
                cases = cases.Where(c => o.Cases.Contains(c.Id)).ToList();
            foreach (PlaySweepCase c in cases)
            {
                c.Trace = o.Trace;
                if (o.Stick.HasValue)
                {
                    c.Stick = o.Stick.Value;
                    c.StickAtSec = o.StickAtSec;
                }
                c.PlayerShiftM = o.PlayerShiftM;
                c.PlayerShiftAtSec = o.PlayerShiftAtSec;
            }
            if (cases.Count == 0)
            {
                Console.Error.WriteLine("Seçimle eşleşen kombo yok.");
                return 2;
            }

            string label = o.Label ?? (o.All ? "headless-tum" : "headless-" + string.Join("-", weapons.Select(Fold)));
            SessionState.SetBool("Dovus.PlaySweep.SpeedSet", true);
            SessionState.SetFloat("Dovus.PlaySweep.Speed", o.Speed);
            PlaySweep.OutputDir = outDir;
            Console.WriteLine($"[SweepV2] {cases.Count} kombo, {weapons.Count} silah @ {o.Speed:0.#}x → {outDir}");

            PlaySweep.Start(cases, label, o.SecondWeapon ?? "");
            long frames = 0;
            long maxFrames = (long)cases.Count * 60 * 60 * 2;
            int lastReported = 0;
            var lap = Stopwatch.StartNew();
            while (PlaySweep.Running && frames < maxFrames)
            {
                World.Step(FrameSec);
                frames++;
                int done = PlaySweep.Results.Count;
                if (done != lastReported && (done % 48 == 0 || done == cases.Count) && !o.Quiet)
                {
                    lastReported = done;
                    int pass = PlaySweep.Results.Count(r => r.Pass);
                    Console.WriteLine($"  {done}/{cases.Count} ({pass} geçti) {lap.Elapsed.TotalSeconds:0}s");
                }
            }
            if (PlaySweep.Running)
            {
                PlaySweep.Stop("kare sınırı");
                Console.Error.WriteLine($"Kare sınırı aşıldı ({frames}); sonuçlar eksik.");
            }
            wall.Stop();

            List<PlaySweepResult> results = PlaySweep.Results.ToList();
            Console.WriteLine();
            Console.WriteLine($"[SweepV2] {PlaySweep.LastSummary}");
            Console.WriteLine($"[SweepV2] süre {wall.Elapsed.TotalSeconds:0.0} sn, {frames} kare, oyun logunda {errors} hata");

            string csvPath = Path.Combine(outDir, label.Replace('+', '-') + ".csv");
            var report = Report.Build(results, weapons);
            Console.WriteLine();
            Console.WriteLine(report.Table());

            Report.Agreement agreement = null;
            if (!string.IsNullOrEmpty(o.Compare))
            {
                agreement = Report.Compare(csvPath, o.Compare);
                Console.WriteLine();
                Console.WriteLine(agreement.Text());
            }

            string summaryPath = Path.Combine(outDir, label.Replace('+', '-') + "-ozet.md");
            File.WriteAllText(summaryPath, report.Markdown(wall.Elapsed, o.Speed, agreement));
            Console.WriteLine($"[SweepV2] yazıldı: {csvPath}, {summaryPath}");

            if (o.Gate)
            {
                List<string> fails = report.GateFailures(o.All && o.Cases.Count == 0);
                if (fails.Count > 0)
                {
                    Console.Error.WriteLine("KAPI KALDI:");
                    foreach (string f in fails) Console.Error.WriteLine("  " + f);
                    return 1;
                }
                Console.WriteLine("KAPI GEÇTİ");
            }
            return 0;
        }

        static string RepoRoot()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                {
                    if (Directory.Exists(Path.Combine(d.FullName, "unity", "Assets", "Scripts", "Core")))
                        return d.FullName;
                }
            }
            throw new DirectoryNotFoundException("Repo kökü (unity/Assets/Scripts/Core) bulunamadı.");
        }

        internal static string Fold(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch in s.ToLowerInvariant())
            {
                char c = ch switch
                {
                    'ı' => 'i', 'i' => 'i', 'ç' => 'c', 'ş' => 's', 'ğ' => 'g', 'ü' => 'u', 'ö' => 'o', 'â' => 'a', 'î' => 'i', 'û' => 'u',
                    _ => ch,
                };
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString().Replace("i̇", "i");
        }

        static string ResolveWeapon(string input, List<string> names)
        {
            string f = Fold(input);
            if (f == "kitap") f = "buyukitabi";
            string hit = names.FirstOrDefault(n => Fold(n) == f)
                         ?? names.FirstOrDefault(n => Fold(n).StartsWith(f, StringComparison.Ordinal));
            if (hit == null)
                throw new ArgumentException($"Silah bulunamadı: {input}. Seçenekler: {string.Join(", ", names)}");
            return hit;
        }
    }

    sealed class Options
    {
        public bool All;
        public bool Help;
        public bool Gate;
        public bool Quiet;
        public bool Verbose;
        public bool Trace;
        public float Speed = Dovus.Core.Motion.SweepPace.DefaultSpeed;
        public float StartDistM = 3f;
        public string OutDir;
        public string Label;
        public string Compare;
        public string SecondWeapon;
        public Vector2? Stick;
        public float StickAtSec = 0.5f;
        public float PlayerShiftM;
        public float PlayerShiftAtSec = 0.2f;
        public readonly List<string> Weapons = new();
        public readonly HashSet<string> Cases = new();

        public const string Usage =
            "Sweep v2 başsız (Unity'siz) Play taraması\n" +
            "  --all                 10 silah × 144 kombo (1440)\n" +
            "  --weapon X            silah (Kılıç, kilic, Yay, kitap...); virgülle birden çok\n" +
            "  --case R1-R2          kombo (fiil-sıfat), virgülle birden çok: --case 1-11,2-9\n" +
            "  --speed N             Play hız çarpanı (varsayılan 4, Play sweep ile aynı)\n" +
            "  --out DIR             çıktı klasörü (varsayılan tools/SweepV2/out)\n" +
            "  --label NAME          dosya adı (varsayılan headless-tum / headless-<silah>)\n" +
            "  --compare CSV         Play CSV ile kombo kombo karşılaştır (ör. docs/play-sweep/pr35-final-4x.csv)\n" +
            "  --gate                CI kapısı: silah başına ≥142/144, gövdeye giren yok, yerde hatası ≤3; muaf kombo yok\n" +
            "  --stick X,Y --stick-at T   kalıp T sn'ye gelince oyuncu çubuğa basar (hareket testi)\n" +
            "  --player-shift M --player-shift-at T   kalıp T sn'de oyuncu boss'a doğru M m taşınır (1-11 yankı testi)\n" +
            "  --trace               her kombo için kare izi detay dosyasına\n" +
            "  --verbose / --quiet";

        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException(a + " değer ister");
                switch (a)
                {
                    case "--all": o.All = true; break;
                    case "-h":
                    case "--help": o.Help = true; break;
                    case "--gate": o.Gate = true; break;
                    case "--quiet": o.Quiet = true; break;
                    case "--verbose": o.Verbose = true; break;
                    case "--trace": o.Trace = true; break;
                    case "--weapon":
                        o.Weapons.AddRange(Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        break;
                    case "--case":
                        foreach (string c in Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            string[] p = c.Split('-');
                            if (p.Length != 2 || !int.TryParse(p[0], out int v) || !int.TryParse(p[1], out int j)
                                || v < 1 || v > 12 || j < 1 || j > 12)
                                throw new ArgumentException("--case biçimi R1-R2 (1..12): " + c);
                            o.Cases.Add(v + "-" + j);
                        }
                        break;
                    case "--speed": o.Speed = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--dist": o.StartDistM = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--out": o.OutDir = Next(); break;
                    case "--label": o.Label = Next(); break;
                    case "--compare": o.Compare = Next(); break;
                    case "--second": o.SecondWeapon = Next(); break;
                    case "--stick":
                    {
                        string[] p = Next().Split(',');
                        o.Stick = new Vector2(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture));
                        break;
                    }
                    case "--stick-at": o.StickAtSec = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--player-shift": o.PlayerShiftM = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--player-shift-at": o.PlayerShiftAtSec = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    default: throw new ArgumentException("Bilinmeyen argüman: " + a);
                }
            }
            if (!o.All && o.Weapons.Count == 0 && o.Cases.Count == 0 && !o.Help)
                throw new ArgumentException("--all, --weapon veya --case gerekli.");
            return o;
        }
    }
}
