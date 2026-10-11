using System.Collections;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dovus.Tests.PlayMode
{
    /// <summary>H: elle kilitli düşman menzildeyse otomatik en yakını yener; menzil dışıysa oto seçim.</summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float DecoyDistanceM = 2f;
        const float InRangeFrac = 0.8f;
        const float OutOfRangeExtraM = 3f;

        float WeaponRange(int weaponId)
        {
            Assert.That(_catalog.Catalog.TryGetWeapon(weaponId, out RuleEngineV4Weapon weapon), Is.True, $"silah {weaponId}");
            return weapon.RangeM(_catalog.Catalog.Scale.RangeReferenceM);
        }

        IEnumerator LockAndCast(TargetableHost locked)
        {
            Assert.That(_md._targeting, Is.Not.Null, "PlayerTargetingController");
            _md._targeting.SweepSelect(locked);
            Assert.That(_md._targeting.Selected, Is.EqualTo(locked), "kilit");
            Assert.That(_cast.TryLaunchCombo(1, 1, out _), Is.True, $"{_label} cast edilmedi");
            float end = Time.realtimeSinceStartup + CastTimeoutSec;
            while (_cast.IsRunning && Time.realtimeSinceStartup < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator ManualLock_InRangeWins_OutOfRangeFallsBackToAuto()
        {
            FreezeBoss();
            Equip(Yay);
            float range = WeaponRange(Yay);
            var locked = _minions[1].GetComponent<TargetableHost>();
            Assert.That(locked, Is.Not.Null, "kilitlenecek TargetableHost");

            // Yem atış hattının dışında: ok kilide giderken yemi vurup düşürmesin.
            _label = "kilit menzilde";
            Arrange(Vector3.right);
            Place(_minions[0].transform, Origin + Vector3.forward * DecoyDistanceM);
            Place(_minions[1].transform, Origin + Vector3.right * (range * InRangeFrac));
            Physics.SyncTransforms();
            yield return LockAndCast(locked);
            Assert.That(_cast.LastFocus, Is.EqualTo(locked.transform), "menzildeki kilit kazanmalı");

            _label = "kilit menzil dışı";
            Arrange(Vector3.right);
            Place(_minions[2].transform, Origin + Vector3.forward * DecoyDistanceM);
            Place(_minions[1].transform, Origin + Vector3.right * (range + OutOfRangeExtraM));
            Physics.SyncTransforms();
            yield return LockAndCast(locked);
            Assert.That(_cast.LastFocus, Is.EqualTo(_minions[2].transform), "menzil dışı kilitte oto en yakın");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }
    }
}
