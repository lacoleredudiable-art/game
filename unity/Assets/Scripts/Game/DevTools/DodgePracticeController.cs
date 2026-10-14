using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Dodge frame denemesi (O-4.5, D-4). Boss donar, tek sabit kırmızı alan düzenli saldırır:
    /// TEK (1 vuruş: tek dodge yeter) ve ZİNCİR (2 vuruş: yalnız zincirle kurtulunur) sırayla.
    /// Vuruş yalnız oyuncu alanın içindeyse gelir. Vuruş anı dünya saatiyle; süreler DodgeTuning'den türer.
    /// Düğme yok: alana gir, doğru anda dodge'a bas. Panelde yalnız "Boss dondur" ve "Otomatik saldırı" kutusu.
    /// </summary>
    public sealed class DodgePracticeController : MonoBehaviour
    {
        const int PracticeDamage = 22;
        // Yerleşim ve tempo (ölçüm: his için başlangıç tahmini, kuralda yok).
        const float AreaForwardM = 6f;
        const float AreaRadiusM = 3f;
        const int PauseBetweenMs = 1500;

        enum Phase { Pause, Fill1, Fill2 }

        Transform _player;
        Transform _boss;
        BossDirector _bossDirector;
        GameClockHost _clock;
        AttackTelegraphView _live;

        bool _freezeBoss = true;
        bool _auto = true;
        bool _chainTurn;
        Phase _phase = Phase.Pause;
        int _phaseEndMs;
        Vector3 _center;
        bool _centerSet;
        string _next = "";
        string _result = "";

        public void Bind(Transform player, Transform boss, BossDirector bossDirector = null, GameClockHost clock = null)
        {
            _player = player;
            _boss = boss;
            _bossDirector = bossDirector;
            _clock = clock;
            ApplyFreeze();
        }

        void ApplyFreeze()
        {
            if (_bossDirector != null)
                _bossDirector.PracticeFrozen = _freezeBoss;
        }

        DodgeTuning Tuning()
        {
            HexagonInputController input = FindAnyObjectByType<HexagonInputController>();
            return input != null && input.Combat != null && input.Combat.Dodge != null
                ? input.Combat.Dodge
                : new DodgeTuning();
        }

        int WorldMs() => _clock != null ? (int)_clock.Director.WorldTimeMs : 0;

        void Update()
        {
            if (!_auto || _clock == null || _player == null)
                return;

            if (!_centerSet)
            {
                // Alan sabit: oyuncunun başladığı yerin önünde, boss'a doğru.
                Vector3 fwd = _boss != null ? _boss.position - _player.position : _player.forward;
                fwd.y = 0f;
                fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
                _center = _player.position + fwd * AreaForwardM;
                _centerSet = true;
                _phaseEndMs = WorldMs() + PauseBetweenMs;
                _next = NextName();
            }

            int now = WorldMs();
            if (now < _phaseEndMs)
                return;

            DodgeTuning t = Tuning();
            switch (_phase)
            {
                case Phase.Pause:
                    _phase = Phase.Fill1;
                    _phaseEndMs = now + Mathf.Max(200, t.TelegraphLeadMs);
                    ShowArea((_phaseEndMs - now) / 1000f);
                    break;
                case Phase.Fill1:
                    Hit(_chainTurn ? "ZINCIR 1. vurus" : "TEK vurus");
                    if (_chainTurn)
                    {
                        // 2. vuruş: tek pencerenin dışı, zincir penceresinin içi (orta nokta).
                        int gap = (t.IframeMs + t.CombinedIframeMs) / 2;
                        _phase = Phase.Fill2;
                        _phaseEndMs = now + gap;
                        ShowArea(gap / 1000f);
                    }
                    else
                        EndAttack(now);
                    break;
                case Phase.Fill2:
                    Hit("ZINCIR 2. vurus");
                    EndAttack(now);
                    break;
            }
        }

        string NextName() => _chainTurn ? "ZINCIR (2 vurus)" : "TEK (1 vurus)";

        void EndAttack(int now)
        {
            _chainTurn = !_chainTurn;
            _next = NextName();
            _phase = Phase.Pause;
            _phaseEndMs = now + PauseBetweenMs;
        }

        void ShowArea(float sec)
        {
            if (_live != null)
                Destroy(_live.gameObject);
            var go = new GameObject("DodgePracticeTelegraph");
            _live = go.AddComponent<AttackTelegraphView>();
            _live.Begin(_center, Vector3.forward, TelegraphShape.Circle, sec, true, AreaRadiusM, 7f, 1.6f, 38f);
        }

        void Hit(string label)
        {
            Vector3 d = _player.position - _center;
            d.y = 0f;
            if (d.magnitude > AreaRadiusM)
            {
                _result = label + ": alan disinda";
                return;
            }

            ActorStatusHost status = _player.GetComponent<ActorStatusHost>();
            PlayerVitalsHost vitals = _player.GetComponent<PlayerVitalsHost>();
            float taken;
            if (status != null)
            {
                status.ApplyDamage(PracticeDamage, true);
                taken = status.LastAppliedDamage;
            }
            else
            {
                vitals?.ApplyDamage(PracticeDamage, true);
                taken = -1f;
            }
            _result = label + ": " + (taken == 0f ? "KURTULDU" : "VURULDU");
            Debug.Log("[DODGE-DENEME] " + _result);

            // Test sürsün: can bitmesin.
            if (vitals != null && !vitals.IsDown && vitals.Hp < vitals.MaxHp)
                vitals.ApplyHeal(vitals.MaxHp - vitals.Hp);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(8f, 72f, 250f, 150f), "Dodge deneme");
            bool freeze = GUI.Toggle(new Rect(16f, 98f, 230f, 22f), _freezeBoss, "Boss dondur");
            if (freeze != _freezeBoss)
            {
                _freezeBoss = freeze;
                ApplyFreeze();
            }
            _auto = GUI.Toggle(new Rect(16f, 120f, 230f, 22f), _auto, "Otomatik saldiri");
            GUI.Label(new Rect(16f, 144f, 230f, 22f), "Siradaki: " + _next);
            GUI.Label(new Rect(16f, 166f, 230f, 50f), _result);
        }
    }
}
