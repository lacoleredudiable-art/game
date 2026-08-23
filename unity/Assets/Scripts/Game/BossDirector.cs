using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Prototip boss döngüsü: idle yaklaşma + tek YERE ÇAKMA (§11).
    /// Yaklaşma Home'a yazılır — transform'a değil (T7.2 geri tepme kalıcılığı).
    /// </summary>
    public sealed class BossDirector : MonoBehaviour
    {
        enum Phase
        {
            Idle,
            Windup,
            Active,
            Recovery
        }

        GameClock _clock;
        CombatTuning _combat;
        PrototypeTuning _colors;
        BossReactor _reactor;
        BossAttack _attack;
        ExchangeResolver _resolver;
        DodgeState _dodge;
        SentenceEngine _engine;
        Transform _player;
        PlayerVitals _vitals;
        BossTelegraph _telegraph;
        CombatFeel _feel;
        KinematicMotor _playerMotor;

        Phase _phase = Phase.Idle;
        double _phaseStartedWorldMs;
        double _idleUntilWorldMs;
        int _telegraphStartMs;
        bool _strikeResolved;
        bool _playerWasDown;
        Vector3 _originHome;
        readonly System.Random _rng = new();

        public bool IsWindingUp => _phase == Phase.Windup;
        public int TelegraphStartMs => _telegraphStartMs;
        public int StrikeTimeMs => _attack != null ? _attack.StrikeTimeMs(_telegraphStartMs) : 0;

        public void Bind(
            GameClock clock,
            CombatTuning combat,
            PrototypeTuning colors,
            BossReactor reactor,
            PentagonInput input,
            Transform player,
            PlayerVitals vitals,
            BossTelegraph telegraph,
            CombatFeel feel)
        {
            _clock = clock;
            _combat = combat;
            _colors = colors;
            _reactor = reactor;
            _attack = new BossAttack(combat.Boss);
            _resolver = new ExchangeResolver(combat);
            _dodge = input.Dodge;
            _engine = input.Engine;
            _player = player;
            _vitals = vitals;
            _telegraph = telegraph;
            _feel = feel;
            _playerMotor = player.GetComponent<KinematicMotor>();
            _originHome = reactor.Home;
            EnterIdle(clock.Director.WorldTimeMs);
        }

        void Update()
        {
            if (_clock == null || _reactor == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
            HandlePlayerDown(worldMs);

            switch (_phase)
            {
                case Phase.Idle:
                    TickIdle(worldMs, dtSec);
                    break;
                case Phase.Windup:
                    TickWindup(worldMs);
                    break;
                case Phase.Active:
                    TickActive(worldMs);
                    break;
                case Phase.Recovery:
                    TickRecovery(worldMs);
                    break;
            }
        }

        void HandlePlayerDown(double worldMs)
        {
            bool down = _vitals != null && _vitals.IsDown;
            if (down && !_playerWasDown)
            {
                // Boss OYUNCUNUN doğuş noktasının üstünde duruyorsa zincir ölüm olur; yalnızca
                // o zaman eve çekilir. Aksi halde biriken kalıcı knockback (T7.2) korunur ve
                // boss ışınlanmaz (T8.1) — yaklaşma zaten idle'da yeniden başlıyor.
                Vector3 spawn = _vitals.SpawnPos;
                float safe = _combat.Boss.RadiusM;
                Vector3 toSpawn = _reactor.Home - spawn;
                toSpawn.y = 0f;
                if (toSpawn.sqrMagnitude < safe * safe)
                    _reactor.Home = _originHome;

                _telegraph?.Hide();
                _feel?.ClearThreat();
                EnterIdle(worldMs);
            }

            _playerWasDown = down;
        }

        void TickIdle(double worldMs, float dtSec)
        {
            _telegraph?.Hide();
            _feel?.ClearThreat();

            if (_vitals != null && _vitals.IsDown)
            {
                _idleUntilWorldMs = worldMs + _combat.Boss.IdleMinMs;
                return;
            }

            Approach(dtSec);

            if (worldMs >= _idleUntilWorldMs)
                EnterWindup(worldMs);
        }

        void TickWindup(double worldMs)
        {
            if (_attack == null)
                return;

            float p = (float)((worldMs - _phaseStartedWorldMs) / _attack.WindupMs);
            _telegraph?.SetProgress(p);
            _feel?.ShowThreat(p);

            if (worldMs >= _attack.StrikeTimeMs(_telegraphStartMs))
                EnterActive(worldMs);
        }

        void TickActive(double worldMs)
        {
            if (!_strikeResolved)
            {
                ResolveStrike();
                _strikeResolved = true;
                _telegraph?.Slam();
            }

            if (worldMs >= _attack.ActiveEndMs(_telegraphStartMs))
                EnterRecovery(worldMs);
        }

        void TickRecovery(double worldMs)
        {
            _feel?.ClearThreat();
            float fade = 1f - (float)((worldMs - _phaseStartedWorldMs) / _attack.RecoveryMs);
            _telegraph?.Recover(fade);

            if (worldMs >= _attack.RecoveryEndMs(_telegraphStartMs))
                EnterIdle(worldMs);
        }

        void EnterIdle(double worldMs)
        {
            _phase = Phase.Idle;
            _phaseStartedWorldMs = worldMs;
            // T10: panelin Min/Max slider'ları BAĞIMSIZ hareket eder; Min > Max olursa
            // Random.Next negatif aralıkla ArgumentOutOfRangeException fırlatır (tüm boss
            // döngüsünü kilitler). Min/Max burada garantiye alınıyor, slider'lara dokunulmadı.
            int lo = System.Math.Min(_combat.Boss.IdleMinMs, _combat.Boss.IdleMaxMs);
            int hi = System.Math.Max(_combat.Boss.IdleMinMs, _combat.Boss.IdleMaxMs);
            int wait = _rng.Next(lo, hi + 1);
            _idleUntilWorldMs = worldMs + wait;
            _telegraph?.Hide();
        }

        void EnterWindup(double worldMs)
        {
            _phase = Phase.Windup;
            _phaseStartedWorldMs = worldMs;
            _telegraphStartMs = (int)worldMs;
            _strikeResolved = false;
            FacePlayer();
        }

        void EnterActive(double worldMs)
        {
            _phase = Phase.Active;
            _phaseStartedWorldMs = worldMs;
        }

        void EnterRecovery(double worldMs)
        {
            _phase = Phase.Recovery;
            _phaseStartedWorldMs = worldMs;
        }

        void Approach(float dtSec)
        {
            if (_player == null || dtSec <= 0f)
                return;

            Vector3 home = _reactor.Home;
            Vector3 to = _player.position - home;
            to.y = 0f;
            float pad = _colors != null ? _colors.BossApproachStopPadM : 0.35f;
            float stop = _reactor.BodyRadiusM + (_playerMotor != null ? _playerMotor.BodyRadiusM : 0.5f) + pad;
            if (to.sqrMagnitude <= stop * stop)
                return;

            home += to.normalized * _combat.Boss.ApproachSpeedMps * dtSec;
            _reactor.Home = home;
            FacePlayer();
        }

        void FacePlayer()
        {
            if (_player == null)
                return;
            Vector3 to = _player.position - _reactor.Home;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void ResolveStrike()
        {
            if (_vitals != null && _vitals.IsDown)
                return;

            float dist = 0f;
            if (_player != null)
            {
                Vector3 d = _player.position - _reactor.Home;
                d.y = 0f;
                dist = d.magnitude;
            }

            int? press = _dodge?.PressTimeMs;
            // Eski basış bu telegrafa ait değil → "geç kaldın". Eşik basma anı DEĞİL, i-frame
            // sonu: telegraf başlarken dokunulmazlık hâlâ açıksa basış bu saldırıya aittir ve
            // sebebi "erken bastın" olmalı — `press < telegraphStart` bunu da yutuyordu (T8.1).
            if (press.HasValue && _dodge.IframeEndMs(press.Value) < _telegraphStartMs)
                press = null;

            var input = new ExchangeInput
            {
                TelegraphStartMs = _telegraphStartMs,
                StrikeTimeMs = _attack.StrikeTimeMs(_telegraphStartMs),
                DodgePressMs = press,
                InEffectVolume = _attack.IsInEffectVolume(dist, 0f)
            };

            ExchangeResult result = _resolver.Resolve(input);
            _feel?.OnExchange(result);

            if (result.Outcome == ExchangeOutcome.Hit)
            {
                _engine?.Abort();
                _vitals?.ApplyDamage(_combat.Boss.Damage);
            }
        }
    }
}
