using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
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
