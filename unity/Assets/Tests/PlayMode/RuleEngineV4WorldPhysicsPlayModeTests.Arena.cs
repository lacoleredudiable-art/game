using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Dilim arenası: sütunlar, 18 m dış duvar, kozalar ve 18–22 m görsel halka.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string ArenaRoot = "RuleEngineV4SliceArena";
        const float WallProbeY = 1f;
        const float RingProbeRadiusM = 0.05f;

        RuleEngineV4SliceArena ArenaSpec => _catalog.Catalog.SliceArena;

        [UnityTest]
        public IEnumerator Arena_BuildsColumnsWallAndCocoons()
        {
            yield return null;
            RuleEngineV4SliceArena spec = ArenaSpec;
            Assert.That(spec.Columns.Count, Is.EqualTo(3), "spec sütun sayısı");
            Assert.That(spec.Cocoons.Count, Is.EqualTo(2), "spec koza sayısı");
            Assert.That(spec.OuterWallCollisionRadiusM, Is.EqualTo(18f), "spec duvar yarıçapı");

            Transform[] columns = ArenaChildren("Columns");
            Assert.That(columns.Length, Is.EqualTo(spec.Columns.Count), "sahnedeki sütun sayısı");
            for (int i = 0; i < columns.Length; i++)
            {
                RuleEngineV4SliceArenaColumn c = spec.Columns[i];
                Assert.That(Flat(columns[i].position - new Vector3(c.XM, 0f, c.ZM)).magnitude, Is.LessThan(PosTolM), $"sütun {i + 1} yeri");
                Collider col = columns[i].GetComponent<Collider>();
                Assert.That(col != null && !col.isTrigger, $"sütun {i + 1} katı collider değil");
                Assert.That(col.bounds.extents.x, Is.EqualTo(c.RadiusM).Within(PosTolM), $"sütun {i + 1} yarıçapı");
                Assert.That(col.bounds.size.y, Is.EqualTo(c.HeightM).Within(PosTolM), $"sütun {i + 1} yüksekliği");
            }

            Transform[] wall = ArenaChildren("OuterWallCollision");
            Assert.That(wall.Length, Is.EqualTo(spec.OuterWallSegments), "duvar segment sayısı");
            Assert.That(wall.All(w => w.GetComponent<Collider>() is { isTrigger: false }), "duvar segmenti katı değil");

            SliceArenaCocoonHost[] cocoons = Cocoons();
            Assert.That(cocoons.Length, Is.EqualTo(spec.Cocoons.Count), "sahnedeki koza sayısı");
            for (int i = 0; i < cocoons.Length; i++)
            {
                RuleEngineV4SliceArenaCocoon c = spec.Cocoons[i];
                Assert.That(Flat(cocoons[i].transform.position - new Vector3(c.XM, 0f, c.ZM)).magnitude, Is.LessThan(PosTolM), $"koza {i + 1} yeri");
                Assert.That(cocoons[i].MaxHp, Is.EqualTo(c.MaxHp), $"koza {i + 1} canı");
                Assert.That(cocoons[i].Hp, Is.EqualTo(c.MaxHp), $"koza {i + 1} tam canlı değil");
            }

            var open = new List<int>();
            for (int deg = 0; deg < 360; deg++)
            {
                Vector3 dir = Quaternion.Euler(0f, deg, 0f) * Vector3.forward;
                float r = spec.OuterWallCollisionRadiusM;
                bool blocked = Physics.RaycastAll(Vector3.up * WallProbeY, dir, r + PosTolM, ~0, QueryTriggerInteraction.Ignore)
                    .Any(h => h.collider.transform.IsChildOf(wall[0].parent));
                if (!blocked)
                    open.Add(deg);
            }
            Assert.That(open, Is.Empty, "dış duvar 18 m'de kesintili; açık yönler (derece): " + string.Join(", ", open));
        }

        [UnityTest]
        public IEnumerator OuterRing_18To22_HasNoCollision()
        {
            yield return null;
            RuleEngineV4SliceArena spec = ArenaSpec;
            float inner = spec.OuterWallCollisionRadiusM + PosTolM + RingProbeRadiusM;
            float outer = spec.OuterWallVisualOuterRadiusM - PosTolM;
            var hits = new HashSet<string>();
            for (float r = inner; r <= outer; r += 0.25f)
            for (int deg = 0; deg < 360; deg += 5)
            for (float y = 0.5f; y <= spec.OuterWallHeightM; y += 1f)
            {
                Vector3 p = Quaternion.Euler(0f, deg, 0f) * Vector3.forward * r + Vector3.up * y;
                foreach (Collider c in Physics.OverlapSphere(p, RingProbeRadiusM, ~0, QueryTriggerInteraction.Ignore))
                    hits.Add($"{c.name}@r{r:0.00}");
            }
            Assert.That(hits, Is.Empty, "18–22 m halkasında çarpışma var: " + string.Join(", ", hits.Take(12)));
        }

        [UnityTest]
        public IEnumerator Dash_StopsAtColumn()
        {
            FreezeBoss();
            Equip(Kilic);
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 center = new(col.XM, 0f, col.ZM);
            ParkAll();
            Place(_player, center - Vector3.right * (col.RadiusM + 2.5f));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Place(_minions[0].transform, center + Vector3.right * (col.RadiusM + 2.5f));
            Physics.SyncTransforms();

            float startX = _player.position.x;
            yield return Cast(3, 1);

            float radius = _physics.BodyRadius(_player);
            float face = center.x - col.RadiusM;
            Assert.That(_player.position.x - startX, Is.GreaterThan(1f), "dash başlamadı");
            Assert.That(_player.position.x + radius, Is.LessThanOrEqualTo(face + PosTolM),
                $"oyuncu sütunun içine/arkasına geçti (x={_player.position.x:0.00}, sütun yüzü={face:0.00})");
        }

        [UnityTest]
        public IEnumerator Arrow_StopsAtColumn()
        {
            FreezeBoss();
            Equip(Yay);
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 center = new(col.XM, 0f, col.ZM);
            ParkAll();
            Place(_player, center - Vector3.right * (col.RadiusM + 4f));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            SliceLightMinionHost behind = _minions[0];
            Place(behind.transform, center + Vector3.right * (col.RadiusM + 3f));
            Physics.SyncTransforms();

            CommandPlan plan = PlannedFor(1, 2, Yay);
            Assert.That(plan.Commands.OfType<MermiFirlatCommand>().Any(), "planda mermi yok");
            yield return Run(plan, behind.transform);

            Assert.That(behind.Hp, Is.EqualTo(behind.MaxHp), "ok sütunu geçip arkadaki düşmana vurdu");
        }

        [UnityTest]
        public IEnumerator Cocoon_TakesDamageAndBreaks_NotDisplaced()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            SliceArenaCocoonHost cocoon = Cocoons()[0];
            Transform t = cocoon.transform;
            Vector3 at = t.position;
            Place(_player, at - Vector3.right * (cocoon.BodyRadiusM() * t.lossyScale.x + 0.6f));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Physics.SyncTransforms();

            yield return Run(PlannedFor(5, 2, Kilic), t);
            Assert.That((t.position - at).magnitude, Is.LessThan(0.01f), "koza itildi");

            CommandPlan hit = PlannedFor(1, 2, Kilic);
            int before = cocoon.Hp;
            yield return Run(hit, t);
            Assert.That(cocoon.Hp, Is.LessThan(before), "koza hasar almadı");
            Assert.That((t.position - at).magnitude, Is.LessThan(0.01f), "koza vuruşla yerinden oynadı");

            cocoon.ApplyDamage(cocoon.Hp - 1);
            Assert.That(cocoon.IsBroken, Is.False, "koza erken kırıldı");
            yield return Run(hit, t);
            Assert.That(cocoon.IsBroken, Is.True, "koza son vuruşla kırılmadı");
            Assert.That(cocoon.gameObject.activeSelf, Is.False, "kırılan koza sahnede duruyor");
            Assert.That((t.position - at).magnitude, Is.LessThan(0.01f), "koza kırılırken yerinden oynadı");
        }

        static Transform[] ArenaChildren(string group)
        {
            GameObject root = GameObject.Find(ArenaRoot);
            Assert.That(root, Is.Not.Null, "arena kökü yok");
            Transform g = root.transform.Find(group);
            Assert.That(g, Is.Not.Null, $"arena grubu yok: {group}");
            return g.Cast<Transform>().ToArray();
        }

        static SliceArenaCocoonHost[] Cocoons() =>
            ArenaChildren("Cocoons").Select(c => c.GetComponent<SliceArenaCocoonHost>()).ToArray();
    }
}
