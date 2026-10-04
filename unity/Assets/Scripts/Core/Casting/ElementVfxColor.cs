using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Casting
{
    public readonly struct ElementVfxColor
    {
        public ElementVfxColor(string primary, string secondary, float brightness, float saturation)
        {
            Primary = primary ?? string.Empty;
            Secondary = secondary ?? string.Empty;
            Brightness = brightness;
            Saturation = saturation;
        }

        public string Primary { get; }
        public string Secondary { get; }
        public float Brightness { get; }
        public float Saturation { get; }
    }
}
