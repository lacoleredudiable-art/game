using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// v4 mana havuzu + kombo başı bekleme (scale bloğu). Bekleme skill kullanılınca başlar;
    /// boşa giden skill (<see cref="IsFizzle"/>) manayı harcar, beklemenin fizzle_cooldown_frac kadarı uygulanır.
    /// Dolum dünya zamanından tembel hesaplanır.
    /// </summary>
    public sealed class RuleEngineV4CastEconomy
    {
        readonly RuleEngineV4Scale _scale;
        readonly Dictionary<string, double> _readyAtMs = new(StringComparer.Ordinal);
        double _manaAtMs;
        bool _clockSeen;
        float _mana;

        public RuleEngineV4CastEconomy(RuleEngineV4Scale scale)
        {
            _scale = scale ?? throw new ArgumentNullException(nameof(scale));
            _mana = scale.ManaPool;
        }

        public static string ComboKey(int verb, int adjective) => verb + "-" + adjective;

        /// <summary>Fedakarlık sıfatı (rün 8): kural kitabı K 22:15 "Fedakarlık mana ödemez"; canı öder, bekleme yine işler.</summary>
        public const int SacrificeAdjective = 8;

        public static bool IsManaFree(int adjective) => adjective == SacrificeAdjective;

        public float Mana(double worldMs)
        {
            Regen(worldMs);
            return _mana;
        }

        public double CooldownRemainingSec(string comboKey, double worldMs) =>
            _readyAtMs.TryGetValue(comboKey, out double readyAt) && readyAt > worldMs
                ? (readyAt - worldMs) / Units.SecToMs
                : 0;

        public RuleEngineV4CastDenial Check(string comboKey, double worldMs, bool manaFree = false)
        {
            if (CooldownRemainingSec(comboKey, worldMs) > 0)
                return RuleEngineV4CastDenial.Cooldown;
            return !manaFree && Mana(worldMs) < _scale.ManaCostPerSkill
                ? RuleEngineV4CastDenial.Mana
                : RuleEngineV4CastDenial.None;
        }

        /// <summary>Mana yetmez ya da bekleme sürüyorsa false; aksi halde mana harcanır (manaFree ise harcanmaz), tam bekleme başlar.</summary>
        public bool TryBegin(string comboKey, double worldMs, out RuleEngineV4CastDenial denial, bool manaFree = false)
        {
            denial = Check(comboKey, worldMs, manaFree);
            if (denial != RuleEngineV4CastDenial.None)
                return false;
            if (!manaFree)
                _mana -= _scale.ManaCostPerSkill;
            _readyAtMs[comboKey] = worldMs + _scale.CooldownSec * Units.SecToMs;
            return true;
        }

        public static bool IsFizzle(RuleEngineV4CastOutcome outcome) =>
            outcome is RuleEngineV4CastOutcome.NoValidTarget or RuleEngineV4CastOutcome.PendingExpired;

        /// <summary>
        /// Skill sonucu: boşa gittiyse harcanan mana geri gelmez, bekleme kullanım anından kısaltılır;
        /// ıska ve isabet tam beklemede kalır.
        /// </summary>
        public void Settle(string comboKey, double castWorldMs, RuleEngineV4CastOutcome outcome)
        {
            if (IsFizzle(outcome))
                _readyAtMs[comboKey] = castWorldMs + _scale.CooldownSec * _scale.FizzleCooldownFrac * Units.SecToMs;
        }

        void Regen(double worldMs)
        {
            if (_clockSeen && worldMs > _manaAtMs)
            {
                double gained = (worldMs - _manaAtMs) / Units.SecToMs * _scale.ManaRegenPerSec;
                _mana = (float)Math.Min(_scale.ManaPool, _mana + gained);
            }
            if (!_clockSeen || worldMs > _manaAtMs)
                _manaAtMs = worldMs;
            _clockSeen = true;
        }
    }
}
