using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// VFX asset yokken trail/impact için renkli primitive (küre veya çizgi).
    /// Stiller + renkler: docs/prezentasyon-katmani.json → vfx_binding
    /// (Resources/Presentation/prezentasyon-katmani). LivingEffectView'e dokunmaz.
    /// </summary>
    public static class PlaceholderFactory
    {
        const string CatalogResourcePath = "Presentation/prezentasyon-katmani";
        const string TrailAssetFolder = "Vfx/Trail";
        const string ImpactAssetFolder = "Vfx/Impact";

        // asset_missing_handling.log_warning — stil başına bir kez.
        static readonly HashSet<string> WarnedMissing = new(StringComparer.Ordinal);

        static bool _catalogReady;
        static readonly Dictionary<string, Color> ElementPrimary =
            new(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, bool> TrailUsesLine =
            new(StringComparer.Ordinal);
        static readonly HashSet<string> TrailStyles = new(StringComparer.Ordinal);
        static readonly HashSet<string> ImpactStyles = new(StringComparer.Ordinal);

        /// <summary>
        /// Trail stili için asset yoksa element rengiyle çizgi veya küre üretir.
        /// </summary>
        public static GameObject CreateTrail(
            string styleId,
            string elementName,
            Vector3 from,
            Vector3 to,
            Transform parent = null)
        {
            EnsureCatalog();
            styleId = styleId ?? string.Empty;

            GameObject asset = TryLoadPrefab(TrailAssetFolder, styleId);
            if (asset != null)
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                instance.name = $"Trail_{styleId}";
                instance.transform.position = from;
                return instance;
            }

            WarnMissingOnce("trail", styleId);
            Color color = ResolveElementColor(elementName);
            bool useLine = TrailUsesLine.TryGetValue(styleId, out bool line) ? line : true;
            return useLine
                ? CreateLinePlaceholder($"PlaceholderTrail_{styleId}", color, from, to, parent)
                : CreateSpherePlaceholder($"PlaceholderTrail_{styleId}", color, Mid(from, to), parent);
        }

        /// <summary>
        /// Impact stili için asset yoksa element rengiyle küre üretir.
        /// <c>none</c> stilinde nesne oluşturmaz.
        /// </summary>
        public static GameObject CreateImpact(
            string styleId,
            string elementName,
            Vector3 position,
            Transform parent = null)
        {
            EnsureCatalog();
            styleId = styleId ?? string.Empty;
            if (string.Equals(styleId, "none", StringComparison.Ordinal))
                return null;

            GameObject asset = TryLoadPrefab(ImpactAssetFolder, styleId);
            if (asset != null)
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                instance.name = $"Impact_{styleId}";
                instance.transform.position = position;
                return instance;
            }

            WarnMissingOnce("impact", styleId);
            Color color = ResolveElementColor(elementName);
            return CreateSpherePlaceholder($"PlaceholderImpact_{styleId}", color, position, parent);
        }

        public static bool TryGetElementColor(string elementName, out Color color)
        {
            EnsureCatalog();
            return ElementPrimary.TryGetValue(elementName ?? string.Empty, out color);
        }

        public static IReadOnlyCollection<string> KnownTrailStyles
        {
            get
            {
                EnsureCatalog();
                return TrailStyles;
            }
        }

        public static IReadOnlyCollection<string> KnownImpactStyles
        {
            get
            {
                EnsureCatalog();
                return ImpactStyles;
            }
        }

        static GameObject TryLoadPrefab(string folder, string styleId)
        {
            if (string.IsNullOrEmpty(styleId))
                return null;
            return Resources.Load<GameObject>($"{folder}/{styleId}");
        }

        static void WarnMissingOnce(string kind, string styleId)
        {
            string key = kind + ":" + styleId;
            if (!WarnedMissing.Add(key))
                return;

            // prezentasyon-katmani.json implementation_notes.asset_missing_handling.log_warning
            Debug.LogWarning(
                $"[PlaceholderFactory] VFX asset missing ({kind}/{styleId}); " +
                "using element_color primitive placeholder.");
        }

        static Color ResolveElementColor(string elementName)
        {
            if (!string.IsNullOrEmpty(elementName) &&
                ElementPrimary.TryGetValue(elementName, out Color c))
                return c;

            // Bilinmeyen element: camgöbeği oyuncu efekti (kırmızı-turuncu yasak).
            return new Color(0.373f, 0.941f, 1f, 0.95f);
        }

        static Vector3 Mid(Vector3 a, Vector3 b) => (a + b) * 0.5f;

        static GameObject CreateSpherePlaceholder(
            string name,
            Color color,
            Vector3 position,
            Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.45f;

            go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh.Get(PrimitiveType.Sphere);
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = MakeGlowMat(color);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        static GameObject CreateLinePlaceholder(
            string name,
            Color color,
            Vector3 from,
            Vector3 to,
            Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = from;

            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = MakeGlowMat(color);
            line.widthMultiplier = 0.12f;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.loop = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to.sqrMagnitude > 1e-6f ? to : from + Vector3.forward * 1.5f);
            line.startColor = color;
            line.endColor = color;
            return go;
        }

        // LivingEffectView / GroundScarField ile aynı saydam Unlit deseni.
        static Material MakeGlowMat(Color c)
        {
            var shader = FindTransparentUnlitShader();
            var mat = new Material(shader);
            ConfigureTransparentFallback(mat);
            c.a = Mathf.Clamp01(c.a > 0.01f ? c.a : 0.95f);
            SetMatColor(mat, c);
            return mat;
        }

        static Shader FindTransparentUnlitShader()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return shader != null ? shader : Shader.Find("Hidden/Internal-Colored");
        }

        static void ConfigureTransparentFallback(Material mat)
        {
            if (!mat.HasProperty("_Surface"))
                return;

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 1f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        static void SetMatColor(Material mat, Color c)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
        }

        static void EnsureCatalog()
        {
            if (_catalogReady)
                return;
            _catalogReady = true;

            var asset = Resources.Load<TextAsset>(CatalogResourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    ParseCatalog(MiniJson.Parse(asset.text));
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[PlaceholderFactory] prezentasyon-katmani okunamadı, gömülü yedek: {e.Message}");
                }
            }

            LoadEmbeddedFallback();
        }

        static void ParseCatalog(JsonValue root)
        {
            JsonValue vb = root["vfx_binding"];

            foreach (KeyValuePair<string, JsonValue> kv in vb["element_colors"].AsObject())
            {
                string hex = kv.Value["primary"].AsString();
                if (TryParseHexColor(hex, out Color c))
                    ElementPrimary[kv.Key] = c;
            }

            foreach (KeyValuePair<string, JsonValue> kv in vb["trail_vfx"].AsObject())
            {
                TrailStyles.Add(kv.Key);
                string style = kv.Value["vfx_style"].AsString();
                TrailUsesLine[kv.Key] = IsLineVfxStyle(style);
            }

            foreach (KeyValuePair<string, JsonValue> kv in vb["impact_vfx"].AsObject())
                ImpactStyles.Add(kv.Key);
        }

        /// <summary>
        /// Resources yoksa docs/prezentasyon-katmani.json vfx_binding.element_colors
        /// primary değerleri (16 Eylül spec).
        /// </summary>
        static void LoadEmbeddedFallback()
        {
            SeedColor("Ateş", "#c45c26");
            SeedColor("Su", "#39646a");
            SeedColor("Hava", "#87a96b");
            SeedColor("Toprak", "#877dd9");
            SeedColor("Aydınlık", "#c9a227");
            SeedColor("Karanlık", "#5c8a7d");

            string[] lineTrails =
            {
                "straight", "thin_streak", "curved_glow", "phase_shimmer", "arc_lightning",
                "blink_line", "double_streak", "tether_line"
            };
            string[] sphereTrails =
            {
                "afterimage_trail", "spiral_inward", "ring_outward", "burst_flash",
                "smoke_puff", "ground_marker", "whoosh_steady"
            };
            foreach (string id in lineTrails)
            {
                TrailStyles.Add(id);
                TrailUsesLine[id] = true;
            }

            foreach (string id in sphereTrails)
            {
                TrailStyles.Add(id);
                TrailUsesLine[id] = false;
            }

            string[] impacts =
            {
                "strike", "pierce_hit", "pulse", "tick", "drain", "cleave", "impact", "reflect",
                "none"
            };
            foreach (string id in impacts)
                ImpactStyles.Add(id);
        }

        static void SeedColor(string name, string hex)
        {
            if (TryParseHexColor(hex, out Color c))
                ElementPrimary[name] = c;
        }

        static bool IsLineVfxStyle(string vfxStyle)
        {
            if (string.IsNullOrEmpty(vfxStyle))
                return false;

            return vfxStyle.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0
                   || vfxStyle.IndexOf("trail", StringComparison.OrdinalIgnoreCase) >= 0
                   || vfxStyle.IndexOf("beam", StringComparison.OrdinalIgnoreCase) >= 0
                   || vfxStyle.IndexOf("lightning", StringComparison.OrdinalIgnoreCase) >= 0
                   || vfxStyle.IndexOf("tether", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool TryParseHexColor(string hex, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            hex = hex.Trim();
            if (hex[0] == '#')
                hex = hex.Substring(1);
            if (hex.Length != 6)
                return false;

            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
                !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
                !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                return false;

            color = new Color(r / 255f, g / 255f, b / 255f, 0.95f);
            return true;
        }
    }
}
