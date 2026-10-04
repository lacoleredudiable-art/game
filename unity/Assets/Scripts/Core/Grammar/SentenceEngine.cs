using System;
using System.Collections.Generic;
using Dovus.Core.Casting;
using Dovus.Core.Element;
using Dovus.Core.Input;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// C├╝mle gramer durum makinesi. Zaman parametre olarak gelir (d├╝nya saati).
    /// Kombo tablosu yok ÔÇö her dizi ┬ğ3/┬ğ5 kurallar─▒ndan t├╝retilir.
    /// </summary>
    public sealed class SentenceEngine
    {
        readonly SentenceTuning _tuning;
        RuneLoadout _loadout;
        readonly List<SentenceWord> _words = new List<SentenceWord>(4);
        readonly List<CompletedSentence> _history = new List<CompletedSentence>();

        double _remainingWindowMs;
        double _armedWindowMs;
        double _remainingRecoveryMs;
        double _appliedWorldMs;
        int _lastWordDwellStacks;

        public SentenceEngine(SentenceTuning? tuning = null, RuneLoadout? loadout = null)
        {
            _tuning = tuning ?? new SentenceTuning();
            _loadout = loadout ?? RuneLoadout.Sequential;
            State = new SentenceState();
        }

        public SentenceState State { get; }
        public RuneLoadout Loadout => _loadout;

        /// <summary>Build se├ğimi yaln─▒z ├ğizim yokken de─şi┼şir; event abonelikleri korunur.</summary>
        public bool TrySetLoadout(RuneLoadout loadout)
        {
            if (loadout == null || State.Phase == SentencePhase.Building)
                return false;
            BeginFresh();
            _loadout = loadout;
            PublishState();
            return true;
        }

        public IReadOnlyList<CompletedSentence> History => _history;

        public event Action<CompletedSentence>? SentenceCompleted;

        /// <summary>
        /// Merkez d├╝z vuru┼ş. Slot s─▒ras─▒na bakmaz: r├╝n 1 hangi yuvada olursa olsun
        /// fiil Sald─▒r─▒'d─▒r. Slot 0 ekran noktas─▒ de─şildir.
        /// </summary>
        public bool BeginBasicStrike(int runeId, double worldTimeMs)
        {
            CatchUp(worldTimeMs);
            if (!RuneInfo.TryFromId(runeId, out Rune rune))
                return false;

            if (State.Phase == SentencePhase.Resolved || State.Phase == SentencePhase.Aborted
                || State.Phase == SentencePhase.Recovering)
                BeginFresh();

            if (State.Phase != SentencePhase.Idle)
                return false;

            StartVerb(0, rune);
            return true;
        }

        /// <summary>Noktaya dokunu┼ş. Ge├ğersiz nokta yok say─▒l─▒r.</summary>
        public void OnDotTouched(int dot, double worldTimeMs)
        {
            CatchUp(worldTimeMs);
            if (!RuneInfo.TryFromDot(dot, _loadout, out Rune rune))
                return;

            // Recovering: yeni fiil kilidi keser (┬ğ5) ÔÇö BeginFresh kalan s├╝reyi s─▒f─▒rlar.
            if (State.Phase == SentencePhase.Resolved || State.Phase == SentencePhase.Aborted
                || State.Phase == SentencePhase.Recovering)
                BeginFresh();

            if (State.Phase == SentencePhase.Idle)
            {
                StartVerb(dot, rune);
                return;
            }

            // Building: kapasite doluysa ├Ânce kapat, sonra yeni fiil
            if (_words.Count >= _tuning.MaxSentenceDots)
            {
                ResolveWithClosing();
                BeginFresh();
                StartVerb(dot, rune);
                return;
            }

            AppendAdjective(dot, rune);
        }

        /// <summary>
        /// Noktada bekleme e┼şi─şi doldu (┬ğ3: dwellMs, en fazla dwellMaxStacks).
        /// S─▒fat yuvas─▒ harcamaz; yaln─▒zca son kelimeyi yo─şunla┼şt─▒r─▒r.
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
            _words[last] = new SentenceWord(w.Slot, w.Rune, w.JumpFromPrevious, _lastWordDwellStacks);
            FreezeWindowForDwell();
            PublishState();
        }

        /// <summary>
        /// Erken kapan─▒┼ş (┬ğ5): c├╝mle kurulurken merkeze basmak, o uzunlu─şun ├Âdemesini al─▒r.
        /// Dodge'un tersi ÔÇö merkez ├Âder, dodge bat─▒r─▒r. Idle/Recovering'de sessizce hi├ğbir ┼şey
        /// yapmaz; d├╝z vuru┼şu girdi katman─▒ OnDotTouched + Commit ile kurar.
        /// </summary>
        public void Commit()
        {
            if (State.Phase != SentencePhase.Building || _words.Count == 0)
                return;

            ResolveWithClosing();
        }

        /// <summary>D├╝nya zaman─▒ ilerlemesi; pencere bitince kapan─▒┼ş ├╝retir.</summary>
        public void Tick(double dtMs)
        {
            if (dtMs < 0)
                dtMs = 0;
            CatchUp(_appliedWorldMs + dtMs);
        }

        /// <summary>
        /// Pencereyi mutlak d├╝nya saatine hizalar. OnDotTouched/OnDwell Tick'i beklemeden
        /// (kare yuvarlamas─▒ ~16 ms) kalan s├╝reyi yer. Ayn─▒ ana kadar zaten uygulanm─▒┼şsa no-op.
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

            // Max c├╝mle: uzatma penceresi yok; zaten ├ğ├Âz├╝lm├╝┼ş olmal─▒
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
        /// Dodge veya vurulma. Building'de yat─▒r─▒m batar (kapan─▒┼ş ve ├Âd├╝l yok); Recovering'de
        /// yaln─▒zca kilidi keser ÔÇö ├Âdenmi┼ş kapan─▒┼ş (History ve LastClosing) yerinde kal─▒r (┬ğ5).
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

        void StartVerb(int slot, Rune rune)
        {
            _words.Clear();
            _lastWordDwellStacks = 0;
            _words.Add(new SentenceWord(slot, rune, JumpKind.None, 0));
            State.Phase = SentencePhase.Building;
            State.LastClosing = null;
            ArmWindowAfterHit();
            PublishState();
        }

        void AppendAdjective(int slot, Rune rune)
        {
            int previousSlot = _words[_words.Count - 1].Slot;
            JumpKind jump = HexagonLayout.ClassifyJump(previousSlot, slot);
            _lastWordDwellStacks = 0;
            _words.Add(new SentenceWord(slot, rune, jump, 0));

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
            // Vuru┼ş sonras─▒ uzatma penceresi ÔÇö T1: CancelWindowForDots(noktaSay─▒s─▒)
            _armedWindowMs = _tuning.CancelWindowForDots(_words.Count);
            _remainingWindowMs = _armedWindowMs;
        }

        /// <summary>
        /// ┬ğ3: bekleme parma─ş─▒n s├╝resini ├Âder, iptal penceresini de─şil ÔÇö bir y─▒─ş─▒n dolunca
        /// pencere bekleme ba┼şlamadan ├Ânceki h├óline d├Âner. Donmasayd─▒ iki y─▒─ş─▒n (2├ù220 ms)
        /// fiilin 420 ms'lik penceresine s─▒─şmaz, dwellMaxStacks = 2 ula┼ş─▒lamaz olurdu.
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

            // Kapan─▒┼ş ├╝reten HER yol (Commit, d├Ârd├╝nc├╝ nokta, pencere zaman a┼ş─▒m─▒) toparlanma
            // kilidine girer; s├╝re ┬ğ5 tablosundan gelir. Kay─▒ttaki faz Resolved kal─▒r ÔÇö ge├ğmi┼ş
            // ve ├Âd├╝l okunuyor.
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
            // LastClosing bilin├ğli korunur ÔÇö son ├Âdeme okunabilsin
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