using Dovus.Game.Config.Sections;
using UnityEngine;

namespace Dovus.Game.Config
{
    public sealed partial class GameTuning
    {
        public ArenaSettings Arena = new ArenaSettings();
        public BossSettings Boss = new BossSettings();
        public CameraSettings Camera = new CameraSettings();
        public HudSettings Hud = new HudSettings();
        public InputSettings Input = new InputSettings();
        public PlayerSettings Player = new PlayerSettings();
        public VisualSettings Visuals = new VisualSettings();

        /// <summary>
        /// Serile??tirilmi?? b??l??m ??emas?? s??r??m??. 1 = i?? i??e Arena/Boss/??? bloklar?? (PLAN 2B.5b).
        /// Gelecek g????ler bu say??y?? art??r??r; tek seferlik y??kseltme burada veya edit??r arac??nda yap??l??r.
        /// </summary>
        [SerializeField, HideInInspector]
        int SectionsVersion;
    }
}
