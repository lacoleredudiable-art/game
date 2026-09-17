using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json state_machine.player_states — anlık durum + capability.
    /// SyncWorld: SentencePhase / dodge / CC / ölüm → player_states öncelik sırası.
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

        /// <summary>true / partial → çizim serbest (drawing.partial = mevcut cümleye ek).</summary>
        public bool AllowsDraw
        {
            get
            {
                string v = CanDraw;
                return string.Equals(v, "true", StringComparison.Ordinal)
                       || string.Equals(v, "partial", StringComparison.Ordinal);
            }
        }

        public bool AllowsDodge =>
            string.Equals(CanDodge, "true", StringComparison.Ordinal);

        /// <summary>false → hareket yok; limited / based_on_cast_mobility / dodge_direction → Game yorumlar.</summary>
        public bool BlocksMove =>
            string.Equals(CanMove, "false", StringComparison.Ordinal);

        public bool MoveLimited =>
            string.Equals(CanMove, "limited", StringComparison.Ordinal);

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

        /// <summary>
        /// Öncelik: dead &gt; stunned &gt; dodging &gt; rooted &gt; casting &gt; drawing &gt; recovering &gt; idle.
        /// SentencePhase eşlemesi: Building→drawing, Recovering→recovering (casting yoksa).
        /// </summary>
        public string ResolveWorldState(
            bool isDead,
            bool isStunned,
            bool isDodging,
            bool isRooted,
            bool isCasting,
            bool isDrawing,
            bool isRecovering)
        {
            if (isDead && _byId.ContainsKey("dead"))
                return "dead";
            if (isStunned && _byId.ContainsKey("stunned"))
                return "stunned";
            if (isDodging && _byId.ContainsKey("dodging"))
                return "dodging";
            if (isRooted && _byId.ContainsKey("rooted"))
                return "rooted";
            if (isCasting && _byId.ContainsKey("casting"))
                return "casting";
            if (isDrawing && _byId.ContainsKey("drawing"))
                return "drawing";
            if (isRecovering && _byId.ContainsKey("recovering"))
                return "recovering";
            return _byId.ContainsKey("idle") ? "idle" : CurrentId;
        }

        public void SyncWorld(
            bool isDead,
            bool isStunned,
            bool isDodging,
            bool isRooted,
            bool isCasting,
            bool isDrawing,
            bool isRecovering)
        {
            string next = ResolveWorldState(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
            TryEnter(next);
        }
    }
}
