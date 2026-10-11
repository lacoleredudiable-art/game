using Dovus.Game.Config;
using Dovus.Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Dovus.Tests.EditMode
{
    /// <summary>
    /// v4 dilim APK'sı: kural_motoru_v4 bayrakları yalnız -dovusV4Slice ile eklenen define'la açılır;
    /// define yokken (repo, editör, normal APK) varsayılan kapalı kalır.
    /// </summary>
    public sealed class RuleEngineV4SliceBuildEditModeTests
    {
        [Test]
        public void NoDefine_FlagsStayOff()
        {
            Assert.That(RuleEngineV4Settings.IsSliceBuild, Is.False, $"editörde {RuleEngineV4Settings.SliceBuildDefine} tanımlı");
            var settings = new GameTuning().RuleEngineV4;
            Assert.That(settings.Enabled, Is.False, "varsayılan Enabled");
            Assert.That(settings.SliceScene, Is.False, "varsayılan SliceScene");
            settings.ApplySliceBuild();
            Assert.That(settings.Enabled, Is.False, "define yokken ApplySliceBuild Enabled açtı");
            Assert.That(settings.SliceScene, Is.False, "define yokken ApplySliceBuild SliceScene açtı");
        }

        [Test]
        public void ProjectDefines_DoNotContainSliceDefine()
        {
            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone })
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                Assert.That(defines.Split(';'), Has.No.Member(RuleEngineV4Settings.SliceBuildDefine), target.TargetName);
            }
        }

        [Test]
        public void V4SliceArg_AddsDefine_OnlyWhenGiven()
        {
            string[] withArg = { "Unity.exe", "-batchmode", "-dovusRelease", AndroidBuilder.V4SliceArg };
            string[] withoutArg = { "Unity.exe", "-batchmode", "-dovusRelease" };
            Assert.That(AndroidBuilder.WantsV4Slice(withArg), Is.True);
            Assert.That(AndroidBuilder.WantsV4Slice(withoutArg), Is.False);

            Assert.That(AndroidBuilder.DefinesFor(release: true, v4Slice: true),
                Is.EqualTo(new[] { RuleEngineV4Settings.SliceBuildDefine }));
            Assert.That(AndroidBuilder.DefinesFor(release: false, v4Slice: true),
                Is.EquivalentTo(new[] { AndroidBuilder.DebugDefine, RuleEngineV4Settings.SliceBuildDefine }));
            Assert.That(AndroidBuilder.DefinesFor(release: true), Is.Empty);
            Assert.That(AndroidBuilder.DefinesFor(release: false), Is.EqualTo(new[] { AndroidBuilder.DebugDefine }));
        }
    }
}
