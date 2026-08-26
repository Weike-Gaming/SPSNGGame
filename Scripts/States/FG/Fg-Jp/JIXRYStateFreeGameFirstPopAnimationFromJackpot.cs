using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameFirstPopAnimationFromJackpot : WkStateFreeGameFirstPopAnimation
    {
        public JIXRYStateFreeGameFirstPopAnimationFromJackpot(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            WkFreeGameDataModel fgdm = gm.freeGameDataModel;
            JIXRYWinManager wm = gameManager.winManager as JIXRYWinManager ?? throw new InvalidCastException();
            WkWinManagerModel winManagerDatamodel = wm.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();

            long jackpotPrize = fgdm.fgJackpotLevel1Prize + fgdm.fgJackpotLevel2Prize + fgdm.fgJackpotLevel3Prize + fgdm.fgJackpotLevel4Prize;

            gm.CheckSwitchStatesInSpin(winManagerDatamodel.tempWinAmount + jackpotPrize);
            gm.PlayFirstLoopAnimation();
            gm.FinishFirstPop();
        }
    }
}