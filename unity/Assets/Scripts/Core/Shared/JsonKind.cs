using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dovus.Core.Shared
{
    /// <summary>
    /// Bağımlılıksız, minimal JSON okuyucu (Shared). Core saf C# kuralı (AGENTS.md #1) yüzünden
    /// System.Text.Json / Newtonsoft yerine burada yaşar. docs/element-sistemi.json gibi
    /// gelişen, iç içe alanları çok olan (special/zone_effect/target_behaviors/engine_modifiers)
    /// dosyaları elle alan-alan string taramak yerine gerçek bir ağaca çözer.
    /// </summary>
    public enum JsonKind
    {
        Null,
        String,
        Number,
        Bool,
        Object,
        Array
    }
}
