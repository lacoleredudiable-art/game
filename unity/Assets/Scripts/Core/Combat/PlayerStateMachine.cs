using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json state_machine.player_states — anlık durum + capability
    /// okuma. SentencePhase'e bağlanmadı (ayrı karar); yalnızca JSON'dan gelen
    /// can_draw / can_move / can_dodge / i_frames yüzeyini taşır.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        readonly Dictionary<string, PlayerStateNode> _byId;

        public PlayerStateMachine(IReadOnlyList<PlayerStateNode> states)
        {
            _byId = new Dictionary<string, PlayerStateNode>(StringComparer.Ordinal);
            if (states != null)
            {
                for (int i = 0; i < states.Count; i++)
                {
                    PlayerStateNode s = states[i];
                    if (!string.IsNullOrEmpty(s.Id))
                        _byId[s.Id] = s;
                }
            }

            CurrentId = _byId.ContainsKey("idle") ? "idle" : string.Empty;
        }

        public string CurrentId { get; private set; }

        public int Count => _byId.Count;

        public PlayerStateNode Current
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentId) || !_byId.TryGetValue(CurrentId, out PlayerStateNode node))
                    return default;
                return node;
            }
        }

        public string CanDraw => Current.CanDraw;
        public string CanMove => Current.CanMove;
        public string CanDodge => Current.CanDodge;
        public bool IFrames => Current.IFrames;

        public bool TryGet(string id, out PlayerStateNode node)
        {
            if (string.IsNullOrEmpty(id))
            {
                node = default;
                return false;
            }
            return _byId.TryGetValue(id, out node);
        }

        /// <summary>Bilinmeyen id sessizce reddedilir (false); mevcut durum değişmez.</summary>
        public bool TryEnter(string id)
        {
            if (string.IsNullOrEmpty(id) || !_byId.ContainsKey(id))
                return false;
            CurrentId = id;
            return true;
        }
    }
}
