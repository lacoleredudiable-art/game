using System;
using System.IO;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;

namespace Dovus.Tests.EditMode
{
    /// <summary>PR1: kural motoru v4 komut planı — Edit Mode (Unity Test Runner).</summary>
    public sealed class RuleEngineV4EditModeTests
    {
        static readonly int[] SliceVerbs = { 1, 2, 3, 4, 6, 9 };
        static readonly int[] SliceAdjectives = { 1, 2, 3, 4, 5, 6 };
        static readonly int[] SliceWeapons = { 2, 4, 6 };

        static RuleEngineV4Planner Planner()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "RuleEngineV4", "kural-motoru-v4.json");
            string json = File.ReadAllText(path);
            return new RuleEngineV4Planner(RuleEngineV4Catalog.FromJson(json));
        }

        [Test]
        public void Slice108_AllPlansValid()
        {
            Assert.That(RuleEngineV4Feature.Enabled, Is.False);
            var planner = Planner();
            foreach (int verb in SliceVerbs)
            foreach (int adj in SliceAdjectives)
            foreach (int weapon in SliceWeapons)
            {
                CommandPlan plan = planner.Plan(verb, adj, weapon);
                Assert.That(plan.Commands.Count, Is.GreaterThan(1), $"{verb}-{adj} w{weapon}");
                Assert.That(plan.Commands[0], Is.TypeOf<OnSureCommand>());
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
