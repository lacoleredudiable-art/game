using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Play'de 1–4 dost çıkarır, vurdurur veya skill attırır.
    /// Takım kombosu tek başına denensin diye.
    /// </summary>
    public sealed class TeamDebugMenu : MonoBehaviour
    {
        string _skill = "5-4";
        // K2: varsayılan kapalı (HUD'un sol üstünü kaplıyordu); yalnız editör / DOVUS_DEBUG dev build.
        bool _open = false;

#if UNITY_EDITOR || DOVUS_DEBUG
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<TeamDebugMenu>() != null)
                return;
            var go = new GameObject(nameof(TeamDebugMenu));
            go.AddComponent<TeamDebugMenu>();
            DontDestroyOnLoad(go);
        }
#endif

        void OnGUI()
        {
            PortalBorderTeamHost host = PortalBorderTeamHost.Instance;
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
            if (host.Border.Active(PortalBorderTeamHooks.PlayerActorId))
                GUILayout.Label(host.Border.AuraLabel(PortalBorderTeamHooks.PlayerActorId));
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

            _skill = GUILayout.TextField(_skill ?? "1-1");
            TeamActor[] actors = FindObjectsOfType<TeamActor>();
            for (int i = 0; i < actors.Length; i++)
            {
                TeamActor actor = actors[i];
                if (actor == null || actor.Id == PortalBorderTeamHooks.PlayerActorId)
                    continue;
                GUILayout.Label(actor.name + "  son:" + actor.LastSkillId);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Vur"))
                    host.CommandHit(actor);
                if (GUILayout.Button("Skil"))
                    host.CommandSkillAt(actor, _skill);
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
