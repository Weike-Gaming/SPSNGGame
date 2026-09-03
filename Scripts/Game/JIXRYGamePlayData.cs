using System;
using System.Linq;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYGamePlayData : WkSlotGamePlayData
    {
        protected bool Equals(JIXRYGamePlayData other)
        {
            return base.Equals(other) &&
                weightageSetId == other.weightageSetId &&
                weightageReelStripSetId == other.weightageReelStripSetId &&
                previousMaxWayWin == other.previousMaxWayWin &&
                ingotValue.SequenceEqual(other.ingotValue) &&
                previousIngotValue.SequenceEqual(other.previousIngotValue) &&
                potFeatureGameFlag == other.potFeatureGameFlag &&
                upcomingPotFeatureGameFlag == other.upcomingPotFeatureGameFlag &&
                savedPreviousPotFeatureGameFlag == other.savedPreviousPotFeatureGameFlag &&
                savedTriggerPotFeatureGameFlag == other.savedTriggerPotFeatureGameFlag &&
                savedBluePotScatter == other.savedBluePotScatter &&
                savedRedPotScatter == other.savedRedPotScatter &&
                savedGreenPotScatter == other.savedGreenPotScatter &&
                fgIngotValue.SequenceEqual(other.fgIngotValue) &&
                fgPreviousIngotValue.SequenceEqual(other.fgPreviousIngotValue) &&
                extraPrizeMultiplier == other.extraPrizeMultiplier &&
                extraPrizeMultiplierIngotValue == other.extraPrizeMultiplierIngotValue &&
                previousExtraPrizeMultiplier == other.previousExtraPrizeMultiplier &&
                extraJackpotType == other.extraJackpotType &&
                previousExtraJackpotType == other.previousExtraJackpotType;

        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JIXRYGamePlayData)obj);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(weightageSetId);
            hashCode.Add(weightageReelStripSetId);
            hashCode.Add(previousMaxWayWin);
            hashCode.Add(ingotValue);
            hashCode.Add(previousIngotValue);
            hashCode.Add(potFeatureGameFlag);
            hashCode.Add(upcomingPotFeatureGameFlag);
            hashCode.Add(savedPreviousPotFeatureGameFlag);
            hashCode.Add(savedTriggerPotFeatureGameFlag);
            hashCode.Add(savedBluePotScatter);
            hashCode.Add(savedRedPotScatter);
            hashCode.Add(savedGreenPotScatter);
            hashCode.Add(fgIngotValue);
            hashCode.Add(fgPreviousIngotValue);
            hashCode.Add(extraPrizeMultiplier);
            hashCode.Add(extraPrizeMultiplierIngotValue);
            hashCode.Add(previousExtraPrizeMultiplier);
            hashCode.Add(extraJackpotType);
            hashCode.Add(previousExtraJackpotType);
            return hashCode.ToHashCode();
        }

        public byte weightageSetId;
        public byte weightageReelStripSetId;

        // From JIXRYGameDataModel
        public byte previousMaxWayWin = 0;
        public uint[] ingotValue = new uint[40];
        public uint[] previousIngotValue = new uint[40];

        public byte potFeatureGameFlag;
        public byte upcomingPotFeatureGameFlag;
        public byte savedPreviousPotFeatureGameFlag;
        public byte savedTriggerPotFeatureGameFlag;

        public int savedBluePotScatter;
        public int savedRedPotScatter;
        public int savedGreenPotScatter;

        // From JIXRYfreeGameDataModel
        public uint[] fgIngotValue = new uint[40];
        public uint[] fgPreviousIngotValue = new uint[40];

        public uint extraPrizeMultiplier = 0;
        public uint extraPrizeMultiplierIngotValue = 0;
        public uint previousExtraPrizeMultiplier = 0;

        public byte extraJackpotType = 0;
        public byte previousExtraJackpotType = 0;

    }
}