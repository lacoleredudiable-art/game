using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>Boss'un (ve sonra küçük canavarların) hedef alabileceği dost türleri.</summary>
    public enum TargetKind
    {
        Player,
        Ally,
        Decoy
    }
}
