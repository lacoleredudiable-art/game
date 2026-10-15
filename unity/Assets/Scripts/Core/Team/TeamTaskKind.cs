using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Takım görevi türü (docs 18-kul-ejderi.md §5.3, §6.3, §7.3).</summary>
    public enum TeamTaskKind
    {
        /// <summary>Uyku Mührü: her taşta 2 s kesintisiz dur; 3 taş 15 s içinde yanmalı (sırayla olabilir).</summary>
        SleepSeal,
        /// <summary>Kor Mührü: 3 taşta AYNI ANDA, birlikte 3 s kesintisiz dur; süre 10 s.</summary>
        HeartSeal,
        /// <summary>Kül Yağmuru: süre dolunca bütün yaşayanlar bir gölge şeridinde olmalı.</summary>
        AshRain,
    }
}
