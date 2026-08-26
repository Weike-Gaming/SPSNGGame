using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameEndPanel : WkStateFreeGameEndPanel
    {
        public JIXRYStateFreeGameEndPanel(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();

            #region burn-in
            if (machineContext!.platformInterface!.IsBurnInMode())
            {
                playerController?.InvokeActions("Key_Spin_BurnIn");
            }
            #endregion
        }

        protected override void BindActions()
        {
            base.BindActions();
            playerController!.AddAction("Key_TakeWin", new WkMysteryProgTakeWinAction(playerController));
            playerController!.AddAction("Key_Spin", new WkMysteryProgTakeWinAction(playerController));

            #region burn-in
            if (machineContext!.platformInterface!.IsBurnInMode())
            {
                playerController!.AddAction("Key_Spin_BurnIn", new WkMysteryProgTakeWinAction(playerController));
            }
            #endregion
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RecoverPreviousIngotData(true);
            gm.RecoverReel();
            gm.RecoverFgCounter();
            gm.FgEndPanelCheckWin();
            gm.PlayWinAnimation();
        }
    }
}
