using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Dovus.Core.Tuning
{
    /// <summary>
    /// O-17.8: ayar dosyasının alan yolları ("Dodge.IframeMs", "Sentence.CancelWindowMs[0]",
    /// "Sentence.Steps[2].RecoverySec"). Dosyada hangi alanın panelden kaydedildiği bu yollarla işaretlenir;
    /// yükleme yalnız işaretli yolları kopyalar. İç içe nesnelerin kimliği korunur (yalnız yaprak alan yazılır).
    /// </summary>
    public static class TuningFieldPaths
    {
        const BindingFlags Fields = BindingFlags.Public | BindingFlags.Instance;

        /// <summary>Kökün bütün yaprak alanları, dosyadaki sırayla.</summary>
        public static List<KeyValuePair<string, object>> Leaves(object root, string prefix)
        {
            var into = new List<KeyValuePair<string, object>>();
            if (root != null)
                Collect(root, prefix ?? string.Empty, into);
            return into;
        }

        /// <summary><paramref name="path"/> yaprağını <paramref name="from"/>'dan <paramref name="to"/>'ya kopyalar.</summary>
        public static bool TryCopy(string path, object from, object to)
        {
            if (string.IsNullOrEmpty(path) || from == null || to == null)
                return false;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryParse(parts[i], out string name, out int index))
                    return false;
                FieldInfo f = from.GetType().GetField(name, Fields);
                if (f == null || IsNonSerialized(f) || to.GetType() != from.GetType())
                    return false;
                bool last = i == parts.Length - 1;
                if (index < 0)
                {
                    if (last)
                    {
                        if (!IsLeaf(f.FieldType))
                            return false;
                        f.SetValue(to, f.GetValue(from));
                        return true;
                    }
                    if (IsLeaf(f.FieldType))
                        return false;
                    from = f.GetValue(from);
                    to = f.GetValue(to);
                }
                else
                {
                    if (!(f.GetValue(from) is Array src) || !(f.GetValue(to) is Array dst))
                        return false;
                    if (index >= src.Length || index >= dst.Length)
                        return false;
                    bool leafElement = IsLeaf(f.FieldType.GetElementType());
                    if (last)
                    {
                        if (!leafElement)
                            return false;
                        dst.SetValue(src.GetValue(index), index);
                        return true;
                    }
                    if (leafElement)
                        return false;
                    from = src.GetValue(index);
                    to = dst.GetValue(index);
                }
                if (from == null || to == null)
                    return false;
            }
            return false;
        }

        /// <summary>Log ve karşılaştırma için kültürden bağımsız değer yazımı.</summary>
        public static string Format(object value) => value switch
        {
            null => "null",
            float f => f.ToString("0.####", CultureInfo.InvariantCulture),
            double d => d.ToString("0.####", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            IFormattable x => x.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

        static void Collect(object node, string prefix, List<KeyValuePair<string, object>> into)
        {
            foreach (FieldInfo f in node.GetType().GetFields(Fields))
            {
                if (IsNonSerialized(f))
                    continue;
                object v = f.GetValue(node);
                Type t = f.FieldType;
                if (t.IsArray)
                {
                    if (!(v is Array arr))
                        continue;
                    bool leafElement = IsLeaf(t.GetElementType());
                    for (int i = 0; i < arr.Length; i++)
                    {
                        object e = arr.GetValue(i);
                        string p = $"{prefix}{f.Name}[{i}]";
                        if (leafElement)
                            into.Add(new KeyValuePair<string, object>(p, e));
                        else if (e != null)
                            Collect(e, p + ".", into);
                    }
                }
                else if (IsLeaf(t))
                    into.Add(new KeyValuePair<string, object>(prefix + f.Name, v));
                else if (v != null)
                    Collect(v, prefix + f.Name + ".", into);
            }
        }

        static bool IsNonSerialized(FieldInfo f) => f.IsDefined(typeof(NonSerializedAttribute), false);

        static bool IsLeaf(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string) || t.IsValueType;

        static bool TryParse(string part, out string name, out int index)
        {
            index = -1;
            name = part;
            int open = part.IndexOf('[');
            if (open < 0)
                return part.Length > 0;
            if (!part.EndsWith("]", StringComparison.Ordinal) || open == 0)
                return false;
            name = part.Substring(0, open);
            return int.TryParse(part.Substring(open + 1, part.Length - open - 2), NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }
    }
}
