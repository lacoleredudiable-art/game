using Dovus.Core.Shared;
using Dovus.Game.Actors;
using Dovus.Game.Diagnostics;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.DevTools
{
    /// <summary>
    /// Play'de 1–4 dost çıkarır, vurdurur veya skill attırır.
    /// Takım kombosu tek başına denensin diye.
    /// </summary>
    public sealed class TeamDebugHud : MonoBehaviour
    {
        string _skill = SkillIds.DenseStrike;
        bool _open = false;
        TeamComboAccess _teamAccess;
        IDebugPanelsChromeSink _chrome;
        bool _chromeVisible = true;
        System.Action<bool> _chromeApply;

        public void Configure(TeamComboAccess teamAccess, IDebugPanelsChromeSink chrome)
        {
            _teamAccess = teamAccess;
            _chrome = chrome;
            if (_chromeApply == null)
            {
                _chromeApply = v => _chromeVisible = v;
                _chrome?.Register(_chromeApply);
                _chromeVisible = _chrome?.Visible ?? true;
            }
        }

        void OnDestroy()
        {
            if (_chromeApply != null)
                _chrome?.Unregister(_chromeApply);
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DOVUS_DEBUG
            if (!_chromeVisible)
                return;
#endif
            TeamComboHost host = _teamAccess?.Host;
            if (host == null)
                return;

            const float width = 280f;
            float height = _open ? 460f : 36f;
            GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUI.skin.box);
            _open = GUILayout.Toggle(_open, _open ? "Takım dene  (kapat)" : "Takım dene");
            if (!_open)
            {
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(host.Line);
            if (host.Border.Active(host.Modifiers.PlayerActorId))
                GUILayout.Label(host.Border.AuraLabel(host.Modifiers.PlayerActorId));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Can %15"))
                host.SetPlayerRatio(0.15f);
            if (GUILayout.Button("Can %5"))
                host.SetPlayerRatio(0.05f);
            if (GUILayout.Button("Can %80"))
                host.SetPlayerRatio(0.80f);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Dost ekle (" + host.Spawned + "/4)"))
                host.SpawnAlly();

            _skill = GUILayout.TextField(_skill ?? SkillIds.DenseStrike);
            TeamActorHost[] actors = FindObjectsOfType<TeamActorHost>();
            for (int i = 0; i < actors.Length; i++)
            {
                TeamActorHost actor = actors[i];
                if (actor == null || actor.Id == host.Modifiers.PlayerActorId)
                    continue;
                GUILayout.Label(actor.name + "  son:" + actor.LastSkillId);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Vur"))
                    host.CommandHit(actor);
                if (GUILayout.Button("Skil"))
                    host.CommandSkillAt(actor, (SkillId)_skill);
                if (GUILayout.Button("Pas"))
                    host.PassBall(actor);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Mayın"))
                    host.SendToMine(actor);
                if (GUILayout.Button("İp"))
                    host.SendToRope(actor);
                if (GUILayout.Button("Taret"))
                    host.TouchTurret(actor);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
    }
}
