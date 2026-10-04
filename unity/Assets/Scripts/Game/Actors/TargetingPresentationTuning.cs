using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.DevTools;
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
    public sealed class TargetingPresentationTuning
        {
            public float RingPaddingM = PlayerTargetingControllerDefaults.RingPaddingM;
            public float RingWidthM = PlayerTargetingControllerDefaults.RingWidthM;
            public float RingGroundOffsetM = PlayerTargetingControllerDefaults.RingGroundOffsetM;
            public int RingSegments = PlayerTargetingControllerDefaults.RingSegments;
            public Color EnemyColor = new(1f, 0.28f, 0.16f, 0.95f);
            public Color AllyColor = new(0.25f, 1f, 0.58f, 0.95f);
            public Vector2 FrameSizePx = new(PlayerTargetingControllerDefaults.FrameWidthPx, PlayerTargetingControllerDefaults.FrameHeightPx);
            public Vector2 FrameOffsetPx = new(0f, -PlayerTargetingControllerDefaults.FrameOffsetYPx);
            public int FrameFontPx = PlayerTargetingControllerDefaults.FrameFontPx;
        }
    
        /// <summary>
        /// Oyuncuya özel seçim + otomatik hedef çözümü. Static hedef durumu tutmaz; gelecekte her
        /// co-op oyuncusu kendi örneğini ve takım kimliğini taşıyabilir.
        /// </summary>
    }
