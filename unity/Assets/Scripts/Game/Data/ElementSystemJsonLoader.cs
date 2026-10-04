using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Core.Mechanic;
using System;
using UnityEngine;
namespace Dovus.Game.Data
{
/// <summary>
    /// Binding sıra adım 1: tek canonical Resources JSON'u bir kez yükler ve bütün
    /// tüketicilere aynı parse edilmiş tasarımı verir.
    /// </summary>
    public static class ElementSystemJsonLoader
    {
        public const string ResourcePath = ElementSystemRuntimeCache.ResourcePath;
        public const string RequiredVersion = ElementSystemRuntimeCache.RequiredVersion;

        public static bool TryLoad(out ElementSystemDesign design) =>
            ElementSystemRuntimeCache.TryGet(out design);

        public static ElementSystemDesign LoadRequired()
        {
            if (TryLoad(out ElementSystemDesign design))
                return design;
            throw new InvalidOperationException(
                $"Resources/{ResourcePath}.json canonical v{RequiredVersion} yüklenemedi.");
        }

        public static void ClearCache() => ElementSystemRuntimeCache.ResetForEditor();
    }
}
