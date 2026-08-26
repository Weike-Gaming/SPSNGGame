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
        public uint[] fgIngotValue = new uint[35];
        public uint[] fgPreviousIngotValue = new uint[35];

        public uint extraPrizeMultiplier = 0;
        public uint extraPrizeMultiplierIngotValue = 0;

        public byte extraJackpotType;

        public uint[] savedPreNudgeRng = new uint[5];
        public uint[] savedPostNudgeIngotValue = new uint[15];
        public uint[] savedPreNudgeIngotValue = new uint[15];
#pragma warning restore CS1591
    }
}
