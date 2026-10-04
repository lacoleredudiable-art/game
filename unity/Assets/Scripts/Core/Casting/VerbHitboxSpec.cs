using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Casting
{
    /// <summary>docs/element-sistemi.json hitbox_vfx.fiil_hitbox satırı (metin boyutlar sayıya çevrilir).</summary>
    public readonly struct VerbHitboxSpec
    {
        public VerbHitboxSpec(
            string shape,
            float sizeA,
            float sizeB,
            bool isRadius,
            float durationSec,
            bool isTimed,
            string multi,
            bool sizeBIsWidth = false)
        {
            Shape = shape ?? string.Empty;
            SizeA = sizeA;
            SizeB = sizeB;
            IsRadius = isRadius;
            DurationSec = durationSec;
            IsTimed = isTimed;
            Multi = multi ?? string.Empty;
            SizeBIsWidth = sizeBIsWidth;
        }

        public string Shape { get; }
        /// <summary>Uzunluk (line/capsule/cone) veya yarıçap (sphere/point/cylinder), metre.</summary>
        public float SizeA { get; }
        /// <summary>Yarıçap (m) ya da koni açısı (°); yarıçaplı şekillerde 0.</summary>
        public float SizeB { get; }
        public bool IsRadius { get; }
        /// <summary>"0.3 sn" → 0.3; "anlık" → 0; "süreli" → 0 ve <see cref="IsTimed"/>.</summary>
        public float DurationSec { get; }
        /// <summary>Süre skill engine'inden gelir (reflect_duration_sec, minion_duration_sec…).</summary>
        public bool IsTimed { get; }
        public string Multi { get; }
        /// <summary>
        /// İkinci sayı yarıçap değil genişlik (çap). Oyun yarıçapı bunun yarısıdır;
        /// kapsülün görünür genişliği yazılan metreyle aynı kalır.
        /// </summary>
        public bool SizeBIsWidth { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Shape);
    }
}
