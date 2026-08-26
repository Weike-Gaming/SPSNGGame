using System;

namespace Weike.Games.JIXRY
{
    /// <summary>
    /// State Data Flag
    /// For easier Property Binding
    /// </summary>
    [Flags]
    public enum JIXRYExtraIngotPosition : byte
    {
#pragma warning disable CS1591
        None = 0,                  // 0000 0000
        MultiplierIn2 = 1 << 3,    // 0000 1000
#pragma warning restore CS1591
    }
}
