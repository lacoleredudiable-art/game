using System;
using System.Collections.Generic;
using Dovus.Core.Tuning;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// Cümle gramer durum makinesi. Zaman parametre olarak gelir (dünya saati).
    /// Kombo tablosu yok — her dizi §3/§5 kurallarından türetilir.
    /// </summary>
    public sealed class SentenceEngine
    {
        readonly SentenceTuning _tuning;
        readonly List<SentenceWord> _words = new List<SentenceWord>(4);
        readonly List<CompletedSentence> _history = new List<CompletedSentence>();

        double _remainingWindowMs;
        double _armedWindowMs;
        double _remainingRecoveryMs;
        double _appliedWorldMs;
        int _lastWordDwellStacks;

        public SentenceEngine(SentenceTuning? tuning = null)
        {
            _tuning = tuning ?? new SentenceTuning();
            State = new SentenceState();
        }

        public SentenceState State { get; }

        public IReadOnlyList<CompletedSentence> History => _history;

        public event Action<CompletedSentence>? SentenceCompleted;

        /// <summary>Noktaya dokunuş. Geçersiz nokta yok sayılır.</summary>
        public void OnDotTouched(int dot, double worldTimeMs)
        {
            CatchUp(worldTimeMs);
            if (!RuneInfo.TryFromDot(dot, out Rune rune))
                return;

            // Recovering: yeni fiil kilidi keser (§5) — BeginFresh kalan süreyi sıfırlar.
            if (State.Phase == SentencePhase.Resolved || State.Phase == SentencePhase.Aborted
                || State.Phase == SentencePhase.Recovering)
                BeginFresh();

            if (State.Phase == SentencePhase.Idle)
            {
                StartVerb(rune);
                return;
            }

            // Building: kapasite doluysa önce kapat, sonra yeni fiil
            if (_words.Count >= _tuning.MaxSentenceDots)
            {
                ResolveWithClosing();
                BeginFresh();
                StartVerb(rune);
                return;
            }

            AppendAdjective(rune);
        }

        /// <summary>
        /// Noktada bekleme eşiği doldu (§3: dwellMs, en fazla dwellMaxStacks).
        /// Sıfat yuvası harcamaz; yalnızca son kelimeyi yoğunlaştırır.
        /// </summary>
        public void OnDwell(double worldTimeMs)
        {
            CatchUp(worldTimeMs);
            if (State.Phase != SentencePhase.Building || _words.Count == 0)
                return;

            if (_lastWordDwellStacks >= _tuning.DwellMaxStacks)
                return;

            _lastWordDwellStacks++;
            int last = _words.Count - 1;
            SentenceWord w = _words[last];
            _words[last] = new SentenceWord(w.Rune, w.JumpFromPrevious, _lastWordDwellStacks);
            FreezeWindowForDwell();
            PublishState();
        }

        /// <summary>
        /// Erken kapanış (§5): cümle kurulurken merkeze basmak, o uzunluğun ödemesini alır.
        /// Dodge'un tersi — merkez öder, dodge batırır. Idle/Recovering'de sessizce hiçbir şey
        /// yapmaz; düz vuruşu girdi katmanı OnDotTouched + Commit ile kurar.
        /// </summary>
        public void Commit()
        {
            if (State.Phase != SentencePhase.Building || _words.Count == 0)
                return;

            ResolveWithClosing();
        }

        /// <summary>Dünya zamanı ilerlemesi; pencere bitince kapanış üretir.</summary>
        public void Tick(double dtMs)
        {
            if (dtMs < 0)
                dtMs = 0;
            CatchUp(_appliedWorldMs + dtMs);
        }

        /// <summary>
        /// Pencereyi mutlak dünya saatine hizalar. OnDotTouched/OnDwell Tick'i beklemeden
        /// (kare yuvarlaması ~16 ms) kalan süreyi yer. Aynı ana kadar zaten uygulanmışsa no-op.
        /// </summary>
        void CatchUp(double worldTimeMs)
        {
            double dt = worldTimeMs - _appliedWorldMs;
            if (dt > 0)
                ApplyTime(dt);
            if (worldTimeMs > _appliedWorldMs)
                _appliedWorldMs = worldTimeMs;
        }

        void ApplyTime(double dtMs)
        {
            if (State.Phase == SentencePhase.Recovering)
            {
                _remainingRecoveryMs -= dtMs;
                if (_remainingRecoveryMs <= 0)
                {
                    BeginFresh();
                    return;
                }

                State.RemainingRecoveryMs = _remainingRecoveryMs;
                return;
            }

            if (State.Phase != SentencePhase.Building || _words.Count == 0)
                return;

            // Max cümle: uzatma penceresi yok; zaten çözülmüş olmalı
            if (_words.Count >= _tuning.MaxSentenceDots)
                return;

            _remainingWindowMs -= dtMs;
            if (_remainingWindowMs <= 0)
            {
                _remainingWindowMs = 0;
                ResolveWithClosing();
                return;
            }

            PublishState();
        }

        /// <summary>
        /// Dodge veya vurulma. Building'de yatırım batar (kapanış ve ödül yok); Recovering'de
        /// yalnızca kilidi keser — ödenmiş kapanış (History ve LastClosing) yerinde kalır (§5).
        /// </summary>
        public void Abort()
        {
            if (State.Phase == SentencePhase.Recovering)
            {
                BeginFresh();
                return;
            }

            if (State.Phase != SentencePhase.Building || _words.Count == 0)
            {
                BeginFresh();
                State.Phase = SentencePhase.Idle;
                PublishState();
                return;
            }

            var completed = new CompletedSentence(
                verb: _words[0].Rune,
                words: SnapshotWords(),
                phase: SentencePhase.Aborted,
                closing: null);

            Finish(completed);
        }

        void StartVerb(Rune rune)
        {
            _words.Clear();
            _lastWordDwellStacks = 0;
            _words.Add(new SentenceWord(rune, JumpKind.None, 0));
            State.Phase = SentencePhase.Building;
            State.LastClosing = null;
            ArmWindowAfterHit();
            PublishState();
        }

        void AppendAdjective(Rune rune)
        {
            Rune previous = _words[_words.Count - 1].Rune;
            JumpKind jump = PentagonLayout.ClassifyJump((int)previous, (int)rune);
            _lastWordDwellStacks = 0;
            _words.Add(new SentenceWord(rune, jump, 0));

            if (_words.Count >= _tuning.MaxSentenceDots)
            {
                ResolveWithClosing();
                return;
            }

            ArmWindowAfterHit();
            PublishState();
        }

        void ArmWindowAfterHit()
        {
            // Vuruş sonrası uzatma penceresi — T1: CancelWindowForDots(noktaSayısı)
            _armedWindowMs = _tuning.CancelWindowForDots(_words.Count);
            _remainingWindowMs = _armedWindowMs;
        }

        /// <summary>
        /// §3: bekleme parmağın süresini öder, iptal penceresini değil — bir yığın dolunca
        /// pencere bekleme başlamadan önceki hâline döner. Donmasaydı iki yığın (2×220 ms)
        /// fiilin 420 ms'lik penceresine sığmaz, dwellMaxStacks = 2 ulaşılamaz olurdu.
        /// </summary>
        void FreezeWindowForDwell()
        {
            _remainingWindowMs = Math.Min(_remainingWindowMs + _tuning.DwellMs, _armedWindowMs);
        }

        void ResolveWithClosing()
        {
            if (_words.Count == 0)
                return;

            Rune last = _words[_words.Count - 1].Rune;
            float effect = _tuning.StepForDots(_words.Count).TotalEffect;
            var closing = new ClosingHit(last, effect, _words.Count);

            var completed = new CompletedSentence(
                verb: _words[0].Rune,
                words: SnapshotWords(),
                phase: SentencePhase.Resolved,
                closing: closing);

            Finish(completed);
        }

        void Finish(CompletedSentence completed)
        {
            _history.Add(completed);

            // Kapanış üreten HER yol (Commit, dördüncü nokta, pencere zaman aşımı) toparlanma
            // kilidine girer; süre §5 tablosundan gelir. Kayıttaki faz Resolved kalır — geçmiş
            // ve ödül okunuyor.
            if (completed.Closing.HasValue)
            {
                _remainingRecoveryMs =
                    _tuning.StepForDots(completed.Closing.Value.DotCount).RecoverySec * 1000.0;
                State.Phase = SentencePhase.Recovering;
            }
            else
            {
                _remainingRecoveryMs = 0;
                State.Phase = completed.Phase;
            }

            State.LastClosing = completed.Closing;
            State.RemainingWindowMs = 0;
            State.ArmedWindowMs = 0;
            State.RemainingRecoveryMs = _remainingRecoveryMs;
            State.Verb = completed.Verb;
            State.Words = completed.Words;
            _remainingWindowMs = 0;
            _armedWindowMs = 0;
            SentenceCompleted?.Invoke(completed);
        }

        void BeginFresh()
        {
            _words.Clear();
            _lastWordDwellStacks = 0;
            _remainingWindowMs = 0;
            _armedWindowMs = 0;
            _remainingRecoveryMs = 0;
            State.Phase = SentencePhase.Idle;
            State.Verb = null;
            State.Words = Array.Empty<SentenceWord>();
            State.RemainingWindowMs = 0;
            State.ArmedWindowMs = 0;
            State.RemainingRecoveryMs = 0;
            // LastClosing bilinçli korunur — son ödeme okunabilsin
        }

        SentenceWord[] SnapshotWords()
        {
            var copy = new SentenceWord[_words.Count];
            _words.CopyTo(copy);
            return copy;
        }

        void PublishState()
        {
            State.Verb = _words.Count > 0 ? _words[0].Rune : null;
            State.Words = SnapshotWords();
            State.RemainingWindowMs = _remainingWindowMs;
            State.ArmedWindowMs = _armedWindowMs;
        }
    }
}
