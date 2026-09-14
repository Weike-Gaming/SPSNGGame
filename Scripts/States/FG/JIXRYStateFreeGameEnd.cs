using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameEnd : WkStateFreeGameEnd
    {
        public JIXRYStateFreeGameEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
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
            gm.CheckisLuckyWin();
            base.EnterState();

        }

        public override void RecoverState()
        {
            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            gm.ChangeReelToLuckyBoost(4);
            base.RecoverState();
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