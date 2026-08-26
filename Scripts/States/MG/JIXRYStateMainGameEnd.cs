using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateMainGameEnd : WkStateMainGameEnd
    {
        public JIXRYStateMainGameEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.SavePotCoinValue();
            gm.SaveFinalIngotAmount();
            gm.ResetDataForMainGame();
        }
    }
}