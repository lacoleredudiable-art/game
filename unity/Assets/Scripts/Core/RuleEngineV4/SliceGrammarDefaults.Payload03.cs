namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarDefaults
    {
        private static partial string PayloadJsonPart03() => @"
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,
              ""hitbox_scale_mult"": 1.1,
              ""swap_cancel"": true
            },
            ""prose_mechanic"": ""2. hedefe seker"",
            ""prose_feel"": ""Bulaşıcı"",
            ""prose_visual"": ""Mor sekme""
          },
          {
            ""id"": ""6-4"",
            ""name"": ""Sabit Bağ"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""1.5 sn root"",
            ""passive"": ""7 sn pasif: Sabitleme"",
            ""engine"": {
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted""
            },
            ""prose_mechanic"": ""1.5 sn root"",
            ""prose_feel"": ""Uzun, sabırlı"",
            ""prose_visual"": ""Kalın zincir""
          },
          {
            ""id"": ""6-6"",
            ""name"": ""Bağlayıcı Bağ"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""Çift CC"",
            ""passive"": ""4 sn pasif: Bağlama"",
            ""engine"": {
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3
            },
            ""prose_mechanic"": ""Çift CC"",
            ""prose_feel"": ""İkiz"",
            ""prose_visual"": ""Çift zincir""
          },
          {
            ""id"": ""6-9"",
            ""name"": ""Odaklı Bağ"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""Tek hedefe uzun kilit"",
            ""passive"": ""5 sn pasif: Odaklama"",
            ""engine"": {
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""Tek hedefe uzun kilit"",
            ""prose_feel"": ""Kilitli"",
            ""prose_visual"": ""Kırmızı hedef""
          }
        ]
      },
      ""9"": {
        ""skills"": [
          {
            ""id"": ""9-1"",
            ""name"": ""Yoğun Arınma"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""1 debuff sil"",
            ""passive"": ""5 sn pasif: Yoğunlaştırma"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2
            },
            ""prose_mechanic"": ""1 debuff sil"",
            ""prose_feel"": ""Ani temizlik"",
            ""prose_visual"": ""Beyaz flaş""
          },
          {
            ""id"": ""9-2"",
            ""name"": ""Emici Arınma"",
            ""adjective"": ""Emme"",
            ""effect"": ""Debuff→can"",
            ""passive"": ""7 sn pasif: Emme"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.3,
              ""damage_mult"": 0.95
            },
            ""prose_mechanic"": ""Debuff→can"",
            ""prose_feel"": ""Dönüştürücü"",
            ""prose_visual"": ""Beyaz→yeşil""
          },
          {
            ""id"": ""9-3"",
            ""name"": ""Sıçrayan Arınma"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""Dosta seker"",
            ""passive"": ""5 sn pasif: Sıçrama"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,
              ""hitbox_scale_mult"": 1.1
            },
            ""prose_mechanic"": ""Dosta seker"",
            ""prose_feel"": ""Bulaşıcı"",
            ""prose_visual"": ""Beyaz sekme""
          },
          {
            ""id"": ""9-4"",
            ""name"": ""Sabit Arınma"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""Yerleşik alan (+2 sn)"",
            ""passive"": ""7 sn pasif: Sabitleme"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted""
            },
            ""prose_mechanic"": ""Yerleşik alan (+2 sn)"",
            ""prose_feel"": ""Kalıcı"",
            ""prose_visual"": ""Beyaz halka""
          },
          {
            ""id"": ""9-6"",
            ""name"": ""Bağlayıcı Arınma"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""Dosta bağlanır"",
            ""passive"": ""4 sn pasif: Bağlama"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3
            },
            ""prose_mechanic"": ""Dosta bağlanır"",
            ""prose_feel"": ""Bağlı"",
            ""prose_visual"": ""Beyaz bağ""
          },
          {
            ""id"": ""9-9"",
            ""name"": ""Odaklı Arınma"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""En ağırı önce"",
            ""passive"": ""5 sn pasif: Odaklama"",
            ""engine"": {
              ""action"": ""cleanse"",
              ""cleanse_count"": 1,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_or_ally"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""En ağırı önce"",
            ""prose_feel"": ""Akıllı"",
            ""prose_visual"": ""Beyaz hedef""
          }
        ]
      }
    }
  },
  ""weapons"": [
    {
      ""id"": 2,
      ""name"": ""Yay"",
      ""type"": ""ranged"",
      ""school"": ""fiziksel"",
      ""damage_mult"": 0.8,
      ""cast_time_mult"": 0.7,
      ""poise_mult"": 0.6,
      ""range_mult"": 2.353,
      ""mobility_mod"": 1,
      ""base_armor"": 0,
      ""reach_m"": 20,
      ""arc_deg"": 0,
      ""hit_shape"": ""arrow"",
      ""born"": ""saplanan"",
      ""effect_travels"": true,
      ""cooldown_mult"": 1,
      ""identity_passive"": ""Koşu atışı kritik %25"",
      ""compatible_verbs"": [
        1,
        3,
        7,
        9
      ],
      ""animations_key"": ""yay"",
      ""basic"": {
        ""kind"": ""volley"",
        ""reach_m"": 20,
        ""shape"": ""arrow"",
        ""interval_sec"": 0.5,
        ""hits"": 1,
        ""radius_m"": 0.12
      },
      ""passive"": {
        ""id"": ""kosu_atisi"",
        ""move_window_sec"": 1,
        ""crit_chance"": 0.25,
        ""base_crit"": 0.05
      },
      ""swap_bonus"": {
        ""id"": ""zirh_yok"",
        ""window_sec"": 2,
        ""ignore_armor"": true
      }
    },
    {
      ""id"": 4,
      ""name"": ""Kılıç"",
      ""type"": ""medium"",
      ""school"": ""fiziksel"",
      ""damage_mult"": 1.0,
      ""cast_time_mult"": 0.9,
      ""poise_mult"": 1.1,
      ""range_mult"": 1.0,
      ""mobility_mod"": 0,
      ""base_armor"": 0,
      ""reach_m"": 2.4,
      ""arc_deg"": 144,
      ""hit_shape"": ""arc"",
      ""born"": ""onunde_yay"",
      ""effect_travels"": true,
      ""cooldown_mult"": 1,
      ""identity_passive"": ""Geniş yay 144 derece"",
      ""compatible_verbs"": [
        1,
        3,
        5,
        9
      ],
      ""animations_key"": ""kilic"",
      ""basic"": {
        ""kind"": ""slash"",
        ""reach_m"": 2.4,
        ""shape"": ""arc"",
        ""interval_sec"": 0.28,
        ""hits"": 3,
        ""radius_m"": 0.5
      },
      ""passive"": {
        ""id"": ""genis_yay"",
        ""arc_deg"": 144
      },
      ""swap_bonus"": {
        ""id"": ""genis"",
        ""window_sec"": 2,
        ""arc_deg"": 180
      }
    }
  ],
  ""elements"": [
    {
      ""id"": 1,
      ""name"": ""Ateş"",
      ""color"": ""#ff4d1a"",
      ""vfx"": ""alev_köz"",
      ""name_prefix"": ""Ateşli"",
      ""status"": ""burn"",
      ""status_effect"": ""3 hasar/sn"",
      ""status_duration"": 4
    }
  ],
  ""ana_classes_80"": {
    ""groups"": {
      ""slice"": {
        ""classes"": [
          {
            ""id"": 1,
            ""name"": ""Dilim"",
            ""runes"": [
              1,
              2,
              3,
              4,
              6,
              9
            ],
            ""category"": ""slice"",
            ""his"": ""Kılıç + Yay dilim""
          }
        ]
      }
    }
  },
  ""state_machine"": {
    ""player_states"": {
      ""idle"": {
        ""can_draw"": true,
        ""can_move"": true,
        ""can_dodge"": true,
        ""can_swap"": true
      },
      ""drawing"": {
        ""can_draw"": ""partial"",
        ""can_move"": false,
        ""can_dodge"": true,
        ""can_swap"": false,
        ""note"": ""Dodge çizimi iptal eder""
      },
      ""casting"": {
        ""can_draw"": false,
        ""can_move"": ""based_on_cast_mobility"",
        ""can_dodge"": false,
        ""can_swap"": false
      },
      ""recovering"": {
        ""can_draw"": true,
        ""can_move"": ""limited"",
        ""can_dodge"": true,
        ""can_swap"": true,
        ""note"": ""Swap recovery keser""
      },
      ""dodging"": {
        ""can_draw"": false,
        ""can_move"": ""dodge_direction"",
        ""i_frames"": true,
        ""can_swap"": false
      },
      ""stunned"": {
        ""can_draw"": false,
        ""can_move"": false,
        ""can_dodge"": false,
        ""can_swap"": false
      },
      ""rooted"": {
        ""can_draw"": true,
        ""can_move"": false,
        ""can_dodge"": true,
        ""can_swap"": true
      },
      ""channeling"": {
        ""can_draw"": false,
        ""can_move"": false,
        ""can_dodge"": true,
        ""interruptible"": true,
        ""can_swap"": false
      },";
    }
}
