using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameInit : WkStateFreeGameInit
    {
        public JIXRYStateFreeGameInit(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
            wantToSpin = false;
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.InitiateFreeGame();

            #region burn-in
            if (machineContext!.platformInterface!.IsBurnInMode())
            {
                playerController?.InvokeActions("Key_Spin_BurnIn");
            }
            #endregion

            #region Fg Init Help page
            // Due to new BMM requirements, this overrides WkStatePlay's isHelpEnable = false
            WkSlotGameDataModel wkSlotGameDataModel = gm.dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            wkSlotGameDataModel.isHelpEnable = true;
            #endregion
        }

        protected override void BindActions()
        {
            base.BindActions();
            InvokeMethod(() => playerController!.AddAction("Key_Demo", new WkEnableFreeGameCheatAction(playerController)), 0.46f);
            InvokeMethod(() => playerController!.AddAction("Key_Spin", new JIXRYTriggerFreeGameSpinAction(playerController)), 0.46f);
            
            #region burn-in
            if (machineContext!.platformInterface!.IsBurnInMode())
            {
                playerController!.AddAction("Key_Spin_BurnIn", new JIXRYTriggerFreeGameSpinAction(playerController));
            }
            #endregion

            #region Fg Init Help page
            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            gm.dataModel.isHelpEnable = true;

            playerController?.AddAction("Key_Help", new JIXRYEnableFgInitStateHelpPageAction(playerController));
            playerController?.AddAction("Key_HelpOnScreen", new JIXRYEnableFgInitStateHelpPageAction(playerController));
            #endregion
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RecoverPreviousIngotData(true);
            gm.RecoverReel();
            gm.UpdateBetDisplay();
            gm.InitiateFreeGame();
            gm.RecoverFgCounter();
            gm.PlayWinAnimation();

            WkSlotGameDataModel wkSlotGameDataModel = gm.dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            if (machineContext!.platformInterface!.IsErrLockup() ||
                machineContext!.platformInterface!.IsTrxnInProg())
            {
                wkSlotGameDataModel.isHelpEnable = false;
            }
            else
            {
                wkSlotGameDataModel.isHelpEnable = true;
            }
        }

        public override void ExitState()
        {
            base.ExitState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.ResetRngForFirstFgSpin();
            gm.CloseFgInitHelpPage();
        }

        public override void PIIdle()
        {
            base.PIIdle();

            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            WkSlotGameDataModel wkSlotGameDataModel = gm.dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            wkSlotGameDataModel.isHelpEnable = true;
        }
    }
}