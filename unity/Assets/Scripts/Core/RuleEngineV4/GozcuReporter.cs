using System.Collections.Generic;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Gözcü satırı: <c>[GOZCU] &lt;kural&gt; &lt;değer&gt; kare=&lt;n&gt;</c>.
    /// Aynı kural <c>intervalSec</c> içinde en çok bir kez yazılır; zaman parametredir.
    /// </summary>
    public sealed class GozcuReporter
    {
        public const string Tag = "[GOZCU]";

        readonly double _intervalSec;
        readonly Dictionary<string, double> _lastSec = new();

        public GozcuReporter(double intervalSec)
        {
            _intervalSec = intervalSec;
        }

        /// <summary>Yazılan satır sayısı (kısıtlanan ihlaller sayılmaz).</summary>
        public int Reported { get; private set; }

        public bool TryReport(string rule, string value, int frame, double nowSec, out string line)
        {
            if (_lastSec.TryGetValue(rule, out double last) && nowSec - last < _intervalSec)
            {
                line = null;
                return false;
            }
            _lastSec[rule] = nowSec;
            Reported++;
            line = Format(rule, value, frame);
            return true;
        }

        public static string Format(string rule, string value, int frame) =>
            Tag + " " + rule + " " + (string.IsNullOrEmpty(value) ? "-" : value.Replace(' ', '_')) + " kare=" + frame;
    }
}
