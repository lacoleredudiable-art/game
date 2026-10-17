using Dovus.Core.Grammar;

namespace CoreTests;

/// <summary>Dilim motor yükleyici — portal/takım etiket testleri için.</summary>
partial class SkillMechanicTagTests
{
    public static SkillMotor LoadMotorPublic() =>
        SkillMotor.FromJson(SliceGrammarTestPaths.ReadSliceGrammarJson());
}
