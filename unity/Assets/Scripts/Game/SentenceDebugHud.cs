using System.Text;
using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game
{
    /// <summary>Fiil + sıfat debug metni — kabul kriteri doğrulama.</summary>
    public sealed class SentenceDebugHud : MonoBehaviour
    {
        const float NoteHoldSec = 1.2f;

        Text _text;
        SentenceEngine _engine;
        PlayerVitals _vitals;
        string _note;
        float _noteUntil;

        public void BindVitals(PlayerVitals vitals) => _vitals = vitals;

        public void Configure(SentenceEngine engine, Transform canvasRoot)
        {
            _engine = engine;
            var go = new GameObject("SentenceDebug");
            go.transform.SetParent(canvasRoot, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.52f, 0.82f);
            rect.anchorMax = new Vector2(0.98f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = go.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null)
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 28;
            _text.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            _text.alignment = TextAnchor.UpperRight;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
        }

        public void NoteDodge(bool abortedSentence)
        {
            Note(abortedSentence ? "DODGE (cümle iptal)" : "DODGE (kilit kesildi)");
        }

        public void NoteCommit() => Note("ERKEN KAPANIŞ (merkez öder)");

        public void NoteBasicStrike() => Note("DÜZ VURUŞ");

        public void NoteExchange(ExchangeResult result)
        {
            if (result.Outcome == ExchangeOutcome.Dodged)
            {
                string grade = result.Grade switch
                {
                    DodgeGrade.Mukemmel => "MÜKEMMEL",
                    DodgeGrade.Harika => "HARİKA",
                    DodgeGrade.Temiz => "TEMİZ",
                    DodgeGrade.Siyirdi => "SIYIRDI",
                    _ => "SIYIRMA"
                };
                Note($"{grade}  {result.ReactionMs / 1000f:0.00} sn");
                return;
            }

            if (result.Outcome == ExchangeOutcome.Hit)
                Note(result.HitReasonText ?? "vuruldun");
        }

        void Note(string text)
        {
            _note = text;
            _noteUntil = Time.unscaledTime + NoteHoldSec;
        }

        void LateUpdate()
        {
            if (_text == null || _engine == null)
                return;

            var sb = new StringBuilder(64);
            SentenceState s = _engine.State;
            if (s.Phase == SentencePhase.Building && s.Words.Count > 0)
            {
                sb.Append("çizim: ");
                AppendWords(sb, s);
                sb.Append("\npencere: ").Append(s.RemainingWindowMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Recovering && s.Words.Count > 0)
            {
                sb.Append("kapanış: ");
                AppendWords(sb, s);
                if (s.LastClosing.HasValue)
                    sb.Append(" → ").Append(s.LastClosing.Value.Type);
                // §5: toparlanma bir poz değil, kilitli süre. Kesme becerisi burada okunur.
                sb.Append("\nkilit: ").Append(s.RemainingRecoveryMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Aborted)
            {
                sb.Append("iptal (ödeme yok)");
            }
            else
            {
                sb.Append("beşgen: sürükle · merkez: vur · disk: dodge");
            }

            if (_vitals != null)
            {
                sb.Append('\n');
                sb.Append(_vitals.IsDown ? "ölüm — dönüş " : "can: ");
                if (_vitals.IsDown)
                    sb.Append("…");
                else
                    sb.Append(_vitals.Hp).Append('/').Append(_vitals.MaxHp);
            }

            if (Time.unscaledTime < _noteUntil && !string.IsNullOrEmpty(_note))
                sb.Append('\n').Append(_note);

            _text.text = sb.ToString();
        }

        static void AppendWords(StringBuilder sb, SentenceState s)
        {
            for (int i = 0; i < s.Words.Count; i++)
            {
                if (i > 0) sb.Append('-');
                SentenceWord w = s.Words[i];
                sb.Append((int)w.Rune);
                if (i == 0)
                    sb.Append('(').Append(Name(w.Rune)).Append(')');
                else
                    sb.Append('[').Append(Name(w.Rune)).Append(']');
                if (w.IntensityStacks > 0)
                    sb.Append('×').Append(w.IntensityStacks + 1);
            }
        }

        static string Name(Rune r) => r switch
        {
            Rune.Igne => "İĞNE",
            Rune.Suru => "SÜRÜ",
            Rune.Kabuk => "KABUK",
            Rune.Zehir => "ZEHİR",
            Rune.Sarsinti => "SARSINTI",
            _ => "?"
        };
    }
}
