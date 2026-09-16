using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json "chain_mechanics": ardışık cast'lerin pattern eşleşmesi,
    /// Links artan bonusu ve tam zincirde Finisher. Kombo tablosu DEĞİL — katalog
    /// SkillMotor.Chains'ten gelir; pattern string'i ("1-X-X-X-X-X") JSON'dan parse edilir.
    /// X = zincirin çapa elementiyle aynı (ulti notundaki X-X-X-X ile aynı dil:
    /// "X-X-X-X formatı Cursor için dizi olarak yazıldı" → aynı element tekrarı).
    /// ManifestationDirector'a bağlanmaz (Görev 5 yasak).
    /// </summary>
    public sealed class ChainDirector
    {
        readonly List<ChainNode> _catalog;
        readonly ChainRules _rules;
        readonly List<int> _history = new();

        double _lastCastMs = double.NegativeInfinity;
        double _penaltyUntilMs;
        ChainNode? _active;
        int _linkCount;
        string _lastFinisher = string.Empty;

        public ChainDirector(IReadOnlyList<ChainNode> chains, in ChainRules rules)
        {
            _catalog = new List<ChainNode>(chains ?? Array.Empty<ChainNode>());
            _rules = rules;
        }

        public ChainDirector(IReadOnlyList<ChainNode> chains)
            : this(chains, ChainRules.DefaultFromSpec) { }

        public ChainNode? Active => _active;
        public int LinkCount => _linkCount;
        public float LinkBonus =>
            _active != null && _linkCount > 0 && _linkCount <= _active.Value.Links.Length
                ? _active.Value.Links[_linkCount - 1]
                : 1f;
        public string LastFinisher => _lastFinisher;
        public bool InBreakPenalty(double worldMs) => worldMs < _penaltyUntilMs;

        /// <summary>
        /// Skill kapanışı (element dot 1..6). Window aşılırsa veya pattern kırılırsa zincir
        /// düşer (break_penalty); aksi halde Links sırasıyla ilerler. Finisher yalnızca
        /// pattern'in tüm slotları dolduğunda.
        /// </summary>
        public ChainStepResult RegisterCast(int elementDot, double worldMs)
        {
            _lastFinisher = string.Empty;

            if (elementDot < 1)
                return ChainStepResult.None;

            if (worldMs < _penaltyUntilMs)
                return ChainStepResult.None;

            if (_history.Count > 0 &&
                (worldMs - _lastCastMs) / 1000.0 > _rules.ChainWindowSec)
            {
                BreakChain(worldMs);
                _lastCastMs = worldMs;
                return ChainStepResult.None;
            }

            bool hadProgress = _active != null && _linkCount > 0;
            _history.Add(elementDot);
            TrimHistoryToMax();

            // Geçmişin tamamı pattern önekiyle örtüşmeli; kısmi önek = kırılma.
            if (!TryMatchExactHistory(out ChainNode chain, out int linkCount))
            {
                if (hadProgress)
                {
                    // Yanlış element / uyumsuz geçmiş: zincir kırılır, ceza başlar.
                    // Bu cast yeni zincir açmaz — break_penalty bitsin.
                    BreakChain(worldMs);
                    _lastCastMs = worldMs;
                    return ChainStepResult.None;
                }

                _history.Clear();
                _history.Add(elementDot);
                if (!TryMatchExactHistory(out chain, out linkCount))
                {
                    _history.Clear();
                    _active = null;
                    _linkCount = 0;
                    _lastCastMs = worldMs;
                    return ChainStepResult.None;
                }
            }

            _active = chain;
            _linkCount = linkCount;
            _lastCastMs = worldMs;

            float bonus = linkCount > 0 && linkCount <= chain.Links.Length
                ? chain.Links[linkCount - 1]
                : 1f;

            int patternLen = CountPatternSlots(chain.Pattern);
            bool finisher = patternLen > 0 && linkCount >= patternLen;
            if (finisher)
            {
                _lastFinisher = chain.Finisher ?? string.Empty;
                // Tam zincir sonrası sıfırla — bir sonraki cast yeni zincir dener.
                _history.Clear();
                _active = null;
                _linkCount = 0;
            }

            return new ChainStepResult(
                matched: true,
                chain: chain,
                linkCount: linkCount,
                linkBonus: bonus,
                finisherTriggered: finisher,
                finisher: finisher ? (chain.Finisher ?? string.Empty) : string.Empty);
        }

        void BreakChain(double worldMs)
        {
            if (_active != null || _history.Count > 0)
                _penaltyUntilMs = worldMs + _rules.BreakPenaltySec * 1000.0;
            _history.Clear();
            _active = null;
            _linkCount = 0;
        }

        void TrimHistoryToMax()
        {
            int max = _rules.MaxChainLength > 0 ? _rules.MaxChainLength : 6;
            while (_history.Count > max)
                _history.RemoveAt(0);
        }

        bool TryMatchExactHistory(out ChainNode chain, out int linkCount)
        {
            chain = default;
            linkCount = 0;
            if (_history.Count == 0)
                return false;

            for (int i = 0; i < _catalog.Count; i++)
            {
                ChainNode candidate = _catalog[i];
                int slots = CountPatternSlots(candidate.Pattern);
                if (_history.Count > slots)
                    continue;
                int matched = MatchPrefixLength(candidate.Pattern, _history);
                if (matched != _history.Count)
                    continue;
                // Birden fazla aday (olmamalı); ilk tam önek yeterli.
                chain = candidate;
                linkCount = matched;
                return true;
            }

            return false;
        }

        /// <summary>
        /// pattern "1-X-X-X-X-X" → slot slot eşleştir. Digit = zorunlu element;
        /// X = çapa (ilk somut digit veya ilk X'te gelen cast) ile aynı.
        /// Dönüş: eşleşen önek uzunluğu (0 = yok).
        /// </summary>
        internal static int MatchPrefixLength(string pattern, IReadOnlyList<int> casts)
        {
            if (string.IsNullOrEmpty(pattern) || casts == null || casts.Count == 0)
                return 0;

            string[] tokens = pattern.Split('-');
            if (tokens.Length == 0)
                return 0;

            int anchor = 0;
            bool hasAnchor = false;
            int matched = 0;
            int n = Math.Min(tokens.Length, casts.Count);

            for (int i = 0; i < n; i++)
            {
                string raw = tokens[i].Trim();
                int cast = casts[i];

                if (IsWildcard(raw))
                {
                    if (!hasAnchor)
                    {
                        anchor = cast;
                        hasAnchor = true;
                    }
                    else if (cast != anchor)
                        break;
                    matched++;
                    continue;
                }

                if (!int.TryParse(raw, out int required))
                    break;
                if (cast != required)
                    break;
                if (!hasAnchor)
                {
                    anchor = required;
                    hasAnchor = true;
                }
                matched++;
            }

            // Geçmiş pattern'den uzunsa ve ilk patternLen slot tam eşleştiyse tam say.
            // Önek: yalnızca baştan kesintisiz eşleşme.
            return matched;
        }

        internal static int CountPatternSlots(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return 0;
            int n = 0;
            foreach (string t in pattern.Split('-'))
            {
                if (t.Trim().Length > 0) n++;
            }
            return n;
        }

        static bool IsWildcard(string token) =>
            token.Length == 1 && (token[0] == 'X' || token[0] == 'x');
    }

    /// <summary>chain_mechanics.rules — sayılar JSON'dan, uydurma yok.</summary>
    public readonly struct ChainRules
    {
        public ChainRules(
            float chainWindowSec, float breakPenaltySec, int maxChainLength, float finisherMult)
        {
            ChainWindowSec = chainWindowSec;
            BreakPenaltySec = breakPenaltySec;
            MaxChainLength = maxChainLength;
            FinisherMult = finisherMult;
        }

        public float ChainWindowSec { get; }
        public float BreakPenaltySec { get; }
        public int MaxChainLength { get; }
        public float FinisherMult { get; }

        /// <summary>
        /// Spec varsayılanı (docs/element-sistemi.json chain_mechanics.rules).
        /// SkillMotor kuralları henüz expose etmediği için ctor kolaylığı; testler
        /// FromJsonRoot ile JSON'dan okumayı tercih eder.
        /// </summary>
        public static ChainRules DefaultFromSpec { get; } = new(0.8f, 2.0f, 6, 2.0f);

        public static ChainRules FromJsonRoot(JsonValue root)
        {
            JsonValue r = root["chain_mechanics"]["rules"];
            return new ChainRules(
                chainWindowSec: r["chain_window_sec"].AsFloat(),
                breakPenaltySec: r["break_penalty_sec"].AsFloat(),
                maxChainLength: r["max_chain_length"].AsInt(),
                finisherMult: r["finisher_mult"].AsFloat());
        }
    }

    public readonly struct ChainStepResult
    {
        public ChainStepResult(
            bool matched, ChainNode? chain, int linkCount, float linkBonus,
            bool finisherTriggered, string finisher)
        {
            Matched = matched;
            Chain = chain;
            LinkCount = linkCount;
            LinkBonus = linkBonus;
            FinisherTriggered = finisherTriggered;
            Finisher = finisher ?? string.Empty;
        }

        public static ChainStepResult None { get; } =
            new(false, null, 0, 1f, false, string.Empty);

        public bool Matched { get; }
        public ChainNode? Chain { get; }
        public int LinkCount { get; }
        public float LinkBonus { get; }
        public bool FinisherTriggered { get; }
        public string Finisher { get; }
    }
}
