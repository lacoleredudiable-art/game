using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct RuneDefinition
    {
        public RuneDefinition(
            int id, string name, string verbFace, string adjectiveFace,
            string adjectivePrefix, string verbNoun, string family, string category,
            string targetMode, string baseEffect, float passiveDurationDefault)
        {
            Id = id;
            Name = name ?? string.Empty;
            VerbFace = verbFace ?? string.Empty;
            AdjectiveFace = adjectiveFace ?? string.Empty;
            AdjectivePrefix = adjectivePrefix ?? string.Empty;
            VerbNoun = verbNoun ?? string.Empty;
            Family = family ?? string.Empty;
            Category = category ?? string.Empty;
            TargetMode = targetMode ?? string.Empty;
            BaseEffect = baseEffect ?? string.Empty;
            PassiveDurationDefault = passiveDurationDefault;
        }

        public int Id { get; }
        public string Name { get; }
        public string VerbFace { get; }
        public string AdjectiveFace { get; }
        public string AdjectivePrefix { get; }
        public string VerbNoun { get; }
        public string Family { get; }
        public string Category { get; }
        public string TargetMode { get; }
        public string BaseEffect { get; }
        public float PassiveDurationDefault { get; }
    }
}
