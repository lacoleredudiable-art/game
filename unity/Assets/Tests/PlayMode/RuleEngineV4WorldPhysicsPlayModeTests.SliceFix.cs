using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Dilim düzeltmeleri (v4/dilim-duzeltme): glif eşlemesi, statik işaret, ölçek, girdi.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        /// <summary>Editörde SessionState'e klasör yazılırsa ekran (overlay UI dahil) kaydedilir.</summary>
        const string CaptureDirSessionKey = "Dovus.RuleEngineV4.CaptureDir";
        /// <summary>Duvar nişanında oyuncu 18 m çarpışma yarıçapının bu kadar içinde durur.</summary>
        const float WallAimStandoffM = 5f;

        static readonly int[] GlyphSliceRunes = { 1, 2, 3, 4, 6, 9 };
        static readonly int[] GlyphSliceVerbCells = { 0, 10, 8, 11, 6, 9 };
        static readonly int[] GlyphSliceAdjectiveCells = { 22, 21, 18, 15, 13, 23 };

        [UnityTest]
        public IEnumerator Glyph_SliceRunes_SpriteRectIsMappedCell()
        {
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            for (int i = 0; i < GlyphSliceRunes.Length; i++)
            {
                int rune = GlyphSliceRunes[i];
                runes.Play(rune, rune, Color.white);
                Texture tex = runes.VerbSprite.texture;
                Assert.That(runes.VerbVisible, Is.True, $"fiil {rune} görünmüyor");
                Assert.That(runes.AdjectiveVisible, Is.True, $"sıfat {rune} görünmüyor");
                Assert.That(runes.VerbSprite.rect, Is.EqualTo(GlyphCellRect(tex, GlyphSliceVerbCells[i])),
                    $"fiil {rune} → hücre {GlyphSliceVerbCells[i]}");
                Assert.That(runes.AdjectiveSprite.rect, Is.EqualTo(GlyphCellRect(tex, GlyphSliceAdjectiveCells[i])),
                    $"sıfat {rune} → hücre {GlyphSliceAdjectiveCells[i]}");
            }
        }

        [UnityTest]
        public IEnumerator Glyph_MissingName_HidesLetter_WarnsOnce()
        {
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            string order = Resources.Load<TextAsset>(RuleVfxDefaults.GlyphOrderResource).text;
            const string onlyFirst = "{\"rune_glyphs\":{\"verbs\":{\"1\":\"AK\"},\"adjectives\":{\"1\":\"VORN\"}}}";
            runes.GlyphMap = RuleEngineV4GlyphMap.FromJson(onlyFirst, order);

            var warnings = new List<string>();
            void Collect(string msg, string stack, LogType type)
            {
                if (type == LogType.Warning && msg.Contains("[DragonRuneFlash]"))
                    warnings.Add(msg);
            }
            Application.logMessageReceived += Collect;
            try
            {
                runes.Play(1, 2, Color.white);
                Assert.That(runes.VerbVisible, Is.True, "adı olan fiil gizlendi");
                Assert.That(runes.AdjectiveVisible, Is.False, "adı olmayan sıfat görünüyor");
                runes.Play(1, 2, Color.white);
                runes.Play(1, 2, Color.white);
                Assert.That(runes.AdjectiveVisible, Is.False, "adı olmayan sıfat görünüyor (tekrar)");
            }
            finally
            {
                Application.logMessageReceived -= Collect;
                runes.GlyphMap = null;
            }
            Assert.That(warnings, Has.Count.EqualTo(1), string.Join("\n", warnings));
            Assert.That(warnings[0], Does.Contain(RuleEngineV4GlyphDefaults.AdjectivePrefix + 2));
        }

        [UnityTest]
        public IEnumerator Glyph_Flash_ShevSkar_SolVis_Capture()
        {
            yield return null;
            DragonRuneFlashView runes = RuneFlash();
            runes.Play(3, 3, Color.white);
            yield return new WaitForSecondsRealtime(VfxPlanDefaults.RuneSnapSec);
            Assert.That(runes.VerbVisible && runes.AdjectiveVisible, "SHEV-SKAR görünmüyor");
            yield return CaptureScreen("glif_SHEV_SKAR.png");

            runes.Play(2, 2, Color.white);
            yield return new WaitForSecondsRealtime(VfxPlanDefaults.RuneSnapSec);
            Assert.That(runes.VerbVisible && runes.AdjectiveVisible, "SOL-VIS görünmüyor");
            yield return CaptureScreen("glif_SOL_VIS.png");
        }

        [UnityTest]
        public IEnumerator MarkOnly_Column_NineNineThenThreeNine_PullsToPoint()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 center = new(col.XM, 0f, col.ZM);
            Transform column = ArenaChildren("Columns")[0];

            Place(_player, center - Vector3.right * (col.RadiusM + 3f));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Physics.SyncTransforms();
            yield return Cast(9, 9);

            RuleEngineV4SurfaceMark mark = SingleSurfaceMark(column);
            Vector3 point = mark.transform.position;
            Assert.That(Flat(point - center).magnitude, Is.EqualTo(col.RadiusM).Within(PosTolM), "işaret sütun yüzeyinde değil");
            Assert.That(Flat(point - center).normalized.x, Is.LessThan(-0.99f), "işaret çarpma noktasında değil");

            yield return PullToSurfaceMark(mark, center - Vector3.right * (col.RadiusM + 5f), Vector3.forward);
        }

        [UnityTest]
        public IEnumerator MarkOnly_Wall_NineNineThenThreeNine_PullsToPoint()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            Transform[] wall = ArenaChildren("OuterWallCollision");
            float r = ArenaSpec.OuterWallCollisionRadiusM;

            Place(_player, Vector3.right * (r - WallAimStandoffM));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Physics.SyncTransforms();
            yield return Cast(9, 9);

            RuleEngineV4SurfaceMark mark = SingleSurfaceMark(null);
            Assert.That(wall.Contains(mark.Surface.transform), "işaret duvar segmentinde değil: " + mark.Surface.name);
            Assert.That(Mathf.Abs(mark.transform.position.z), Is.LessThan(PosTolM), "işaret nişan çizgisinde değil");
            Assert.That(mark.transform.position.x, Is.LessThan(r), "işaret 18 m duvarın dışında");

            yield return PullToSurfaceMark(mark, Vector3.right * (r - WallAimStandoffM - 1f), Vector3.forward);
        }

        [UnityTest]
        public IEnumerator MarkOnly_VisualRing_NotMarkable()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            GameObject.Find(ArenaRoot).transform.Find("OuterWallCollision").gameObject.SetActive(false);
            float r = ArenaSpec.OuterWallCollisionRadiusM;
            Place(_player, Vector3.right * (r - 3f));
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Place(_allies[0], _player.position - Vector3.right * 2f);
            Physics.SyncTransforms();

            yield return Cast(9, 9);

            Assert.That(_cast.LastSurfaceAim.IsValid, Is.False, "18–22 m halkasına nişan alındı");
            Assert.That(_cast.Marks.SurfaceMarkCount, Is.EqualTo(0), "halkaya işaret kondu");
            Assert.That(Object.FindObjectsByType<RuleEngineV4SurfaceMark>(), Is.Empty, "halkada işaret nesnesi var");
            Assert.That(_cast.LastFocus, Is.SameAs(_allies[0]), "işaret menzildeki dosta gitmedi");
        }

        [UnityTest]
        public IEnumerator MarkOnly_DamageHealAndAutoTarget_NeverPickColumnOrWall()
        {
            FreezeBoss();
            Equip(Kilic);
            ParkAll();
            RuleEngineV4SliceArenaColumn col = ArenaSpec.Columns[0];
            Vector3 center = new(col.XM, 0f, col.ZM);
            Vector3 stand = center - Vector3.right * (col.RadiusM + 3f);
            Place(_player, stand);
            _player.rotation = Quaternion.LookRotation(Vector3.right);
            Physics.SyncTransforms();
            yield return Cast(9, 9);
            SingleSurfaceMark(ArenaChildren("Columns")[0]);

            foreach (TargetableHost t in _md._liveTargetables.Live)
                Assert.That(IsMarkOnly(t.transform), Is.False, $"otomatik hedef adayı statik yüzey: {t.name}");

            var picked = new List<string>();
            foreach (int verb in new[] { 1, 2 })
            foreach (int adj in GlyphSliceRunes)
            {
                Place(_player, stand);
                _player.rotation = Quaternion.LookRotation(Vector3.right);
                Place(_minions[0].transform, stand - Vector3.right * 2f);
                Place(_allies[0], stand - Vector3.forward * 2f);
                Physics.SyncTransforms();
                _label = $"{verb}-{adj}";
                yield return Cast(verb, adj);
                if (IsMarkOnly(_cast.LastFocus))
                    picked.Add($"{verb}-{adj} → {_cast.LastFocus.name}");
            }
            Assert.That(picked, Is.Empty, "hasar/şifa statik yüzeyi seçti: " + string.Join(", ", picked));
        }

        IEnumerator PullToSurfaceMark(RuleEngineV4SurfaceMark mark, Vector3 from, Vector3 facing)
        {
            Vector3 point = mark.transform.position;
            Place(_player, from);
            _player.rotation = Quaternion.LookRotation(facing);
            Physics.SyncTransforms();
            float before = Flat(point - _player.position).magnitude;

            yield return Cast(3, 9);
            yield return CaptureScreen($"cekme_3-9_{mark.Surface.name}.png");

            Assert.That(_cast.LastFocus, Is.SameAs(mark.transform), "3-9 yüzey işaretini hedeflemedi");
            float radius = _physics.BodyRadius(_player);
            float after = Flat(point - _player.position).magnitude;
            // v4 dash: katı yüzeyden gövde + iç pay önce biter; kalıp koşucusu varışa gövde + durma payı bırakır.
            float contact = radius + KinematicMotorControllerDefaults.MoveStopInsetM
                + radius + MotionTemplateBodyHostDefaults.DefaultStopGapM;
            Assert.That(before - after, Is.GreaterThan(1f), $"3-9 çekmedi ({before:0.00} → {after:0.00} m)");
            Assert.That(after, Is.InRange(radius - PosTolM, contact + PosTolM),
                $"oyuncu işaret noktasına gelmedi (mesafe {after:0.00} m, dash teması {contact:0.00} m)");
        }

        RuleEngineV4SurfaceMark SingleSurfaceMark(Transform expectedSurface)
        {
            RuleEngineV4SurfaceMark[] marks = Object.FindObjectsByType<RuleEngineV4SurfaceMark>();
            Assert.That(marks, Has.Length.EqualTo(1), "yüzey işareti sayısı");
            Assert.That(_cast.Marks.SurfaceMarkCount, Is.EqualTo(1), "kayıtlı yüzey işareti");
            RuleEngineV4SurfaceMark mark = marks[0];
            Assert.That(mark.Surface, Is.Not.Null, "işaretin yüzeyi yok");
            if (expectedSurface != null)
                Assert.That(mark.Surface.transform, Is.SameAs(expectedSurface), "işaret yanlış yüzeyde");
            var body = mark.GetComponent<RuleEngineV4PhysicsBodyHost>();
            Assert.That(body != null && body.WeightTier == RuleEngineV4WeightTier.Anchored, "işaret çapalı değil");
            Assert.That(mark.GetComponent<TargetableHost>(), Is.Null, "yüzey işareti hedeflenebilir");
            return mark;
        }

        static bool IsMarkOnly(Transform t) =>
            t != null
            && (t.GetComponentInParent<RuleEngineV4MarkOnlySurface>() != null
                || t.GetComponent<RuleEngineV4SurfaceMark>() != null);

        DragonRuneFlashView RuneFlash()
        {
            var runes = _player.GetComponentInChildren<DragonRuneFlashView>(true);
            Assert.That(runes, Is.Not.Null, "DragonRuneFlashView");
            return runes;
        }

        /// <summary>0 = sol üst; texture y alttan.</summary>
        static Rect GlyphCellRect(Texture atlas, int cell)
        {
            int cols = RuleVfxArtDefaults.GlyphCols;
            int rows = RuleVfxArtDefaults.GlyphRows;
            float cw = atlas.width / cols;
            float ch = atlas.height / rows;
            return new Rect(cell % cols * cw, (rows - 1 - cell / cols) * ch, cw, ch);
        }

        static string SessionCaptureDir()
        {
#if UNITY_EDITOR
            string dir = UnityEditor.SessionState.GetString(CaptureDirSessionKey, string.Empty);
            return string.IsNullOrEmpty(dir) ? null : dir;
#else
            return null;
#endif
        }

        IEnumerator CaptureScreen(string fileName)
        {
            string dir = SessionCaptureDir();
            if (dir == null)
            {
                yield return CaptureIfRequested(fileName);
                yield break;
            }
            yield return new WaitForEndOfFrame();
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, fileName), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
