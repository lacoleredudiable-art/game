using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Işık işi (teşhis 5, [K] "LED gibi beyaz, canlı ışık", O-8.5/8.6/8.8): telefonda hep açılan
/// ön ayar B'nin fiili değerleri kaynaktan okunur (Unity gerekmez). Eski değerler: güneş 1,07,
/// mavi renk filtresi, vinyet 0,36, bloom eşik 1,3, mavi kenar ışığı.
/// </summary>
[TestFixture]
public class LookBrightLedTests
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

    static float[] Color3(string src, string name)
    {
        var m = Regex.Match(src, name + @"\s*=\s*new Color\(\s*([0-9.]+)f,\s*([0-9.]+)f,\s*([0-9.]+)f");
        Assert.That(m.Success, name + " bulunamadı");
        return new[] { F(m.Groups[1].Value), F(m.Groups[2].Value), F(m.Groups[3].Value) };
    }

    static string Arena() => Read("unity", "Assets", "Scripts", "Game", "Config", "Sections", "ArenaSettings.cs");
    static string Defaults() => Read("unity", "Assets", "Scripts", "Game", "Arena", "LookPresetsDefaults.cs");
    static string Presets() => Read("unity", "Assets", "Scripts", "Game", "Arena", "LookPresetController.Presets.cs");
    static string ProfileB() => Read("unity", "Assets", "Resources", "Look", "Look_B_Keskin.asset");

    static float Yaml(string yaml, string key)
    {
        var m = Regex.Match(yaml, key + @":\s*\n\s*m_OverrideState: 1\s*\n\s*m_Value: (-?[0-9.]+)");
        Assert.That(m.Success, key + " bulunamadı");
        return F(m.Groups[1].Value);
    }

    [Test]
    public void PresetB_SunIntensity_IsBrightAndDrivenByKeyLight()
    {
        float eff = Const(Arena(), "KeyLightIntensity") * Const(Defaults(), "SunIntensityPresetBMult");
        // Gamma'da 1,5-1,7 idi (teşhis 5 §4). Linear renk uzayına geçişle aynı ekran parlaklığı için
        // 5,4 civarına dönüştürüldü (LinearColorSpaceTests formülü).
        Assert.That(eff, Is.InRange(5.0f, 5.8f), "ana ışık fiili şiddeti (Linear, teşhis 5 §4 + Linear aşaması)");
    }

    [Test]
    public void PresetB_SunColor_IsPureWhite_NotHardcodedBlue()
    {
        var c = Color3(Arena(), "KeyLightColor");
        Assert.That(c, Is.EqualTo(new[] { 1f, 1f, 1f }));
        Assert.That(Presets(), Does.Not.Contain("new Color(0.92f, 0.95f, 1f)"), "B güneş rengi koda gömülü kalmasın");
    }

    [Test]
    public void PresetB_Ambient_IsBrighter()
    {
        Assert.That(Const(Defaults(), "SkyPresetBLerp"), Is.EqualTo(0f), "B ambiyanı koyulaştırmasın");
        Assert.That(Color3(Arena(), "AmbientSky")[0], Is.GreaterThanOrEqualTo(0.75f));
        Assert.That(Color3(Arena(), "AmbientEquator")[0], Is.GreaterThanOrEqualTo(0.62f));
        Assert.That(Color3(Arena(), "AmbientGround")[0], Is.GreaterThanOrEqualTo(0.42f));
    }

    [Test]
    public void PresetB_Shadows_NotBlack()
    {
        Assert.That(Const(Defaults(), "ShadowStrength"), Is.InRange(0.7f, 0.8f));
    }

    [Test]
    public void RimLight_BlueSecondSun_IsRemoved()
    {
        Assert.That(Const(Arena(), "RimLightIntensity"), Is.EqualTo(0f));
    }

    [Test]
    public void ProfileB_NoBlueVeil_LowVignette_LowBloomThreshold()
    {
        string y = ProfileB();
        Assert.That(Regex.IsMatch(y, @"colorFilter:\s*\n\s*m_OverrideState: 1\s*\n\s*m_Value: \{r: 1, g: 1, b: 1,"),
            Is.True, "mavi renk filtresi kalkmalı");
        Assert.That(Yaml(y, "temperature"), Is.EqualTo(0f), "beyaz dengesi soğuk kaymasın");
        Assert.That(Yaml(y, "contrast"), Is.InRange(5f, 15f));
        Assert.That(Yaml(y, "saturation"), Is.InRange(-8f, 0f));
        Assert.That(Yaml(y, "postExposure"), Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(Yaml(y, "intensity"), Is.LessThanOrEqualTo(0.6f).And.GreaterThanOrEqualTo(0.0f));
        Assert.That(Yaml(y, "threshold"), Is.InRange(0.9f, 1.0f));
        var vig = Regex.Match(y, @"Look_B_Keskin_Vignette[\s\S]*?intensity:\s*\n\s*m_OverrideState: 1\s*\n\s*m_Value: ([0-9.]+)");
        Assert.That(F(vig.Groups[1].Value), Is.InRange(0.15f, 0.2f));
    }
}
