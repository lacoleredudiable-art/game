using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class ShimSurfaceTests
{
    const int MaxShimFileCount = 18;
    const int MaxShimTotalLines = 6333;

    static string ShimDir =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "SweepV2", "Shim"));

    [Test]
    public void SweepV2_shim_stays_within_file_count_and_line_ceiling()
    {
        Assert.That(Directory.Exists(ShimDir), Is.True, ShimDir);
        string[] files = Directory.GetFiles(ShimDir, "*.cs");
        Assert.That(files.Length, Is.LessThanOrEqualTo(MaxShimFileCount),
            () => $"shim file count {files.Length} (max {MaxShimFileCount})");
        int totalLines = files.Sum(f => File.ReadAllLines(f).Length);
        Assert.That(totalLines, Is.LessThanOrEqualTo(MaxShimTotalLines),
            () => $"shim total lines {totalLines} (max {MaxShimTotalLines})");
    }
}
