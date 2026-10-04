using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Data;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.DevTools
{
    /// <summary>Fiil + sÄ±fat debug metni â€” kabul kriteri doÄŸrulama.</summary>
    public sealed class SentenceDebugHud : MonoBehaviour
    {
        const float NoteHoldSec = 1.2f;

        Text _text;
        GameObject _root;
        SentenceEngine _engine;
        SkillMotor _skills;
        PlayerVitals _vitals;
        string _note;
        float _noteUntil;

        public void BindVitals(PlayerVitals vitals) => _vitals = vitals;

        public void Configure(SentenceEngine engine, Transform canvasRoot, SkillMotor skills = null, bool show = false)
        {
            _engine = engine;
            _skills = skills ?? SkillMotorLoader.Load();
            _root = new GameObject("SentenceDebug");
            _root.transform.SetParent(canvasRoot, false);
            // Canvas ScreenSpaceCamera'ya geÃ§tiÄŸi iÃ§in layer artÄ±k Ã¶nemli: yeni GameObject
            // Default'ta doÄŸuyor ve Overlay kameranÄ±n cullingMask'i yalnÄ±zca UI (T8.1).
            if (canvasRoot != null)
                _root.layer = canvasRoot.gameObject.layer;
            var rect = _root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.52f, 0.82f);
            rect.anchorMax = new Vector2(0.98f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = _root.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_text.font == null)
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 28;
            _text.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            _text.alignment = TextAnchor.UpperRight;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            // Telefonda / varsayÄ±landa kapalÄ± â€” premium HUD'u boÄŸuyordu.
            _root.SetActive(show);

#if UNITY_EDITOR || DOVUS_DEBUG
            DebugPanelsChrome.Register(ApplyChrome);
            ApplyChrome(DebugPanelsChrome.Visible);
#endif
        }

#if UNITY_EDITOR || DOVUS_DEBUG
        void ApplyChrome(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
        }

        void OnDestroy() => DebugPanelsChrome.Unregister(ApplyChrome);
#endif
        public void NoteDodge(bool abortedSentence)
        {
            Note(abortedSentence ? "DODGE (cÃ¼mle iptal)" : "DODGE (kilit kesildi)");
        }

        public void NoteCommit() => Note("ERKEN KAPANIÅ (merkez Ã¶der)");

        public void NoteBasicStrike() => Note("DÃœZ VURUÅ");

        public void NoteSkillBang(string title, string mechanics)
        {
            if (string.IsNullOrEmpty(title))
                return;
            Note(string.IsNullOrEmpty(mechanics) ? title : title + "  [" + mechanics + "]");
        }

        public void NoteExchange(ExchangeResult result)
        {
            if (result.Outcome == ExchangeOutcome.Dodged)
            {
                string grade = result.Grade switch
                {
                    DodgeGrade.Mukemmel => "PERFECT",
                    DodgeGrade.Harika => "HARÄ°KA",
                    DodgeGrade.Temiz => "TEMÄ°Z",
                    DodgeGrade.Siyirdi => "SIYIRDI",
                    _ => "SIYIRMA"
                };
                Note($"{grade}  {result.ReactionMs / 1000f:0.00} sn");
                return;
            }

            if (result.Outcome == ExchangeOutcome.Hit)
            {
                Note(result.HitReasonText ?? "vuruldun");
                return;
            }

            // Etki hacminin dÄ±ÅŸÄ±ndaydÄ±: derece yok. Yazmazsak oyuncu "neden derece almadÄ±m"
            // sorusunu cevapsÄ±z bÄ±rakÄ±yor (T8.1).
            Note("MENZÄ°L DIÅI (derece yok)");
        }

        void Note(string text)
        {
            _note = text;
            _noteUntil = Time.unscaledTime + NoteHoldSec;
        }

        void LateUpdate()
        {
            // O11: gizliyken her kare metin kurma (telefonda varsayÄ±lan gizli).
            if (_text == null || _engine == null || _root == null || !_root.activeSelf)
                return;

            var sb = new StringBuilder(64);
            SentenceState s = _engine.State;
            if (s.Phase == SentencePhase.Building && s.Words.Count > 0)
            {
                string skill = SkillLine(s);
                if (!string.IsNullOrEmpty(skill))
                    sb.Append(skill).Append('\n');
                sb.Append("Ã§izim: ");
                AppendWords(sb, s);
                sb.Append("\npencere: ").Append(s.RemainingWindowMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Recovering && s.Words.Count > 0)
            {
                string skill = SkillLine(s);
                if (!string.IsNullOrEmpty(skill))
                    sb.Append(skill).Append('\n');
                sb.Append("kapanÄ±ÅŸ: ");
                AppendWords(sb, s);
                if (s.LastClosing.HasValue)
                    sb.Append(" â†’ ").Append(Name(s.LastClosing.Value.Type));
                // Â§5: toparlanma bir poz deÄŸil, kilitli sÃ¼re. Kesme becerisi burada okunur.
                sb.Append("\nkilit: ").Append(s.RemainingRecoveryMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Aborted)
            {
                sb.Append("iptal (Ã¶deme yok)");
            }
            else
            {
                sb.Append("altÄ±gen: sÃ¼rÃ¼kle Â· merkez: vur Â· disk: dodge");
            }

            if (_vitals != null)
            {
                sb.Append('\n');
                if (_vitals.IsDown)
                    sb.Append("Ã¶lÃ¼m â€” dÃ¶nÃ¼ÅŸ ").Append(_vitals.RespawnInSec.ToString("0.0")).Append(" sn");
                else
                    sb.Append("can: ").Append(_vitals.Hp).Append('/').Append(_vitals.MaxHp);
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
                    sb.Append('\u00d7').Append(w.IntensityStacks + 1);
            }
        }

        static string Name(Dovus.Core.Element.Rune r) => RuneInfo.DisplayName(r);

        string SkillLine(SentenceState s)
        {
            if (s.Words.Count == 0)
                return null;
            // JSON yÃ¼klenemediyse gÃ¶mÃ¼lÃ¼ yedek yok: boÅŸ motor dÃ¶ner, burada aÃ§Ä±kÃ§a yazÄ±lÄ±r.
            if (_skills == null || !_skills.IsV61)
                return "element-sistemi JSON yok";
            SkillResolution r = _skills.ResolveWords(s.Words);
            if (r.IsEmpty)
                return null;
            string line = r.DisplayName;
            if (!string.IsNullOrEmpty(r.VerbName))
                line += "  Â·  " + r.VerbName;
            if (!string.IsNullOrEmpty(r.AdjectiveName) && s.Words.Count >= 1)
                line += " / " + r.AdjectiveName;
            if (!string.IsNullOrEmpty(r.SilhouetteAxis)
                && !string.Equals(r.SilhouetteAxis, "none", System.StringComparison.Ordinal)
                && s.Words.Count >= 3)
                line += " {" + r.SilhouetteAxis + "}";
            if (r.HitboxScaleMult > 0f && System.Math.Abs(r.HitboxScaleMult - 1f) > 0.05f
                && s.Words.Count >= 2)
                line += " Ã—" + r.HitboxScaleMult.ToString("0.#");
            if (r.Mechanics != null && r.Mechanics.Length > 0)
                line += "  [" + string.Join(",", r.Mechanics) + "]";
            return line;
        }
    }
}
