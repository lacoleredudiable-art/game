using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Play denemesi: yerde bir uyarı doğar, süre bitince oyuncuya vuruş gelir.
    /// F8 daire. Menü koni / şerit ve "sıyrılabilir" bayrağını seçer.
    /// Hasar 22 — PlayerVitals başlığındaki spec çakması. BossTuning.Damage şu an 0.
    /// </summary>
    public sealed class DodgePractice : MonoBehaviour
    {
        const int PracticeDamage = 22;

        Transform _player;
        Transform _boss;
        bool _dodgeable = true;
        AttackTelegraph _live;

        bool _chromeVisible = true;

        public void Bind(Transform player, Transform boss)
        {
            _player = player;
            _boss = boss;
#if UNITY_EDITOR || DOVUS_DEBUG
            DebugPanelsChrome.Register(ApplyChrome);
            ApplyChrome(DebugPanelsChrome.Visible);
#endif
        }

#if UNITY_EDITOR || DOVUS_DEBUG
        void ApplyChrome(bool visible) => _chromeVisible = visible;

        void OnDestroy() => DebugPanelsChrome.Unregister(ApplyChrome);
#endif

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame)
                Spawn(TelegraphShape.Circle);
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DOVUS_DEBUG
            if (!_chromeVisible)
                return;
#endif
            GUI.Box(new Rect(8f, 72f, 210f, 132f), "Dodge deneme");
            _dodgeable = GUI.Toggle(new Rect(16f, 98f, 190f, 22f), _dodgeable, "Sıyrılabilir");
            if (GUI.Button(new Rect(16f, 122f, 190f, 22f), "Daire (F8)"))
                Spawn(TelegraphShape.Circle);
            if (GUI.Button(new Rect(16f, 146f, 92f, 22f), "Koni"))
                Spawn(TelegraphShape.Cone);
            if (GUI.Button(new Rect(114f, 146f, 92f, 22f), "Şerit"))
                Spawn(TelegraphShape.Line);
        }

        void Spawn(TelegraphShape shape)
        {
            if (_player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    _player = found.transform;
            }
            if (_player == null)
                return;

            if (_live != null)
                Destroy(_live.gameObject);

            var go = new GameObject("DodgePracticeTelegraph");
            _live = go.AddComponent<AttackTelegraph>();
            Vector3 origin = _player.position;
            Vector3 forward = _player.forward;
            forward.y = 0f;
            if (_boss != null)
            {
                Vector3 fromBoss = _player.position - _boss.position;
                fromBoss.y = 0f;
                if (fromBoss.sqrMagnitude > 0.01f)
                    forward = fromBoss.normalized;
            }

            DodgeTuning tuning = new DodgeTuning();
            HexagonInput input = FindAnyObjectByType<HexagonInput>();
            if (input != null && input.Combat != null && input.Combat.Dodge != null)
                tuning = input.Combat.Dodge;

            float lead = Mathf.Max(0.2f, tuning.TelegraphLeadMs / 1000f);
            _live.Begin(origin, forward, shape, lead, _dodgeable, 3.2f, 7f, 1.6f, 38f);
            _live.Completed += OnDone;
        }

        void OnDone(AttackTelegraph telegraph)
        {
            if (_player == null || telegraph == null)
                return;
            ActorStatus status = _player.GetComponent<ActorStatus>();
            if (status != null)
            {
                status.ApplyDamage(PracticeDamage, telegraph.Dodgeable);
                return;
            }

            PlayerVitals vitals = _player.GetComponent<PlayerVitals>();
            vitals?.ApplyDamage(PracticeDamage, telegraph.Dodgeable);
        }
    }
}
