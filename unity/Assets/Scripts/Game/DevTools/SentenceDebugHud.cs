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
using Dovus.Game.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.DevTools
{
    /// <summary>Fiil + sıfat debug metni — kabul kriteri doğrulama.</summary>
    public sealed class SentenceDebugHud : MonoBehaviour, ISentenceDebugSink
    {
        const float NoteHoldSec = 1.2f;

        Text _text;
        GameObject _root;
        SentenceEngine _engine;
        SkillMotor _skills;
        PlayerVitalsHost _vitals;
        string _note;
        float _noteUntil;

        public void BindVitals(PlayerVitalsHost vitals) => _vitals = vitals;

        public void Configure(SentenceEngine engine, Transform canvasRoot, SkillMotor skills = null, bool show = false)
        {
            _engine = engine;
            _skills = skills ?? SkillMotorLoader.Load();
            _root = new GameObject("SentenceDebug");
            _root.transform.SetParent(canvasRoot, false);
            // Canvas ScreenSpaceCamera'ya geçtiği için layer artık önemli: yeni GameObject
            // Default'ta doğuyor ve Overlay kameranın cullingMask'i yalnızca UI (T8.1).
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

            // Telefonda / varsayılanda kapalı — premium HUD'u boğuyordu.
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
            Note(abortedSentence ? "DODGE (cümle iptal)" : "DODGE (kilit kesildi)");
        }

        public void NoteCommit() => Note("ERKEN KAPANIŞ (merkez öder)");

        public void NoteBasicStrike() => Note("DÜZ VURUŞ");

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
                    DodgeGrade.Harika => "HARİKA",
                    DodgeGrade.Temiz => "TEMİZ",
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

            // Etki hacminin dışındaydı: derece yok. Yazmazsak oyuncu "neden derece almadım"
            // sorusunu cevapsız bırakıyor (T8.1).
            Note("MENZİL DIŞI (derece yok)");
        }

        void Note(string text)
        {
            _note = text;
            _noteUntil = Time.unscaledTime + NoteHoldSec;
        }

        void LateUpdate()
        {
            // O11: gizliyken her kare metin kurma (telefonda varsayılan gizli).
            if (_text == null || _engine == null || _root == null || !_root.activeSelf)
                return;

            var sb = new StringBuilder(64);
            SentenceState s = _engine.State;
            if (s.Phase == SentencePhase.Building && s.Words.Count > 0)
            {
                string skill = SkillLine(s);
                if (!string.IsNullOrEmpty(skill))
                    sb.Append(skill).Append('\n');
                sb.Append("çizim: ");
                AppendWords(sb, s);
                sb.Append("\npencere: ").Append(s.RemainingWindowMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Recovering && s.Words.Count > 0)
            {
                string skill = SkillLine(s);
                if (!string.IsNullOrEmpty(skill))
                    sb.Append(skill).Append('\n');
                sb.Append("kapanış: ");
                AppendWords(sb, s);
                if (s.LastClosing.HasValue)
                    sb.Append(" → ").Append(Name(s.LastClosing.Value.Type));
                // §5: toparlanma bir poz değil, kilitli süre. Kesme becerisi burada okunur.
                sb.Append("\nkilit: ").Append(s.RemainingRecoveryMs.ToString("0")).Append(" ms");
            }
            else if (s.Phase == SentencePhase.Aborted)
            {
                sb.Append("iptal (ödeme yok)");
            }
            else
            {
                sb.Append("altıgen: sürükle · merkez: vur · disk: dodge");
            }

            if (_vitals != null)
            {
                sb.Append('\n');
                if (_vitals.IsDown)
                    sb.Append("ölüm — dönüş ").Append(_vitals.RespawnInSec.ToString("0.0")).Append(" sn");
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
            // JSON yüklenemediyse gömülü yedek yok: boş motor döner, burada açıkça yazılır.
            if (_skills == null || !_skills.IsV61)
                return "element-sistemi JSON yok";
            SkillResolution r = _skills.ResolveWords(s.Words);
            if (r.IsEmpty)
                return null;
            string line = r.Identity.DisplayName;
            if (!string.IsNullOrEmpty(r.Identity.VerbName))
                line += "  ·  " + r.Identity.VerbName;
            if (!string.IsNullOrEmpty(r.Identity.AdjectiveName) && s.Words.Count >= 1)
                line += " / " + r.Identity.AdjectiveName;
            if (!string.IsNullOrEmpty(r.Presentation.SilhouetteAxis)
                && !string.Equals(r.Presentation.SilhouetteAxis, "none", System.StringComparison.Ordinal)
                && s.Words.Count >= 3)
                line += " {" + r.Presentation.SilhouetteAxis + "}";
            if (r.Scaling.HitboxScaleMult > 0f && System.Math.Abs(r.Scaling.HitboxScaleMult - 1f) > 0.05f
                && s.Words.Count >= 2)
                line += " ×" + r.Scaling.HitboxScaleMult.ToString("0.#");
            if (r.Mechanics != null && r.Mechanics.Length > 0)
                line += "  [" + string.Join(",", r.Mechanics) + "]";
            return line;
        }
    }
}
