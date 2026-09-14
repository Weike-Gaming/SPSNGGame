using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameJackpotAnnouncement : WkStateFreeGameRandomJackpotAnnouncement
    {
        private const float PanelDelay = 6.8f;

        public JIXRYStateFreeGameJackpotAnnouncement(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.UpdateFreeGameWinAmount();
            //gm.SavePostNudgeIngotValue();
            InvokeMethod(RunFinishJp, PanelDelay);
        }

        public override void RecoverState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.ChangeReelToLuckyBoost(4);
            base.RecoverState();
            gm.RecoverFgCounter();
            gm.RecoverPreviousPotFeatureState();
            //gm.RecoverFgIngotDigit();
            gm.RecoverTempIngotDigit();
            gm.ForceResetAnimationBitMask();
            gm.UpdateBetDisplay();
            gm.RecoverFgIngotData();
            gm.RecoverReelJackpot();
            gm.FgCheckWin(false);
            gm.RecoverWinAmount();
            gm.DoneNudge();
            gm.FgCheckWinIngot();
            gm.RecoverJackpotPrize();
            gm.RecoverJackpotAmount();
            gm.CheckFgExtraPrize();
            InvokeMethod(RunFinishJp, PanelDelay);
        }

        public override void PIError()
        {
            base.PIError();
            CancelInvokeMethod(RunFinishJp);
        }

        private void RunFinishJp()
        {
            GetGameManagerChecked<JIXRYGameManager>().RunFinishJackpotAnnouncementCommand();
        }
    }
}