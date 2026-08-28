using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameEndFromJackpot : WkStateFreeGameEnd
    {
        public JIXRYStateFreeGameEndFromJackpot(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.SaveFinalIngotAmount(true);
            gm.SavePreviousIngotData();
            gm.SaveFreeGameCount();
            gm.SetCurrentFeature();
            gm.SavePreviousPotFeatureState();
            //gm.SavePostNudgeIngotValue();
            base.EnterState();
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            gm.RecoverFgCounter();
            gm.RecoverPreviousPotFeatureState();
            //gm.RecoverFgIngotDigit();
            gm.RecoverReel();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;
            rm.PlayPrizeMultiplierTransformationWithoutAnimation();
            rm.ForceUpdateRecoverTransform();
        }
    }
}
