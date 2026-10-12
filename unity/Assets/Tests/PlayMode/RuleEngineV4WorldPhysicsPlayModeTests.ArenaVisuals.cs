using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Composition.Builders;
using Dovus.Game.Skills.RuleEngineV4;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// "slice_arena_visuals": ArenaVisuals kökü çarpışmasız ve hedeflenemez; sütun/koza modelleri çarpışma
    /// gövdelerinin çocuğu; kaburgalar JSON seed'inden; draw call / üçgen yalnız loglanır (bütçe 60).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string VisualsRoot = "ArenaVisuals";
        const string VisualPrefix = "Visual_";
        const int DrawCallBudget = 60;
        const string BudgetReportFile = "arena-butce.txt";
        const int StatFrames = 10;
        const float CameraSettleSec = 1.5f;
        const float AngleTolDeg = 0.01f;
        const int RibHalves = 2;
        const string RibKorEmissionKeyword = "_KOR_EMISSION";
        /// <summary>Genel çekim kaburga taçlarının (≈27–34 m) üstünden ve güneyden.</summary>
        static readonly Vector3 OverviewFrom = new(0f, 52f, -68f);
        static readonly Vector3 OverviewAt = new(0f, 6f, 2f);
        const float PlanShotHeightM = 95f;
        const float HeartAimHeightM = 16f;
        const float HeartShotBackM = 30f;
        /// <summary>Kaburga altı: oyuncu kemer koridorunda, kamera en alçak eğimde (yukarı bakar) kuzeye.</summary>
        static readonly Vector3 UnderRibStand = new(-4f, 0f, -8f);
        const float UnderRibYawDeg = 0f;
        const float UnderRibPitchDeg = -8f;

        RuleEngineV4SliceArenaVisuals VisualsSpec => _catalog.Catalog.SliceArenaVisuals;

        [UnityTest]
        public IEnumerator ArenaVisuals_HaveNoColliders_OutsidePhysicsLayers()
        {
            yield return null;
            Transform root = Visuals();
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty, "ArenaVisuals altında Collider var");
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty, "ArenaVisuals altında Rigidbody var");
            foreach (Transform holder in BodyHolders())
                Assert.That(holder.GetComponentsInChildren<Collider>(true), Is.Empty, $"{holder.parent.name}/{holder.name} altında Collider var");

            var wrongLayer = root.GetComponentsInChildren<Transform>(true).Concat(BodyHolders().SelectMany(h => h.GetComponentsInChildren<Transform>(true)))
                .Where(t => t.gameObject.layer != RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer)
                .Select(t => t.name).ToList();
            Assert.That(wrongLayer, Is.Empty, "görsel katman dışında: " + string.Join(", ", wrongLayer.Take(8)));
        }

        [UnityTest]
        public IEnumerator ArenaVisuals_NotTargetable_AutoTargetAndMarkNineNine()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            yield return null;
            Transform root = Visuals();
            Assert.That(root.GetComponentsInChildren<TargetableHost>(true), Is.Empty, "ArenaVisuals altında TargetableHost var");
            foreach (TargetableHost t in _md._liveTargetables.Live)
                Assert.That(IsVisual(t.transform), Is.False, $"otomatik hedef adayı görsel: {t.name}");

            // Kaburga kemerinin altından 18 m duvara nişan (ayaklar duvarın dışında): işaret görsele değil duvara düşmeli.
            Transform rib = Ribs()[VisualsSpec.Ribs.Count / 2];
            float r = ArenaSpec.OuterWallCollisionRadiusM;
            Place(_player, new Vector3(-(r - WallAimStandoffM), 0f, rib.position.z));
            _player.rotation = Quaternion.LookRotation(Vector3.left);
            Physics.SyncTransforms();
            yield return Cast(9, 9);
            RuleEngineV4SurfaceMarkHost mark = SingleSurfaceMark(null);
            Assert.That(IsVisual(mark.Surface.transform), Is.False, "9-9 işareti görsele kondu: " + mark.Surface.name);
            Assert.That(ArenaChildren("OuterWallCollision").Contains(mark.Surface.transform), "9-9 işareti duvarda değil: " + mark.Surface.name);
            Assert.That(IsVisual(_cast.LastFocus), Is.False, "9-9 odağı görsel: " + _cast.LastFocus);
        }

        [UnityTest]
        public IEnumerator ArenaVisuals_ColumnAndCocoonModels_ChildOfColliderBodies_ColliderUnchanged()
        {
            yield return null;
            Transform[] columns = ArenaChildren("Columns");
            for (int i = 0; i < columns.Length; i++)
            {
                RuleEngineV4SliceArenaColumn c = ArenaSpec.Columns[i];
                AssertDressed(columns[i], VisualsSpec.ColumnModel, $"sütun {i + 1}");
                Collider col = columns[i].GetComponent<Collider>();
                Assert.That(col.bounds.extents.x, Is.EqualTo(c.RadiusM).Within(PosTolM), $"sütun {i + 1} collider yarıçapı değişti");
                Assert.That(col.bounds.size.y, Is.EqualTo(c.HeightM).Within(PosTolM), $"sütun {i + 1} collider yüksekliği değişti");
            }

            SliceArenaCocoonHost[] cocoons = Cocoons();
            for (int i = 0; i < cocoons.Length; i++)
            {
                RuleEngineV4SliceArenaCocoon c = ArenaSpec.Cocoons[i];
                AssertDressed(cocoons[i].transform, VisualsSpec.CocoonModel, $"koza {i + 1}");
                Collider col = cocoons[i].GetComponent<Collider>();
                Assert.That(col.bounds.extents.x, Is.EqualTo(c.RadiusM).Within(PosTolM), $"koza {i + 1} collider yarıçapı değişti");
                Assert.That(col.bounds.size.y, Is.EqualTo(c.HeightM).Within(PosTolM), $"koza {i + 1} collider yüksekliği değişti");
            }
        }

        [UnityTest]
        public IEnumerator ArenaVisuals_Ribs_SevenWithLodGroup_SeedReproducesTransforms()
        {
            yield return null;
            RuleEngineV4SliceArenaRibs spec = VisualsSpec.Ribs;
            Transform[] ribs = Ribs();
            Assert.That(spec.Count, Is.EqualTo(7), "JSON kaburga sayısı");
            Assert.That(ribs.Length, Is.EqualTo(spec.Count), "sahnedeki kaburga sayısı");

            IReadOnlyList<RuleEngineV4SliceArenaPlacement> layout = spec.Layout();
            IReadOnlyList<RuleEngineV4SliceArenaPlacement> again = spec.Layout();
            for (int i = 0; i < ribs.Length; i++)
            {
                Assert.That(again[i], Is.EqualTo(layout[i]), $"kaburga {i}: aynı seed farklı yerleşim");
                Transform rib = ribs[i];
                RuleEngineV4SliceArenaPlacement p = layout[i];
                Assert.That((rib.localPosition - new Vector3(p.XM, 0f, p.ZM)).magnitude, Is.LessThan(PosTolM), $"kaburga {i} yeri");
                Assert.That(Quaternion.Angle(rib.localRotation, Quaternion.Euler(0f, p.YawDeg, 0f)), Is.LessThan(AngleTolDeg), $"kaburga {i} dönüşü");
                Assert.That(rib.localScale.x, Is.EqualTo(p.Scale).Within(1e-4f), $"kaburga {i} ölçeği");

                var lod = rib.GetComponent<LODGroup>();
                Assert.That(lod, Is.Not.Null, $"kaburga {i} LODGroup yok");
                LOD[] lods = lod.GetLODs();
                Assert.That(lods.Length, Is.EqualTo(2), $"kaburga {i} LOD sayısı");
                Assert.That(lods.All(l => l.renderers.Length > 0 && l.renderers.All(x => x != null)), $"kaburga {i} boş LOD");
                Assert.That(lods[0].renderers.Intersect(lods[1].renderers), Is.Empty, $"kaburga {i} LOD0 ile LOD1 aynı model");
                Assert.That(lods.All(l => l.renderers.Length == RibHalves), $"kaburga {i}: her LOD'da sol+sağ yarım olmalı");

                // Bütçe: kor ayrı pass değil (tek malzeme, emisyon), gölge yok, GPU instancing açık.
                foreach (Renderer r in rib.GetComponentsInChildren<Renderer>())
                {
                    Assert.That(r.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off), $"kaburga {i} {r.name} gölge döküyor");
                    Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), $"kaburga {i} {r.name} ek malzeme (kor pass'i) taşıyor");
                    Assert.That(r.sharedMaterial.enableInstancing, $"kaburga {i} {r.name} malzemesinde GPU instancing kapalı");
                    Assert.That(r.sharedMaterial.IsKeywordEnabled(RibKorEmissionKeyword), $"kaburga {i} {r.name} kor emisyonu kapalı");
                }
            }
            Assert.That(layout[spec.HeartIndex].Scale, Is.EqualTo(layout.Max(p => p.Scale)), "kalp kaburgası en kalın değil");
        }

        [UnityTest]
        public IEnumerator ArenaVisuals_RuneStone_AtJsonPosition()
        {
            yield return null;
            Transform stone = Visuals().Find("RuneStone");
            Assert.That(stone, Is.Not.Null, "RuneStone yok");
            Assert.That(VisualsSpec.RuneStone.XM, Is.EqualTo(0f), "JSON rün taşı x");
            Assert.That(VisualsSpec.RuneStone.ZM, Is.EqualTo(-19f), "JSON rün taşı z");
            Assert.That(Flat(stone.position - new Vector3(0f, 0f, -19f)).magnitude, Is.LessThan(PosTolM), "rün taşı (0, -19)'da değil");
            Assert.That(stone.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(0), "rün taşı modeli yok");
        }

        /// <summary>Bütçe aşımı testi düşürmez; grup kapatılarak ölçülen fark pahalı parçayı gösterir.</summary>
        [UnityTest]
        public IEnumerator ArenaVisuals_GameCamera_DrawCallsAndTriangles_Logged()
        {
            FreezeBoss();
            ParkAll();
            yield return new WaitForSecondsRealtime(CameraSettleSec);
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "Camera.main");

            var report = new StringBuilder();
            FrameStats all = default;
            yield return MeasureFrames(s => all = s);
            report.AppendLine($"[arena-butce] oyun kamerası {cam.transform.position} tümü: {all} (bütçe {DrawCallBudget} draw call)");

            var groups = Visuals().Cast<Transform>().Select(t => (t.name, (IList<GameObject>)new[] { t.gameObject })).ToList();
            groups.Add(("Visual_ (sütun+koza)", BodyHolders().Select(h => h.gameObject).ToList()));
            foreach ((string name, IList<GameObject> objs) in groups)
            {
                foreach (GameObject go in objs)
                    go.SetActive(false);
                FrameStats without = default;
                yield return MeasureFrames(s => without = s);
                foreach (GameObject go in objs)
                    go.SetActive(true);
                report.AppendLine($"[arena-butce]   {name}: +{all.DrawCalls - without.DrawCalls} draw call, +{all.Triangles - without.Triangles} üçgen");
            }
            if (all.DrawCalls > DrawCallBudget)
                report.AppendLine($"[arena-butce] UYARI: {all.DrawCalls} draw call > bütçe {DrawCallBudget}");
            Debug.Log(report.ToString());
            string dir = SessionCaptureDir();
            if (dir != null)
            {
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, BudgetReportFile), report.ToString());
            }
        }

        [UnityTest]
        [Ignore("Eski hata: WaitForEndOfFrame batchmode'da gelmiyor, 12 Ekim")]
        public IEnumerator ArenaVisuals_Evidence_GameCameraShots()
        {
            FreezeBoss();
            ParkAll();
            Camera cam = Camera.main;
            var follow = cam.GetComponent<FollowCameraController>();
            var orbit = Object.FindAnyObjectByType<CameraOrbitController>();
            if (orbit != null)
                orbit.enabled = false;

            follow.enabled = false;
            yield return Shot(cam, OverviewFrom, OverviewAt, "ortam_arena_genel.png");
            yield return Shot(cam, Vector3.up * PlanShotHeightM, Vector3.zero, "ortam_plan_ustten.png", Vector3.forward);

            RuleEngineV4SliceArenaPlacement heart = VisualsSpec.Ribs.Layout()[VisualsSpec.Ribs.HeartIndex];
            var heartAt = new Vector3(0f, HeartAimHeightM, heart.ZM);
            yield return Shot(cam, heartAt + new Vector3(0f, 0f, HeartShotBackM), heartAt, "ortam_kalp_kaburga_kuzey.png");

            yield return FollowShot(follow, UnderRibStand, UnderRibYawDeg, UnderRibPitchDeg, "ortam_karakter_kaburga_alti.png");
            follow.enabled = true;
        }

        IEnumerator Shot(Camera cam, Vector3 from, Vector3 at, string file, Vector3? up = null)
        {
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, up ?? Vector3.up));
            yield return new WaitForSecondsRealtime(0.2f);
            yield return CaptureScreen(file);
        }

        IEnumerator FollowShot(FollowCameraController follow, Vector3 stand, float yawDeg, float pitchDeg, string file)
        {
            Place(_player, stand);
            _player.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            Physics.SyncTransforms();
            follow.enabled = true;
            follow.OrbitYawDeg = yawDeg;
            follow.OrbitPitchDeg = pitchDeg;
            yield return new WaitForSecondsRealtime(CameraSettleSec);
            yield return CaptureScreen(file);
        }

        struct FrameStats
        {
            public long DrawCalls;
            public long Batches;
            public long SetPass;
            public long Triangles;
            public long ShadowCasters;

            public override string ToString() =>
                $"draw call={DrawCalls} batch={Batches} setpass={SetPass} üçgen={Triangles} gölge döküm={ShadowCasters}";
        }

        static IEnumerator MeasureFrames(System.Action<FrameStats> done)
        {
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            using var shadows = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            var max = new FrameStats();
            for (int i = 0; i < StatFrames; i++)
            {
                yield return null;
                long drawCount = draws.LastValue, batchCount = batches.LastValue, triCount = tris.LastValue;
#if UNITY_EDITOR
                // Editörde URP altında profil sayaçları draw call/batch için 0 döner; Game görünümü istatistiği okunur.
                drawCount = System.Math.Max(drawCount, UnityEditor.UnityStats.drawCalls);
                triCount = System.Math.Max(triCount, UnityEditor.UnityStats.triangles);
#endif
                max.DrawCalls = System.Math.Max(max.DrawCalls, drawCount);
                max.Batches = System.Math.Max(max.Batches, batchCount);
                max.SetPass = System.Math.Max(max.SetPass, setPass.LastValue);
                max.Triangles = System.Math.Max(max.Triangles, triCount);
                max.ShadowCasters = System.Math.Max(max.ShadowCasters, shadows.LastValue);
            }
            done(max);
        }

        static void AssertDressed(Transform body, string model, string label)
        {
            Transform holder = body.Find(VisualPrefix + model);
            Assert.That(holder, Is.Not.Null, $"{label}: {VisualPrefix}{model} çocuğu yok");
            Assert.That(holder.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(0), $"{label}: model renderer yok");
            Assert.That(holder.lossyScale.x, Is.EqualTo(1f).Within(1e-3f), $"{label}: model dünya ölçeği 1 değil");
        }

        static Transform Visuals()
        {
            GameObject root = GameObject.Find(VisualsRoot);
            Assert.That(root, Is.Not.Null, "ArenaVisuals kökü yok");
            return root.transform;
        }

        static Transform[] Ribs()
        {
            Transform g = Visuals().Find("Ribs");
            Assert.That(g, Is.Not.Null, "Ribs grubu yok");
            return g.Cast<Transform>().ToArray();
        }

        static IEnumerable<Transform> BodyHolders() =>
            ArenaChildren("Columns").Concat(ArenaChildren("Cocoons"))
                .SelectMany(b => b.Cast<Transform>())
                .Where(c => c.name.StartsWith(VisualPrefix));

        static bool IsVisual(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name == VisualsRoot || t.name.StartsWith(VisualPrefix))
                    return true;
            }
            return false;
        }
    }
}
