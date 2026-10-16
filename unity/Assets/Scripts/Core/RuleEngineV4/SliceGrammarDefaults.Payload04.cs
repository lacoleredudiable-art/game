namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarDefaults
    {
        private static partial string PayloadJsonPart04() => @"
      ""dead"": {
        ""can_draw"": false,
        ""can_move"": false,
        ""can_dodge"": false,
        ""can_swap"": false
      }
    },
    ""boss_states"": {
      ""idle"": {
        ""exits_to"": [
          ""approach"",
          ""telegraph""
        ]
      },
      ""approach"": {
        ""exits_to"": [
          ""telegraph""
        ]
      },
      ""telegraph"": {
        ""exits_to"": [
          ""active"",
          ""interrupted""
        ]
      },
      ""active"": {
        ""exits_to"": [
          ""recovery""
        ]
      },
      ""recovery"": {
        ""exits_to"": [
          ""idle"",
          ""telegraph""
        ]
      },
      ""staggered"": {
        ""duration_sec"": 7,
        ""exits_to"": [
          ""idle""
        ]
      }
    }
  },
  ""uyumsuz_cizim"": {
    ""description"": ""Silah compatible_verbs ile uyumsuz çizim davranışı."",
    ""rule"": ""Her çizim çalışır. Uyumsuz = hafif ceza + pasif yok."",
    ""effects"": {
      ""uyumlu"": {
        ""damage_mult"": 1.0,
        ""cast_time_mult"": 1.0,
        ""passive"": ""çalışır"",
        ""ui_color"": ""yeşil""
      },
      ""uyumsuz"": {
        ""damage_mult"": 0.8,
        ""cast_time_mult"": 1.2,
        ""passive"": ""çalışmaz"",
        ""ui_color"": ""sarı""
      }
    },
    ""ceza_reason"": ""Özgürlük + ceza dengesi. Uyumlu = daha güçlü, uyumsuz = hâlâ kullanılabilir."",
    ""degistirilebilir"": ""Dengeleme aşamasında %20 ayarlanır."",
    ""isim_degismez"": true,
    ""ui_etiketi"": {
      ""uyumlu"": ""Uyumlu — [Silah] pasifi aktif"",
      ""uyumsuz"": ""Uyumsuz — %80 etki""
    },
    ""sifat_uyumsuzlugu"": ""Yok. Tüm sıfatlar tüm fiillerle çalışır. Sadece fiil-silah uyumsuzluğu var.""
  },
  ""global_rules"": {
    ""ally_skill_range_m"": 6,
    ""composition_rules"": {
      ""damage_zero_cancels_status"": ""damage_mult = 0.0 ise status UYGULANMAZ"",
      ""invulnerability_blocks_harm"": ""invuln varsa hostile etki UYGULANMAZ"",
      ""heal_overrides_damage"": ""action = heal ise damage_mult sıfır"",
      ""target_mode_filter"": ""target_mode uyumsuz hedefe skill UYGULANMAZ"",
      ""status_no_stacking"": ""Aynı status 2 kez uygulanmaz; ikincisi süreyi uzatır (alpha)""
    },
    ""resource_system"": {
      ""type"": ""mana"",
      ""max_mana"": 100,
      ""regen_per_sec"": 8,
      ""regen_delay_after_cast_sec"": 1.5,
      ""all_values_tunable"": true
    },
    ""cooldown_rules"": {
      ""global_cooldown_sec"": 0.3,
      ""max_concurrent_casts"": 1,
      ""all_values_tunable"": true
    },
    ""player_stats"": {
      ""max_hp"": 100,
      ""max_mana"": 100,
      ""move_speed_mps"": 5.0,
      ""dodge_distance_m"": 3.8,
      ""dodge_cooldown_sec"": 0.42,
      ""i_frame_on_dodge_ms"": 260
    },
    ""boss_stats_default"": {
      ""max_hp"": 22000,
      ""armor"": 100,
      ""armor_hard"": 150,
      ""magic_resist"": 0.1,
      ""poise_bar"": 3500,
      ""stagger_duration_sec"": 7
    }
  },
  ""crit_system"": {
    ""base_crit_chance"": 0.05,
    ""crit_multiplier"": 2.0,
    ""max_crit_chance"": 0.75
  },
  ""weapon_skill_interaction"": {
    ""tier"": ""B_plus_C_starting"",
    ""description"": ""Çarpan + varyant (animasyon + hitbox). Mekanik değişim C'ye (gelecek) saklı."",
    ""layers"": {
      ""multiplier"": ""silah_tablosundan"",
      ""animation"": ""animations[silah][fiil]"",
      ""hitbox"": ""animasyona_gömülü"",
      ""passive"": ""compatible_verbs_e_göre""
    },
    ""range_source"": ""animasyona_gömülü"",
    ""name_formula"": ""{element_prefix} {adjective_prefix} {animation_name}"",
    ""name_shortening"": {
      ""rule"": ""İsim 4+ kelime olursa kısalt"",
      ""example"": ""Ateşli Yoğun Sihirli Dürtme → Ateşli Dürtme""
    },
    ""swap"": {
      ""enabled"": true,
      ""when"": ""savaş_içi"",
      ""weapons_carried"": 2,
      ""recovery_cancel"": true,
      ""cooldown_sec"": 1.2,
      ""animation_sec"": 0.25,
      ""cancels_combo"": true,
      ""dodge_cancels_swap"": true,
      ""passive_changes"": true
    }
  },
  ""mobility_cc"": {
    ""description"": ""Cast mobility, i-frame, CC önceliği, poise kuralları."",
    ""fiil_base_mobility"": {
      ""1"": ""free_move"",
      ""2"": ""free_move"",
      ""3"": ""free_move"",
      ""4"": ""slowed_move"",
      ""5"": ""slowed_move"",
      ""6"": ""slowed_move"",
      ""7"": ""free_move"",
      ""8"": ""free_move"",
      ""9"": ""free_move"",
      ""10"": ""rooted"",
      ""11"": ""slowed_move"",
      ""12"": ""rooted""
    },
    ""sifat_mobility_override"": {
      ""1"": ""bir_seviye_kisitla"",
      ""2"": ""degistirmez"",
      ""3"": ""bir_seviye_serbestle"",
      ""4"": ""rooted_zorla"",
      ""5"": ""bir_seviye_kisitla"",
      ""6"": ""degistirmez"",
      ""7"": ""degistirmez"",
      ""8"": ""bir_seviye_serbestle"",
      ""9"": ""rooted_zorla"",
      ""10"": ""degistirmez"",
      ""11"": ""degistirmez"",
      ""12"": ""bir_seviye_serbestle""
    },
    ""weapon_mobility_mod"": {
      ""1"": 1,
      ""2"": 2,
      ""3"": 0,
      ""4"": 0,
      ""5"": -1,
      ""6"": -1,
      ""7"": -1,
      ""8"": 0,
      ""9"": 1,
      ""10"": -1
    },
    ""mobility_resolution"": ""final = fiil_base + sifat_override + silah_mod. +1 ve üstü = free, -1 = bir kısıtla, -2 ve altı = rooted."",
    ""i_frame"": [
      {
        ""source"": ""dodge"",
        ""duration_ms"": 260,
        ""condition"": ""her_zaman""
      },
      {
        ""source"": ""3-7"",
        ""duration_ms"": 400,
        ""condition"": ""dash_sirasinda""
      },
      {
        ""source"": ""11-10"",
        ""duration_ms"": 300,
        ""condition"": ""minion_spawn_aninda""
      }
    ],
    ""cc_priority"": [
      {
        ""rank"": 1,
        ""cc"": ""stun"",
        ""duration_sec"": 2
      },
      {
        ""rank"": 2,
        ""cc"": ""root"",
        ""duration_sec"": 3
      },
      {
        ""rank"": 3,
        ""cc"": ""silence"",
        ""duration_sec"": 3
      },
      {
        ""rank"": 4,
        ""cc"": ""knockback"",
        ""duration_sec"": ""anlık""
      },
      {
        ""rank"": 5,
        ""cc"": ""slow"",
        ""duration_sec"": 4
      },
      {
        ""rank"": 6,
        ""cc"": ""blind"",
        ""duration_sec"": 3
      },
      {
        ""rank"": 7,
        ""cc"": ""disarm"",
        ""duration_sec"": 3
      },
      {
        ""rank"": 8,
        ""cc"": ""taunt"",
        ""duration_sec"": 3
      }
    ],
    ""cc_stacking"": {
      ""same_cc"": ""süre_uzar"",
      ""different_cc"": ""ikisi_de_uygulanır_ama_bazıları_çakışır""
    },
    ""cc_priority_table"": {
      ""stun_root"": ""stun (root gizli)"",
      ""stun_slow"": ""stun"",
      ""stun_blind"": ""stun + blind"",
      ""root_slow"": ""root (slow gizli)"",
      ""root_silence"": ""root + silence"",
      ""slow_blind"": ""slow + blind"",
      ""silence_blind"": ""silence + blind"",
      ""knockback_stun"": ""knockback + stun""
    },
    ""cc_duration_mult"": {
      ""base"": 1.0,
      ""baglama"": 1.5,
      ""sabitleme"": 2.0,
      ""yogunlastirma"": 0.7,
      ""yayma"": 0.8
    },
    ""cast_time_mobility"": {
      ""lt_0_3"": ""free_move"",
      ""0_3_to_0_5"": ""free_move_vEYA_slowed"",
      ""0_5_to_1_0"": ""slowed_move"",
      ""gt_1_0"": ""rooted""
    },
    ""interrupt"": {
      ""startup"": ""kesilebilir (dodge)"",
      ""active"": ""kesilemez"",
      ""recovery"": ""kesilebilir (dodge, swap, yeni skill)""
    },
    ""poise"": {
      ""hafif"": 10,
      ""orta"": 25,
      ""agir"": 50,
      ""kesme_esigi"": ""gelen_hasar > poise ise iptal + 1 sn stun""
    }
  },
  ""status_system"": {
    ""enabled"": false,
    ""enabled_in"": ""alpha"",
    ""statuses"": [
      {
        ""id"": ""burn"",
        ""element"": ""Ateş"",
        ""type"": ""debuff"",
        ""duration_sec"": 4,
        ""tick_damage"": 3,
        ""max_stacks"": 3
      },
      {
        ""id"": ""regen"",
        ""element"": ""Su"",
        ""type"": ""buff"",
        ""duration_sec"": 3,
        ""tick_heal"": 5,
        ""max_stacks"": 1
      },
      {
        ""id"": ""haste"",
        ""element"": ""Hava"",
        ""type"": ""buff"",
        ""duration_sec"": 3,
        ""speed_bonus"": 0.2,
        ""max_stacks"": 1
      },
      {
        ""id"": ""shield"",
        ""element"": ""Toprak"",
        ""type"": ""buff"",
        ""duration_sec"": 3,
        ""absorb"": 30,
        ""max_stacks"": 1
      },
      {
        ""id"": ""cleanse"",
        ""element"": ""Aydınlık"",
        ""type"": ""instant"",
        ""removes"": 1
      },
      {
        ""id"": ""weaken"",
        ""element"": ""Karanlık"",
        ""type"": ""debuff"",
        ""duration_sec"": 3,
        ""damage_reduction"": 0.15,
        ""max_stacks"": 1
      }
    ],
    ""interactions"": {
      ""enabled"": false,
      ""enabled_in"": ""beta""
    }
  },
  ""hitbox_vfx"": {
    ""description"": ""Hitbox şekli + boyut + VFX tablosu."",
    ""fiil_hitbox"": {
      ""1"": {
        ""shape"": ""capsule"",
        ""base_size"": ""1.5m × 0.5m"",
        ""duration"": ""anlık"",
        ""multi"": false
      },
      ""2"": {
        ""shape"": ""sphere"",
        ""base_size"": ""2m yarıçap"",
        ""duration"": ""anlık"",
        ""multi"": ""dost""
      },
      ""3"": {
        ""shape"": ""line"",
        ""base_size"": ""3m × 0.8m"",
        ""cross_section"": ""width"",
        ""duration"": ""0.3 sn"",
        ""multi"": false
      },
      ""4"": {
        ""shape"": ""sphere"",
        ""base_size"": ""1.5m yarıçap"",
        ""duration"": ""süreli"",
        ""multi"": false
      },
      ""5"": {
        ""shape"": ""sphere"",
        ""base_size"": ""3m yarıçap"",
        ""duration"": ""anlık"",
        ""multi"": true
      },
      ""6"": {
        ""shape"": ""cone"",
        ""base_size"": ""4m × 60°"",
        ""duration"": ""anlık"",
        ""multi"": true
      },
      ""7"": {
        ""shape"": ""capsule"",
        ""base_size"": ""3m × 1m"",
        ""cross_section"": ""width"",
        ""duration"": ""anlık"",
        ""multi"": false
      },
      ""8"": {
        ""shape"": ""sphere"",
        ""base_size"": ""2m yarıçap"",
        ""duration"": ""anlık"",
        ""multi"": ""dost""
      },
      ""9"": {
        ""shape"": ""sphere"",
        ""base_size"": ""2.5m yarıçap"",
        ""duration"": ""anlık"",
        ""multi"": ""dost""
      },
      ""10"": {
        ""shape"": ""sphere"",
        ""base_size"": ""1.5m yarıçap"",
        ""duration"": ""süreli"",";
    }
}
