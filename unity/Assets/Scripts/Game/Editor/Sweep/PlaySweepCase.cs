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
    /// <summary>Tek cast: fiil + sıfat, boss'a merkez mesafesi, silah.</summary>
    public sealed class PlaySweepCase
    {
        public int Verb;
        public int Adj;
        public float StartDistM = 3f;
        public string Weapon = "Kılıç";
        public string Label = "";
        /// <summary>Kare kare iz detay dosyasına yazılır.</summary>
        public bool Trace;
        /// <summary>Kalıp başladıktan BossShiftAtSec sonra boss X ekseninde bu kadar kayar.</summary>
        public float BossShiftX;
        public float BossShiftAtSec = 0.2f;
        public float BossShiftDurSec = 0.3f;
        /// <summary>
        /// Kalıp başladıktan PlayerShiftAtSec sonra oyuncu boss'a doğru bu kadar taşınır
        /// (eksi: uzaklaşır). Kalıp dışı yer değişimi; tek-sistem ölçümü bunu görür.
        /// </summary>
        public float PlayerShiftM;
        public float PlayerShiftAtSec = 0.2f;
        public float PlayerShiftDurSec = 0.1f;
        /// <summary>Kalıp zamanı (sn). Editör o anda duraklar; ekran görüntüsü için.</summary>
        public float[] PauseAtSec;
        /// <summary>Kalıp StickAtSec'e gelince çubuk bu yöne basılır, kalıp bitince bırakılır.</summary>
        public Vector2 Stick;
        public float StickAtSec = 0.5f;

        public string Id => Verb + "-" + Adj;
    }
}
#endif
