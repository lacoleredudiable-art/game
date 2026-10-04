using Dovus.Core.Equipment;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Skills.Execution;
using System.Collections.Generic;
using UnityEngine;
using CoreCannonBlast = Dovus.Core.Equipment.CannonBlast;

namespace Dovus.Game.Skills.Weapons
{
    public sealed class CannonBlast
    {
        readonly ICannonBlastHost _host;
        readonly CannonRecoil _cannonRecoil = new();
        readonly List<Transform> _cannonBodies = new();
        bool _cannonQueued;
        float _cannonImpactX;
        float _cannonImpactZ;

        public CannonBlast(ICannonBlastHost host) => _host = host;


        public void TryCannonBlast(float impactX, float impactZ)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || profile.HitShape != "ballistic")
                return;
            if (_host.MotionBody != null && _host.MotionBody.IsDisplacing)
            {
                _cannonQueued = true;
                _cannonImpactX = impactX;
                _cannonImpactZ = impactZ;
                return;
            }

            ApplyCannonBlast(impactX, impactZ);
        }

        public void TickCannonRecoil()
        {
            bool playing = _host.MotionBody != null && _host.MotionBody.IsDisplacing;
            if (_cannonQueued && !playing)
            {
                _cannonQueued = false;
                ApplyCannonBlast(_cannonImpactX, _cannonImpactZ);
            }
            if (!playing)
                _host.SetRecoilInTemplate(false);
        }

        public void ApplyCannonBlast(float impactX, float impactZ)
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            if (profile == null || _host.Player == null)
                return;
            float splash = profile.BasicRadiusM > 0f ? profile.BasicRadiusM : CannonBlastDefaults.SplashRadiusFallbackM;
            float bossPush = profile.BossPushM > 0f ? profile.BossPushM : 0.5f;
            float arena = _host.Motor != null ? _host.Motor.Tuning.Arena.ArenaHalfSizeM : CannonBlastDefaults.ArenaHalfSizeFallbackM;
            float playerR = _host.PlayerBodyRadiusM();
            float bossR = CannonBlastDefaults.BossBodyRadiusFallbackM;
            if (_host.Boss != null && _host.Boss.BodyRadiusM > CannonBlastDefaults.BossBodyRadiusEpsilonM)
                bossR = _host.Boss.BodyRadiusM;

            if (_host.Boss != null)
            {
                Vector3 boss = _host.Boss.transform.position;
                float dx = boss.x - impactX;
                float dz = boss.z - impactZ;
                if (dx * dx + dz * dz <= splash * splash)
                    _host.Boss.React(new Vector3(impactX, boss.y, impactZ), bossPush, 0f, CannonBlastDefaults.BossReactShakeSec, _host.WorldTimeMs);
            }

            PushCannonBodies(impactX, impactZ, splash, arena, bossR);

            if (profile.RecoilM <= 0f || _host.RecoilInTemplate || _host.CasterRecoilSuppressed)
                return;
            Vector3 player = _host.Player.position;
            float dirX = player.x - impactX;
            float dirZ = player.z - impactZ;
            if (dirX * dirX + dirZ * dirZ < 0.0001f)
            {
                dirX = -_host.Player.forward.x;
                dirZ = -_host.Player.forward.z;
            }

            float x = player.x;
            float z = player.z;
            float bossX = _host.Boss != null ? _host.Boss.transform.position.x : player.x;
            float bossZ = _host.Boss != null ? _host.Boss.transform.position.z : player.z;
            _cannonRecoil.Queue(
                dirX, dirZ, profile.RecoilM, bossX, bossZ,
                playerR + bossR + CannonBlastDefaults.ImpactPlayerSepPadM, arena, playerR);
            if (_cannonRecoil.TryApply(false, ref x, ref z))
                _host.Player.position = new Vector3(x, player.y, z);
        }

        public void PushCannonBodies(float impactX, float impactZ, float splash, float arena, float bossR)
        {
            _cannonBodies.Clear();
            System.Collections.Generic.IReadOnlyList<SummonExecutor> summons = SummonExecutor.Live;
            for (int i = 0; i < summons.Count; i++)
            {
                SummonExecutor summon = summons[i];
                if (summon != null)
                    summon.CollectWithin(impactX, impactZ, splash, _cannonBodies);
            }

            IReadOnlyList<Targetable> targets = Targetable.Live;
            float splashSq = splash * splash;
            for (int i = 0; i < targets.Count; i++)
            {
                Targetable target = targets[i];
                if (target == null)
                    continue;
                Transform body = target.transform;
                if (body == _host.Player || (_host.Boss != null && body == _host.Boss.transform) || (_host.Ally != null && body == _host.Ally.transform))
                    continue;
                float dx = body.position.x - impactX;
                float dz = body.position.z - impactZ;
                if (dx * dx + dz * dz <= splashSq)
                    _cannonBodies.Add(body);
            }

            float bossX = _host.Boss != null ? _host.Boss.transform.position.x : impactX;
            float bossZ = _host.Boss != null ? _host.Boss.transform.position.z : impactZ;
            float minSep = CannonBlastDefaults.BlastSepBaseM + bossR + CannonBlastDefaults.BlastSepPadM;
            for (int i = 0; i < _cannonBodies.Count; i++)
            {
                Transform body = _cannonBodies[i];
                if (body == null)
                    continue;
                float x = body.position.x;
                float z = body.position.z;
                CoreCannonBlast.Move(ref x, ref z, x - impactX, z - impactZ, splash, bossX, bossZ, minSep, arena, CannonBlastDefaults.PlayerPushMinSepM);
                body.position = new Vector3(x, body.position.y, z);
            }
        }

        public void CutTemplateForSwap(bool instant)
        {
            if (_host.MotionBody != null && _host.MotionBody.IsDisplacing)
            {
                _host.StopMotionBody();
                _host.EndMotionAnim();
            }
            _host.AbortRecoveringSentence();
            if (instant && _host.Clock != null && _host.WeaponSwap != null)
                _host.SetSwapInstantDrawUntil(
                    _host.Clock.Director.WorldTimeMs + _host.WeaponSwap.Rules.AnimationSec * SkillsTimeDefaults.SecToMs);
        }

    }
}
