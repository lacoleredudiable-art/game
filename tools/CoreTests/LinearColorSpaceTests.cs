using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Linear renk uzayı aşaması (Mustafa kararı Q18=B): proje ayarı ve Gamma'dan dönüştürülen ışık sayıları
/// kaynaktan okunur (Unity gerekmez). Görsel doğrulama telefonda ve editörde yapılır.
/// </summary>
[TestFixture]
public class LinearColorSpaceTests
{
    static string Root() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    static string Read(params string[] rel) => File.ReadAllText(Path.Combine(Root(), Path.Combine(rel)));

    static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    static float Const(string src, string name)
    {
        var m = Regex.Match(src, name + @"\s*=\s*(-?[0-9.]+)f");
        Assert.That(m.Success, name + " bulunamadı");
        return F(m.Groups[1].Value);
    }

    static string ProjectSettings() => Read("unity", "ProjectSettings", "ProjectSettings.asset");

    [Test]
    public void ProjectUsesLinearColorSpace()
    {
        Assert.That(ProjectSettings(), Does.Match(@"m_ActiveColorSpace:\s*1\b"));
    }

    [Test]
    public void AndroidGraphicsApis_AreVulkanAndGles3_Only()
    {
        // m_APIs: her API 4 bayt (little-endian): 21 = Vulkan (0x15), 11 = OpenGLES3 (0x0b); GLES2 = 8.
        var m = Regex.Match(ProjectSettings(),
            @"m_BuildTarget: AndroidPlayer\s*\n\s*m_APIs: ([0-9a-f]+)\s*\n\s*m_Automatic: (\d)");
        Assert.That(m.Success, "Android grafik API listesi yazılı olmalı");
        Assert.That(m.Groups[1].Value, Is.EqualTo("150000000b000000"), "Vulkan, sonra GLES3; GLES2 yok");
        Assert.That(m.Groups[2].Value, Is.EqualTo("0"), "elle seçilmiş liste (Auto kapalı)");
    }

    [Test]
    public void KeyLightIntensity_MatchesGammaDesignValue_ConvertedToLinear()
    {
        const float gammaIntensity = 1.6f;      // Linear öncesi (LED beyaz kararı, teşhis 5)
        const float skyAmbient = 0.78f;         // ArenaSettings.AmbientSky ortalaması
        double nl = Math.Sin(52.0 * Math.PI / 180.0); // KeyLightEuler.x = 52°, yukarı bakan zemin
        double expected = (Math.Pow(skyAmbient + gammaIntensity * nl, 2.2) - Math.Pow(skyAmbient, 2.2)) / nl;

        var arena = Read("unity", "Assets", "Scripts", "Game", "Config", "Sections", "ArenaSettings.cs");
        float actual = Const(arena, "KeyLightIntensity");

        Assert.That(actual, Is.EqualTo((float)expected).Within(0.15f), "Linear ana ışık şiddeti formülle uyuşmalı");
    }

    [Test]
    public void LavaPointLight_IsGammaPeakMatched()
    {
        var defaults = Read("unity", "Assets", "Scripts", "Game", "Arena", "LavaDecorDefaults.cs");
        Assert.That(Const(defaults, "PointLightIntensity"), Is.EqualTo((float)Math.Pow(2.4, 2.2)).Within(0.1f));
    }
}
