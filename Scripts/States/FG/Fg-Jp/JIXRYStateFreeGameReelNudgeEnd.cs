using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameReelNudgeEnd : WkSlotStateCore
    {
        public JIXRYStateFreeGameReelNudgeEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            base.EnterState();
            gm.winManager.modelData.GetModelDataChecked<JIXRYWinManagerModel>().totalFgWinAmount = (long)gm.winManager.GetWinAmount();
            gm.DoneNudge();
            gm.FgCheckWinIngot();
            gm.CheckFgExtraJackpot();
            gm.CheckFgExtraPrize();
        }
    }
}
