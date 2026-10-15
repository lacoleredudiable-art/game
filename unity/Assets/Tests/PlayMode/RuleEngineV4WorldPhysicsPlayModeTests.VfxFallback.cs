using System.Linq;
using Dovus.Game.Vfx;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Envanter B3: dilimde eksik VFX için yedek primitive çizilmez; [V4-FALLBACK] LogError yazılır ve nesne dönmez.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        [Test]
        public void B3_EksikEfekt_V4Acikken_YedekCizilmez_FallbackYazar()
        {
            var factory = new PlaceholderFactory(ScriptableObject.CreateInstance<VfxLibrary>());
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[V4-FALLBACK\] efekt: .*trail/yok_stil"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[V4-FALLBACK\] efekt: .*impact/yok_stil"));
            GameObject trail = factory.CreateTrail("yok_stil", "Ates", Vector3.zero, Vector3.forward);
            GameObject impact = factory.CreateImpact("yok_stil", "Ates", Vector3.zero);
            // TearDown konsol hatalarını toplar; beklenen iki [V4-FALLBACK] burada doğrulanıp düşülür.
            Assert.That(_errors.Count(e => e.Contains("[V4-FALLBACK] efekt:") && e.Contains("trail/yok_stil")), Is.EqualTo(1));
            Assert.That(_errors.Count(e => e.Contains("[V4-FALLBACK] efekt:") && e.Contains("impact/yok_stil")), Is.EqualTo(1));
            _errors.Clear();
            Assert.That(trail, Is.Null, "dilimde yedek trail çizilmemeli");
            Assert.That(impact, Is.Null, "dilimde yedek impact çizilmemeli");
        }
    }
}
