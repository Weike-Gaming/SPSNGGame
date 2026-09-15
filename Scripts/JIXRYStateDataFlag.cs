using System;

namespace Weike.Games.JIXRY
{
    /// <summary>
    /// State Data Flag
    /// For easier Property Binding
    /// </summary>
    [Flags]
    public enum JIXRYStateDataFlag : byte
    {
#pragma warning disable CS1591
        MAIN_GAME = 0,
        FREE_GAME_LW = 0x01,  // 0000 0001
        FREE_GAME_JP = 0x02,  // 0000 0010
        FREE_GAME_LB = 0x04,  // 0000 0100
        FREE_GAME_LWJP = 0x08,  // 0000 1000
        FREE_GAME_LWLB = 0x010, // 0001 0000
        FREE_GAME_JPLB = 0x020, // 0010 0000
        FREE_GAME_LWJPLB = 0x040, // 0100 0000
#pragma warning restore CS1591
    }
}