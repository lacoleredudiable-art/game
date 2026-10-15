using System.Collections;
using System.Linq;
using Dovus.Game.Casting;
using Dovus.Game.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        // Arayüz sadeleştirme: LOCK, koyu kutu + başlık, ELEMENT çipi savaş ekranından kalktı.
        [UnityTest]
        public IEnumerator HudSadelik_LockKutuBaslikElementCipi_YokAmaDodgeRunlerCizgilerVar()
        {
            yield return null;
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            string[] names = all.Select(t => t.name).ToArray();
            Assert.That(names, Does.Not.Contain("LockOnButton"), "LOCK düğmesi hâlâ var");
            Assert.That(names, Does.Not.Contain("CombatRuneTray"), "koyu kutu hâlâ var");
            Assert.That(names.Any(n => n.Contains("ElementHold") || n.Contains("ElementRadial")), Is.False,
                "ELEMENT çipi/menüsü hâlâ var");
            Assert.That(
                Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Any(t => t.text.Contains("RÜN ZİNCİRİ")),
                Is.False, "RÜN ZİNCİRİ başlığı hâlâ var");
            Assert.That(names, Does.Contain("DodgeButton"), "dodge kalkmamalı");
            Assert.That(names, Does.Contain("HexLink1"), "rün çizgileri kalmalı");
            Assert.That(Object.FindAnyObjectByType<HexagonView>(), Is.Not.Null);
        }

        // ELEMENT artık build ekranında seçilir: çipe bas → UYGULA → oynanan element değişir.
        [UnityTest]
        public IEnumerator HudSadelik_BuildEkraninda_ElementSecilirVeUygulanir()
        {
            var hud = Object.FindAnyObjectByType<BuildSelectHud>();
            hud.Open();
            yield return null;
            int current = _md.SelectedElementPaint?.Id ?? -1;
            Button[] chips = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b.name.StartsWith("ElementChip")).ToArray();
            Assert.That(chips.Length, Is.EqualTo(6), "build ekranında 6 element çipi olmalı");
            Button target = chips.First(b => b.name != "ElementChip" + current);
            int targetId = int.Parse(target.name.Substring("ElementChip".Length));
            target.onClick.Invoke();
            Assert.That(hud.SweepElementId, Is.EqualTo(targetId));
            hud.SweepApplyAndStart();
            yield return null;
            Assert.That(_md.SelectedElementPaint?.Id, Is.EqualTo(targetId), "seçilen element uygulanmadı");
        }
    }
}
