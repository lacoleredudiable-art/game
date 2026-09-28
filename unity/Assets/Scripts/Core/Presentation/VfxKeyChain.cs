using System;
using System.Collections.Generic;

namespace Dovus.Core.Presentation
{
    /// <summary>
    /// hitbox_vfx.vfx_prefab_naming (<c>VFX_{Element}_{FiilId}_{SifatId}</c>) için geri düşme
    /// zinciri: tam anahtar → <c>VFX_{Element}_{FiilId}</c> → <c>VFX_{FiilId}</c>. 144 prefab
    /// yerine en genel eşleşme yeter; sıfat runtime'da davranışı değiştirir. Zincirin sonunda
    /// hiçbiri yoksa çağıran prosedürel şekle düşer.
    /// </summary>
    public static class VfxKeyChain
    {
        const string Prefix = "VFX_";

        public static IReadOnlyList<string> Expand(string key)
        {
            var chain = new List<string>(3);
            if (string.IsNullOrEmpty(key))
                return chain;

            chain.Add(key);
            if (!key.StartsWith(Prefix, StringComparison.Ordinal))
                return chain;

            string[] parts = key.Substring(Prefix.Length).Split('_');
            if (parts.Length != 3 || parts[0].Length == 0 || parts[1].Length == 0)
                return chain;

            chain.Add(Prefix + parts[0] + "_" + parts[1]);
            chain.Add(Prefix + parts[1]);
            return chain;
        }
    }
}
