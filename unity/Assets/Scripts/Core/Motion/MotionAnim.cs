using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Kalıp anahtarının Animator state/trigger karşılığı. Silah klibi yalnız tablodan gelir.</summary>
    public readonly struct MotionAnimClip
    {
        public MotionAnimClip(string key, string state, string trigger, bool fallback)
        {
            Key = key ?? string.Empty;
            State = state ?? string.Empty;
            Trigger = trigger ?? string.Empty;
            Fallback = fallback;
        }

        public string Key { get; }
        public string State { get; }
        public string Trigger { get; }
        public bool Fallback { get; }
    }

    /// <summary>
    /// Anahtar → state/trigger. weapon ve verb 0 ise herkese uyar.
    /// Daha özel satır (silah, sonra fiil) genel satırın önüne geçer.
    /// </summary>
    public sealed class MotionAnimTable
    {
        public const string FallbackState = "Locomotion";
        public const string AttackFallbackState = "BasicStrike";

        readonly List<Row> _rows;
        readonly string _fallbackState;

        MotionAnimTable(List<Row> rows, string fallbackState)
        {
            _rows = rows ?? new List<Row>();
            _fallbackState = string.IsNullOrEmpty(fallbackState) ? FallbackState : fallbackState;
        }

        public static MotionAnimTable BuiltIn { get; } = new MotionAnimTable(DefaultRows(), FallbackState);

        public static MotionAnimTable Parse(JsonValue root)
        {
            if (root.Kind != JsonKind.Object || !root.Has("anim_bridge"))
            {
                DesignWarnings.Once(
                    "motion.anim.table",
                    "Animasyon tablosu yok. Yerleşik locomotion/saldırı kliplerine düşülüyor.");
                return BuiltIn;
            }

            JsonValue table = root["anim_bridge"];
            string fallback = table["fallback_state"].AsString(FallbackState);
            var rows = new List<Row>();
            foreach (JsonValue row in table["rows"].AsArray())
            {
                string key = row["key"].AsString();
                string state = row["state"].AsString();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(state))
                    continue;
                rows.Add(new Row(
                    key,
                    state,
                    row["trigger"].AsString(),
                    WeaponKey(row),
                    row["verb"].AsInt(0)));
            }

            if (rows.Count == 0)
            {
                DesignWarnings.Once(
                    "motion.anim.table",
                    "Animasyon tablosu boş. Yerleşik locomotion/saldırı kliplerine düşülüyor.");
                return BuiltIn;
            }

            return new MotionAnimTable(rows, fallback);
        }

        public MotionAnimClip Resolve(string key, int weaponId, int verbId) =>
            Resolve(key, weaponId > 0 ? weaponId.ToString(System.Globalization.CultureInfo.InvariantCulture) : "", verbId);

        public MotionAnimClip Resolve(string key, string weaponKey, int verbId)
        {
            if (string.IsNullOrEmpty(key))
            {
                DesignWarnings.Once(
                    "motion.anim.missing.",
                    "Animasyon anahtarı boş. Yedek " + _fallbackState + ".");
                return new MotionAnimClip(string.Empty, _fallbackState, string.Empty, true);
            }

            Row best = null;
            int bestScore = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (!string.Equals(row.Key, key, StringComparison.Ordinal))
                    continue;
                if (!WeaponMatches(row.WeaponKey, weaponKey))
                    continue;
                if (row.VerbId != 0 && row.VerbId != verbId)
                    continue;
                int score = (string.IsNullOrEmpty(row.WeaponKey) ? 0 : 2) + (row.VerbId != 0 ? 1 : 0);
                if (score > bestScore)
                {
                    best = row;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                DesignWarnings.Once(
                    "motion.anim.missing." + key,
                    "Animasyon anahtarı yok: " + key + ". Yedek " + _fallbackState + ".");
                return new MotionAnimClip(key, _fallbackState, string.Empty, true);
            }

            return new MotionAnimClip(key, best.State, best.Trigger, false);
        }

        public static bool IsKnown(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            for (int i = 0; i < Known.Length; i++)
            {
                if (Known[i] == key)
                    return true;
            }
            return false;
        }

        /// <summary>Fazda anahtar yazılmamışsa hareket türünden yedek.</summary>
        public static string FallbackKey(string motion) => motion switch
        {
            "lunge" => "lunge",
            "dash" or "blink" or "return" or "pull" => "dash",
            "retreat" => "backstep",
            "sidestep" => "sidestep",
            "spin" or "fan" => "spin",
            "leap" or "hop" or "hover" => "leap",
            "slam" => "land",
            "throw" => "hook_throw",
            "hold" => "cast",
            "channel" => "cast",
            _ => "cast"
        };

        public static bool IsLocomotionKey(string key) =>
            key is "dash" or "backstep" or "sidestep" or "recover";

        public static readonly string[] Known =
        {
            "windup", "lunge", "dash", "backstep", "sidestep",
            "spin", "leap", "land", "hook_throw", "recover", "cast",
            "dash_fast"
        };

        static List<Row> DefaultRows()
        {
            // O-anim(c): JSON anim_bridge ile birebir — bkz. tools/build-motion-templates.py.
            var rows = new List<Row>();
            Add(rows, "windup", "CastPierce");
            Add(rows, "lunge", "BasicStrike");
            Add(rows, "dash", "Dodge");
            Add(rows, "backstep", "Backstep");
            Add(rows, "sidestep", "Sidestep");
            Add(rows, "spin", "Spin");
            Add(rows, "leap", "JumpAttack");
            Add(rows, "land", "JumpAttack");
            Add(rows, "hook_throw", "Throw");
            Add(rows, "recover", "Locomotion");
            Add(rows, "cast", "CastChannel");
            Add(rows, "dash_fast", "Dodge");
            return rows;
        }

        static void Add(List<Row> rows, string key, string state) =>
            rows.Add(new Row(key, state, string.Empty, string.Empty, 0));

        static string WeaponKey(JsonValue row)
        {
            if (!row.Has("weapon"))
                return string.Empty;
            JsonValue value = row["weapon"];
            if (value.Kind == JsonKind.Number)
            {
                int n = value.AsInt(0);
                return n == 0 ? string.Empty : n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            string text = value.AsString();
            if (string.IsNullOrEmpty(text) || text == "0" || text == "*")
                return string.Empty;
            return text;
        }

        static bool WeaponMatches(string rowKey, string weaponKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return true;
            if (string.IsNullOrEmpty(weaponKey))
                return false;
            if (string.Equals(rowKey, weaponKey, StringComparison.OrdinalIgnoreCase))
                return true;
            int colon = weaponKey.LastIndexOf(':');
            if (colon >= 0 && string.Equals(rowKey, weaponKey.Substring(colon + 1), StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        sealed class Row
        {
            public Row(string key, string state, string trigger, string weaponKey, int verbId)
            {
                Key = key;
                State = state;
                Trigger = trigger ?? string.Empty;
                WeaponKey = weaponKey ?? string.Empty;
                VerbId = verbId;
            }

            public string Key { get; }
            public string State { get; }
            public string Trigger { get; }
            public string WeaponKey { get; }
            public int VerbId { get; }
        }
    }

    /// <summary>Kalıp hızının bacak blend'i. İleri/strafe bakışa göredir.</summary>
    public readonly struct LocoBlend
    {
        public LocoBlend(float speedMps, float speed01, float forward, float strafe)
        {
            SpeedMps = speedMps;
            Speed01 = speed01;
            Forward = forward;
            Strafe = strafe;
        }

        public float SpeedMps { get; }
        public float Speed01 { get; }
        /// <summary>1 ileri, -1 geri.</summary>
        public float Forward { get; }
        /// <summary>1 sağa, -1 sola.</summary>
        public float Strafe { get; }

        public static LocoBlend FromVelocity(
            float velX, float velZ, float faceX, float faceZ, float refMps)
        {
            float speed = MathF.Sqrt(velX * velX + velZ * velZ);
            float reference = refMps > MotionAnimDefaults.Min05f ? refMps : MotionAnimDefaults.Lit64f;
            float flen = MathF.Sqrt(faceX * faceX + faceZ * faceZ);
            if (flen < 0.0001f)
            {
                faceX = 0f;
                faceZ = 1f;
            }
            else
            {
                faceX /= flen;
                faceZ /= flen;
            }

            if (speed < MotionAnimDefaults.Min05f)
                return new LocoBlend(0f, 0f, 0f, 0f);

            float forward = (velX * faceX + velZ * faceZ) / speed;
            float strafe = (velX * faceZ - velZ * faceX) / speed;
            float speed01 = Math.Clamp(speed / reference, 0f, 1f);
            return new LocoBlend(
                speed,
                speed01,
                Math.Clamp(forward, -1f, 1f),
                Math.Clamp(strafe, -1f, 1f));
        }

        /// <summary>
        /// Koşu klibi en çok 2× oynar. Gövde bunu aşarsa döngü hızlanmaz;
        /// <see cref="DashFastKey"/> tutulur (gerçek dash klibi animasyon paketiyle gelir).
        /// </summary>
        public const float TemplatePlaybackCap = 2f;

        public const string DashFastKey = "dash_fast";

        public static float MatchPlayback(float worldMps, float clipRunMps)
        {
            if (clipRunMps <= MotionAnimDefaults.Min05f || worldMps <= clipRunMps)
                return 1f;
            float need = worldMps / clipRunMps;
            return MathF.Min(need, TemplatePlaybackCap);
        }

        public static bool NeedsDashPose(float worldMps, float clipRunMps)
        {
            float clip = clipRunMps > MotionAnimDefaults.Min05f ? clipRunMps : MotionAnimDefaults.Lit224f;
            return worldMps > clip * TemplatePlaybackCap + MotionAnimDefaults.Epsilon01f;
        }

        /// <summary>Koşu anahtarı 2×'i aşan gövde hızında dash pozuna döner. Saldırı anahtarı kalır.</summary>
        public static string PresentationKey(string phaseKey, float worldMps, float clipRunMps)
        {
            if (NeedsDashPose(worldMps, clipRunMps) && MotionAnimTable.IsLocomotionKey(phaseKey))
                return DashFastKey;
            return phaseKey ?? string.Empty;
        }
    }
}
