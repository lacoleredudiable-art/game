using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dovus.Core.Shared
{
    public sealed class JsonValue
    {
        public static readonly JsonValue Null = new JsonValue();
        static readonly Dictionary<string, JsonValue> EmptyObject = new(StringComparer.Ordinal);
        static readonly List<JsonValue> EmptyArray = new();

        readonly string _str = string.Empty;
        readonly double _num;
        readonly bool _bool;
        readonly Dictionary<string, JsonValue> _obj = EmptyObject;
        readonly List<JsonValue> _arr = EmptyArray;

        JsonValue() { Kind = JsonKind.Null; }
        JsonValue(string s) { Kind = JsonKind.String; _str = s ?? string.Empty; }
        JsonValue(double n) { Kind = JsonKind.Number; _num = n; }
        JsonValue(bool b) { Kind = JsonKind.Bool; _bool = b; }
        JsonValue(Dictionary<string, JsonValue> o) { Kind = JsonKind.Object; _obj = o; }
        JsonValue(List<JsonValue> a) { Kind = JsonKind.Array; _arr = a; }

        public static JsonValue Of(string s) => new JsonValue(s);
        public static JsonValue Of(double n) => new JsonValue(n);
        public static JsonValue Of(bool b) => new JsonValue(b);
        public static JsonValue Of(Dictionary<string, JsonValue> o) => new JsonValue(o ?? EmptyObject);
        public static JsonValue Of(List<JsonValue> a) => new JsonValue(a ?? EmptyArray);

        public JsonKind Kind { get; }
        public bool IsNull => Kind == JsonKind.Null;

        public string AsString(string fallback = "") => Kind == JsonKind.String ? _str : fallback;
        public double AsDouble(double fallback = 0d) => Kind == JsonKind.Number ? _num : fallback;
        public float AsFloat(float fallback = 0f) => Kind == JsonKind.Number ? (float)_num : fallback;
        public int AsInt(int fallback = 0) => Kind == JsonKind.Number ? (int)_num : fallback;
        public bool AsBool(bool fallback = false) => Kind == JsonKind.Bool ? _bool : fallback;

        public IReadOnlyList<JsonValue> AsArray() => Kind == JsonKind.Array ? _arr : EmptyArray;

        public IReadOnlyDictionary<string, JsonValue> AsObject() =>
            Kind == JsonKind.Object ? _obj : EmptyObject;

        /// <summary>Nesne değilse Null döner (zincirleme güvenli: a["x"]["y"].AsFloat()).</summary>
        public JsonValue this[string field]
        {
            get
            {
                if (Kind == JsonKind.Object && _obj.TryGetValue(field, out JsonValue v))
                    return v;
                return Null;
            }
        }

        public bool Has(string field) => Kind == JsonKind.Object && _obj.ContainsKey(field);

        /// <summary>
        /// target_behaviors gibi alanlar sürümden sürüme şekil değiştiriyor: bazen
        /// düz string ("kendine_topla"), bazen {"read_as": "Kendine topla"} nesnesi.
        /// İkisini de tek metne indirger.
        /// </summary>
        public string AsTextOrField(string nestedField, string fallback = "")
        {
            if (Kind == JsonKind.String) return _str;
            if (Kind == JsonKind.Object && _obj.TryGetValue(nestedField, out JsonValue v) && v.Kind == JsonKind.String)
                return v._str;
            return fallback;
        }
    }
}
