using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Composition.Builders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// O-7.3: görsel duvar 360° kapalı halka; iç yüz çarpışma iç yüzüyle aynı düzlemlerde (18 m), dış yüz 22 m,
    /// yükseklik 4 m, düz koyu taş. O-7.7 / O-8.7: duvar ötesinde 300 m düz zemin, üstel sis 0,012 #4A5260; kamera
    /// arka planı sis rengi, zeminin bittiği kenar sisin içinde görünmez.
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float ShellTolM = 1e-3f;
        const float ShellProbeHeightFrac = 0.5f;
        const float ShellOuterProbeM = 40f;
        const float ShellTopProbeLiftM = 10f;
        /// <summary>Renk karşılaştırması: 8 bitlik bir kanal adımı.</summary>
        const float ShellColorTol = 1.5f / 255f;
        /// <summary>Zemin kenarı çekimi: arena merkezinin üstünden, ufkun biraz altına bakan kamera.</summary>
        const float EdgeShotHeightM = 30f;
        const float EdgeShotPitchDeg = 5f;
        /// <summary>Yalnız zemini çizmek için geçici katman (oyunda kullanılmıyor).</summary>
        const int EdgeShotLayer = 31;
        /// <summary>Ardışık iki satır arasında kanal başına en çok bu kadar sıçrama: kenar görünmez.</summary>
        const int EdgeStepMaxLevels = 2;
        /// <summary>En alt satır sis renginden en az bu kadar ayrışmalı: zemin gerçekten çizildi.</summary>
        const int GroundPresentMinLevels = 3;

        RuleEngineV4SliceArenaBeyond BeyondSpec => _catalog.Catalog.SliceArenaVisuals.Beyond;

        [UnityTest]
        public IEnumerator O_7_3_GorselDuvar_KapaliHalka_CarpismaIleAyniYuz()
        {
            yield return null;
            RuleEngineV4SliceArena spec = ArenaSpec;
            Assert.That(spec.OuterWallVisualInnerRadiusM, Is.EqualTo(18f), "JSON görsel iç yüz");
            Assert.That(spec.OuterWallVisualOuterRadiusM, Is.EqualTo(22f), "JSON görsel dış yüz");
            Assert.That(spec.OuterWallHeightM, Is.EqualTo(4f), "JSON duvar yüksekliği");
            Assert.That(spec.OuterWallColorHex, Is.EqualTo("#3A4048"), "JSON duvar rengi");

            Transform wall = GameObject.Find(ArenaRoot).transform.Find(RuleEngineV4SliceArenaShellBuilderDefaults.WallName);
            Assert.That(wall, Is.Not.Null, "görsel duvar yok");
            Assert.That(wall.GetComponentsInChildren<Collider>(true), Is.Empty, "görsel duvarda collider var");
            Assert.That(wall.gameObject.layer, Is.EqualTo(RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer), "görsel duvar katmanı");
            var renderer = wall.GetComponent<MeshRenderer>();
            Assert.That(renderer != null && renderer.enabled, "görsel duvar çizilmiyor");
            ColorUtility.TryParseHtmlString(spec.OuterWallColorHex, out Color stone);
            AssertColor(renderer.sharedMaterial.GetColor(RuleEngineV4SliceArenaShellBuilderDefaults.BaseColorProperty), stone, "duvar rengi");

            var geometry = new RuleEngineV4SliceArenaWall(spec);
            Transform collision = GameObject.Find(ArenaRoot).transform.Find("OuterWallCollision");
            var probe = wall.gameObject.AddComponent<MeshCollider>();
            probe.sharedMesh = wall.GetComponent<MeshFilter>().sharedMesh;
            Physics.SyncTransforms();
            int visualMask = 1 << RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer;
            var fails = new List<string>();
            try
            {
                float y = spec.OuterWallHeightM * ShellProbeHeightFrac;
                for (int deg = 0; deg < 360; deg++)
                {
                    Vector3 dir = Quaternion.Euler(0f, deg, 0f) * Vector3.forward;
                    float cos = Mathf.Cos(NearestFaceOffsetRad(geometry, deg));
                    Vector3 origin = Vector3.up * y;
                    if (!Physics.Raycast(origin, dir, out RaycastHit inner, ShellOuterProbeM, visualMask, QueryTriggerInteraction.Ignore))
                    {
                        fails.Add($"{deg}°: görsel duvar kapalı değil (iç yüz yok)");
                        continue;
                    }
                    float wallHit = Physics.RaycastAll(origin, dir, ShellOuterProbeM, ~0, QueryTriggerInteraction.Ignore)
                        .Where(h => h.collider.transform.IsChildOf(collision)).Select(h => h.distance).DefaultIfEmpty(-1f).Min();
                    if (Mathf.Abs(inner.distance - wallHit) > ShellTolM)
                        fails.Add($"{deg}°: görsel iç yüz {inner.distance:0.000} m, çarpışma iç yüzü {wallHit:0.000} m");
                    if (Mathf.Abs(inner.distance * cos - spec.OuterWallVisualInnerRadiusM) > ShellTolM)
                        fails.Add($"{deg}°: iç yüz uzaklığı {inner.distance * cos:0.000} m ≠ {spec.OuterWallVisualInnerRadiusM} m");

                    Vector3 outside = origin + dir * ShellOuterProbeM;
                    if (Physics.Raycast(outside, -dir, out RaycastHit outer, ShellOuterProbeM, visualMask, QueryTriggerInteraction.Ignore))
                    {
                        float r = (ShellOuterProbeM - outer.distance) * cos;
                        if (Mathf.Abs(r - spec.OuterWallVisualOuterRadiusM) > ShellTolM)
                            fails.Add($"{deg}°: dış yüz uzaklığı {r:0.000} m ≠ {spec.OuterWallVisualOuterRadiusM} m");
                    }
                    else
                    {
                        fails.Add($"{deg}°: dış yüz yok");
                    }

                    float midR = (spec.OuterWallVisualInnerRadiusM + spec.OuterWallVisualOuterRadiusM) * 0.5f;
                    Vector3 above = dir * midR + Vector3.up * (spec.OuterWallHeightM + ShellTopProbeLiftM);
                    if (!Physics.Raycast(above, Vector3.down, out RaycastHit top, spec.OuterWallHeightM + ShellTopProbeLiftM, visualMask,
                            QueryTriggerInteraction.Ignore) || Mathf.Abs(top.point.y - spec.OuterWallHeightM) > ShellTolM)
                        fails.Add($"{deg}°: üst yüz {spec.OuterWallHeightM} m'de değil");
                }
            }
            finally
            {
                Object.Destroy(probe);
                Physics.SyncTransforms();
            }
            Assert.That(fails, Is.Empty, string.Join("\n", fails.Take(20)));
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        [UnityTest]
        public IEnumerator O_8_7_DuvarOtesi_Zemin300_UstelSis_KenarGorunmez()
        {
            yield return null;
            yield return null;
            RuleEngineV4SliceArenaBeyond beyond = BeyondSpec;
            Assert.That(beyond.HasBlock, "JSON slice_arena_visuals.beyond_wall yok");
            Assert.That(beyond.GroundRadiusM, Is.EqualTo(300f), "JSON zemin yarıçapı");
            Assert.That(beyond.FogDensity, Is.EqualTo(0.012f).Within(1e-6f), "JSON sis yoğunluğu");
            Assert.That(beyond.FogColorHex, Is.EqualTo("#4A5260"), "JSON sis rengi");
            ColorUtility.TryParseHtmlString(beyond.FogColorHex, out Color fog);

            Assert.That(RenderSettings.fog, "sis kapalı");
            Assert.That(RenderSettings.fogMode, Is.EqualTo(FogMode.Exponential), "sis üstel değil");
            Assert.That(RenderSettings.fogDensity, Is.EqualTo(beyond.FogDensity).Within(1e-6f), "sis yoğunluğu (görünüm ön ayarı ezmiş olabilir)");
            AssertColor(RenderSettings.fogColor, fog, "sis rengi");
            Camera main = Camera.main;
            Assert.That(main.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor), "kamera arka planı düz renk değil");
            AssertColor(main.backgroundColor, fog, "kamera arka planı (gökyüzü) sis rengi değil");
            Assert.That(main.farClipPlane, Is.GreaterThan(beyond.GroundRadiusM), "uzak kırpma zemin kenarından önce");

            Transform ground = GameObject.Find(ArenaRoot).transform.Find(RuleEngineV4SliceArenaShellBuilderDefaults.GroundName);
            Assert.That(ground, Is.Not.Null, "duvar ötesi zemini yok");
            Assert.That(ground.GetComponentsInChildren<Collider>(true), Is.Empty, "zeminde collider var");
            Assert.That(ground.gameObject.layer, Is.EqualTo(RuleEngineV4SliceArenaVisualsBuilderDefaults.VisualLayer), "zemin katmanı");
            Bounds b = ground.GetComponent<MeshRenderer>().bounds;
            Assert.That(b.extents.x, Is.EqualTo(beyond.GroundRadiusM).Within(PosTolM), "zemin yarıçapı (x)");
            Assert.That(b.extents.z, Is.EqualTo(beyond.GroundRadiusM).Within(PosTolM), "zemin yarıçapı (z)");
            Assert.That(b.size.y, Is.LessThan(ShellTolM), "zemin düz değil");
            Assert.That(b.max.y, Is.LessThanOrEqualTo(0f), "zemin arena zemininin üstünde");

            Color32[] column = RenderGroundOnlyColumn(main, ground.gameObject, out Color32 top);
            Color32 fog32 = fog;
            Assert.That(MaxChannelDiff(top, fog32), Is.LessThanOrEqualTo(1), $"gökyüzü sis rengi değil: {top}");
            Assert.That(MaxChannelDiff(column[0], fog32), Is.GreaterThanOrEqualTo(GroundPresentMinLevels),
                $"zemin çizilmedi (en alt satır {column[0]}, sis {fog32})");
            int worst = 0, worstRow = -1;
            for (int y = 1; y < column.Length; y++)
            {
                int step = MaxChannelDiff(column[y], column[y - 1]);
                if (step > worst)
                {
                    worst = step;
                    worstRow = y;
                }
            }
            Debug.Log($"[O-8.7] zemin kenarı: ardışık satır sıçraması en çok {worst} seviye (satır {worstRow}), en alt {column[0]}, gök {top}");
            Assert.That(worst, Is.LessThanOrEqualTo(EdgeStepMaxLevels),
                $"zeminin bittiği kenar görünüyor: satır {worstRow}'da {worst} seviyelik sıçrama");
            Assert.That(_errors, Is.Empty, string.Join("\n", _errors));
        }

        /// <summary>Ortadaki piksel sütunu, aşağıdan yukarı; yalnız zemin çizilir, sis ve arka plan oyun kamerasınınki.</summary>
        Color32[] RenderGroundOnlyColumn(Camera main, GameObject ground, out Color32 topPixel)
        {
            var go = new GameObject("ZeminKenariKamerasi");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.enabled = false;
            cam.cullingMask = 1 << EdgeShotLayer;
            go.transform.SetPositionAndRotation(Vector3.up * EdgeShotHeightM, Quaternion.Euler(EdgeShotPitchDeg, 0f, 0f));
            int layer = ground.layer;
            ground.layer = EdgeShotLayer;
            Texture2D tex;
            try
            {
                tex = RenderGameCamera(cam);
            }
            finally
            {
                ground.layer = layer;
                Object.Destroy(go);
            }
            int x = tex.width / 2;
            var column = new Color32[tex.height];
            for (int y = 0; y < tex.height; y++)
                column[y] = tex.GetPixel(x, y);
            topPixel = column[tex.height - 1];
            Object.Destroy(tex);
            return column;
        }

        static float NearestFaceOffsetRad(RuleEngineV4SliceArenaWall geometry, int deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float step = 2f * Mathf.PI / geometry.Segments;
            int i = Mathf.RoundToInt(a / step);
            return a - geometry.SegmentAngleRad(i);
        }

        static int MaxChannelDiff(Color32 a, Color32 b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));

        static void AssertColor(Color actual, Color expected, string what)
        {
            Assert.That(Mathf.Abs(actual.r - expected.r) <= ShellColorTol && Mathf.Abs(actual.g - expected.g) <= ShellColorTol
                && Mathf.Abs(actual.b - expected.b) <= ShellColorTol, $"{what}: {actual} ≠ {expected}");
        }
    }
}
