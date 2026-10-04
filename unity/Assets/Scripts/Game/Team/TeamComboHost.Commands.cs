using Dovus.App.Team;
using Dovus.Core.Border;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Team;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Data;
using Dovus.Game.Platform;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Team
{
    public sealed partial class TeamComboHost
    {
        public void SpawnAlly()
        {
            if (!Bind() || _spawned.Count >= 4 || _player == null)
                return;
            int n = _spawned.Count + 1;
            Vector3 pos = _player.position + new Vector3(TeamComboDefaults.AllySpawnBaseX - n * TeamComboDefaults.AllySpawnStepX, 0f, TeamComboDefaults.AllySpawnZ);
            pos.y = TeamComboDefaults.ActorGroundY;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Dost " + n;
            go.transform.position = pos;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                SharedTint.Apply(renderer, new Color(0.35f, 0.9f, 0.55f));
            var dummy = go.AddComponent<AllyDummyController>();
            int maxHp = _vitals != null ? _vitals.MaxHp : TeamComboDefaults.VitalsMaxHpFallback;
            dummy.Bind(maxHp, TeamComboDefaults.AllyDummyHpRatio);
            var actor = go.AddComponent<TeamActorHost>();
            actor.Id = _nextId++;
            actor.Radius = TeamComboDefaults.TeamActorRadiusM;
            _spawned.Add(go);
            _line = go.name + " geldi";
        }

        public void SetPlayerRatio(float ratio)
        {
            if (!Bind() || _vitals == null)
                return;
            int target = Mathf.Clamp(Mathf.RoundToInt(_vitals.MaxHp * Mathf.Clamp01(ratio)), 1, _vitals.MaxHp);
            if (_vitals.Hp > target)
                _vitals.ApplyDamage(_vitals.Hp - target);
            else
                _vitals.ApplyHeal(target - _vitals.Hp);
            _line = "Can %" + Mathf.RoundToInt(ratio * TeamComboDefaults.HpPercentScale);
        }

        public void CommandCast(TeamActorHost actor, string skillId)
        {
            if (!Bind() || actor == null || string.IsNullOrEmpty(skillId))
                return;
            RefreshActors();
            actor.LastSkillId = skillId;
            Disc boss = BossDisc();
            _border.OnSkill(actor.Id, skillId, actor.HpRatio);
            Body body = ToBody(actor);
            Body target = FirstOther(actor);
            _portal.Cast(skillId, body, target, _bodies, boss);
            TeamPulse pulse = _team.Cast(skillId, actor, FindAlly(target.Id), _allies, boss);
            ApplyPulse(pulse);
            ApplyMoves();
            _line = actor.name + " → " + skillId;
        }

        public void CommandHit(TeamActorHost actor)
        {
            if (!Bind() || actor == null)
                return;
            RefreshActors();
            float x = actor.transform.position.x;
            float z = actor.transform.position.z;
            bool struck = false;
            if (_boss != null)
            {
                float dx = _boss.position.x - x;
                float dz = _boss.position.z - z;
                struck = true;
                x = _boss.position.x;
                z = _boss.position.z;
                if (dx * dx + dz * dz < TeamComboDefaults.NearBossDistSqr)
                    struck = true;
            }
            if (_team.TryRopeMid(out float mx, out float mz))
            {
                x = mx;
                z = mz;
                struck = false;
            }
            TeamPulse pulse = _team.AllyHit(actor, x, z, struck || _team.AttackBroken);
            ApplyPulse(pulse);
            float mult = _team.DamageMult(actor.Id) * _portal.BuffFor(actor.Id).DamageMult * _team.BossIncomingMult;
            _line = actor.name + " vurdu x" + mult.ToString("0.00");
        }

        public void CommandSkillAt(TeamActorHost actor, string skillId)
        {
            if (actor == null)
                return;
            CommandCast(actor, skillId);
            if (_team.TryMine(out float mx, out float mz))
            {
                TeamPulse pulse = _team.AllyUsedSkill(actor, skillId, actor.transform.position.x, actor.transform.position.z);
                if (pulse.MineMult <= 0f)
                {
                    actor.transform.position = new Vector3(mx, 0f, mz);
                    pulse = _team.AllyUsedSkill(actor, skillId, mx, mz);
                }
                ApplyPulse(pulse);
                if (pulse.MineMult > 0f)
                {
                    _line = "Mayın x" + pulse.MineMult.ToString("0");
                    Burst(new Vector3(mx, TeamComboDefaults.MineBurstHeightY, mz), new Color(1f, 0.45f, 0.1f));
                }
            }
        }

        public void SendToMine(TeamActorHost actor)
        {
            if (actor == null || !_team.TryMine(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost mayında";
        }

        public void SendToRope(TeamActorHost actor)
        {
            if (actor == null || !_team.TryRopeMid(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost ipin ortasında";
        }

        public void SendToTurret(TeamActorHost actor)
        {
            if (actor == null || !_team.TryTurret(out float x, out float z))
                return;
            actor.transform.position = new Vector3(x, TeamComboDefaults.ActorGroundY, z);
            _line = "Dost tarete dokunuyor";
        }

        public void TouchTurret(TeamActorHost actor)
        {
            if (actor == null)
                return;
            SendToTurret(actor);
            string skill = _team.TouchTurret(actor);
            _line = string.IsNullOrEmpty(skill) ? "Taret kopyalamadı" : "Taret kopyaladı: " + skill;
            if (!string.IsNullOrEmpty(skill) && _boss != null)
                Burst(_boss.position + Vector3.up, new Color(1f, 0.85f, 0.3f));
        }

        public void PassBall(TeamActorHost from)
        {
            if (from == null)
                return;
            RefreshActors();
            TeamActorHost to = null;
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != from && _actors[i].Id != Modifiers.PlayerActorId)
                {
                    if (to == null || to.Id == _team.BallHolder)
                        to = _actors[i];
                }
            }
            if (to == null)
                return;
            bool ok = _team.PassBall(from, to);
            _line = ok ? "Pas " + from.Id + " → " + to.Id : "Pas olmadı";
        }

    }
}
