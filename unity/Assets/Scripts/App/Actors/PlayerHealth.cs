using System;
using Dovus.Core.Combat;

namespace Dovus.App.Actors
{
    public enum HpLossKind
    {
        None,
        Hit,
        Down
    }

    public readonly struct HpLossResult
    {
        public HpLossResult(HpLossKind kind) => Kind = kind;
        public HpLossKind Kind { get; }
    }

    public sealed class PlayerHealth
    {
        int _hp;
        int _authoredMaxHp;
        float _respawnAtSec = -1f;

        public int Hp => _hp;
        public int MaxHp { get; private set; }
        public bool DevHpEnabled { get; private set; }
        public bool SuppressDown { get; set; }
        public bool IsDown => _respawnAtSec >= 0f;

        public void Bind(int maxHp, float startRatio = 1f)
        {
            _authoredMaxHp = Math.Max(1, maxHp);
            DevHpEnabled = false;
            MaxHp = _authoredMaxHp;
            float ratio = Math.Clamp(startRatio, 0f, 1f);
            _hp = Math.Clamp((int)Math.Round((float)(MaxHp * ratio)), 1, MaxHp);
        }

        public void SetDevHp(bool enabled)
        {
            if (_authoredMaxHp <= 0)
                _authoredMaxHp = MaxHp > 0 && MaxHp != DevPlayerHp.Pool ? MaxHp : Math.Max(1, MaxHp);
            DevHpEnabled = enabled;
            MaxHp = DevPlayerHp.Resolve(enabled, _authoredMaxHp);
            if (enabled)
            {
                _hp = MaxHp;
                _respawnAtSec = -1f;
                return;
            }

            _hp = Math.Clamp(_hp, 1, MaxHp);
        }

        public void SetMaxHp(int maxHp)
        {
            _authoredMaxHp = Math.Max(1, maxHp);
            if (DevHpEnabled)
                return;
            MaxHp = _authoredMaxHp;
            _hp = Math.Min(_hp, MaxHp);
        }

        public int ApplyHeal(int amount)
        {
            if (IsDown || amount <= 0 || _hp >= MaxHp)
                return 0;
            int before = _hp;
            _hp = Math.Min(MaxHp, _hp + amount);
            return _hp - before;
        }

        public HpLossResult ApplyHpLoss(int amount, float nowSec, float respawnWaitSec)
        {
            if (amount <= 0)
                return new HpLossResult(HpLossKind.None);

            _hp = Math.Max(0, _hp - amount);
            if (SuppressDown && _hp <= 0)
                _hp = 1;
            if (_hp > 0)
                return new HpLossResult(HpLossKind.Hit);

            _respawnAtSec = nowSec + respawnWaitSec;
            return new HpLossResult(HpLossKind.Down);
        }

        public bool ReviveDue(float nowSec) => IsDown && nowSec >= _respawnAtSec;

        public void Revive()
        {
            _hp = MaxHp;
            _respawnAtSec = -1f;
        }

        public float RespawnInSec(float nowSec) =>
            IsDown ? Math.Max(0f, _respawnAtSec - nowSec) : 0f;

        /// <summary>Play sweep: ölüm zamanlayıcısı kapalı, can yarıya (eski PlayerVitals alan yansıması).</summary>
        public void ResetForSweepCase()
        {
            _respawnAtSec = -1f;
            _hp = Math.Max(1, MaxHp / 2);
        }
    }
}
