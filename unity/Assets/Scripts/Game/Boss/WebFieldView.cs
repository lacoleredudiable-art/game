using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>Ağ Örme alanları: Core set + collider'sız disk görselleri, yavaşlatma tikleri.</summary>
    public sealed class WebFieldView : MonoBehaviour
    {
        const float DiscHeightY = 0.025f;
        const float DiscThicknessScale = 0.02f;

        GameClockHost _clock;
        CombatTuning _combat;
        BossDirector _boss;
        BossVitals _bossVitals;
        Transform _player;
        ActorStatusHost _playerStatus;
        readonly List<AllyDummyController> _allies = new();
        WebFieldSet _set;
        double _nextRefreshMs;
        readonly List<DiscVisual> _visuals = new();
        static Texture2D _glowTex;

        struct DiscVisual
        {
            public GameObject Go;
            public double ExpireMs;
        }

        public void Bind(
            GameClockHost clock,
            CombatTuning combat,
            BossDirector boss,
            BossVitals bossVitals,
            Transform player,
            ActorStatusHost playerStatus)
        {
            Unbind();
            _clock = clock;
            _combat = combat;
            _boss = boss;
            _bossVitals = bossVitals;
            _player = player;
            _playerStatus = playerStatus;
            if (_boss != null)
                _boss.AttackStruck += OnAttackStruck;
            RebuildSet();
        }

        public void RegisterAlly(AllyDummyController ally)
        {
            if (ally != null && !_allies.Contains(ally))
                _allies.Add(ally);
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (_boss != null)
                _boss.AttackStruck -= OnAttackStruck;
            ClearAll();
        }

        void RebuildSet()
        {
            if (_combat == null)
                return;
            BossTuning b = _combat.Boss;
            _set = new WebFieldSet(
                b.WebFieldMaxCount,
                b.WebFieldRadiusM,
                b.WebFieldLifeSec * BossTimeDefaults.SecToMs,
                b.WebFieldMinCenterDistM);
        }

        void OnAttackStruck(BossAttackKind kind)
        {
            if (kind != BossAttackKind.WebField || _set == null || _boss == null || _clock == null)
                return;
            Vector3 t = _boss.LastWebFieldTarget;
            double now = _clock.Director.WorldTimeMs;
            _set.Add(t.x, t.z, now);
            SpawnDisc(t.x, t.z, now + _combat.Boss.WebFieldLifeSec * BossTimeDefaults.SecToMs);
        }

        void Update()
        {
            if (_clock == null || _set == null || _combat == null)
                return;

            if (_bossVitals != null && _bossVitals.IsDown)
            {
                ClearAll();
                return;
            }

            double now = _clock.Director.WorldTimeMs;
            _set.Prune(now);
            PruneVisuals(now);

            double refreshMs = _combat.Boss.WebFieldRefreshSec * BossTimeDefaults.SecToMs;
            if (refreshMs <= 0f || now < _nextRefreshMs)
                return;
            _nextRefreshMs = now + refreshMs;
            if (_set.Count == 0)
                return;

            double slowMs = refreshMs * 2.0;
            float slowMult = _combat.Status.SlowSpeedMult;
            if (_player != null && _playerStatus != null)
            {
                Vector3 p = _player.position;
                if (_set.Contains(p.x, p.z, now))
                    _playerStatus.Board.Apply(StatusKind.Slow, slowMs, slowMult);
            }
            for (int i = 0; i < _allies.Count; i++)
            {
                AllyDummyController a = _allies[i];
                if (a == null || a.IsDown || a.Board == null)
                    continue;
                Vector3 p = a.transform.position;
                if (_set.Contains(p.x, p.z, now))
                    a.Board.Apply(StatusKind.Slow, slowMs, slowMult);
            }
        }

        void SpawnDisc(float x, float z, double expireMs)
        {
            var go = new GameObject("WebFieldDisc");
            go.transform.SetParent(transform, false);
            var mesh = BuildDiscMesh();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = MakeWebMat();
            float r = _combat.Boss.WebFieldRadiusM;
            go.transform.position = new Vector3(x, DiscHeightY, z);
            go.transform.localScale = new Vector3(r * 2f, DiscThicknessScale, r * 2f);
            _visuals.Add(new DiscVisual { Go = go, ExpireMs = expireMs });
        }

        void PruneVisuals(double nowMs)
        {
            for (int i = _visuals.Count - 1; i >= 0; i--)
            {
                if (_visuals[i].ExpireMs > nowMs)
                    continue;
                if (_visuals[i].Go != null)
                    Destroy(_visuals[i].Go);
                _visuals.RemoveAt(i);
            }
        }

        void ClearAll()
        {
            _set?.Clear();
            for (int i = 0; i < _visuals.Count; i++)
            {
                if (_visuals[i].Go != null)
                    Destroy(_visuals[i].Go);
            }
            _visuals.Clear();
            _nextRefreshMs = 0;
        }

        static Mesh BuildDiscMesh()
        {
            const int seg = 32;
            var mesh = new Mesh { name = "WebFieldDisc" };
            var verts = new Vector3[seg + 1];
            var uvs = new Vector2[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(a);
                float sz = Mathf.Sin(a);
                verts[i + 1] = new Vector3(cx * 0.5f, 0f, sz * 0.5f);
                uvs[i + 1] = new Vector2(cx * 0.5f + 0.5f, sz * 0.5f + 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i == seg - 1 ? 1 : i + 2;
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material MakeWebMat()
        {
            var shader = AssetLoader.FindShader("Sprites/Default", null);
            if (shader == null)
                shader = AssetLoader.FindShader("Universal Render Pipeline/Unlit", null);
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            Texture2D tex = GlowTexture();
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            var c = new Color(0.82f, 0.88f, 0.95f, 0.38f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            else
                mat.color = c;
            return mat;
        }

        static Texture2D GlowTexture()
        {
            if (_glowTex != null)
                return _glowTex;
            const int size = 64;
            const float half = size * 0.5f;
            _glowTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "WebFieldGlow"
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = d <= WebFieldViewDefaults.DiscAlphaDistanceInner ? WebFieldViewDefaults.DiscAlphaFloor : Mathf.Lerp(WebFieldViewDefaults.DiscAlphaFloor, WebFieldViewDefaults.DiscAlphaCeiling, Mathf.Clamp01((d - WebFieldViewDefaults.DiscAlphaDistanceInner) / WebFieldViewDefaults.DiscAlphaMidSpan));
                if (d > 1f)
                    a = 0f;
                _glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _glowTex.Apply(false, true);
            return _glowTex;
        }
    }
}
