#if UNITY_EDITOR
using Dovus.Core.Boss;
using Dovus.Core.Element;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Dovus.Game.Editor
{
    public sealed class PlaySweepResult
    {
        public PlaySweepCase Case;
        public string Name = "";
        public string Template = "";
        public string Weapon = "";
        public bool Cast;
        public bool Hit;
        public bool Position;
        public bool NotInside;
        public bool OneSystem;
        public bool NoErrors;
        public bool OnTime;
        public bool NoTeleport;
        public bool Grounded;
        /// <summary>
        /// Planın taşıdığı boss etkisi görüldü mü (yalnız o anahtar varsa sınanır):
        /// ters_kontrol → boss ters kontrolde; dikkat_ceker → yem boss aggro'sunu tutuyor.
        /// </summary>
        public bool Effect = true;
        public float FootLiveM;
        public float FootSettleM;
        public string ExpectedPos = "";
        public string ActualPos = "";
        public float Damage;
        public float MinDist;
        public float Contact;
        public float TemplateSec;
        public float ExpectedSec;
        public float TotalSec;
        public string Effects = "";
        public string Legs = "";
        public readonly List<string> Notes = new();

        public bool Pass => Cast && Hit && Position && NotInside && OneSystem && NoErrors && OnTime && NoTeleport && Grounded && Effect;
    }
}
#endif
