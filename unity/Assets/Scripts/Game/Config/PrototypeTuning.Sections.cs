using Dovus.Game.Config.Sections;
using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class PrototypeTuning
    {
        public ArenaSettings Arena = new ArenaSettings();
        public BossSettings Boss = new BossSettings();
        public CameraSettings Camera = new CameraSettings();
        public HudSettings Hud = new HudSettings();
        public InputSettings Input = new InputSettings();
        public PlayerSettings Player = new PlayerSettings();
        public VisualSettings Visuals = new VisualSettings();

        /// <summary>
        /// Serileştirilmiş bölüm şeması sürümü. 1 = iç içe Arena/Boss/… blokları (PLAN 2B.5b).
        /// Gelecek göçler bu sayıyı artırır; tek seferlik yükseltme burada veya editör aracında yapılır.
        /// </summary>
        [SerializeField, HideInInspector]
        int SectionsVersion;
    }
}
