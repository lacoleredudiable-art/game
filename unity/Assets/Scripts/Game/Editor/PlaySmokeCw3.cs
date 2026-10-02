#if UNITY_EDITOR
using Dovus.Game;
using Dovus.Game.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Dovus.Game.Editor
{
    /// <summary>cw-3: tek Play duman testi (~75 sn), kamera + lock-on + silah + hasar.</summary>
    public static class PlaySmokeCw3
    {
        const int TargetFrames = 4500;
        static int _frame;
        static float _maxCamDelta;
        static Vector3 _lastCamPos;
        static bool _running;
        static int _weaponStep;
        static readonly string[] Weapons = { "kilic", "yay", "cekic", "top" };

        public static void Run()
        {
            if (!EditorApplication.isPlaying || _running)
                return;
            _running = true;
            _frame = 0;
            _maxCamDelta = 0f;
            _weaponStep = 0;
            _lastCamPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AnimPreview.EnterFight();
            EditorApplication.update += Tick;
            Debug.Log("[PlaySmokeCw3] started");
        }

        static void Tick()
        {
            _frame++;
            var cam = Camera.main;
            if (cam != null)
            {
                float d = Vector3.Distance(cam.transform.position, _lastCamPos);
                _maxCamDelta = Mathf.Max(_maxCamDelta, d);
                _lastCamPos = cam.transform.position;
            }

            if (_frame == 600)
                NudgeNearRock();
            if (_frame == 1200)
                ToggleLockOn();
            if (_frame == 1800)
                SwapWeapon();
            if (_frame == 2400)
                SwapWeapon();
            if (_frame == 3000)
                SwapWeapon();
            if (_frame == 3600)
                TakeHit();
            if (_frame >= TargetFrames)
            {
                EditorApplication.update -= Tick;
                _running = false;
                Debug.Log($"[PlaySmokeCw3] done frames={_frame} maxCamDelta={_maxCamDelta:F4}m");
            }
        }

        static void NudgeNearRock()
        {
            var motor = Object.FindAnyObjectByType<KinematicMotor>();
            if (motor == null)
                return;
            foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c != null && c.name == "CameraBlocker")
                {
                    Vector3 to = c.bounds.center - motor.transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f)
                        motor.transform.position += to.normalized * 1.5f;
                    break;
                }
            }
        }

        static void ToggleLockOn()
        {
            var view = Object.FindAnyObjectByType<HexagonView>();
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (view?.LockOnButton != null)
                view.LockOnButton.onClick.Invoke();
            else if (follow != null)
                follow.LockOnActive = !follow.LockOnActive;
            view?.RefreshLockOnVisual();
        }

        static void SwapWeapon()
        {
            if (_weaponStep >= Weapons.Length)
                return;
            AnimPreview.Equip(Weapons[_weaponStep]);
            _weaponStep++;
        }

        static void TakeHit()
        {
            var feel = Object.FindAnyObjectByType<CombatFeel>();
            feel?.OnExchange(new Dovus.Core.Combat.ExchangeResult
            {
                Outcome = Dovus.Core.Combat.ExchangeOutcome.Hit,
            });
        }
    }
}
#endif
