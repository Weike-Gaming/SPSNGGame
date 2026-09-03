using System.Runtime.InteropServices;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class JIXRYHistoryMainGameData : WkSlotHistoryMainGameData
    {
#pragma warning disable CS1591
        public uint[] ingotValue = new uint[40];
        public byte savedTriggerPotFeatureGameFlag;
#pragma warning restore CS1591
    }
}
