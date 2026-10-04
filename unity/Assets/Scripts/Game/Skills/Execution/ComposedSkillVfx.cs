using Dovus.Core.Mechanic;
using Dovus.Core.Tuning;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dovus.Game.Skills.Execution
{
    /// <summary>
    /// <see cref="VisualRecipe"/>'yi dünyada oynatır: fiil maddesini (Substance/{verb}) silahın
    /// yoluna ve sıfatın silüetine göre dizer. Taşıyıcı (hitbox primitive'i) yok olunca yeni parça
    /// üretmez; dünyadaki parçalar ömrünü doldurur.
    /// </summary>
    public sealed class ComposedSkillVfx : MonoBehaviour
    {
        static VfxLibrary _vfx;

        public static void Bind(VfxLibrary vfx) => _vfx = vfx;

        static VfxLibrary Lib => _vfx ?? throw new System.InvalidOperationException("ComposedSkillVfx.Bind ile VfxLibrary bağlanmalı.");

        const float MaxLifeSec = 12f;

        VisualRecipe _recipe;
        List<VisualPiece> _ordered;
        string _substanceKey;
        Transform _anchor, _owner;
        Color _tint;
        bool _hasTint;
        Vector3 _origin;
        /// <summary>Hitbox göğüs hizasında doğar; yerden çıkan parçalar arena zeminine oturur.</summary>
        float _groundY;
        Quaternion _frame;
        float _cycleT, _age, _loopWait;
        int _next;
        bool _looping, _anchorLost, _hasHaze;
        float _growStart;
        readonly List<Live> _live = new();
        LineRenderer _tether, _ring;

        Mesh _chunkMesh;
        Material _chunkMaterial;
        float _life;
        static readonly Dictionary<(Material, Color), Material> ChunkMaterials = new();

        struct Live
        {
            public Transform Holder;
            public Transform Chunk;
            public float ChunkHeight;
            public float ChunkRestY;
            public Vector3 Spin;
            public VisualPiece Piece;
            public Vector3 Base;
            public Quaternion Frame;
            public float Age;
        }

        public static ComposedSkillVfx Play(VisualRecipe recipe, string substanceKey, Transform anchor,
            Transform owner, Vector3 origin, Vector3 direction, string colorHex)
        {
            var go = new GameObject($"SkillVfx_{recipe.Substance}_{recipe.Layout}");
            var c = go.AddComponent<ComposedSkillVfx>();
            c._recipe = recipe;
            c._ordered = recipe.Pieces.OrderBy(p => p.DelaySec).ToList();
            c._hasHaze = recipe.Pieces.Any(p => p.Haze);
            c._substanceKey = substanceKey;
            c._anchor = anchor;
            c._owner = owner;
            c._origin = origin;
            c._groundY = FeelVfx.GroundY;
            VfxLibrary lib = Lib;
            Vector3 flat = new(direction.x, 0f, direction.z);
            c._frame = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized) : Quaternion.identity;
            if (recipe.BornAheadM > 0 && owner != null)
            {
                Vector3 d = origin - owner.position;
                d.y = 0f;
                float tol = lib.Composition.CasterOriginToleranceM;
                if (d.sqrMagnitude < tol * tol)
                    c._origin += c._frame * Vector3.forward * (float)recipe.BornAheadM;
            }
            c._hasTint = !string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out c._tint);
            c._growStart = lib.Composition.GrowStartScale;
            lib.TryResolve(substanceKey, out _, out c._life);
            if (c._life <= 0f)
                c._life = 2f;
            if (lib.TryResolveChunk(substanceKey, out c._chunkMesh, out Material source))
                c._chunkMaterial = ChunkMaterial(source, c._hasTint ? c._tint : Color.white, lib);
            if (recipe.Tether && owner != null)
                c._tether = c.MakeLine("Tether", 2, false);
            if (recipe.GroundRingRadiusM > 0)
                c._ring = c.MakeLine("Anchor", 24, true);
            return c;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_anchor == null)
                _anchorLost = true;

            if (!_anchorLost && _recipe.Follow)
                _origin = _anchor.position;
            if (_recipe.FollowOwner && _owner != null)
                _origin = _owner.position;

            _cycleT += dt;
            while (_next < _ordered.Count && _ordered[_next].DelaySec <= _cycleT)
            {
                Spawn(_ordered[_next], _next);
                _next++;
            }

            if (_next >= _ordered.Count && _recipe.Loop && !_anchorLost)
            {
                _loopWait += dt;
                if (_loopWait >= _recipe.LoopEverySec)
                {
                    _loopWait = 0f;
                    _cycleT = 0f;
                    _next = 0;
                    _looping = true;
                }
            }

            Animate(dt);
            UpdateLines();

            bool pending = _next < _ordered.Count;
            if ((_anchorLost && !pending && _live.Count == 0) || _age > MaxLifeSec)
                Destroy(gameObject);
        }

        void Spawn(VisualPiece p, int index)
        {
            bool carried = p.Carried && !_looping && _anchor != null;
            if (p.Carried && _looping && _anchorLost)
                return;
            if (_looping && _hasHaze && !p.Haze && !p.Carried)
                return;
            var local = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            Vector3 basePos = _origin + _frame * local;
            if (!carried)
                basePos.y = _groundY + (float)p.Y;
            var holder = new GameObject("Piece").transform;
            holder.SetPositionAndRotation(basePos + Vector3.down * (float)p.RiseM, _frame);
            holder.SetParent(carried ? _anchor : transform, true);

            VfxLibrary lib = Lib;
            float size = (float)(_recipe.PieceSizeM * p.Scale);
            GameObject inst = lib.TrySpawn(_substanceKey, holder.position, _frame, holder, size);
            if (inst == null)
            {
                Destroy(holder.gameObject);
                return;
            }
            if (_hasTint)
                VfxLibrary.Tint(inst, _tint, lib.SubstanceTintStrength);
            var live = new Live { Holder = holder, Piece = p, Base = basePos, Frame = _frame };
            if (_chunkMesh != null && !p.Haze)
                live.Chunk = SpawnChunk(holder, size, carried, index, out live.ChunkHeight, out live.ChunkRestY,
                    out live.Spin);
            _live.Add(live);
            ApplyScale(holder, p, 0f);
        }

        Transform SpawnChunk(Transform holder, float size, bool carried, int index, out float height,
            out float restY, out Vector3 spin)
        {
            SkillVisualTuning t = Lib.Composition;
            var go = new GameObject("Chunk");
            go.transform.SetParent(holder, false);
            go.AddComponent<MeshFilter>().sharedMesh = _chunkMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = _chunkMaterial;
            Bounds b = _chunkMesh.bounds;
            float extent = Mathf.Max(0.01f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            float k = size / extent;
            go.transform.localScale = Vector3.one * k;
            height = b.size.y * k;
            restY = carried ? -b.center.y * k : -b.min.y * k;
            const float golden = 137.508f;
            float tilt = t.ChunkTiltDeg;
            go.transform.localRotation = Quaternion.Euler((index % 3 - 1) * tilt, index * golden % 360f,
                (index % 2 == 0 ? 1 : -1) * tilt * 0.6f);
            go.transform.localPosition = new Vector3(0f, carried ? restY : restY - height, 0f);
            float s = t.ChunkSpinDegPerSec;
            spin = carried ? new Vector3(s, s * 0.5f, s * 0.25f) : Vector3.zero;
            Destroy(go, _life);
            return go.transform;
        }

        /// <summary>
        /// Paket dokusu (varsa) + element rengine hafif çekim + düşük ışıma: karanlık arenada
        /// kaya/buz siyah leke değil, okunur katı cisim olur. Dokusuz kaynak (buz) parlak yüzey alır.
        /// </summary>
        static Material ChunkMaterial(Material source, Color tint, VfxLibrary lib)
        {
            var key = (source, tint);
            if (ChunkMaterials.TryGetValue(key, out Material m) && m != null)
                return m;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return source;
            m = new Material(shader) { name = "Chunk_" + (source != null ? source.name : "Generated") };
            Texture tex = source != null && source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
            if (tex != null)
                m.SetTexture("_BaseMap", tex);
            Color c = Color.Lerp(Color.white, tint, tex != null ? lib.SubstanceTintStrength : 0.5f);
            c.a = 1f;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", tex != null ? 0.2f : 0.85f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", tint * lib.Composition.ChunkEmission);
            ChunkMaterials[key] = m;
            return m;
        }

        void Animate(float dt)
        {
            float moveSec = Mathf.Max(0.01f, (float)_recipe.PieceMoveSec);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Live l = _live[i];
                if (l.Holder == null || l.Holder.childCount == 0)
                {
                    if (l.Holder != null)
                        Destroy(l.Holder.gameObject);
                    _live.RemoveAt(i);
                    continue;
                }
                l.Age += dt;
                float f = Mathf.Clamp01(l.Age / moveSec);
                float e = 1f - (1f - f) * (1f - f);
                if (l.Holder.parent == transform)
                {
                    Vector3 move = l.Frame * new Vector3((float)l.Piece.MoveX * e, 0f, (float)l.Piece.MoveZ * e);
                    Vector3 rise = Vector3.down * (float)l.Piece.RiseM * (1f - e);
                    l.Holder.position = l.Base + move + rise;
                }
                ApplyScale(l.Holder, l.Piece, e);
                if (l.Chunk != null)
                    AnimateChunk(l, dt);
                _live[i] = l;
            }
        }

        /// <summary>Yerden çıkar, durur, batar; taşınan parça takla atar.</summary>
        void AnimateChunk(in Live l, float dt)
        {
            if (l.Spin != Vector3.zero)
            {
                l.Chunk.Rotate(l.Spin * dt, Space.Self);
                return;
            }
            SkillVisualTuning t = Lib.Composition;
            float up = Mathf.Clamp01(l.Age / Mathf.Max(0.01f, t.RiseSec));
            up = 1f - (1f - up) * (1f - up);
            float holdEnd = _life * t.ChunkHoldFrac;
            float down = Mathf.Clamp01((l.Age - holdEnd) / Mathf.Max(0.01f, _life - holdEnd));
            Vector3 lp = l.Chunk.localPosition;
            lp.y = l.ChunkRestY - l.ChunkHeight * (1f - up) - l.ChunkHeight * down;
            l.Chunk.localPosition = lp;
        }

        void ApplyScale(Transform holder, VisualPiece p, float e)
        {
            float s = _recipe.Grow ? Mathf.Lerp(_growStart, 1f, e) : 1f;
            holder.localScale = new Vector3(s, s, s * Mathf.Max(0.05f, (float)p.StretchZ));
        }

        LineRenderer MakeLine(string name, int points, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = points;
            lr.loop = loop;
            lr.useWorldSpace = true;
            lr.widthMultiplier = Mathf.Max(0.06f, (float)_recipe.PieceSizeM * 0.15f);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            lr.sharedMaterial = shader != null ? new Material(shader) : null;
            Color c = _hasTint ? _tint : Color.white;
            lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 0.85f);
            if (lr.sharedMaterial != null && lr.sharedMaterial.HasProperty("_BaseColor"))
                lr.sharedMaterial.SetColor("_BaseColor", lr.startColor);
            return lr;
        }

        void UpdateLines()
        {
            bool alive = !_anchorLost;
            if (_tether != null)
            {
                _tether.enabled = alive && _owner != null;
                if (_tether.enabled)
                {
                    _tether.SetPosition(0, _owner.position + Vector3.up);
                    _tether.SetPosition(1, _anchor.position + Vector3.up * 0.5f);
                }
            }
            if (_ring != null)
            {
                _ring.enabled = alive;
                if (alive)
                {
                    float r = (float)_recipe.GroundRingRadiusM;
                    for (int i = 0; i < _ring.positionCount; i++)
                    {
                        float a = i * Mathf.PI * 2f / _ring.positionCount;
                        _ring.SetPosition(i, new Vector3(_origin.x + Mathf.Sin(a) * r, _groundY + 0.05f, _origin.z + Mathf.Cos(a) * r));
                    }
                }
            }
        }
    }
}
