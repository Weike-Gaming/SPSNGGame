using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateJackpotAnnouncement : WkStateRandomJackpotAnnouncement
    {
        private const float PanelDelay = 6.82f;

        public JIXRYStateJackpotAnnouncement(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            InvokeMethod(RunFinishJp, PanelDelay);
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.UpdateBetDisplay();
            gm.RecoverPreviousIngotData(false);
            gm.RecoverReel();
            gm.MgCheckWin();
            gm.RecoverJackpotPrize();
            gm.RecoverFgCounter();
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