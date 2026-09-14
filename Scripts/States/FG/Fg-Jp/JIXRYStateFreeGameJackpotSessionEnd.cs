using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameJackpotSessionEnd : WkStateJackpotSessionEnd
    {
        public JIXRYStateFreeGameJackpotSessionEnd(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RunCheckWinCommands();
            gm.RunJackpotSessionEndCommand();
            gm.CheckFgExtraPrize();
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.CheckJackpotPrizeReady();           
        }

        public override void RecoverState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.ChangeReelToLuckyBoost(4);
            base.RecoverState();
            gm.RecoverFgCounter();
            gm.RecoverPreviousPotFeatureState();
            //gm.RecoverFgIngotDigit();
            gm.RecoverTempIngotDigit();
            gm.ForceResetAnimationBitMask();
            gm.UpdateBetDisplay();
            gm.RecoverFgIngotData();
            gm.RecoverReelJackpot();
            gm.FgCheckWin(false);
            gm.RecoverWinAmount();
            gm.DoneNudge();
            gm.FgCheckWinIngot();
            gm.RecoverJackpotPrize();
            gm.RecoverJackpotAmount();
            gm.CheckFgExtraPrize();

            gm.RunCheckWinCommands();
            gm.RunJackpotSessionEndCommand();
        }
    }
}
