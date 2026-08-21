using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// T7.1: bağlantı katmanı denetimi. Bunlar Core-only testler; ManifestationDirector Unity/Game
/// katmanında olduğu için burada derlenmez — bu testler Director'ın dayandığı Core sözleşmesini
/// (LivingEffect.SetWords/ArmClosing/Tick) doğrudan doğrular. Play mode doğrulaması ayrıca gerekir.
/// </summary>
[TestFixture]
public class ManifestationWiringTests
{
    ManifestationTuning Tuning() => new();

    // Kriter (a): 4 kelimeli cümlede son sıfat hedef silüete işliyor.
    [Test]
    public void FourthAdjective_ReachesLivingEffectTarget_ViaCompletedSentenceWords()
    {
        var engine = new SentenceEngine(new SentenceTuning());
        var manifest = Tuning();

        CompletedSentence? completed = null;
        engine.SentenceCompleted += s => completed = s;

        engine.OnDotTouched(5, 0);    // fiil: SARSINTI
        engine.OnDotTouched(1, 50);   // sıfat 1: İĞNE
        engine.OnDotTouched(2, 100);  // sıfat 2: SÜRÜ
        engine.OnDotTouched(4, 150);  // sıfat 3 (4. kelime): ZEHİR — dokunuş anında çözülür

        Assert.That(completed, Is.Not.Null, "4. nokta cümleyi dokunuş anında çözmeli");
        Assert.That(completed!.Words, Has.Count.EqualTo(4));

        // Director'ın 4. dokunuştan önceki karelerde SyncFromSentence ile kurduğu gibi:
        // etki ilk üç kelimeyle yaşıyor olsun.
        var firstThree = new List<SentenceWord>
        {
            completed.Words[0], completed.Words[1], completed.Words[2]
        };
        var effect = new LivingEffect(completed.Verb, 0, 0, 0, 1, firstThree, manifest);
        EffectSilhouette targetWithThree = effect.Target;

        // OnSentenceCompleted'ın yapması gereken: kapanış kurulmadan önce tam listeyi iletmek.
        effect.SetWords(completed.Words);

        EffectSilhouette expectedWithFour = SilhouetteBuilder.FromWords(completed.Words, manifest);
        Assert.That(effect.Target.Spread, Is.EqualTo(expectedWithFour.Spread).Within(1e-5f));
        Assert.That(effect.Target.Spread, Is.Not.EqualTo(targetWithThree.Spread).Within(1e-4f),
            "4. kelime (ZEHİR) hedef silüete işlemeli");
    }

    // Kriter (a) uzantısı: LivingEffect.SetWords, AwaitingClosing fazında da kabul etmeli
    // (Director ArmClosing'i SetWords'ten önce çağırırsa bile sıra bozulmamalı).
    [Test]
    public void SetWords_AcceptedDuringAwaitingClosing()
    {
        var manifest = Tuning();
        var words = new[] { new SentenceWord(Rune.Sarsinti, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Sarsinti, 0, 0, 0, 1, words, manifest);

        effect.ArmClosing(new ClosingHit(Rune.Zehir, 7f, 4));
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.AwaitingClosing));

        var fourWords = new[]
        {
            new SentenceWord(Rune.Sarsinti, JumpKind.None, 0),
            new SentenceWord(Rune.Igne, JumpKind.Long, 0),
            new SentenceWord(Rune.Suru, JumpKind.Short, 0),
            new SentenceWord(Rune.Zehir, JumpKind.Short, 0)
        };
        effect.SetWords(fourWords);

        EffectSilhouette expected = SilhouetteBuilder.FromWords(fourWords, manifest);
        Assert.That(effect.Target.Spread, Is.EqualTo(expected.Spread).Within(1e-5f));
    }

    // Kriter (b): menzilini bitirmiş etki cümle kapanmadan ölmüyor ve ArmClosing yutulmuyor.
    [Test]
    public void EffectPastRange_StaysAliveUntilSentenceCloses_AndAcceptsArmClosing()
    {
        var manifest = Tuning();
        var words = new[] { new SentenceWord(Rune.Igne, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Igne, 0, 0, 0, 1, words, manifest);

        // İĞNE: 12 m / 16 m/s = 0.75 sn'de menzili bitirir. 4 noktalı cümle §5'e göre en az
        // 1.20 sn + toparlanma + sessizlik sürer — yani menzil cümleden önce dolar.
        // 2 sn'lik tick, menzili bitirmiş etkinin "beklediğini" gösterir (MaxHoldPastRangeSec
        // güvenlik payının çok altında).
        for (int i = 0; i < 40; i++)
            effect.Tick(0.05f);

        Assert.That(effect.Travel, Is.GreaterThanOrEqualTo(effect.MaxRange));
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Traveling),
            "menzilini bitirmiş etki cümle kapanmadan sönmemeli");

        effect.ArmClosing(new ClosingHit(Rune.Igne, 1f, 1));
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.AwaitingClosing),
            "ArmClosing, menzil dolduğu için Fading'e düşmüş bir etkide yutulmamalı");

        effect.FireClosingBang();
        Assert.That(effect.PaidClosing, Is.True);
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Banging));
    }

    // Kriter (c): Abort hâlâ kapanış üretmiyor — menzili bitirmiş/bekleyen bir etkide de.
    [Test]
    public void Abort_PastRange_StillProducesNoClosingBang()
    {
        var manifest = Tuning();
        var words = new[] { new SentenceWord(Rune.Sarsinti, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Sarsinti, 0, 0, 0, 1, words, manifest);

        for (int i = 0; i < 40; i++)
            effect.Tick(0.05f); // menzili geçmiş, bekliyor

        effect.Abort();
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Fading));

        effect.FireClosingBang();
        Assert.That(effect.PaidClosing, Is.False);
        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Fading));
    }

    // Güvenlik payı: cümle normalde hep kapanır ama motor Core'da yalıtık test edildiğinde
    // MaxHoldPastRangeSec'in kendisi de çalışmalı (aksi hâlde sonsuza asılı kalır).
    [Test]
    public void EffectPastRange_EventuallyFadesViaSafetyNet_IfNeverClosed()
    {
        var manifest = Tuning();
        manifest.MaxHoldPastRangeSec = 0.2f; // testte hızlandırılmış güvenlik payı
        var words = new[] { new SentenceWord(Rune.Igne, JumpKind.None, 0) };
        var effect = new LivingEffect(Rune.Igne, 0, 0, 0, 1, words, manifest);

        // İĞNE menzili 0.75 sn'de dolar; +0.2 sn güvenlik payı = ~0.95 sn'de sönme başlamalı.
        for (int i = 0; i < 22; i++)
            effect.Tick(0.05f); // 1.1 sn — fade tetiklenmiş ama henüz FadeDurationSec dolmamış

        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Fading),
            "cümle hiç kapanmazsa güvenlik payı sonunda sönmeye başlamalı");

        for (int i = 0; i < 40; i++)
            effect.Tick(0.05f); // yeterince zaman: sönme tamamlanmalı

        Assert.That(effect.Phase, Is.EqualTo(LivingEffectPhase.Dead));
    }
}
