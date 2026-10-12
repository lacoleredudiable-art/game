using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// O-17.8: dilimde (<see cref="RuleEngineV4Feature.Enabled"/>) değeri ayar dosyasından ya da koddan değil
    /// kural-motoru-v4.json'dan gelen ayarlar. <see cref="Entry.Path"/> ayar dosyasının alan yoludur ("combat.*",
    /// "prototype.*"); dosyada olmayan ayarlar "GameTuning.*" adını taşır. Açılış kaynak log'u ve paneldeki gri
    /// kaydırıcılar (Y4, Y10) bu tabloyu okur; değeri asıl kullanan yer (kamera, can, boss hasarı, arena) aynı JSON
    /// alanını okur.
    /// </summary>
    public static class RuleEngineV4SliceTuningSources
    {
        public readonly struct Entry
        {
            public Entry(string path, string jsonKey, float value, bool hasJson, bool missingFallsBackToTuning)
            {
                Path = path;
                JsonKey = jsonKey;
                Value = value;
                HasJson = hasJson;
                MissingFallsBackToTuning = missingFallsBackToTuning;
            }

            public string Path { get; }
            /// <summary>JSON alanı, ör. "slice_camera.distance_m".</summary>
            public string JsonKey { get; }
            /// <summary>JSON değeri; alan yoksa okuyucunun kod varsayılanı.</summary>
            public float Value { get; }
            public bool HasJson { get; }
            /// <summary>
            /// JSON'da yoksa kullanan taraf ayar değerine düşer ([V4-FALLBACK] ile); false ise
            /// <see cref="Value"/> kod varsayılanıdır (scale, slice_arena).
            /// </summary>
            public bool MissingFallsBackToTuning { get; }
        }

        public static IReadOnlyList<Entry> Resolve(RuleEngineV4Catalog catalog)
        {
            RuleEngineV4SliceCamera cam = catalog.SliceCamera;
            RuleEngineV4SliceBossAttacks boss = catalog.SliceBossAttacks;
            RuleEngineV4Scale scale = catalog.Scale;
            RuleEngineV4SliceArena arena = catalog.SliceArena;
            var list = new List<Entry>
            {
                Tuned("prototype.CameraDistanceM", "slice_camera.distance_m", cam.DistanceM, cam.HasDistanceM),
                Tuned("prototype.CameraDefaultPitchDeg", "slice_camera.pitch_deg", cam.PitchDeg, cam.HasPitchDeg),
                Tuned("prototype.CameraLookHeightM", "slice_camera.look_height_m", cam.LookHeightM, cam.HasLookHeightM),
                Tuned("GameTuning.Camera.CameraFovDeg", "slice_camera.fov_deg", cam.FovDeg, cam.HasFovDeg),
                Scale("prototype.PlayerMaxHp", "player_max_hp", scale.PlayerMaxHp, scale),
                Scale("combat.Boss.MaxHp", "boss_max_hp", scale.BossMaxHp, scale),
                Scale("combat.Boss.PoiseMax", "boss_poise_bar", scale.BossPoiseBar, scale),
                Tuned("combat.Boss.Damage", "slice_boss_attacks.leg_slam_damage", boss.LegSlamDamage, boss.HasLegSlamDamage),
                Tuned("combat.Boss.VolleyDamage", "slice_boss_attacks.web_spit_damage_per_projectile",
                    boss.WebSpitDamagePerProjectile, boss.HasWebSpitDamagePerProjectile),
                Tuned("combat.Boss.FireConeDamage", "slice_boss_attacks.poison_breath_damage",
                    boss.PoisonBreathDamage, boss.HasPoisonBreathDamage),
                Tuned("combat.Boss.PounceDamage", "slice_boss_attacks.pounce_damage", boss.PounceDamage, boss.HasPounceDamage),
                Arena("combat.SkillMotion.ArenaHalfSizeM", arena),
                Arena("GameTuning.Arena.ArenaHalfSizeM", arena),
            };
            return list;
        }

        public static bool TryGet(IReadOnlyList<Entry> entries, string path, out Entry entry)
        {
            for (int i = 0; entries != null && i < entries.Count; i++)
            {
                if (entries[i].Path == path)
                {
                    entry = entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        static Entry Tuned(string path, string key, float value, bool has) => new(path, key, value, has, true);

        static Entry Scale(string path, string key, float value, RuleEngineV4Scale scale)
        {
            bool has = true;
            foreach (string missing in scale.MissingKeys)
                has &= missing != key;
            return new Entry(path, "scale." + key, value, has, false);
        }

        static Entry Arena(string path, RuleEngineV4SliceArena arena) =>
            new(path, "slice_arena.outer_wall_collision_radius_m", arena.OuterWallCollisionRadiusM,
                arena.HasOuterWallGeometry, false);
    }
}
