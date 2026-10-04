using Dovus.Core.Dodge;

namespace Dovus.Game.Diagnostics
{
    /// <summary>SentenceDebugHud yüzeyi — runtime klasörler DevTools'a bağlanmaz.</summary>
    public interface ISentenceDebugSink
    {
        void NoteDodge(bool abortedSentence);
        void NoteCommit();
        void NoteBasicStrike();
        void NoteSkillBang(string title, string mechanics);
        void NoteExchange(ExchangeResult result);
    }
}
