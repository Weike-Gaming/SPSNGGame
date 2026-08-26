using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYJackpotHorizontalLayout : WkJackpotHorizontalLayout
    {
        [SerializeField] bool isBonus = false;

        protected override ulong GetPrizeAmount()
        {
            if (isBonus)
            {
                JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
                if (gm is null) { return base.GetPrizeAmount(); }
                JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
                if (dm is null) { return base.GetPrizeAmount(); }

                uint betMultiplier = dm.getSelectedBetMultipliers[dm.targetedBetMultiplier];
                return base.GetPrizeAmount() * betMultiplier;
            }
            else
            {
                return base.GetPrizeAmount();
            }           
        }
    }
}
