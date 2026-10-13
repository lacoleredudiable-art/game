using System.Collections;
using System.IO;
using System.Linq;
using Dovus.Core.Element;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Casting;
using Dovus.Game.Vfx;
using Dovus.Game.Weapons;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dovus.Tests.PlayMode
{
    /// <summary>
    /// Kılıç kanıtı (gerçek çizim): Kılıç ATIL ve Kilic_VUR'da kor izinin en yeni örneği her kare
    /// elde görünen kılıcın Vfx_Kabza→Vfx_Tip doğrusu boyunca uzanır; VUR'un Impact karesi önden
    /// kaydedilir. ATIL sırasında boss ve yaratıklar ekrandayken oyun kamerasının draw call /
    /// üçgen sayısı loglanır (bütçe raporu; test eşiği değil).
    /// </summary>
    public sealed partial class RuleEngineV4WorldPhysicsPlayModeTests
    {
        const float BladeSampleTolM = 1e-3f;
        const float FrontCamDistM = 2.6f;
        const float FrontCamChestFrac = 0.3f;
        const float FrontCamFov = 45f;
        const float BossAheadM = 9f;
        const float MinionSideM = 1.5f;
        /// <summary>Kılıç VUR hedefi: v6 çizim hedef kapısının yakın dövüş menzili içinde.</summary>
        const float StrikeReachFrac = 0.4f;
        const int CombatDrawCallBudget = 60;
        const int TimelineMaxChars = 4000;
        const int FrontCaptureEveryFrames = 3;
        static readonly string[] TimelineStates =
            { SwordDashClip, SwordStrikeClip, "CastSweep", "CastPierce", "BasicStrike", "BasicStrikeB", "BasicStrikeC", "Dodge", "Ortak_Takla", "Locomotion" };

        sealed class BladeLog
        {
            public int TrailFrames;
            public int OnBladeFrames;
            public float MinCoverage = float.MaxValue;
            public float MaxTipErrM;
            public float MaxOffLineM;
            public int ImpactFrames;
            public int ClipFrames;
            public readonly System.Text.StringBuilder Timeline = new();
            public override string ToString() =>
                $"iz karesi={TrailFrames} kılıç boyunca={OnBladeFrames} en düşük kaplama={MinCoverage:0.000} "
                + $"uç hatası={MaxTipErrM * 1000f:0.00} mm doğrudan sapma={MaxOffLineM * 1000f:0.00} mm vuruş karesi={ImpactFrames} klip karesi={ClipFrames}";
        }

        sealed class CombatStats
        {
            public int Frames;
            public int MaxDrawCalls;
            public int MaxSetPass;
            public int MaxTriangles;
            public int MaxVertices;
            public long ProfilerMaxSetPass;
            public long ProfilerMaxTriangles;
            public string Breakdown = string.Empty;
            public override string ToString() =>
                $"kare={Frames} draw call={MaxDrawCalls} setpass={MaxSetPass} üçgen={MaxTriangles} köşe={MaxVertices} "
                + $"(profiler: setpass={ProfilerMaxSetPass} üçgen={ProfilerMaxTriangles}) bütçe={CombatDrawCallBudget}\n"
                + $"kadrajdaki görünür renderer'lar: {Breakdown}";
        }

        [UnityTest]
        public IEnumerator K3_RealDraw_SwordTrailRunsAlongBlade_ImpactFront_CombatDrawCalls()
        {
            FreezeBoss();
            HexagonInputController input = InputController();
            Animator anim = PlayerAnimator();
            SkillAnimVfxEventHost events = anim.GetComponent<SkillAnimVfxEventHost>();
            Assert.That(events, Is.Not.Null, "oyuncuda SkillAnimVfxEventHost yok");
            var vfx = _player.GetComponentInChildren<RuleDrivenVfxDirector>();
            Assert.That(vfx, Is.Not.Null, "RuleDrivenVfxDirector");

            yield return WaitSentenceIdle(input);
            RuneLoadout saved = input.Engine.Loadout;
            Assert.That(input.Engine.TrySetLoadout(new RuneLoadout(RuleEngineV4Slice.Runes.ToArray())), Is.True, "build seçilemedi");
            try
            {
                Equip(Kilic);
                // VUR'un hedef kapısı kadrajdaki uzak boss'u seçebilir; savaş ölçümü yalnız ATIL'de.
                ArrangeCombatInView(0, bossInView: false, StrikeReachFrac);
                yield return WaitSentenceIdle(input);
                var strike = new BladeLog();
                yield return DrawAndTrackBlade(input, anim, events, vfx, AttackVerb, SwordStrikeClip, "kilic_vur", strike, null);
                Debug.Log("[k3kanit] Kilic_VUR kor izi " + strike + "\n" + strike.Timeline);
                AssertBladeTrail(strike, "Kilic_VUR");
                Assert.That(strike.ImpactFrames, Is.GreaterThan(0), "Kilic_VUR: Impact olayı gelmedi");
                yield return WaitUntilIdle();

                int strikeAdjDot = _lastDrawAdjDot;
                int standing = System.Array.FindIndex(_minions, m => !m.IsDown);
                Assert.That(standing, Is.GreaterThanOrEqualTo(0), "VUR sonrası ayakta yaratık yok");
                ArrangeCombatInView(standing, bossInView: true, InRangeFrac);
                yield return WaitSentenceIdle(input);
                var dash = new BladeLog();
                var stats = new CombatStats();
                yield return DrawAndTrackBlade(input, anim, events, vfx, DashVerb, SwordDashClip, "kilic_atil", dash, stats, strikeAdjDot);
                Debug.Log("[k3kanit] Kılıç ATIL kor izi " + dash + "\n" + dash.Timeline);
                Debug.Log("[k3kanit] Kılıç ATIL savaş ölçümü " + stats + " kameralar="
                    + string.Join(",", Camera.allCameras.Select(c => c.name)));
                if (stats.MaxDrawCalls > CombatDrawCallBudget)
                    Debug.LogWarning($"[k3kanit] draw call bütçesi aşıldı: {stats}");
                AssertBladeTrail(dash, "Kılıç ATIL");
                Assert.That(stats.Frames, Is.GreaterThan(0), "ATIL sırasında savaş ölçümü alınmadı");
                yield return WaitUntilIdle();
            }
            finally
            {
                input.Engine.TrySetLoadout(saved);
            }
        }

        static void AssertBladeTrail(BladeLog log, string label)
        {
            Assert.That(log.ClipFrames, Is.GreaterThan(0), $"{label}: klip oynamadı (kalıp faz klibi ezmiş olabilir); {log}\n{log.Timeline}");
            Assert.That(log.TrailFrames, Is.GreaterThan(0), $"{label}: kor izi görülmedi");
            Assert.That(log.OnBladeFrames, Is.EqualTo(log.TrailFrames), $"{label}: iz kılıç boyunca değil; {log}");
        }

        /// <summary>Oyuncu kameranın baktığı yöne döner; hedef menzilde, diğer yaratıklar (ve istenirse boss) kadrajda.</summary>
        void ArrangeCombatInView(int target, bool bossInView, float reachFrac)
        {
            Camera cam = Camera.main;
            Assert.That(cam, Is.Not.Null, "ana kamera yok");
            Vector3 fwd = Flat(cam.transform.forward).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            Arrange(fwd);
            float reach = WeaponRange(Kilic) * reachFrac;
            Place(_minions[target].transform, Origin + fwd * reach);
            int other = 0;
            for (int i = 0; i < _minions.Length; i++)
            {
                if (i == target)
                    continue;
                float s = other++ == 0 ? -MinionSideM : MinionSideM;
                Place(_minions[i].transform, Origin + fwd * (reach + MinionSideM) + side * s);
            }
            if (bossInView)
                Place(_boss, Origin + fwd * BossAheadM);
            Physics.SyncTransforms();
        }

        IEnumerator DrawAndTrackBlade(
            HexagonInputController input, Animator anim, SkillAnimVfxEventHost events, RuleDrivenVfxDirector vfx,
            int verb, string expectedClip, string label, BladeLog log, CombatStats stats, int skipAdjDot = 0)
        {
            int impactBefore = events.ImpactEvents;
            yield return DrawSliceCombo(input, verb, label, skipAdjDot);
            log.Timeline.Append($"{_label} köprü klibi={_md.AnimationBridge.LastClipName} yedek={_md.AnimationBridge.LastUsedFallbackState} ");

            string dir = CaptureDir();
            Canvas hex = dir != null ? Object.FindAnyObjectByType<HexagonView>()?.Canvas : null;
            if (hex != null)
                hex.enabled = false;
            ProfilerRecorder setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            ProfilerRecorder tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            try
            {
                int frame = 0;
                int extraRenderFrame = int.MinValue;
                bool Watching() => _cast.IsRunning || vfx.TrailOn || EmberTrailVisible();
                yield return EachLateFrame(CastTimeoutSec, () =>
                {
                    frame++;
                    // Önden kayıt kamerası o kareye ve sonrakine ek render ekler; ölçüm yalnız oyun kamerası kareleri.
                    if (stats != null && _cast.IsRunning && frame > extraRenderFrame + 1 && BossAndMinionsOnScreen())
                        SampleCombatStats(stats, setPass, tris);

                    AnimatorClipInfo[] ci = anim.GetCurrentAnimatorClipInfo(0);
                    if (ci.Any(c => c.clip.name == expectedClip && c.weight > ClipActiveWeight))
                        log.ClipFrames++;
                    if (log.Timeline.Length < TimelineMaxChars)
                    {
                        AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
                        string stName = TimelineStates.FirstOrDefault(st.IsName) ?? "?";
                        log.Timeline.Append($"<{stName}> ");
                        log.Timeline.Append($"[{frame} t={Time.time:0.000} cast={(_cast.IsRunning ? 1 : 0)} iz={(vfx.TrailOn ? 1 : 0)} "
                            + $"klip={(ci.Length > 0 ? ci[0].clip.name : "-")} n={st.normalizedTime:0.00}] ");
                    }
                    bool impact = events.ImpactEvents > impactBefore;
                    if (impact)
                    {
                        impactBefore = events.ImpactEvents;
                        log.ImpactFrames++;
                        if (dir != null)
                        {
                            SaveFront(dir, $"{label}_{frame:000}_vurus_onden.png");
                            extraRenderFrame = frame;
                        }
                    }

                    if (!vfx.TrailOn || vfx.MeshTrail == null || vfx.MeshTrail.SampleCount < 2)
                        return Watching();
                    log.TrailFrames++;
                    if (MeasureNewestSample(anim, vfx.MeshTrail, log))
                        log.OnBladeFrames++;
                    if (dir != null)
                    {
                        SaveScreen(dir, $"{label}_{frame:000}_iz.png");
                        if (frame % FrontCaptureEveryFrames == 0)
                        {
                            SaveFront(dir, $"{label}_{frame:000}_iz_onden.png");
                            extraRenderFrame = frame;
                        }
                    }
                    return Watching();
                });
            }
            finally
            {
                setPass.Dispose();
                tris.Dispose();
                if (hex != null)
                    hex.enabled = true;
            }
        }

        /// <summary>En yeni şerit çifti = (kabza tarafı, uç); uç Vfx_Tip'te, kabza tarafı kabza→uç doğrusunda.</summary>
        static bool MeasureNewestSample(Animator anim, EmberMeshTrailView trail, BladeLog log)
        {
            if (!WeaponBladePoints.TryActiveBladePoints(anim.transform, out Transform hilt, out Transform tip))
                return false;
            Vector3[] v = trail.RibbonMesh.vertices;
            if (v.Length < 2)
                return false;
            Vector3 innerV = v[v.Length - 2];
            Vector3 tipV = v[v.Length - 1];
            Vector3 blade = tip.position - hilt.position;
            float bladeLen = blade.magnitude;
            if (bladeLen <= Mathf.Epsilon)
                return false;
            float tipErr = Vector3.Distance(tipV, tip.position);
            float t = Vector3.Dot(innerV - hilt.position, blade) / (bladeLen * bladeLen);
            float offLine = Vector3.Distance(innerV, hilt.position + blade * t);
            float coverage = Vector3.Distance(innerV, tipV) / bladeLen;
            log.MaxTipErrM = Mathf.Max(log.MaxTipErrM, tipErr);
            log.MaxOffLineM = Mathf.Max(log.MaxOffLineM, offLine);
            log.MinCoverage = Mathf.Min(log.MinCoverage, coverage);
            return tipErr <= BladeSampleTolM && offLine <= BladeSampleTolM && t >= -BladeSampleTolM && t <= 1f;
        }

        void SampleCombatStats(CombatStats stats, ProfilerRecorder setPass, ProfilerRecorder tris)
        {
            stats.Frames++;
            stats.ProfilerMaxSetPass = System.Math.Max(stats.ProfilerMaxSetPass, setPass.LastValue);
            stats.ProfilerMaxTriangles = System.Math.Max(stats.ProfilerMaxTriangles, tris.LastValue);
#if UNITY_EDITOR
            int drawCalls = UnityEditor.UnityStats.drawCalls;
            if (drawCalls >= stats.MaxDrawCalls)
                stats.Breakdown = VisibleRendererBreakdown();
            stats.MaxDrawCalls = Mathf.Max(stats.MaxDrawCalls, drawCalls);
            stats.MaxSetPass = Mathf.Max(stats.MaxSetPass, UnityEditor.UnityStats.setPassCalls);
            stats.MaxTriangles = Mathf.Max(stats.MaxTriangles, UnityEditor.UnityStats.triangles);
            stats.MaxVertices = Mathf.Max(stats.MaxVertices, UnityEditor.UnityStats.vertices);
#endif
        }

        /// <summary>Ana kamera kadrajındaki açık renderer'lar: grup → adet / malzeme alt-mesh sayısı / üçgen.</summary>
        string VisibleRendererBreakdown()
        {
            Camera cam = Camera.main;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            string Group(Renderer r)
            {
                Transform t = r.transform;
                if (t.IsChildOf(_player))
                    return "oyuncu";
                if (t.IsChildOf(_boss))
                    return "boss";
                if (_minions.Any(m => m != null && t.IsChildOf(m.transform)))
                    return "yaratık";
                if (_allies.Any(a => a != null && t.IsChildOf(a)))
                    return "müttefik";
                return t.root.name;
            }
            static int Tris(Renderer r)
            {
                Mesh m = r is SkinnedMeshRenderer s ? s.sharedMesh
                    : r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
                if (m == null)
                    return 0;
                int n = 0;
                for (int i = 0; i < m.subMeshCount; i++)
                    n += (int)m.GetIndexCount(i) / 3;
                return n;
            }
            return string.Join(" | ", Object.FindObjectsByType<Renderer>()
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && (cam.cullingMask & (1 << r.gameObject.layer)) != 0
                    && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                .GroupBy(Group)
                .Select(g => (name: g.Key, count: g.Count(), mats: g.Sum(r => r.sharedMaterials.Length), tris: g.Sum(Tris)))
                .OrderByDescending(g => g.mats)
                .Select(g => $"{g.name}: {g.count} renderer, {g.mats} malzeme, {g.tris} üçgen"));
        }

        bool BossAndMinionsOnScreen()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return false;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            bool OnScreen(Transform t) => t.GetComponentsInChildren<Renderer>()
                .Any(r => r.enabled && r.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, r.bounds));
            return OnScreen(_boss) && _minions.Any(m => m != null && OnScreen(m.transform));
        }

        /// <summary>Önden kayıt: geçici kamera oyuncunun baktığı yönden göğsüne bakar.</summary>
        void SaveFront(string dir, string fileName)
        {
            var go = new GameObject("K3FrontCaptureCam");
            Camera cam = go.AddComponent<Camera>();
            if (Camera.main != null)
                cam.CopyFrom(Camera.main);
            cam.enabled = false;
            cam.fieldOfView = FrontCamFov;
            // Oyuncu kökü kapsül merkezinde; göğüs gövde mesh sınırından alınır.
            SkinnedMeshRenderer body = _player.GetComponentsInChildren<SkinnedMeshRenderer>()
                .FirstOrDefault(r => r.enabled && r.gameObject.activeInHierarchy);
            Vector3 chest = body != null
                ? body.bounds.center + Vector3.up * (body.bounds.extents.y * FrontCamChestFrac)
                : _player.position;
            Vector3 fwd = Flat(_player.forward).normalized;
            go.transform.position = chest + fwd * FrontCamDistM;
            go.transform.rotation = Quaternion.LookRotation(chest - go.transform.position, Vector3.up);
            var rt = new RenderTexture(CaptureW, CaptureH, 24);
            int errorsBefore = _errors.Count;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            if (_errors.Count > errorsBefore)
            {
                Debug.LogWarning("önden yakalama sırasında: " + string.Join("\n", _errors.Skip(errorsBefore)));
                _errors.RemoveRange(errorsBefore, _errors.Count - errorsBefore);
            }
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(CaptureW, CaptureH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, fileName), tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
