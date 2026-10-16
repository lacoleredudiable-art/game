namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarDefaults
    {
        private static partial string PayloadJsonPart02() => @"
              ""hitbox_scale_mult"": 1.1
            },
            ""prose_mechanic"": ""Dosta seker"",
            ""prose_feel"": ""Bulaşıcı"",
            ""prose_visual"": ""Yeşil sekme""
          },
          {
            ""id"": ""2-4"",
            ""name"": ""Sabit Şifa"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""Yerleşik şifa alanı"",
            ""passive"": ""7 sn pasif: Sabitleme"",
            ""engine"": {
              ""action"": ""heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted""
            },
            ""prose_mechanic"": ""Yerleşik şifa alanı"",
            ""prose_feel"": ""Kalıcı, sabırlı"",
            ""prose_visual"": ""Yeşil halka""
          },
          {
            ""id"": ""2-6"",
            ""name"": ""Bağlayıcı Şifa"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""Dosta bağlanır"",
            ""passive"": ""4 sn pasif: Bağlama"",
            ""engine"": {
              ""action"": ""heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3,
              ""portal_op"": ""hook""
            },
            ""prose_mechanic"": ""Dosta bağlanır"",
            ""prose_feel"": ""Bağlı, sürekli"",
            ""prose_visual"": ""Yeşil bağ""
          },
          {
            ""id"": ""2-9"",
            ""name"": ""Odaklı Şifa"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""En yaralıyı bulur"",
            ""passive"": ""5 sn pasif: Odaklama"",
            ""engine"": {
              ""action"": ""heal"",
              ""base_heal"": 35,
              ""base_damage"": 0,
              ""base_cost"": 14,
              ""base_cooldown"": 1.0,
              ""hitbox"": ""self_aura"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""En yaralıyı bulur"",
            ""prose_feel"": ""Akıllı, odaklı"",
            ""prose_visual"": ""Yeşil ok""
          }
        ]
      },
      ""3"": {
        ""skills"": [
          {
            ""id"": ""3-1"",
            ""name"": ""Yoğun Adım"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""Kısa, hızlı dash"",
            ""passive"": ""5 sn pasif: Yoğunlaştırma"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2
            },
            ""prose_mechanic"": ""Kısa, hızlı dash"",
            ""prose_feel"": ""Ani, kısa"",
            ""prose_visual"": ""Beyaz iz""
          },
          {
            ""id"": ""3-2"",
            ""name"": ""Emici Adım"",
            ""adjective"": ""Emme"",
            ""effect"": ""Dash + can çal"",
            ""passive"": ""7 sn pasif: Emme"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.3,
              ""damage_mult"": 0.95,
              ""border_mode"": ""tier_20""
            },
            ""prose_mechanic"": ""Dash + can çal"",
            ""prose_feel"": ""Sinsi, emici"",
            ""prose_visual"": ""Kırmızı iz""
          },
          {
            ""id"": ""3-3"",
            ""name"": ""Sıçrayan Adım"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""Çift dash"",
            ""passive"": ""5 sn pasif: Sıçrama"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,
              ""hitbox_scale_mult"": 1.1
            },
            ""prose_mechanic"": ""Çift dash"",
            ""prose_feel"": ""Çevik, ikiz"",
            ""prose_visual"": ""Çift beyaz""
          },
          {
            ""id"": ""3-4"",
            ""name"": ""Sabit Adım"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""Dash + armor"",
            ""passive"": ""7 sn pasif: Sabitleme"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted"",
              ""armor_add"": 25,
              ""buff_duration_sec"": 3.0,
              ""portal_op"": ""anchor_or_recall""
            },
            ""prose_mechanic"": ""Dash + armor"",
            ""prose_feel"": ""Güvenli"",
            ""prose_visual"": ""Mavi kalkan""
          },
          {
            ""id"": ""3-6"",
            ""name"": ""Bağlayıcı Adım"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""Dash + root"",
            ""passive"": ""4 sn pasif: Bağlama"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3
            },
            ""prose_mechanic"": ""Dash + root"",
            ""prose_feel"": ""Kontrolcü"",
            ""prose_visual"": ""Zincir izi""
          },
          {
            ""id"": ""3-9"",
            ""name"": ""Odaklı Adım"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""Dash + kilitlenme"",
            ""passive"": ""5 sn pasif: Odaklama"",
            ""engine"": {
              ""action"": ""dash"",
              ""dash_distance_m"": 3.0,
              ""base_damage"": 0,
              ""base_cost"": 10,
              ""base_cooldown"": 0.9,
              ""hitbox"": ""dash_line"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""Dash + kilitlenme"",
            ""prose_feel"": ""Kilitli"",
            ""prose_visual"": ""Mavi hedef""
          }
        ]
      },
      ""4"": {
        ""skills"": [
          {
            ""id"": ""4-1"",
            ""name"": ""Yoğun Kalkan"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""Dar, güçlü armor"",
            ""passive"": ""5 sn pasif: Yoğunlaştırma"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2
            },
            ""prose_mechanic"": ""Dar, güçlü armor"",
            ""prose_feel"": ""Sağlam"",
            ""prose_visual"": ""Mavi dar halka""
          },
          {
            ""id"": ""4-2"",
            ""name"": ""Emici Kalkan"",
            ""adjective"": ""Emme"",
            ""effect"": ""Hasar→can"",
            ""passive"": ""7 sn pasif: Emme"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.3,
              ""damage_mult"": 0.95
            },
            ""prose_mechanic"": ""Hasar→can"",
            ""prose_feel"": ""Emici"",
            ""prose_visual"": ""Kırmızı→mavi""
          },
          {
            ""id"": ""4-3"",
            ""name"": ""Sıçrayan Kalkan"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""Dosta seker"",
            ""passive"": ""5 sn pasif: Sıçrama"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Sıçrama"",
              ""bounce_targets"": 2,
              ""bounce_damage_mult"": 0.5,
              ""hitbox_scale_mult"": 1.1
            },
            ""prose_mechanic"": ""Dosta seker"",
            ""prose_feel"": ""Paylaşımcı"",
            ""prose_visual"": ""Mavi sekme""
          },
          {
            ""id"": ""4-4"",
            ""name"": ""Sabit Kalkan"",
            ""adjective"": ""Sabitleme"",
            ""effect"": ""Uzun armor"",
            ""passive"": ""7 sn pasif: Sabitleme"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Sabitleme"",
              ""poise_damage_mult"": 1.5,
              ""lifetime_add"": 2.0,
              ""cast_mobility"": ""rooted""
            },
            ""prose_mechanic"": ""Uzun armor"",
            ""prose_feel"": ""Kalıcı"",
            ""prose_visual"": ""Mavi kubbe""
          },
          {
            ""id"": ""4-6"",
            ""name"": ""Bağlayıcı Kalkan"",
            ""adjective"": ""Bağlama"",
            ""effect"": ""Dosta bağlanır"",
            ""passive"": ""4 sn pasif: Bağlama"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Bağlama"",
              ""apply_root_sec"": 1.5,
              ""apply_slow"": 0.3
            },
            ""prose_mechanic"": ""Dosta bağlanır"",
            ""prose_feel"": ""Bağlı"",
            ""prose_visual"": ""Mavi bağ""
          },
          {
            ""id"": ""4-9"",
            ""name"": ""Odaklı Kalkan"",
            ""adjective"": ""Odaklama"",
            ""effect"": ""En çok hasarlıyı korur"",
            ""passive"": ""5 sn pasif: Odaklama"",
            ""engine"": {
              ""action"": ""shield"",
              ""shield_absorb"": 50,
              ""base_damage"": 0,
              ""base_cost"": 12,
              ""base_cooldown"": 1.2,
              ""hitbox"": ""self"",
              ""label"": ""Odaklama"",
              ""ignore_armor"": true,
              ""damage_mult"": 1.15,
              ""max_targets"": 1
            },
            ""prose_mechanic"": ""En çok hasarlıyı korur"",
            ""prose_feel"": ""Akıllı"",
            ""prose_visual"": ""Mavi hedef""
          }
        ]
      },
      ""6"": {
        ""skills"": [
          {
            ""id"": ""6-1"",
            ""name"": ""Yoğun Bağ"",
            ""adjective"": ""Yoğunlaştırma"",
            ""effect"": ""1.5 sn stun"",
            ""passive"": ""5 sn pasif: Yoğunlaştırma"",
            ""engine"": {
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Yoğunlaştırma"",
              ""damage_mult"": 1.35,
              ""hitbox_scale_mult"": 0.55,
              ""poise_damage_mult"": 1.2
            },
            ""prose_mechanic"": ""1.5 sn stun"",
            ""prose_feel"": ""Sert, kısa"",
            ""prose_visual"": ""Beyaz dar halka""
          },
          {
            ""id"": ""6-2"",
            ""name"": ""Emici Bağ"",
            ""adjective"": ""Emme"",
            ""effect"": ""CC + can çal"",
            ""passive"": ""7 sn pasif: Emme"",
            ""engine"": {
              ""action"": ""cc"",
              ""cc_kind"": ""root"",
              ""cc_duration_sec"": 1.5,
              ""base_damage"": 8,
              ""base_cost"": 14,
              ""base_cooldown"": 1.5,
              ""hitbox"": ""target"",
              ""label"": ""Emme"",
              ""lifesteal"": 0.3,
              ""damage_mult"": 0.95
            },
            ""prose_mechanic"": ""CC + can çal"",
            ""prose_feel"": ""Sinsi"",
            ""prose_visual"": ""Mor halka""
          },
          {
            ""id"": ""6-3"",
            ""name"": ""Sıçrayan Bağ"",
            ""adjective"": ""Sıçrama"",
            ""effect"": ""2. hedefe seker"",
            ""passive"": ""5 sn pasif: Sıçrama"",
            ""engine"": {";
    }
}
