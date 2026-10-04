using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using UnityEngine;

namespace Dovus.Game.Skills.Weapons
{
    public sealed class OrbController
    {
        readonly IOrbControllerHost _host;
        readonly OrbAnchor _orb = new();

        public OrbController(IOrbControllerHost host) => _host = host;

        public OrbAnchor Orb => _orb;


        public void Tick(double worldMs)
        {
            if (_host.Player == null || _host.EquippedProfile == null || _host.EquippedProfile.HitShape != "orb")
                return;
            Vector3 p = _host.Player.position;
            if (_orb.AtHand && !_orb.IsMoving)
                _orb.SnapToHand(p.x, p.z);
            _orb.Tick(worldMs, p.x, p.z);
        }

        public bool TryPlace(float targetX, float targetZ)
        {
            if (_host.Player == null || _host.EquippedProfile == null || _host.EquippedProfile.OrbPlaceM <= 0f || _host.Clock == null)
                return false;
            Vector3 p = _host.Player.position;
            return _orb.TryPlace(p.x, p.z, targetX, targetZ, _host.Clock.Director.WorldTimeMs, _host.EquippedProfile);
        }

        public bool TryRecall()
        {
            if (_host.Player == null || _host.EquippedProfile == null || _host.Clock == null)
                return false;
            Vector3 p = _host.Player.position;
            return _orb.TryRecall(p.x, p.z, _host.Clock.Director.WorldTimeMs, _host.EquippedProfile);
        }

        /// <summary>
        /// K3: silah düğmesi dokunuşu her zaman <see cref="TryRequestWeaponSwap"/>. Küre kuşanılıyken
        /// düğmeyi bu kadar (JSON weapons[].orb.hold_sec) basılı tutmak <see cref="ToggleOrb"/> yapar; 0 = uzun basma yok.
        /// </summary>
        public float SwapButtonHoldSec => IsOrbWeapon() ? _host.EquippedProfile.OrbHoldSec : 0f;

        /// <summary>Editör kısayolu ve HUD. Çizim alanına dokunmaz.</summary>
        public bool Toggle()
        {
            if (!IsOrbWeapon() || _host.Player == null || _host.Clock == null)
                return false;
            if (OrbHudCommand.Tap(_orb.AtHand) == OrbGestureResult.Place)
            {
                if (!TryCurrentOrbTarget(out float x, out float z))
                    return false;
                return TryPlace(x, z);
            }
            return TryRecall();
        }

        bool IsOrbWeapon()
        {
            WeaponCombatProfile profile = _host.EquippedProfile;
            return profile != null && profile.HitShape == "orb" && profile.OrbPlaceM > 0f;
        }

        bool TryCurrentOrbTarget(out float x, out float z)
        {
            Transform mark = null;
            if (_host.Targeting != null && _host.Targeting.Selected != null && _host.Targeting.Selected.IsAvailable)
                mark = _host.Targeting.Selected.transform;
            if (mark == null && _host.Boss != null)
                mark = _host.Boss.transform;
            if (mark != null && mark != _host.Player)
            {
                x = mark.position.x;
                z = mark.position.z;
                return true;
            }
            Vector3 face = _host.FlatBodyForward();
            float dist = _host.EquippedProfile != null && _host.EquippedProfile.OrbPlaceM > 0f
                ? _host.EquippedProfile.OrbPlaceM
                : 8f;
            Vector3 p = _host.Player.position;
            x = p.x + face.x * dist;
            z = p.z + face.z * dist;
            return true;
        }
    }
}
