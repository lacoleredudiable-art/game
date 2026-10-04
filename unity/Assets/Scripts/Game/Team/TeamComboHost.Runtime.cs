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
        void RefreshActors()
        {
            _actors.Clear();
            if (_player != null)
            {
                TeamActorHost actor = _player.GetComponent<TeamActorHost>();
                if (actor == null)
                    actor = _player.gameObject.AddComponent<TeamActorHost>();
                actor.Id = Modifiers.PlayerActorId;
                actor.Radius = TeamComboDefaults.TeamActorRadiusM;
                if (_vitals != null && _vitals.MaxHp > 0)
                    actor.HpRatio = (float)_vitals.Hp / _vitals.MaxHp;
                actor.TemplateOwnsPosition = _motion != null && _motion.IsDisplacing;
                _actors.Add(actor);
            }

            System.Collections.Generic.IReadOnlyList<AllyDummyController> dummies = AllyDummyController.Live;
            for (int i = 0; i < dummies.Count; i++)
            {
                AllyDummyController dummy = dummies[i];
                if (dummy == null)
                    continue;
                TeamActorHost actor = dummy.GetComponent<TeamActorHost>();
                if (actor == null)
                {
                    actor = dummy.gameObject.AddComponent<TeamActorHost>();
                    actor.Id = _nextId++;
                    actor.Radius = TeamComboDefaults.TeamActorRadiusM;
                }
                actor.HpRatio = dummy.Ratio;
                _actors.Add(actor);
            }

            _bodies.Clear();
            _allies.Clear();
            for (int i = 0; i < _actors.Count; i++)
            {
                _bodies.Add(ToBody(_actors[i]));
                _allies.Add(_actors[i]);
            }
        }

        void SenseBodies(in Disc boss)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                TeamActorHost actor = _actors[i];
                Body body = ToBody(actor);
                _portal.Sense(body, false, boss, out _);
            }
            if (_boss != null)
            {
                BossReactorController reactor = CachedBossReactor();
                float radius = reactor != null ? reactor.BodyRadiusM : TeamComboDefaults.BossBodyRadiusFallbackM;
                var bossBody = new Body(TeamComboDefaults.BossPortalBodyId, _boss.position.x, _boss.position.y, _boss.position.z, radius, false, true);
                _portal.Sense(bossBody, false, boss, out _);
            }
        }

        void ApplyMoves()
        {
            IReadOnlyList<Placement> moves = _portal.Drain();
            for (int i = 0; i < moves.Count; i++)
                ApplyOne(moves[i]);
        }

        void ApplyOne(Placement move)
        {
            TeamActorHost actor = FindActor(move.ActorId);
            if (actor == null)
                return;
            if (actor.TemplateOwnsPosition && actor.Id == Modifiers.PlayerActorId)
                return;
            if (move.Teleport && actor.Id == Modifiers.PlayerActorId)
                Modifiers.MarkIntentionalTeleport();
            actor.transform.position = new Vector3(move.X, move.Y, move.Z);
            if (!move.TransferDebuffs)
                return;
            AllyDummyController dummy = actor.GetComponent<AllyDummyController>();
            dummy?.EnsureStatusBoard();
            if (dummy != null && dummy.Board != null && _bossStatus != null)
                PortalSystem.MoveHostile(dummy.Board, _bossStatus.Board);
        }

        void PushHooks(TeamActorHost player)
        {
            int id = player.Id;
            PortalBuff buff = _portal.BuffFor(id);
            var table = Modifiers.Table;
            table.Set(
                id,
                new ActorModifiers(
                    _border.AttackSpeedMult(id) * _team.AttackSpeedMult(id),
                    _border.DamageMult(id) * _team.DamageMult(id) * buff.DamageMult,
                    _border.LifestealAdd(id),
                    _border.ColumnMoveSpeedMult(id) * _team.MoveSpeedMult(id) * buff.MoveSpeedMult,
                    buff.DamageTakenMult));
            table.BossIncomingMult = _team.BossIncomingMult;
            table.BossStrikeScale = _portal.StrikeScale;
            Modifiers.SetMiss(id, buff.MissChance);
            Modifiers.SetTaken(id, buff.DamageTakenMult);
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i].Id == id)
                    continue;
                PortalBuff allyBuff = _portal.BuffFor(_actors[i].Id);
                Modifiers.SetMiss(_actors[i].Id, allyBuff.MissChance);
                Modifiers.SetTaken(_actors[i].Id, allyBuff.DamageTakenMult);
            }
        }

        void RefreshAura(TeamActorHost player)
        {
            bool on = _border.Active(player.Id);
            if (on && _aura == null && _player != null)
            {
                _aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _aura.name = "BorderAura";
                Collider col = _aura.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                _aura.transform.SetParent(_player, false);
                _aura.transform.localPosition = new Vector3(0f, TeamComboDefaults.BorderAuraLocalY, 0f);
                _aura.transform.localScale = new Vector3(TeamComboDefaults.BorderAuraScaleXZ, TeamComboDefaults.BorderAuraScaleY, TeamComboDefaults.BorderAuraScaleXZ);
                Renderer renderer = _aura.GetComponent<Renderer>();
                if (renderer != null)
                    SharedTint.Apply(renderer, new Color(1f, 0.2f, 0.25f, 0.85f));
            }
            if (_aura != null)
                _aura.SetActive(on);
        }

    }
}
