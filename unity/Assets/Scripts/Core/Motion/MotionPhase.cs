using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    public sealed class MotionPhase
    {
        public MotionPhase(
            string name,
            string motion,
            float durationSec,
            string facing,
            string homing,
            string gate,
            float maxHoldSec,
            float distanceM,
            float forwardM,
            float side,
            float sideM,
            float heightM,
            float yawDeg,
            float gapM,
            float overshootM,
            float shotM,
            float driftM,
            float walkMps,
            float behindM,
            float snapAt,
            float[] curve,
            MotionHitSpec hit,
            string land = "",
            bool plant = false,
            string anim = "",
            float animSpeed = 1f)
        {
            Name = name ?? string.Empty;
            Motion = motion ?? "hold";
            Land = land ?? string.Empty;
            Plant = plant;
            Anim = string.IsNullOrEmpty(anim) ? MotionAnimTable.FallbackKey(Motion) : anim;
            AnimSpeed = animSpeed > MotionTemplateCatalogDefaults.MinAnimSpeed ? animSpeed : 1f;
            DurationSec = durationSec;
            Facing = string.IsNullOrEmpty(facing) ? "target" : facing;
            Homing = string.IsNullOrEmpty(homing) ? "none" : homing;
            Gate = gate ?? string.Empty;
            MaxHoldSec = maxHoldSec;
            DistanceM = distanceM;
            ForwardM = forwardM;
            Side = side;
            SideM = sideM;
            HeightM = heightM;
            YawDeg = yawDeg;
            GapM = gapM;
            OvershootM = overshootM;
            ShotM = shotM;
            DriftM = driftM;
            WalkMps = walkMps;
            BehindM = behindM;
            SnapAt = snapAt;
            Curve = curve;
            Hit = hit;
        }

        public string Name { get; }
        public string Motion { get; }
        /// <summary>"behind" ise faz hedefinin öte kenarında biter.</summary>
        public string Land { get; }
        /// <summary>Bu faz bitince yere bir nokta çakılır; sonraki "plant" vuruşu orada kalır.</summary>
        public bool Plant { get; }
        public float DurationSec { get; }
        public string Facing { get; }
        public string Homing { get; }
        public string Gate { get; }
        public float MaxHoldSec { get; }
        public float DistanceM { get; }
        public float ForwardM { get; }
        public float Side { get; }
        public float SideM { get; }
        public float HeightM { get; }
        public float YawDeg { get; }
        public float GapM { get; }
        public float OvershootM { get; }
        public float ShotM { get; }
        public float DriftM { get; }
        public float WalkMps { get; }
        public float BehindM { get; }
        public float SnapAt { get; }
        public float[] Curve { get; }
        public MotionHitSpec Hit { get; }
        /// <summary>Animator tablosundaki anahtar (windup, lunge, dash, spin…).</summary>
        public string Anim { get; }
        /// <summary>Klip oynatma hızı. 1 normal.</summary>
        public float AnimSpeed { get; }

        /// <summary>
        /// Bu faz kökü yerden kaldırır. Şablon verisindeki hareket + height_m bunu söyler
        /// (hop/leap/slam/hover, ya da yüksekliği olan channel).
        /// </summary>
        public bool Airborne => VerticalCurve.LeavesGround(Motion, HeightM);

        /// <summary>Zemin kökünün üstündeki yükseklik (m). Yer fazında 0.</summary>
        public float HeightAboveGround(float uLinear) =>
            VerticalCurve.Height(Motion, HeightM, uLinear);

        /// <summary>Aynı faz; mesafe / arkaya iniş gramer yedeğiyle doldurulmuş kopya.</summary>
        public MotionPhase WithTravel(float distanceM, float behindM, string land) =>
            new MotionPhase(
                Name, Motion, DurationSec, Facing, Homing, Gate, MaxHoldSec,
                distanceM, ForwardM, Side, SideM, HeightM, YawDeg, GapM, OvershootM,
                ShotM, DriftM, WalkMps, behindM, SnapAt, Curve, Hit,
                land ?? Land, Plant, Anim, AnimSpeed);

        public MotionPhase WithHoming(string homing) =>
            new MotionPhase(
                Name, Motion, DurationSec, Facing, homing, Gate, MaxHoldSec,
                DistanceM, ForwardM, Side, SideM, HeightM, YawDeg, GapM, OvershootM,
                ShotM, DriftM, WalkMps, BehindM, SnapAt, Curve, Hit,
                Land, Plant, Anim, AnimSpeed);
    }
}
