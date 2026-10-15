using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Trial
{
    /// <summary>Oyun dünyasının her karede döngüye verdiği anlık durum.</summary>
    public readonly struct TrialTick
    {
        public TrialTick(double nowMs, float playerDistanceToBossM, bool playerDamagedBoss, bool playerDead, bool bossDead, float bossHp01)
        {
            NowMs = nowMs;
            PlayerDistanceToBossM = playerDistanceToBossM;
            PlayerDamagedBoss = playerDamagedBoss;
            PlayerDead = playerDead;
            BossDead = bossDead;
            BossHp01 = bossHp01;
        }

        public double NowMs { get; }
        public float PlayerDistanceToBossM { get; }

        /// <summary>Bu karede oyuncu boss'a vurdu (uyku bozar, O-17.1).</summary>
        public bool PlayerDamagedBoss { get; }
        public bool PlayerDead { get; }
        public bool BossDead { get; }
        public float BossHp01 { get; }
    }

    /// <summary>
    /// Deneme döngüsü durum makinesi: silah seçimi → arena (boss uyuyor) → uyanış → dövüş →
    /// ölüm menüsü / sonuç → tekrar (O-17.1 – O-17.3). Saf mantık: zaman parametre, sahne/menü yok.
    /// </summary>
    public sealed class TrialLoop
    {
        readonly int _maxRevives;
        readonly TrialStats _stats = new();
        double _phaseEnteredMs;
        double _fightStartMs;
        double _fightEndMs;
        float _lastBossHp01 = 1f;

        public TrialLoop(WeaponLoadout loadout, int maxRevives = TrialDefaults.MaxRevives)
        {
            Loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _maxRevives = Math.Max(0, maxRevives);
        }

        public WeaponLoadout Loadout { get; }
        public TrialPhase Phase { get; private set; } = TrialPhase.WeaponSelect;
        public int Attempts { get; private set; }
        public int RevivesUsed { get; private set; }

        /// <summary>true: diriliş hakkı kullanıldı, ana kod canı yenileyip <see cref="AckRevive"/> çağırmalı.</summary>
        public bool ReviveRequested { get; private set; }

        public TrialResult? LastResult { get; private set; }
        public TrialStats Stats => _stats;

        /// <summary>Uyanış sinemasında oyuncu hasar almaz (SI-10.4).</summary>
        public bool PlayerInvulnerable => Phase == TrialPhase.BossWaking;

        /// <summary>Boss ve oyuncu yapay zekâsı yalnız dövüşte işler.</summary>
        public bool CombatActive => Phase == TrialPhase.Fight;

        public event Action<TrialPhase, TrialPhase>? PhaseChanged;

        /// <summary>Silah seçimi tamamsa denemeyi başlatır (arena, boss uyuyor).</summary>
        public bool TryStart(double nowMs)
        {
            if (Phase != TrialPhase.WeaponSelect || !Loadout.IsComplete)
                return false;
            BeginAttempt(nowMs);
            return true;
        }

        /// <summary>Ölüm menüsünden ya da sonuç ekranından "Tekrar": 3 sn sonra arenada.</summary>
        public bool TryRetry(double nowMs)
        {
            if (Phase != TrialPhase.DeathMenu && Phase != TrialPhase.Result)
                return false;
            Enter(TrialPhase.Retrying, nowMs);
            return true;
        }

        /// <summary>Ölüm menüsünden ya da sonuç ekranından "Silah değiştir": seçim ekranına döner.</summary>
        public bool TryChangeWeapons(double nowMs)
        {
            if (Phase != TrialPhase.DeathMenu && Phase != TrialPhase.Result)
                return false;
            Enter(TrialPhase.WeaponSelect, nowMs);
            return true;
        }

        /// <summary>Savaş içi silah geçişi; yalnız dövüşte.</summary>
        public bool TrySwapWeapon(bool allowed = true) => Phase == TrialPhase.Fight && Loadout.TrySwap(allowed);

        public void NoteSkillCast()
        {
            if (Phase == TrialPhase.Fight)
                _stats.NoteSkillCast();
        }

        public void NoteDamage(string comboId, float damage)
        {
            if (Phase == TrialPhase.Fight)
                _stats.NoteDamage(comboId, damage);
        }

        public void AckRevive() => ReviveRequested = false;

        public void Tick(in TrialTick t)
        {
            double inPhaseMs = t.NowMs - _phaseEnteredMs;
            switch (Phase)
            {
                case TrialPhase.BossAsleep:
                    if (t.PlayerDamagedBoss || t.PlayerDistanceToBossM <= TrialDefaults.WakeRadiusM)
                        Enter(TrialPhase.BossWaking, t.NowMs);
                    break;

                case TrialPhase.BossWaking:
                    if (inPhaseMs >= TrialDefaults.WakeSec * Units.SecToMs)
                    {
                        _fightStartMs = t.NowMs;
                        Enter(TrialPhase.Fight, t.NowMs);
                    }
                    break;

                case TrialPhase.Fight:
                    _lastBossHp01 = t.BossHp01;
                    // aynı karede ikisi de biterse boss ölümü önce sayılır (belgede yok; PR'da not)
                    if (t.BossDead)
                    {
                        EndFight(true, t.NowMs);
                        Enter(TrialPhase.BossDying, t.NowMs);
                    }
                    else if (t.PlayerDead && !ReviveRequested)
                    {
                        if (RevivesUsed < _maxRevives)
                        {
                            RevivesUsed++;
                            ReviveRequested = true;
                        }
                        else
                        {
                            EndFight(false, t.NowMs);
                            Enter(TrialPhase.PlayerDying, t.NowMs);
                        }
                    }
                    break;

                case TrialPhase.PlayerDying:
                    if (inPhaseMs >= TrialDefaults.DeathToMenuSec * Units.SecToMs)
                        Enter(TrialPhase.DeathMenu, t.NowMs);
                    break;

                case TrialPhase.Retrying:
                    if (inPhaseMs >= TrialDefaults.RetryToArenaSec * Units.SecToMs)
                        BeginAttempt(t.NowMs);
                    break;

                case TrialPhase.BossDying:
                    if (inPhaseMs >= TrialDefaults.VictoryToResultSec * Units.SecToMs)
                        Enter(TrialPhase.Result, t.NowMs);
                    break;
            }
        }

        void BeginAttempt(double nowMs)
        {
            Attempts++;
            RevivesUsed = 0;
            ReviveRequested = false;
            LastResult = null;
            _lastBossHp01 = 1f;
            _stats.Reset();
            Loadout.ResetForAttempt();
            Enter(TrialPhase.BossAsleep, nowMs);
        }

        void EndFight(bool won, double nowMs)
        {
            _fightEndMs = nowMs;
            string? top = _stats.TopCombo(out float topDamage);
            LastResult = new TrialResult(
                won,
                Attempts,
                (_fightEndMs - _fightStartMs) / Units.SecToMs,
                _stats.SkillCount,
                top,
                topDamage,
                won ? 0f : _lastBossHp01);
        }

        void Enter(TrialPhase next, double nowMs)
        {
            TrialPhase prev = Phase;
            Phase = next;
            _phaseEnteredMs = nowMs;
            PhaseChanged?.Invoke(prev, next);
        }
    }
}
