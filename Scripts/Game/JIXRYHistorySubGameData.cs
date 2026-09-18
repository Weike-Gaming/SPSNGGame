using System.Runtime.InteropServices;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class JIXRYHistorySubGameData : WkSlotHistorySubGameData
    {
#pragma warning disable CS1591
        // From JIXRYGameDataModel
        public byte previousMaxWayWin;

        public byte potFeatureGameFlag;
        public byte savedPreviousPotFeatureGameFlag;

        // From JIXRYFreeGameDataModel
        public uint[] fgIngotValue = new uint[40];
        public uint[] fgPreviousIngotValue = new uint[40];
        public uint[] fgIsMultiply = new uint[20];

        public uint extraPrizeMultiplier = 0;
        public uint extraPrizeMultiplierIngotValue = 0;

        public byte extraJackpotType;

#pragma warning restore CS1591
    }
}
