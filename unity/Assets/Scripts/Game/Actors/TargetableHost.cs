using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Shared;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;
namespace Dovus.Game.Actors
{
    public sealed class TargetableHost : MonoBehaviour
        {
            int _teamId;
            ActorId _actorId;
            string _displayName = string.Empty;
            Func<bool> _available;
            Collider _collider;
    
            public int TeamId => _teamId;
            public ActorId ActorId => _actorId;
            public int TargetKey => ActorTargetKey.FromActorId(_actorId);
            public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
            public bool IsAvailable => _available == null || _available();
    
            // O11: sahne taraması yerine etkin hedef kaydı (hedefleme, sekme, top sıçraması).
            static readonly List<TargetableHost> s_live = new List<TargetableHost>();
            public static IReadOnlyList<TargetableHost> Live => s_live;
    
            void OnEnable()
            {
                if (!s_live.Contains(this))
                    s_live.Add(this);
            }
    
            void OnDisable() => s_live.Remove(this);
    
            public void Configure(int teamId, string displayName, ActorId actorId, Func<bool> available = null)
            {
                _teamId = teamId;
                _actorId = actorId;
                _displayName = displayName ?? string.Empty;
                _available = available;
                _collider = GetComponent<Collider>();
            }
    
            public float DistanceFrom(Vector3 origin)
            {
                _collider ??= GetComponent<Collider>();
                // ClosestPoint tetikleyici collider'da güvenilir değil (Unity noktayı geri verir,
                // mesafe 0 olur ve her düşman menzilde sanılır). Bounds tetikten etkilenmez.
                Vector3 point = _collider != null
                    ? _collider.bounds.ClosestPoint(origin)
                    : transform.position;
                point.y = origin.y;
                return Vector3.Distance(origin, point);
            }
    
            public float MarkerRadius(float padding)
            {
                _collider ??= GetComponent<Collider>();
                if (_collider == null)
                    return Mathf.Max(0.1f, padding);
                Bounds b = _collider.bounds;
                return Mathf.Max(b.extents.x, b.extents.z) + padding;
            }
    
            public float GroundY
            {
                get
                {
                    _collider ??= GetComponent<Collider>();
                    return _collider != null ? _collider.bounds.min.y : transform.position.y;
                }
            }
        }
    
        /// <summary>Hedef işaretinin ayarlanabilir sunum verisi; sahne runtime kurulduğu için bileşende yaşar.</summary>
    }
