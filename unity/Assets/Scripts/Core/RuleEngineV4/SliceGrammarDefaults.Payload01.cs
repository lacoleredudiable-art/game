namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarDefaults
    {
        private static partial string PayloadJsonPart01() => @"
{
  ""system"": {
    ""version"": ""v4-slice-grammar""
  },
  ""combo_system"": {
    ""current_max_length"": 2,
    ""future_lengths"": [
      3,
      4
    ],
    ""combo_structure"": {
      ""1"": ""Kök fiil"",
      ""2"": ""Fiil + Sıfat"",
      ""3_future"": ""(Fiil+Sıfat) + Sıfat"",
      ""4_future"": ""(Fiil+Sıfat) + (Sıfat+Sıfat)""
    },
    ""skills_per_build"": 36,
    ""unique_skills"": 144,
    ""total_build_skill_pairs"": 33264,
    ""player_main_skills"": ""5-6"",
    ""learning_curve"": {
      ""level_1"": ""2'li only"",
      ""level_5"": ""3'lü unlock (future)"",
      ""level_10"": ""4'lü unlock (future)""
    },
    ""drawing_preview"": {
      ""enabled"": true,
      ""live_update"": true,
      ""description"": ""Oyuncu çizerken skill ismi ve efekti anlık gösterilir""
    },
    ""wrong_combo_result"": ""farklı_skill_ceza_değil"",
    ""parse_lock"": ""İlk nokta = o rünün fiil yüzü; ikinci nokta = o rünün sıfat yüzü.""
  },
  ""runes"": [
    {
      ""id"": 1,
      ""name"": ""Saldırı"",
      ""verb_face"": ""Saldırı"",
      ""adjective_face"": ""Yoğunlaştırma"",
      ""adjective_prefix"": ""Yoğun"",
      ""verb_noun"": ""Vuruş"",
      ""family"": ""strike"",
      ""category"": ""hasar"",
      ""target_mode"": ""enemy_only"",
      ""base_effect"": ""Tek hedefe dar, odaklı vuruş"",
      ""passive_duration_default"": 5
    },
    {
      ""id"": 2,
      ""name"": ""İyileştirme"",
      ""verb_face"": ""İyileştirme"",
      ""adjective_face"": ""Emme"",
      ""adjective_prefix"": ""Emici"",
      ""verb_noun"": ""Şifa"",
      ""family"": ""mend"",
      ""category"": ""destek"",
      ""target_mode"": ""self_or_ally"",
      ""base_effect"": ""Can verir"",
      ""passive_duration_default"": 7
    },
    {
      ""id"": 3,
      ""name"": ""Hareket"",
      ""verb_face"": ""Hareket"",
      ""adjective_face"": ""Sıçrama"",
      ""adjective_prefix"": ""Sıçrayan"",
      ""verb_noun"": ""Adım"",
      ""family"": ""motion"",
      ""category"": ""hareket"",
      ""target_mode"": ""self_only"",
      ""base_effect"": ""Konum değiştirir, dash"",
      ""passive_duration_default"": 5
    },
    {
      ""id"": 4,
      ""name"": ""Savunma"",
      ""verb_face"": ""Savunma"",
      ""adjective_face"": ""Sabitleme"",
      ""adjective_prefix"": ""Sabit"",
      ""verb_noun"": ""Kalkan"",
      ""family"": ""guard"",
      ""category"": ""savunma"",
      ""target_mode"": ""self_only"",
      ""base_effect"": ""Hasar emer, armor verir"",
      ""passive_duration_default"": 7
    },
    {
      ""id"": 6,
      ""name"": ""Kontrol"",
      ""verb_face"": ""Kontrol"",
      ""adjective_face"": ""Bağlama"",
      ""adjective_prefix"": ""Bağlayıcı"",
      ""verb_noun"": ""Bağ"",
      ""family"": ""control"",
      ""category"": ""savunma"",
      ""target_mode"": ""enemy_only"",
      ""base_effect"": ""CC uygular (root/slow/stun)"",
      ""passive_duration_default"": 4
    },
    {
      ""id"": 9,
      ""name"": ""Arındırma"",
      ""verb_face"": ""Arındırma"",
      ""adjective_face"": ""Odaklama"",
      ""adjective_prefix"": ""Odaklı"",
      ""verb_noun"": ""Arınma"",
      ""family"": ""purge"",
      ""category"": ""destek"",
      ""target_mode"": ""self_or_ally"",
      ""base_effect"": ""Debuff siler"",
      ""passive_duration_default"": 5
    }
  ],
  ""verb_base"": {
    ""1"": {
      ""action"": ""damage"",
      ""base_damage"": 40,
      ""base_poise"": 15,
      ""base_cost"": 12,
      ""base_cooldown"": 0.8,
      ""hitbox"": ""projectile""
    },
    ""2"": {
      ""action"": ""heal"",
      ""base_heal"": 35,
      ""base_damage"": 0,
      ""base_cost"": 14,
      ""base_cooldown"": 1.0,
      ""hitbox"": ""self_aura""
    },
    ""3"": {
      ""action"": ""dash"",
      ""dash_distance_m"": 3.0,
      ""base_damage"": 0,
      ""base_cost"": 10,
      ""base_cooldown"": 0.9,
      ""hitbox"": ""dash_line""
    },
    ""4"": {
      ""action"": ""shield"",
      ""shield_absorb"": 50,
      ""base_damage"": 0,
      ""base_cost"": 12,
      ""base_cooldown"": 1.2,
      ""hitbox"": ""self""
    },
    ""6"": {
      ""action"": ""cc"",
      ""cc_kind"": ""root"",
      ""cc_duration_sec"": 1.5,
      ""base_damage"": 8,
      ""base_cost"": 14,
      ""base_cooldown"": 1.5,
      ""hitbox"": ""target""
    },
    ""9"": {
      ""action"": ""cleanse"",
      ""cleanse_count"": 1,
      ""base_damage"": 0,
      ""base_cost"": 10,
      ""base_cooldown"": 1.0,
      ""hitbox"": ""self_or_ally""
    }
  },
  ""adjective_mods"": {
    ""1"": {
      ""label"": ""Yoğunlaştırma"",
      ""damage_mult"": 1.35,
      ""hitbox_scale_mult"": 0.55,
      ""poise_damage_mult"": 1.2
    },
    ""2"": {
      ""label"": ""Emme"",
      ""lifesteal"": 0.3,
      ""damage_mult"": 0.95
    },
    ""3"": {
      ""label"": ""Sıçrama"",
      ""bounce_targets"": 2,
      ""bounce_damage_mult"": 0.5,
      ""hitbox_scale_mult"": 1.1
    },
    ""4"": {
      ""label"": ""Sabitleme"",
      ""poise_damage_mult"": 1.5,
      ""lifetime_add"": 2.0,
      ""cast_mobility"": ""rooted""
    },
    ""6"": {
      ""label"": ""Bağlama"",
      ""apply_root_sec"": 1.5,
      ""apply_slow"": 0.3
    },
    ""9"": {
      ""label"": ""Odaklama"",
      ""ignore_armor"": true,
      ""damage_mult"": 1.15,
      ""max_targets"": 1
    }
  },
  ""skills"": {
    ""by_verb"": {
      ""1"": {
        ""skills"": [
          {
            ""id"": ""1-1"",
            ""name"": ""Yoğun Vuruş"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""Dar, odaklı, yüksek hasar"",
            ""passive"": ""5 sn boyunca vuruşlar +%15 hasar"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2,
              ""swap_cancel"": true
            },
            ""prose_mechanic"": ""Dar, odaklı, yüksek hasar"",
            ""prose_feel"": ""Sert, kararlı, tek noktaya"",
            ""prose_visual"": ""Tek çizgi, beyaz iz""
          },
          {
            ""id"": ""1-2"",
            ""name"": ""Emici Vuruş"",
            ""adjective"": ""Emme"",
            ""effect"": ""Vurur, %30 can çalar"",
            ""passive"": ""7 sn boyunca vuruşlar can çalar"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.3,
              ""damage_mult"": 0.95,
              ""border_mode"": ""tier_20""
            },
            ""prose_mechanic"": ""Vurur, %30 can çalar"",
            ""prose_feel"": ""Aç, emici"",
            ""prose_visual"": ""Kırmızı iz, kan akışı""
          },
          {
            ""id"": ""1-3"",
            ""name"": ""Sıçrayan Vuruş"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""2. hedefe %50 seker"",
            ""passive"": ""5 sn boyunca vuruşlar seker"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,
              ""hitbox_scale_mult"": 1.1
            },
            ""prose_mechanic"": ""2. hedefe %50 seker"",
            ""prose_feel"": ""Çevik, kaotik"",
            ""prose_visual"": ""Sarı kıvılcım sekmesi""
          },
          {
            ""id"": ""1-4"",
            ""name"": ""Sabit Vuruş"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""Poise ×1.5"",
            ""passive"": ""7 sn boyunca yerinde kalırsın, poise kırma ×1.5"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted"",
              ""swap_cancel"": true
            },
            ""prose_mechanic"": ""Poise ×1.5"",
            ""prose_feel"": ""Ağır, kırıcı"",
            ""prose_visual"": ""Yere çakma, toz""
          },
          {
            ""id"": ""1-6"",
            ""name"": ""Bağlayıcı Vuruş"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""1.5 sn root"",
            ""passive"": ""4 sn boyunca vuruşlar hedefi yavaşlatır"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3
            },
            ""prose_mechanic"": ""1.5 sn root"",
            ""prose_feel"": ""Kontrolcü"",
            ""prose_visual"": ""Zincir, bağ izi""
          },
          {
            ""id"": ""1-9"",
            ""name"": ""Odaklı Vuruş"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""Zırh yoksayar"",
            ""passive"": ""5 sn boyunca vuruşlar zırhı yoksayar"",
            ""engine"": {
              ""action"": ""damage"",
              ""base_damage"": 40,
              ""base_poise"": 15,
              ""base_cost"": 12,
              ""base_cooldown"": 0.8,
              ""hitbox"": ""projectile"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""Zırh yoksayar"",
            ""prose_feel"": ""Delici, keskin"",
            ""prose_visual"": ""Mavi nokta, delme""
          }
        ]
      },
      ""2"": {
        ""skills"": [
          {
            ""id"": ""2-1"",
            ""name"": ""Yoğun Şifa"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""Anlık büyük can"",
            ""passive"": ""5 sn pasif: Yoğunlaştırma"",
            ""engine"": {
              ""action"": ""heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2
            },
            ""prose_mechanic"": ""Anlık büyük can"",
            ""prose_feel"": ""Ani, güçlü"",
            ""prose_visual"": ""Yeşil patlama""
          },
          {
            ""id"": ""2-2"",
            ""name"": ""Emici Şifa"",
            ""adjective"": ""Emme"",
            ""effect"": ""Dren ile verir"",
            ""passive"": ""7 sn pasif: Emme"",
            ""engine"": {
              ""action"": ""drain_heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.35,
              ""damage_mult"": 0.95
            },
            ""prose_mechanic"": ""Dren ile verir"",
            ""prose_feel"": ""Sömüren"",
            ""prose_visual"": ""Kırmızı→yeşil akış""
          },
          {
            ""id"": ""2-3"",
            ""name"": ""Sıçrayan Şifa"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""Dosta seker"",
            ""passive"": ""5 sn pasif: Sıçrama"",
            ""engine"": {
              ""action"": ""heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,";
    }
}
