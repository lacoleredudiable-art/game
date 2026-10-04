using Dovus.App.Team;
using Dovus.Core.Border;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Platform;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Team
{
    public sealed partial class TeamComboHost
    {
        void RefreshVisuals()
        {
            for (int i = 0; i < _visuals.Count; i++)
            {
                if (_visuals[i] != null)
                    Destroy(_visuals[i]);
            }
            _visuals.Clear();
            IReadOnlyList<DoorView> doors = _portal.Doors;
            for (int i = 0; i < doors.Count; i++)
            {
                DoorView door = doors[i];
                GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gate.name = "PortalGate";
                Collider col = gate.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                gate.transform.position = new Vector3(door.X, TeamComboDefaults.GateMarkerY, door.Z);
                gate.transform.localScale = new Vector3(door.Radius * 2f, TeamComboDefaults.GateThicknessY, door.Radius * 2f);
                Renderer renderer = gate.GetComponent<Renderer>();
                if (renderer != null)
                    SharedTint.Apply(renderer, new Color(0.45f, 0.35f, 1f, 0.9f));
                _visuals.Add(gate);
            }
            if (_team.TryMine(out float mx, out float mz))
                _visuals.Add(Marker("Mine", mx, mz, new Color(1f, 0.4f, 0.15f)));
            if (_team.TryRopeMid(out float rx, out float rz))
                _visuals.Add(Marker("Rope", rx, rz, new Color(0.15f, 0.15f, 0.15f)));
            if (_team.TryTurret(out float tx, out float tz))
                _visuals.Add(Marker("Turret", tx, tz, new Color(0.9f, 0.8f, 0.3f)));
        }

        GameObject Marker(string name, float x, float z, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            go.transform.position = new Vector3(x, TeamComboDefaults.WorldMarkerY, z);
            go.transform.localScale = Vector3.one * TeamComboDefaults.WorldMarkerScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, color);
            return go;
        }

        void Burst(Vector3 pos, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TeamBurst";
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * TeamComboDefaults.BurstFxScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, color);
            Destroy(go, TeamComboDefaults.BurstFxLifetimeSec);
        }

        TeamActorHost PlayerActor()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == Modifiers.PlayerActorId)
                    return _actors[i];
            }
            return _actors.Count > 0 ? _actors[0] : null;
        }

        TeamActorHost FindActor(int id)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == id)
                    return _actors[i];
            }
            return null;
        }

        IAllyPlayer FindAlly(int id) => FindActor(id);

        Body FirstOther(TeamActorHost self)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != self)
                    return ToBody(_actors[i]);
            }
            return default;
        }
    }
}
