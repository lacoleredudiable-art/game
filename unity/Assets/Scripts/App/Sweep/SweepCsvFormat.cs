using System;
using System.Globalization;

namespace Dovus.App.Sweep
{
    public static class SweepCsvFormat
    {
        public static string Quote(string s) => "\"" + (s ?? "").Replace("\"", "'") + "\"";

        public static string Bit(bool b) => b ? "1" : "0";

        public static string Number(float f) => f.ToString("F2", CultureInfo.InvariantCulture);
    }
}
