using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    public sealed class RuleEngineV4EditModeTests
    {
        static RuleEngineV4Catalog Catalog()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "RuleEngineV4", "kural-motoru-v4.json");
            return RuleEngineV4Catalog.FromJson(File.ReadAllText(path));
        }

        static RuleEngineV4Planner Planner() => new(Catalog());

        [Test]
        public void SliceWeapons_CekicOut_RulesStayInJson()
        {
            Assert.That(RuleEngineV4Slice.IsSliceWeapon(6), Is.False, "Çekiç dilimde");
            Assert.That(RuleEngineV4Slice.IsSliceWeapon(2), Is.True, "Yay dilimde değil");
            Assert.That(RuleEngineV4Slice.IsSliceWeapon(4), Is.True, "Kılıç dilimde değil");
            RuleEngineV4Catalog catalog = Catalog();
            Assert.That(catalog.TryGetWeapon(6, out _), Is.True, "Çekiç silah kuralı JSON'dan silinmiş");
            Assert.That(new RuleEngineV4Planner(catalog).Plan(1, 2, 6).IsValid, Is.True, "Çekiç planı");
            Assert.That(catalog.SkillAnim.Resolve(1, 2, 6).Clip, Is.EqualTo("Cekic_VUR"), "Çekiç skill_anim");
        }

        [Test]
        public void Slice108_AllPlansValid()
        {
            Assert.That(RuleEngineV4Feature.Enabled, Is.False);
            var planner = Planner();
            foreach (int rune in RuleEngineV4Slice.Runes)
            foreach (int adj in RuleEngineV4Slice.Runes)
            foreach (int weapon in RuleEngineV4Slice.Weapons)
            {
                CommandPlan plan = planner.Plan(rune, adj, weapon);
                Assert.That(plan.Commands.Count, Is.GreaterThan(1), $"{rune}-{adj} w{weapon}");
            }
        }

        [Test]
        public void WeaponRhythm_ChangesDeliveryRange()
        {
            var planner = Planner();
            float kilic = ((YakinVurusCommand)planner.Plan(1, 2, 4).Commands.First(c => c is YakinVurusCommand)).RangeM;
            float yay = ((MermiFirlatCommand)planner.Plan(1, 2, 2).Commands.First(c => c is MermiFirlatCommand)).RangeM;
            Assert.That(yay, Is.GreaterThan(kilic));
        }
    }
}
