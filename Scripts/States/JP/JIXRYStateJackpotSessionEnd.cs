using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateJackpotSessionEnd : WkStateJackpotSessionEnd
    {
        public JIXRYStateJackpotSessionEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RunCheckWinCommands();
            gm.RunJackpotSessionEndCommand();
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.CheckJackpotPrizeReady();
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            
            gm.UpdateBetDisplay();
            gm.RecoverPreviousIngotData(false);
            gm.RecoverReelJackpot();
            gm.MgCheckWin();
            gm.RecoverJackpotPrize();
            gm.RunCheckWinCommands();
            gm.RecoverFgCounter();
            InvokeMethod(gm.RunJackpotSessionEndCommand, 0.2f);
        }
    }
}
