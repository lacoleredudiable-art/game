using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

namespace Dovus.Core.Presentation
{
    /// <summary>
    /// v6.1.1 animations[weaponKey][verbId] → mevcut Quaternius controller state.
    /// JSON adı okunabilir skill animasyon adıdır; controller'da özel clip yoksa fiil
    /// action/hitbox'ından mevcut Cast* state'e düşer. Animator state yokluğu Game'de no-op'tur.
    /// </summary>
    public sealed class AnimationDatabase
    {
        readonly Dictionary<string, AnimationBinding> _bindings =
            new Dictionary<string, AnimationBinding>(StringComparer.Ordinal);

        public int Count => _bindings.Count;

        public static AnimationDatabase FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static AnimationDatabase FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static AnimationDatabase FromJsonRoot(JsonValue root)
        {
            if (root.IsNull)
                throw new ArgumentException("JSON kökü okunamadı.", nameof(root));
            var database = new AnimationDatabase();
            JsonValue animations = root["animations"];
            JsonValue verbBase = root["verb_base"];
            foreach (var weapon in animations.AsObject())
            {
                if (weapon.Value.Kind != JsonKind.Object)
                    continue;
                foreach (var verb in weapon.Value.AsObject())
                {
                    if (!int.TryParse(verb.Key, out int verbId))
                        continue;
                    string displayName = verb.Value.AsString();
                    JsonValue engine = verbBase[verb.Key];
                    string state = ResolveExistingState(
                        engine["action"].AsString(),
                        engine["hitbox"].AsString());
                    database._bindings[Key(weapon.Key, verbId)] =
                        new AnimationBinding(weapon.Key, verbId, displayName, state);
                }
            }
            return database;
        }

        public bool TryGet(string weaponAnimationsKey, int verbId, out AnimationBinding binding)
        {
            return _bindings.TryGetValue(Key(weaponAnimationsKey, verbId), out binding);
        }

        static string Key(string weaponKey, int verbId) =>
            (weaponKey ?? string.Empty) + ":" + verbId;

        static string ResolveExistingState(string action, string hitbox)
        {
            if (action is "heal" or "buff" or "cleanse" or "summon" or "tempo")
                return "CastChannel";
            if (action is "shield" or "reflect")
                return "CastGuard";
            if (action == "dash" || hitbox == "dash_line")
                return "CastSweep";
            if (hitbox == "ground_ring")
                return "CastSlam";
            if (hitbox is "projectile" or "target")
                return "CastPierce";
            return "CastChannel";
        }
    }

    public readonly struct AnimationBinding
    {
        public AnimationBinding(
            string weaponKey,
            int verbId,
            string displayName,
            string animatorState)
        {
            WeaponKey = weaponKey ?? string.Empty;
            VerbId = verbId;
            DisplayName = displayName ?? string.Empty;
            AnimatorState = animatorState ?? string.Empty;
        }

        public string WeaponKey { get; }
        public int VerbId { get; }
        public string DisplayName { get; }
        public string AnimatorState { get; }
    }
}
