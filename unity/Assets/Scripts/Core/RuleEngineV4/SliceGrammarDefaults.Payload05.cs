namespace Dovus.Core.RuleEngineV4
{
    static partial class SliceGrammarDefaults
    {
        private static partial string PayloadJsonPart05() => @"
        ""multi"": false
      },
      ""11"": {
        ""shape"": ""point"",
        ""base_size"": ""1m yarıçap"",
        ""duration"": ""anlık"",
        ""multi"": false
      },
      ""12"": {
        ""shape"": ""cylinder"",
        ""base_size"": ""4m yarıçap"",
        ""duration"": ""süreli"",
        ""multi"": true
      }
    },
    ""sifat_override"": {
      ""1"": {
        ""size_mult"": 0.4,
        ""damage_mult"": 1.3
      },
      ""2"": {
        ""size_mult"": 1.0,
        ""damage_mult"": 0.8,
        ""lifesteal"": 0.3
      },
      ""3"": {
        ""size_mult"": 1.0,
        ""chain_count"": 2
      },
      ""4"": {
        ""size_mult"": 1.0,
        ""duration_mult"": 3
      },
      ""5"": {
        ""size_mult"": 2.5,
        ""damage_mult"": 0.7
      },
      ""6"": {
        ""size_mult"": 1.0,
        ""add_cc"": ""root""
      },
      ""7"": {
        ""size_mult"": 1.5,
        ""add_cc"": ""blind""
      },
      ""8"": {
        ""size_mult"": 1.0,
        ""add_buff"": 0.2
      },
      ""9"": {
        ""size_mult"": 0.5,
        ""single_target"": true
      },
      ""10"": {
        ""size_mult"": 1.0,
        ""add_reflect"": 0.4
      },
      ""11"": {
        ""size_mult"": 1.0,
        ""second_trigger"": 0.3
      },
      ""12"": {
        ""size_mult"": 1.0,
        ""duration_mult"": 2
      }
    },
    ""weapon_size_mult"": {
      ""1"": 0.5,
      ""2"": 2.353,
      ""3"": 1.529,
      ""4"": 1.0,
      ""5"": 1.176,
      ""6"": 1.0,
      ""7"": 3.0,
      ""8"": 2.5,
      ""9"": 2.8,
      ""10"": 0.5
    },
    ""hitbox_formula"": ""final = base × weapon.range_mult × adjective.size_mult"",
    ""vfx_prefab_naming"": ""VFX_{Element}_{FiilId}_{SifatId}"",
    ""vfx_prefab_count"": 72,
    ""vfx_prefab_note"": ""6 element × 12 fiil = 72 ana prefab. Sıfat runtime modifiye eder."",
    ""element_color"": {
      ""1"": {
        ""primary"": ""#ff4d1a"",
        ""secondary"": ""#ffaa00"",
        ""brightness"": 1.2,
        ""saturation"": 1.0
      },
      ""2"": {
        ""primary"": ""#1a8cff"",
        ""secondary"": ""#00d9ff"",
        ""brightness"": 1.0,
        ""saturation"": 0.8
      },
      ""3"": {
        ""primary"": ""#b3e6ff"",
        ""secondary"": ""#ffffff"",
        ""brightness"": 1.1,
        ""saturation"": 0.6
      },
      ""4"": {
        ""primary"": ""#8b6f3a"",
        ""secondary"": ""#4d3f1a"",
        ""brightness"": 0.9,
        ""saturation"": 0.9
      },
      ""5"": {
        ""primary"": ""#ffd94d"",
        ""secondary"": ""#ffffff"",
        ""brightness"": 1.4,
        ""saturation"": 0.7
      },
      ""6"": {
        ""primary"": ""#4d1a66"",
        ""secondary"": ""#000000"",
        ""brightness"": 0.7,
        ""saturation"": 1.0
      }
    },
    ""element_shape_icon"": {
      ""1"": ""triangle_up"",
      ""2"": ""circle"",
      ""3"": ""wave"",
      ""4"": ""square"",
      ""5"": ""star"",
      ""6"": ""triangle_down""
    },
    ""hit_feel"": {
      ""capsule"": {
        ""shake"": 0.1,
        ""hit_stop"": 0.04,
        ""sound"": ""kisa""
      },
      ""sphere"": {
        ""shake"": 0.2,
        ""hit_stop"": 0.08,
        ""sound"": ""orta""
      },
      ""cone"": {
        ""shake"": 0.15,
        ""hit_stop"": 0.06,
        ""sound"": ""orta""
      },
      ""line"": {
        ""shake"": 0.12,
        ""hit_stop"": 0.04,
        ""sound"": ""kisa""
      },
      ""cylinder"": {
        ""shake"": 0.25,
        ""hit_stop"": 0.1,
        ""sound"": ""uzun""
      }
    },
    ""crit_multiplier"": {
      ""shake"": 1.5,
      ""flash"": ""altin"",
      ""duration"": 0.1
    }
  }
}";
    }
}
