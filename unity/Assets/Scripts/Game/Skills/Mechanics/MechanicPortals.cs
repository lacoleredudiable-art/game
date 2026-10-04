using Dovus.Core.Mechanic;
using Dovus.Game.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Mechanics
{
    public sealed class MechanicPortals
    {
        struct MechanicTimer
        {
            public double DueMs;
            public Action Run;
        }

        sealed class PortalPair
        {
            public GameObject A;
            public GameObject B;
            public double UntilMs;
            public bool Inside;
        }

        readonly IMechanicPortalsHost _host;
        readonly List<MechanicTimer> _timers = new();
        readonly List<PortalPair> _portals = new();

        public MechanicPortals(IMechanicPortalsHost host) => _host = host;

        public void ScheduleAfter(double now, float delaySec, Action run) =>
            _timers.Add(new MechanicTimer { DueMs = now + delaySec * SkillsTimeDefaults.SecToMs, Run = run });

        public void TickTimers(double worldMs)
        {
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                if (worldMs < _timers[i].DueMs)
                    continue;
                Action run = _timers[i].Run;
                _timers.RemoveAt(i);
                run();
            }
        }

        public void OpenPortal(MechanicPlan plan, Vector3 aimDir, double untilMs)
        {
            if (_host.Player == null)
                return;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = _host.Player.forward;
            Vector3 a = _host.Player.position;
            Vector3 b = _host.ClampToArena(a + aimDir.normalized * (float)plan.Body.ReachM);
            _portals.Add(new PortalPair
            {
                A = CreatePortalGate(a),
                B = CreatePortalGate(b),
                UntilMs = untilMs,
                Inside = true
            });
        }

        GameObject CreatePortalGate(Vector3 pos)
        {
            MechanicGrammar grammar = _host.MechanicEngine;
            float r = grammar != null ? (float)grammar.Rules.Param("portal_trigger_radius_m") : 1f;
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gate.name = "MechanicPortal";
            _host.DestroyUnityObject(gate.GetComponent<Collider>());
            gate.transform.SetParent(_host.DirectorTransform, true);
            gate.transform.position = new Vector3(pos.x, MechanicPortalsDefaults.GateGroundYM, pos.z);
            gate.transform.localScale = new Vector3(r * 2f, MechanicPortalsDefaults.GateThicknessY, r * 2f);
            return gate;
        }

        public void TickPortals(double worldMs)
        {
            if (_portals.Count == 0 || _host.Player == null)
                return;
            MechanicGrammar grammar = _host.MechanicEngine;
            float r = grammar != null ? (float)grammar.Rules.Param("portal_trigger_radius_m") : 1f;
            Vector3 p = _host.Player.position;
            for (int i = _portals.Count - 1; i >= 0; i--)
            {
                PortalPair pair = _portals[i];
                if (worldMs >= pair.UntilMs || pair.A == null || pair.B == null)
                {
                    if (pair.A != null)
                        _host.DestroyUnityObject(pair.A);
                    if (pair.B != null)
                        _host.DestroyUnityObject(pair.B);
                    _portals.RemoveAt(i);
                    continue;
                }
                Vector3 a = pair.A.transform.position;
                Vector3 b = pair.B.transform.position;
                bool inA = _host.FlatDistance(p, a) <= r;
                bool inB = _host.FlatDistance(p, b) <= r;
                if (!inA && !inB)
                    pair.Inside = false;
                else if (!pair.Inside)
                {
                    _host.TeleportPlayer(inA ? b : a);
                    pair.Inside = true;
                }
            }
        }

        public int LeftoverCount() => _portals.Count + _timers.Count;

        public void ClearSweepState()
        {
            foreach (PortalPair pair in _portals)
            {
                if (pair.A != null)
                    _host.DestroyUnityObject(pair.A);
                if (pair.B != null)
                    _host.DestroyUnityObject(pair.B);
            }
            _portals.Clear();
            _timers.Clear();
        }
    }
}
