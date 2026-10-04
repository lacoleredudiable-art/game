using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dovus.Core.Tuning;

namespace Dovus.Core.Mechanic
{
    /// <summary>Reçetedeki tek görsel parça. Yerel çerçeve: X sağ, Y yukarı, Z ileri (cast yönü).</summary>
    public struct VisualPiece
    {
        public double X, Y, Z;
        public double DelaySec;
        public double Scale;
        public double StretchZ;
        /// <summary>Parça bu kadar aşağıdan doğup Y'ye yükselir.</summary>
        public double RiseM;
        /// <summary>Parça ömrü boyunca bu kadar yer değiştirir (çekim, kavis, yayılma).</summary>
        public double MoveX, MoveZ;
        /// <summary>Taşıyıcıya (mermi) bağlı, onunla uçar.</summary>
        public bool Carried;
        /// <summary>Katı cismi olmayan, yolun ucunda asılı kalan pus; döngüde yalnız bu parçalar yenilenir.</summary>
        public bool Haze;
    }
}
