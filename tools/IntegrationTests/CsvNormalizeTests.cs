using NUnit.Framework;
using SweepV2;
namespace IntegrationTests;

[TestFixture]
public sealed class CsvNormalizeTests
{
    const string SampleHeader =
        "kombo,isim,silah,gecti,notlar\n";

    [Test]
    public void Crlf_and_lf_input_same_normalized_hash()
    {
        string body = "1-11,\"test\",Kılıç,1,\"ortam mesajı\"\n";
        string crlf = SampleHeader.Replace("\n", "\r\n") + body.Replace("\n", "\r\n");
        string lf = SampleHeader + body;
        string nCrlf = CsvNormalize.NormalizeText(crlf);
        string nLf = CsvNormalize.NormalizeText(lf);
        Assert.That(nCrlf, Is.EqualTo(nLf));
        Assert.That(CsvNormalize.Sha256Hex(nCrlf), Is.EqualTo(CsvNormalize.Sha256Hex(nLf)));
        Assert.That(nCrlf, Does.Not.Contain("notlar"));
    }

    [Test]
    public void Normalize_strips_notes_column_only()
    {
        string csv = "kombo,silah,gecti,notlar\n2-9,Yay,1,diag\n";
        string norm = CsvNormalize.NormalizeText(csv);
        Assert.That(norm, Is.EqualTo("kombo,silah,gecti\n2-9,Yay,1\n"));
    }

    [Test]
    public void Sha256_hex_is_uppercase_64()
    {
        string hash = CsvNormalize.Sha256Hex("x\n");
        Assert.That(hash.Length, Is.EqualTo(64));
        Assert.That(hash, Does.Match("^[0-9A-F]+$"));
    }
}
