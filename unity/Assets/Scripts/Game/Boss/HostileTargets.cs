using Dovus.Core.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Düşman tarafının (boss; sonra küçük canavarlar) hedef alabileceği dostların kaydı.
    /// Oyuncu, dost kukla ve dikkat çeken yemler buraya yazılır; seçim saf
    /// <see cref="TargetPicker"/>'da. Yok olan (Destroy edilmiş) dönüşümler kendiliğinden düşer.
    /// </summary>
    public sealed class HostileTargets : MonoBehaviour
    {
        public sealed class Entry
        {
            public int Id;
            public TargetKind Kind;
            public Transform Transform;
            public float RadiusM;
            public Func<bool> Alive;
            public Func<bool> Stealthed;
            public Func<bool> Taunting;
            /// <summary>Boss ham hasarı (DamagePipeline öncesi). Null: hasar almaz.</summary>
            public Action<float> Damage;
            /// <summary>Yem: vurulunca yok olur. Null: öldürülemez.</summary>
            public Action Kill;

            public bool IsAlive => Transform != null && (Alive == null || Alive());
            public bool IsStealthed => Stealthed != null && Stealthed();
            public bool IsTaunting => Taunting != null && Taunting();
        }

        readonly List<Entry> _entries = new();
        readonly List<HostileCandidate> _scratch = new();
        int _nextId;

        public TargetingConfig Config { get; private set; } = new TargetingConfig();

        public IReadOnlyList<Entry> Entries
        {
            get
            {
                Prune();
                return _entries;
            }
        }

        public void Configure(TargetingConfig config) => Config = config ?? new TargetingConfig();

        public int Register(
            Transform transform,
            TargetKind kind,
            float radiusM,
            Func<bool> alive,
            Func<bool> stealthed = null,
            Func<bool> taunting = null,
            Action<float> damage = null,
            Action kill = null)
        {
            if (transform == null)
                return -1;
            var e = new Entry
            {
                Id = ++_nextId,
                Kind = kind,
                Transform = transform,
                RadiusM = Mathf.Max(0.05f, radiusM),
                Alive = alive,
                Stealthed = stealthed,
                Taunting = taunting,
                Damage = damage,
                Kill = kill
            };
            _entries.Add(e);
            return e.Id;
        }

        public Entry Find(int id)
        {
            Prune();
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Id == id)
                    return _entries[i];
            return null;
        }

        /// <summary>Ağırlıklı seçim; roll01 çağıranın tohumlu rastgelesinden. −1: geçerli hedef yok.</summary>
        public int Pick(double roll01) => TargetPicker.Pick(Candidates(), Config, roll01);

        public bool ShouldRetarget(int currentId) => TargetPicker.ShouldRetarget(Candidates(), currentId, Config);

        /// <summary>Tarama sondası: şu an dikkat çeken canlı bir yem var mı (seçim yem önceliğiyle bunu döndürür).</summary>
        public bool DecoyHoldsAggro()
        {
            int id = TargetPicker.Pick(Candidates(), Config, 0.5);
            Entry e = id >= 0 ? Find(id) : null;
            return e != null && e.Kind == TargetKind.Decoy;
        }

        IReadOnlyList<HostileCandidate> Candidates()
        {
            Prune();
            _scratch.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                Vector3 p = e.Transform.position;
                _scratch.Add(new HostileCandidate(e.Id, e.Kind, p.x, p.z, e.IsAlive, e.IsStealthed, e.IsTaunting));
            }
            return _scratch;
        }

        void Prune()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.Transform == null || (e.Kind == TargetKind.Decoy && !e.IsAlive))
                    _entries.RemoveAt(i);
            }
        }
    }
}
