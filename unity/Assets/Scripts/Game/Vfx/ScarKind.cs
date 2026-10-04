using Dovus.Game.Casting;
using Dovus.Game.Config;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;
namespace Dovus.Game.Vfx
{
    public enum ScarKind
        {
            Crack,
            Needle,
            Swarm,
            Acid,
            Strike
        }
    
        /// <summary>
        /// T4: kalıcı dünya izi. Sayı yazılmaz — yerde çatlak / birikinti kalır.
        /// İz süreye bağlı silinmez (§8/T4); tavan dolunca en eski iz DÖNÜŞTÜRÜLÜR
        /// (T11 kare bütçesi — bkz. GameTuning.GroundScarCapCount).
        /// </summary>
    }
