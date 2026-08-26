using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGamePrizeBoostMultiplierFromJackpot : WkSlotStateCore
    {
        public JIXRYStateFreeGamePrizeBoostMultiplierFromJackpot(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();

            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PlayFgIngotMultiplierAnimation();
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
            playerController!.AddAction("Key_Spin", new JIXRYAnimSpeedUpAction(playerController));
            #region burn-in
            if (machineContext!.platformInterface!.IsBurnInMode())
            {
                playerController!.AddAction("Key_Spin_BurnIn", new JIXRYAnimSpeedUpAction(playerController));
            }
            #endregion
        }
    }
}
