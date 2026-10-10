using System.Collections;
using Dovus.Core.Presentation;
using Dovus.Game.Vfx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>Ejder silüeti / rün glifi: skill_anim giriş silüeti ve gerçek atlaslar (Resources/Vfx/Ejder).</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float WingLifeSec = 0.3f;
        const string SilhouetteAtlasName = "Ejder_Siluet_Atlas";
        const string GlyphAtlasName = "Ejder_Glif_Atlas";
        const float StTol = 1e-5f;

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
    }
}
