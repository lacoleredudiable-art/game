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
        const float WingLifeSec = 0.3f;
        const string SilhouetteAtlasName = "Ejder_Siluet_Atlas";
        const string GlyphAtlasName = "Ejder_Glif_Atlas";
        const float StTol = 1e-5f;
        const float SilhouetteSettleSec = 0.6f;

        sealed class SilhouetteWatch
        {
            public readonly List<int> Cells = new();
            public Vector4 FirstSt;
            public float FirstLife;
            public float OnAt = -1f;
            public float OffAt = -1f;
            public float MaxDt;
            public int MaxLive;
        }

        [UnityTest]
        public IEnumerator Yay_Hareket_TaklaEntry_SpawnsOneWingSilhouette()
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
            Assert.That(bridge.LastClipName, Is.EqualTo(TaklaClip), $"{_label} klibi");
            Assert.That(bridge.LastExactClipFound && bridge.LastPlayApplied, Is.True, $"{_label} Takla oynamadı");
            yield return WatchSilhouette(card, w, captureName: "yay3_cift_kanat.png");

            Assert.That(card.PlayCount - livePlaysBefore, Is.EqualTo(1), "Yay+3 tam bir silüet kartı");
            Assert.That(w.Cells, Is.EqualTo(new[] { VfxPlanDefaults.EjderAtlasCellD }), "Yay+3 silüet hücresi D");
            Assert.That(w.MaxLive, Is.EqualTo(1), "aynı anda birden çok silüet");
            Assert.That(w.FirstLife, Is.EqualTo(WingLifeSec).Within(StTol), "binding ömrü");
            Assert.That(w.OnAt, Is.GreaterThanOrEqualTo(0f), "silüet görünmedi");
            Assert.That(w.OffAt, Is.GreaterThan(w.OnAt), "silüet sönmedi");
            Assert.That(w.OffAt - w.OnAt, Is.EqualTo(WingLifeSec).Within(w.MaxDt + StTol), "silüet ömrü ±1 kare");
            AssertSt(w.FirstSt, new Vector4(0.25f, 0.5f, 0.75f, 0.5f), "hücre D");
            AssertRealSilhouetteAtlas(card);
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
            AssertSt(atil.FirstSt, new Vector4(0.25f, 0.5f, 0.25f, 0f), "hücre F");
            AssertRealSilhouetteAtlas(card);
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
            AssertRealSilhouetteAtlas(card);
            AssertSt(card.MainTexST, new Vector4(0.25f, 0.5f, 0.75f, 0.5f), "hücre D");
            card.PlayAlongLine(from, to, color, WingLifeSec, VfxPlanDefaults.EjderAtlasCellF);
            AssertSt(card.MainTexST, new Vector4(0.25f, 0.5f, 0.25f, 0f), "hücre F");

            var runes = _player.GetComponentInChildren<DragonRuneFlashView>(true);
            Assert.That(runes, Is.Not.Null, "DragonRuneFlashView");
            runes.Play(3, 1, Color.white);
            Texture tex = runes.VerbSprite.texture;
            Assert.That(tex.name, Is.EqualTo(GlyphAtlasName), "rün glif atlası");
            Assert.That(tex.width, Is.EqualTo(2048), "glif atlası genişlik");
            Assert.That(tex.height, Is.EqualTo(768), "glif atlası yükseklik");
            Assert.That(runes.AdjectiveSprite.texture, Is.SameAs(tex));
            // 0 = sol üst; fiil rün 3 → glif 2 (sütun 2, satır 0), sıfat rün 1 → glif 12 (sütun 4, satır 1).
            Assert.That(runes.VerbSprite.rect, Is.EqualTo(new Rect(512f, 512f, 256f, 256f)), "fiil glif hücresi");
            Assert.That(runes.AdjectiveSprite.rect, Is.EqualTo(new Rect(1024f, 256f, 256f, 256f)), "sıfat glif hücresi");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        DragonSilhouetteCardView SilhouetteCard()
        {
            var card = _player.GetComponentInChildren<DragonSilhouetteCardView>(true);
            Assert.That(card, Is.Not.Null, "DragonSilhouetteCardView yok");
            return card;
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
                    w.FirstSt = card.MainTexST;
                    w.FirstLife = card.LastLifeSec;
                }
            }
        }

        static bool CaptureRequested() => Environment.GetCommandLineArgs().Contains(CaptureArg);

        static void AssertSt(Vector4 actual, Vector4 expected, string what)
        {
            for (int i = 0; i < 4; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(StTol), $"{what} _MainTex_ST[{i}] = {actual}");
        }

        static void AssertRealSilhouetteAtlas(DragonSilhouetteCardView card)
        {
            Material mat = card.CardMaterial;
            Assert.That(mat, Is.Not.Null, "silüet materyali");
            Texture tex = mat.GetTexture("_MainTex");
            Assert.That(tex, Is.Not.Null, "_MainTex");
            Assert.That(tex.name, Is.EqualTo(SilhouetteAtlasName), "silüet atlası");
            Assert.That(tex.width, Is.EqualTo(1024), "silüet atlası genişlik");
            Assert.That(tex.height, Is.EqualTo(512), "silüet atlası yükseklik");
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
