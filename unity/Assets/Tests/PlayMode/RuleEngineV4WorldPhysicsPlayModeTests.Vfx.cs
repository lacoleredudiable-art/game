using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Ejder silüeti / rün glifi: skill_anim giriş silüeti ve gerçek atlaslar (Resources/Vfx/Ejder).</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const string TaklaClip = "Ortak_Takla";
        const float WingLifeSec = 0.7f;
        const string SharedAtlasName = "Ejder_Vfx_Atlas";
        const int SharedAtlasPx = 2048;
        const string VertexDissolveKeyword = "_VERTEX_DISSOLVE";
        const float StTol = 1e-5f;
        const float SilhouetteSettleSec = 0.6f;

        // Ortak atlas 2048²: siluet bölgesi (0, 768) üstten, 4×2 × 256 px → UV (x, y alt, w, h).
        static readonly Rect SilhouetteCellDUv = new(0.375f, 0.5f, 0.125f, 0.125f);
        static readonly Rect SilhouetteCellFUv = new(0.125f, 0.375f, 0.125f, 0.125f);

        sealed class SilhouetteWatch
        {
            public readonly List<int> Cells = new();
            public RuleEngineV4UvRect FirstUv;
            public float FirstLife;
            public float OnAt = -1f;
            public float OffAt = -1f;
            public float MaxDt;
            public int MaxLive;
        }

        [UnityTest]
        public IEnumerator Yay_Hareket_BackstepEntry_SpawnsOneWingSilhouette()
        {
            FreezeBoss();
            Equip(Yay);
            AnimationBridge bridge = _md.AnimationBridge;
            DragonSilhouetteCardView card = SilhouetteCard();
            _label = "3-1 yay";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();

            var w = new SilhouetteWatch();
            int livePlaysBefore = card.PlayCount;
            Assert.That(_cast.TryLaunchCombo(3, 1, out _), Is.True, $"{_label} cast edilmedi");
            Assert.That(bridge.LastClipName, Is.EqualTo(BackstepClip), $"{_label} klibi");
            Assert.That(bridge.LastExactClipFound && bridge.LastPlayApplied, Is.True, $"{_label} geri sıçrama oynamadı");
            yield return WatchSilhouette(card, w, captureName: "yay3_cift_kanat.png");

            Assert.That(card.PlayCount - livePlaysBefore, Is.EqualTo(1), "Yay+3 tam bir silüet kartı");
            Assert.That(w.Cells, Is.EqualTo(new[] { VfxPlanDefaults.EjderAtlasCellD }), "Yay+3 silüet hücresi D");
            Assert.That(w.MaxLive, Is.EqualTo(1), "aynı anda birden çok silüet");
            Assert.That(w.FirstLife, Is.EqualTo(WingLifeSec).Within(StTol), "binding ömrü");
            Assert.That(w.OnAt, Is.GreaterThanOrEqualTo(0f), "silüet görünmedi");
            Assert.That(w.OffAt, Is.GreaterThan(w.OnAt), "silüet sönmedi");
            Assert.That(w.OffAt - w.OnAt, Is.EqualTo(WingLifeSec).Within(w.MaxDt + StTol), "silüet ömrü ±1 kare");
            AssertUv(w.FirstUv, SilhouetteCellDUv, "hücre D");
            AssertSharedAtlas(card.CardMaterial, "silüet");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator Kilic_DodgeAndHareket_NoWing_AtilKeepsTail()
        {
            FreezeBoss();
            Equip(Kilic);
            DragonSilhouetteCardView card = SilhouetteCard();

            // Sıradan kaçınma (Dodge state, DodgeMotionController): giriş silüeti yok.
            _label = "kılıç dodge";
            Arrange(Vector3.right);
            var input = Object.FindAnyObjectByType<HexagonInputController>();
            Assert.That(input, Is.Not.Null, "HexagonInputController");
            Assert.That(input.Dodge, Is.Not.Null, "DodgeState");
            var dodgeMotion = _player.GetComponent<DodgeMotionController>();
            Assert.That(dodgeMotion, Is.Not.Null, "DodgeMotionController");
            input.Dodge.Begin((int)_md.MechanicsClock.Director.WorldTimeMs);
            bool dodged = false;
            var dodgeWatch = new SilhouetteWatch();
            float dodgeEnd = Time.realtimeSinceStartup + 1.5f;
            int seen = card.PlayCount;
            while (Time.realtimeSinceStartup < dodgeEnd)
            {
                yield return null;
                dodged |= dodgeMotion.IsDisplacing;
                CollectPlays(card, ref seen, dodgeWatch);
            }
            Assert.That(dodged, Is.True, "dodge kayması başlamadı");
            Assert.That(dodgeWatch.Cells, Is.Empty, "dodge silüet doğurdu");

            // Kılıç 3-x yalnız v4 köprüsüyle: çift kanat (D) hiç doğmaz.
            foreach (int adj in RuleEngineV4Slice.Runes)
            {
                _label = $"3-{adj} kılıç";
                Arrange(Vector3.right);
                Place(_minions[0].transform, Origin + Vector3.right * 4f);
                Physics.SyncTransforms();
                var w = new SilhouetteWatch();
                Assert.That(_cast.TryLaunchCombo(3, adj, out _), Is.True, $"{_label} cast edilmedi");
                yield return WatchSilhouette(card, w);
                Assert.That(w.Cells, Has.No.Member(VfxPlanDefaults.EjderAtlasCellD), $"{_label} çift kanat doğurdu");
            }

            // Kılıç ATIL (shout BeginSkill + v4 atılış): kuyruk yayı F hâlâ doğar.
            _label = "3-1 kılıç ATIL";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.right * 4f);
            Physics.SyncTransforms();
            var vfx = _player.GetComponentInChildren<RuleDrivenVfxDirector>();
            Assert.That(vfx, Is.Not.Null, "RuleDrivenVfxDirector");
            vfx.BeginSkill(KilicAtilSkill(), VfxPlanDefaults.WeaponKeyKilic);
            var atil = new SilhouetteWatch();
            Assert.That(_cast.TryLaunchCombo(3, 1, out _), Is.True, $"{_label} cast edilmedi");
            yield return WatchSilhouette(card, atil, captureName: "kilic_atil_kuyruk.png");
            Assert.That(atil.Cells, Is.EqualTo(new[] { VfxPlanDefaults.EjderAtlasCellF }), "Kılıç ATIL kuyruk yayı F");
            AssertUv(atil.FirstUv, SilhouetteCellFUv, "hücre F");
            AssertSharedAtlas(card.CardMaterial, "silüet");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator DragonAtlases_UseRealArt_GridCells()
        {
            yield return null;
            DragonSilhouetteCardView card = SilhouetteCard();
            Vector3 from = _player.position + Vector3.up;
            Vector3 to = from + Vector3.right * 2f;
            var color = new VfxColorRgb(1f, 1f, 1f, 1f);

            card.PlayAlongLine(from, to, color, WingLifeSec, VfxPlanDefaults.EjderAtlasCellD);
            AssertSharedAtlas(card.CardMaterial, "silüet");
            AssertUv(card.CellUv, SilhouetteCellDUv, "hücre D");
            card.PlayAlongLine(from, to, color, WingLifeSec, VfxPlanDefaults.EjderAtlasCellF);
            AssertUv(card.CellUv, SilhouetteCellFUv, "hücre F");

            var runes = _player.GetComponentInChildren<DragonRuneFlashView>(true);
            Assert.That(runes, Is.Not.Null, "DragonRuneFlashView");
            runes.Play(3, 1, Color.white);
            AssertSharedAtlas(runes.LetterMaterial, "rün harfi");
            // 0 = sol üst; fiil rün 3 = SHEV → glif 8 (sütun 0, satır 1), sıfat rün 1 = VORN → glif 22 (sütun 6, satır 2).
            // Harf bölgesi ortak atlasın üstünde: piksel (0, 256..512) ve (1536, 512..768) üstten.
            AssertUv(runes.VerbUv, new Rect(0f, 0.75f, 0.125f, 0.125f), "fiil glif hücresi");
            AssertUv(runes.AdjectiveUv, new Rect(0.75f, 0.625f, 0.125f, 0.125f), "sıfat glif hücresi");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator SharedVfxBatch_SilhouetteRunesRing_OneRendererOneMaterial()
        {
            yield return null;
            DragonSilhouetteCardView card = SilhouetteCard();
            var runes = _player.GetComponentInChildren<DragonRuneFlashView>(true);
            Assert.That(runes, Is.Not.Null, "DragonRuneFlashView");
            EjderVfxBatchHost batch = VfxBatch();
            var ring = Dovus.Game.Skills.RuleEngineV4.RuleEngineV4TargetPickRingView.Create(batch, Color.red, null);
            try
            {
                Vector3 from = _player.position + Vector3.up;
                card.PlayAlongLine(from, from + Vector3.right * 2f, new VfxColorRgb(1f, 1f, 1f, 1f), WingLifeSec,
                    VfxPlanDefaults.EjderAtlasCellF);
                runes.Play(3, 1, Color.white);
                ring.ShowOnSurface(_player.position, Vector3.up);
                yield return null;

                AssertSharedAtlas(batch.SharedMaterial, "batch");
                Assert.That(card.CardMaterial, Is.SameAs(batch.SharedMaterial), "silüet ayrı materyal");
                Assert.That(runes.LetterMaterial, Is.SameAs(batch.SharedMaterial), "harf ayrı materyal");
                Assert.That(ring.RingMaterial, Is.SameAs(batch.SharedMaterial), "halka ayrı materyal");
                Assert.That(batch.QuadCount, Is.EqualTo(4), "silüet 1 + harf 2 + halka 1 dörtgen tek mesh'te");
                Assert.That(batch.Renderer.enabled, Is.True, "batch çizilmiyor");
                Assert.That(batch.BatchMesh.subMeshCount, Is.EqualTo(1), "tek alt mesh");

                Material shared = batch.SharedMaterial;
                Renderer[] others = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
                    .Where(r => r != batch.Renderer && r.enabled && r.sharedMaterials.Contains(shared))
                    .ToArray();
                Assert.That(others, Is.Empty, "ortak materyal ikinci renderer'da: " + string.Join(", ", others.Select(r => r.name)));
                Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                        .Any(c => c.name == "DragonRuneFlashCanvas"), Is.False, "harf flaşı hâlâ UI tuvalinde");
                Assert.That(ring.GetComponent<LineRenderer>(), Is.Null, "halka hâlâ LineRenderer");
            }
            finally
            {
                Object.Destroy(ring.gameObject);
            }
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        DragonSilhouetteCardView SilhouetteCard()
        {
            var card = _player.GetComponentInChildren<DragonSilhouetteCardView>(true);
            Assert.That(card, Is.Not.Null, "DragonSilhouetteCardView yok");
            return card;
        }

        /// <summary>Sahne kurulumunun oyuncu efekt yönetmenine bağladığı ortak atlas batch'i.</summary>
        EjderVfxBatchHost VfxBatch()
        {
            var vfx = _player.GetComponentInChildren<RuleDrivenVfxDirector>();
            Assert.That(vfx, Is.Not.Null, "RuleDrivenVfxDirector");
            Assert.That(vfx.Batch, Is.Not.Null, "ortak efekt batch'i bağlı değil");
            return vfx.Batch;
        }

        IEnumerator WatchSilhouette(DragonSilhouetteCardView card, SilhouetteWatch w, string captureName = null)
        {
            int seen = card.PlayCount;
            bool wasVisible = card.IsVisible;
            bool captured = captureName == null || !CaptureRequested();
            float castEnd = Time.realtimeSinceStartup + CastTimeoutSec;
            float idleAt = -1f;
            while (true)
            {
                yield return null;
                w.MaxDt = Mathf.Max(w.MaxDt, Time.deltaTime);
                w.MaxLive = Mathf.Max(w.MaxLive, DragonSilhouetteCardView.LiveCount);
                CollectPlays(card, ref seen, w);
                if (card.IsVisible && !wasVisible && w.OnAt < 0f)
                    w.OnAt = Time.time;
                if (!card.IsVisible && wasVisible && w.OnAt >= 0f && w.OffAt < 0f)
                    w.OffAt = Time.time;
                wasVisible = card.IsVisible;
                if (!captured && card.IsVisible)
                {
                    captured = true;
                    yield return CaptureIfRequested(captureName);
                }
                if (idleAt < 0f && (!_cast.IsRunning || Time.realtimeSinceStartup > castEnd))
                    idleAt = Time.realtimeSinceStartup;
                if (idleAt >= 0f && Time.realtimeSinceStartup - idleAt > SilhouetteSettleSec && !card.IsVisible)
                    break;
                if (Time.realtimeSinceStartup > castEnd + SilhouetteSettleSec * 2f)
                    break;
            }
        }

        static void CollectPlays(DragonSilhouetteCardView card, ref int seen, SilhouetteWatch w)
        {
            while (seen < card.PlayCount)
            {
                seen++;
                w.Cells.Add(card.LastAtlasCell);
                if (w.Cells.Count == 1)
                {
                    w.FirstUv = card.CellUv;
                    w.FirstLife = card.LastLifeSec;
                }
            }
        }

        static bool CaptureRequested() => Environment.GetCommandLineArgs().Contains(CaptureArg);

        static void AssertUv(RuleEngineV4UvRect actual, Rect expected, string what)
        {
            string got = $"({actual.X}, {actual.Y}, {actual.W}, {actual.H})";
            Assert.That(actual.X, Is.EqualTo(expected.x).Within(StTol), $"{what} UV x = {got}");
            Assert.That(actual.Y, Is.EqualTo(expected.y).Within(StTol), $"{what} UV y = {got}");
            Assert.That(actual.W, Is.EqualTo(expected.width).Within(StTol), $"{what} UV w = {got}");
            Assert.That(actual.H, Is.EqualTo(expected.height).Within(StTol), $"{what} UV h = {got}");
        }

        static void AssertSharedAtlas(Material mat, string what)
        {
            Assert.That(mat, Is.Not.Null, $"{what} materyali");
            Assert.That(mat.shader.name, Is.EqualTo(RuleVfxDefaults.KorShaderName), $"{what} VFX_Kor değil");
            Assert.That(mat.IsKeywordEnabled(VertexDissolveKeyword), Is.True, $"{what} köşe dissolve kapalı");
            Texture tex = mat.GetTexture("_MainTex");
            Assert.That(tex, Is.Not.Null, "_MainTex");
            Assert.That(tex.name, Is.EqualTo(SharedAtlasName), $"{what} ortak atlas değil");
            Assert.That(tex.width, Is.EqualTo(SharedAtlasPx), "ortak atlas genişlik");
            Assert.That(tex.height, Is.EqualTo(SharedAtlasPx), "ortak atlas yükseklik");
            Assert.That(mat.GetTextureScale("_MainTex"), Is.EqualTo(Vector2.one), "atlas ölçeği hücreye kısılmış");
        }

        static SkillResolution KilicAtilSkill() =>
            SkillResolution.Build(
                elementId: "ates",
                elementName: "Ateş",
                displayName: "Hareket Yoğunlaştırma",
                skillId: (SkillId)"3-1",
                skillJob: "strike",
                verbId: "3",
                verbName: "Hareket",
                verbFamily: "motion",
                action: "dash",
                baseDamage: 10f,
                basePoise: 0f,
                hitbox: "dash_line",
                castMobility: "rooted",
                mechanics: Array.Empty<string>(),
                adjectiveId: "1",
                adjectiveName: "Yoğunlaştırma",
                silhouetteAxis: "horizontal",
                damageMult: 1f,
                hitboxScaleMult: 1f,
                poiseDamageMult: 1f,
                length: 2,
                lengthRole: "Temel",
                lengthCastMult: 1f,
                lengthMobility: "rooted",
                flavorElement: string.Empty);
    }
}
