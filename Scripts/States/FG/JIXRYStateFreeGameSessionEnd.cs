using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameSessionEnd : WkStateFreeGameSessionEnd
    {
        public JIXRYStateFreeGameSessionEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.ChangeReelToLuckyBoost(3);
            gm.ResetToMgJackpotData();
            base.EnterState();
            gm.MgCheckWin(false);
            gm.TryPlayWinAnimation();
            gm.RunFgSessionEndCommand();
        }
    }
}
