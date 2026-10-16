using System;
using System.Collections.Generic;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Casting.Input;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Targeting;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>v4 dünya/kapanış yolu: ManifestationDirector yerine doğrudan bağlanan servisler.</summary>
    public sealed class RuleEngineV4CombatContext
    {
        public MonoBehaviour CoroutineHost { get; init; }
        public Transform HostTransform => CoroutineHost != null ? CoroutineHost.transform : null;
        public RuleEngineV4WorldSession Session { get; init; } = new();
        public Transform Player { get; init; }
        public ActorView Visual { get; init; }
        public GameClockHost Clock { get; init; }
        public BossReactorController Boss { get; init; }
        public BossVitals BossVitals { get; init; }
        public BossDirector BossDirector { get; init; }
        public AllyDummyController Ally { get; init; }
        public DamageNumberHud DamageHud { get; init; }
        public Func<PlayerVitalsHost> PlayerVitals { get; init; }
        public HexagonInputController Input { get; init; }
        public PlayerTargetingController Targeting { get; init; }
        public SkillAim SkillAim { get; init; }
        public ClosingQueue ClosingQueue { get; init; }
        public SceneLiveRegistry<TargetableHost> LiveTargetables { get; init; }
        public GameTuning Colors { get; init; }
        public EquipmentItem EquippedWeapon { get; init; }
        public AnimationBridge AnimationBridge { get; init; }
        public Action EnsureAnimationReady { get; init; }
        public Func<IReadOnlyList<SentenceWord>, SkillResolution> ResolveSkillWords { get; init; }
        public Action<float> NotifyCastDealt { get; init; }
        public Action<bool, bool> NotifyBossStruck { get; init; }
        public Func<Vector3?> BossHitPoint { get; init; }
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
