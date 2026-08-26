using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameWinIncrement : WkStateFreeGameWinIncrement
    {
        public JIXRYStateFreeGameWinIncrement(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }
        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            if (gm.nudgeChecked && rm.haveNudge)
            {
                if (!rm.doneNudge)
                    gm.CmdGotNudge();
                else
                    gm.CmdNoNudge();
            }
            else
                gm.CmdNoNudge();
        }
    }
}
