using System.Collections.Generic;

namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Girdi tamponu (O-4.12, 13 §8 adalet paketi). Reddedilen girdi, kuralın verdiği süre kadar saklanır.
    /// Tür başına en çok bir girdi: aynı türden yenisi eskinin yerine geçer ve süreyi tazeler.
    /// Zaman parametredir (ms); Core'da saat okunmaz.
    /// </summary>
    public sealed class InputBuffer
    {
        public readonly struct Entry
        {
            public Entry(ActionKind kind, int token, int expiresAtMs)
            {
                Kind = kind;
                Token = token;
                ExpiresAtMs = expiresAtMs;
            }

            public ActionKind Kind { get; }
            public int Token { get; }
            public int ExpiresAtMs { get; }
        }

        readonly List<Entry> _entries = new List<Entry>(4);

        public int Count => _entries.Count;

        public IReadOnlyList<Entry> Entries => _entries;

        public void Push(ActionKind kind, int token, int nowMs, int ttlMs)
        {
            if (ttlMs <= 0)
                return;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Kind == kind)
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }

            _entries.Add(new Entry(kind, token, nowMs + ttlMs));
        }

        /// <summary>Süresi dolanları atar. <c>nowMs == ExpiresAtMs</c> = dolmuş (dışlayan sınır).</summary>
        public void Expire(int nowMs)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (nowMs >= _entries[i].ExpiresAtMs)
                    _entries.RemoveAt(i);
            }
        }

        public bool Remove(ActionKind kind)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Kind == kind)
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Clear() => _entries.Clear();
    }
}
