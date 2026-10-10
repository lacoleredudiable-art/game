using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    public sealed class RuleEngineV4EditModeTests
    {
        static RuleEngineV4Planner Planner()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "RuleEngineV4", "kural-motoru-v4.json");
            return new RuleEngineV4Planner(RuleEngineV4Catalog.FromJson(File.ReadAllText(path)));
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
