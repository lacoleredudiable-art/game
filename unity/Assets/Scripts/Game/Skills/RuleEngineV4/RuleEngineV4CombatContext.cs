using System;
using System.Collections.Generic;
using Dovus.Core.Boss;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Casting.Input;
using Dovus.Game.Config;
using Dovus.Game.Hud;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Targeting;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>v4 dünya/kapanış yolu: RuleEngineV4SentenceHost yerine doğrudan bağlanan servisler.</summary>
    public sealed class RuleEngineV4CombatContext
    {
        public MonoBehaviour CoroutineHost { get; set; }
        public Transform HostTransform => CoroutineHost != null ? CoroutineHost.transform : null;
        public RuleEngineV4WorldSession Session { get; set; } = new();
        public Transform Player { get; set; }
        public ActorView Visual { get; set; }
        public GameClockHost Clock { get; set; }
        public BossReactorController Boss { get; set; }
        public BossVitals BossVitals { get; set; }
        public BossDirector BossDirector { get; set; }
        public AllyDummyController Ally { get; set; }
        public DamageNumberHud DamageHud { get; set; }
        public Func<PlayerVitalsHost> PlayerVitals { get; set; }
        public HexagonInputController Input { get; set; }
        public PlayerTargetingController Targeting { get; set; }
        public SkillAim SkillAim { get; set; }
        public ClosingQueue ClosingQueue { get; set; }
        public SceneLiveRegistry<TargetableHost> LiveTargetables { get; set; }
        public GameTuning Colors { get; set; }
        public EquipmentItem EquippedWeapon { get; set; }
        public AnimationBridge AnimationBridge { get; set; }
        public Action EnsureAnimationReady { get; set; }
        public Func<IReadOnlyList<SentenceWord>, SkillResolution> ResolveSkillWords { get; set; }
        public Action<float> NotifyCastDealt { get; set; }
        public Action<bool, bool> NotifyBossStruck { get; set; }
        public Func<Vector3?> BossHitPoint { get; set; }
        public Action<Transform, float> PlaceMark { get; set; }
        public Action<RuleEngineV4SurfaceAim, float> PlaceSurfaceMark { get; set; }
        public RuleEngineV4CastHost CastHost { get; set; }

        public double WorldTimeMs =>
            Clock != null ? Clock.Director.WorldTimeMs : 0d;

        public int EquippedWeaponNumber()
        {
            if (EquippedWeapon == null)
                return 0;
            string id = EquippedWeapon.Id ?? string.Empty;
            int colon = id.LastIndexOf(':');
            return int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out int weaponId) ? weaponId : 0;
        }

        public Color OwnerColor =>
            Colors != null ? Colors.Visuals.PlayerColor : Color.white;
    }
}
