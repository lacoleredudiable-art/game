using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct ElementPaintNode
    {
        public ElementPaintNode(
            int id,
            string name,
            string namePrefix,
            string colorHex,
            string vfx,
            string status = "",
            string statusEffect = "",
            float statusDurationSec = 0f)
        {
            Id = id;
            Name = name ?? string.Empty;
            NamePrefix = namePrefix ?? string.Empty;
            ColorHex = colorHex ?? string.Empty;
            Vfx = vfx ?? string.Empty;
            Status = status ?? string.Empty;
            StatusEffect = statusEffect ?? string.Empty;
            StatusDurationSec = statusDurationSec;
        }

        public int Id { get; }
        public string Name { get; }
        public string NamePrefix { get; }
        public string ColorHex { get; }
        public string Vfx { get; }
        /// <summary>elements[].status — burn, regen, haste, shield, cleanse, weaken.</summary>
        public string Status { get; }
        /// <summary>elements[].status_effect metni.</summary>
        public string StatusEffect { get; }
        /// <summary>elements[].status_duration saniye.</summary>
        public float StatusDurationSec { get; }
    }
}
