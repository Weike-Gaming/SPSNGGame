using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateSpin : WkStateSpin
    {
        private const float CountDown = 7.2f;
        private const float DelayToStopEachReel = 0.3f;
        private bool _hasPreSpin = false;

        public JIXRYStateSpin(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.CheckHaveJackpot();
            EnterPreSpinAnim();
        }

        public override void PIError()
        {
            base.PIError();
            CancelInvokeMethod(OnCountDownFinish);
        }

        private void EnterPreSpinAnim()
        {
            if (gameManager is JIXRYGameManager gm)
            {
                _hasPreSpin = gm.CheckFeatureGameTrigger() || gm.CheckForExtremeBigWinPreSpin() || gm.CheckJackpotTrigger();
                if (_hasPreSpin)
                {
                    playerController?.RemoveAllActions();
                    InvokeMethod(OnCountDownFinish, CountDown);
                }
                else
                {
                    gm.CheckFeatureGame();
                }
            }
        }

        private void OnCountDownFinish()
        {
            if (gameManager is JIXRYGameManager { reelManager: JIXRYReelManager rm })
            {
                rm.FinishPreSpinAnimation(DelayToStopEachReel);
                GetGameManagerChecked<JIXRYGameManager>().CheckFeatureGame();
            }
        }

        public override void RecoverState()
        {          
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.CheckHaveJackpot();
            gm.CheckFeatureGame();
        }
    }
}
