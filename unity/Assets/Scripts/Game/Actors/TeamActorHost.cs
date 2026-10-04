using Dovus.Core.Team;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Sahnedeki oyuncu veya dummy'yi IAllyPlayer yapar.
    /// Ağ oyuncusu aynı arayüzü sonra doldurur.
    /// </summary>
    public sealed class TeamActorHost : MonoBehaviour, IAllyPlayer
    {
        public int Id;
        public float Radius = 0.5f;
        public float HpRatio = 1f;
        public string LastSkillId = string.Empty;
        public bool TemplateOwnsPosition;

        int IAllyPlayer.Id => Id;
        float IAllyPlayer.X => transform.position.x;
        float IAllyPlayer.Y => transform.position.y;
        float IAllyPlayer.Z => transform.position.z;
        float IAllyPlayer.Radius => Radius;
        float IAllyPlayer.HpRatio => HpRatio;
        string IAllyPlayer.LastSkillId => LastSkillId;
        bool IAllyPlayer.TemplateOwnsPosition => TemplateOwnsPosition;
    }
}
