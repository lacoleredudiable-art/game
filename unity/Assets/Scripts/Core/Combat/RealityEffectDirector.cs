using System;
using System.Collections.Generic;
using Dovus.Core.Status;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json manipulation_layers.reality_layer.
    /// revive_block bayrağı + StatusBoard üzerinde partial/full erase.
    /// PlayerVitals respawn'a bağlanmaz; minion/summon sistemi yok (full_erase yalnızca shields).
    /// </summary>
    public sealed class RealityEffectDirector
    {
        /// <summary>karabasan_koruma_silme targets — StatusKind id'leri.</summary>
        public static readonly StatusKind[] PartialEraseKinds =
        {
            StatusKind.Shield,
            StatusKind.Haste,
            StatusKind.DamageReduction
        };

        /// <summary>hiclik_varlik_silme — yalnızca uygulanabilir kısım (shields).</summary>
        public static readonly StatusKind[] FullEraseKinds =
        {
            StatusKind.Shield
        };

        /// <summary>cehennem_dirilis_engeli duration_sec (JSON).</summary>
        public const float DefaultReviveBlockSec = 5f;

        double _reviveBlockedUntilMs;

        public bool IsReviveBlocked(double worldMs) =>
            worldMs < _reviveBlockedUntilMs;

        public float ReviveBlockRemainingSec(double worldMs) =>
            Math.Max(0f, (float)((_reviveBlockedUntilMs - worldMs) / 1000.0));

        /// <summary>
        /// Cehennem revive_block. Yalnızca bayrak — respawn akışına bağlanmaz.
        /// Üst üste gelirse daha uzun bitiş zamanı kazanır.
        /// </summary>
        public void ApplyReviveBlock(double worldMs, float durationSec = DefaultReviveBlockSec)
        {
            if (durationSec <= 0f)
                return;
            double until = worldMs + durationSec * 1000.0;
            if (until > _reviveBlockedUntilMs)
                _reviveBlockedUntilMs = until;
        }

        /// <summary>
        /// Karabasan partial_erase: shield / haste / damage_reduction.
        /// Regen ve diğer buff'lara dokunmaz.
        /// </summary>
        public void ApplyPartialErase(StatusBoard board)
        {
            if (board == null)
                return;
            board.RemoveKinds(PartialEraseKinds);
        }

        /// <summary>
        /// Hiçlik full_erase: yalnızca shields. minions/summons sistem yok — uygulanamaz
        /// (docs/durum.md). JSON hedef string'leri verilirse shields eşlenir, diğerleri atlanır.
        /// </summary>
        public void ApplyFullErase(StatusBoard board)
        {
            if (board == null)
                return;
            board.RemoveKinds(FullEraseKinds);
        }

        /// <summary>
        /// JSON targets listesiyle full_erase. "shield"/"shields" → Shield;
        /// "minions"/"summons" yok sayılır (sistem yok).
        /// </summary>
        public void ApplyFullErase(StatusBoard board, IReadOnlyList<string> targets)
        {
            if (board == null || targets == null || targets.Count == 0)
                return;

            var kinds = new List<StatusKind>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                if (TryMapEraseTarget(targets[i], out StatusKind kind) && kind == StatusKind.Shield)
                    kinds.Add(StatusKind.Shield);
            }
            if (kinds.Count > 0)
                board.RemoveKinds(kinds);
        }

        /// <summary>
        /// JSON targets listesiyle partial_erase. Bilinen StatusKind id'lerini siler.
        /// </summary>
        public void ApplyPartialErase(StatusBoard board, IReadOnlyList<string> targets)
        {
            if (board == null || targets == null || targets.Count == 0)
                return;

            var kinds = new List<StatusKind>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                if (TryMapEraseTarget(targets[i], out StatusKind kind))
                    kinds.Add(kind);
            }
            if (kinds.Count > 0)
                board.RemoveKinds(kinds);
        }

        /// <summary>
        /// reality_layer hedef id'si → StatusKind. "shields" çoğulu Shield'e düşer;
        /// minions/summons tanımsız (false).
        /// </summary>
        public static bool TryMapEraseTarget(string targetId, out StatusKind kind)
        {
            kind = StatusKind.None;
            if (string.IsNullOrEmpty(targetId))
                return false;

            if (string.Equals(targetId, "shields", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetId, "minions", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetId, "summons", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(targetId, "shields", StringComparison.OrdinalIgnoreCase))
                {
                    kind = StatusKind.Shield;
                    return true;
                }
                return false;
            }

            return StatusKindUtil.TryParse(targetId, out kind) && kind != StatusKind.None;
        }
    }
}
