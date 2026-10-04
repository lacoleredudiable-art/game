using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct MainClassNode
    {
        public MainClassNode(int id, string name, int[] runeIds, string category, string feel)
        {
            Id = id;
            Name = name ?? string.Empty;
            RuneIds = runeIds ?? Array.Empty<int>();
            Category = category ?? string.Empty;
            Feel = feel ?? string.Empty;
        }

        public int Id { get; }
        public string Name { get; }
        public int[] RuneIds { get; }
        public string Category { get; }
        public string Feel { get; }
    }
}
