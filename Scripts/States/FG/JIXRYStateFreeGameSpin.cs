using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameSpin : WkStateFreeGameSpin
    {
        private bool _hasPreSpin = false;
        private const float CountDown = 7.2f;
        private const float DelayToStopEachReel = 0.3f;

        public JIXRYStateFreeGameSpin(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            
            gm.OnFGSelectedSetFGReelStrip();
            gm.ResetFgJackpotPrize();
            gm.ResetCacheAmount();
            gm.SetPlayAnimFlag();
            gm.ResetPreviousPotFeatureFlag();

            base.EnterState();
            EnterPreSpinAnim();
            gm.CheckCurrentHitNewFeature();
            gm.CheckFgExtraJackpot();
            gm.CheckFgExtraPrize();
            gm.PlayHitNewFeatureAnimation();
        }

        protected override void BindActions()
        {
            base.BindActions();
            
            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            if (gm.configDataRecover is WkSlotConfigurationData { hasFastStopInFeature: true } slotConfig)
            {
                if (gm.dataModel.GetModelDataChecked<JIXRYGameDataModel>().shouldPlayHitFeatureAnim)
                {
                    // remove
                    playerController!.RemoveAllActionsByKey("Key_Spin"); 
                    if (gameManager is WkSlotMainGameManager
                        {
                            configDataRecover: WkSlotConfigurationData
                            {
                                latchState: >= WkLatchState.WithFastStop
                            }
                        })
                    {
                        playerController!.RemoveAllActionsByKey("Key_Spin_Latch");
                    }
                }
            }
        }

        private void EnterPreSpinAnim()
        {
            if (gameManager is JIXRYGameManager gm)
            {
                _hasPreSpin = gm.CheckFeatureGameTrigger(true) || gm.CheckForExtremeBigWinPreSpin(true) || gm.CheckFreeGameJackpotTrigger();
                if (_hasPreSpin)
                {
                    playerController?.RemoveAllActions();
                    InvokeMethod(OnCountDownFinish, CountDown);
                }
            }
        }

        private void OnCountDownFinish()
        {
            if (gameManager is JIXRYGameManager { reelManager: JIXRYReelManager rm })
            {
                rm.FinishPreSpinAnimation(DelayToStopEachReel);
            }
        }

        public override void RecoverState()
        {
            JIXRYGameManager gm = GetGameManagerChecked<JIXRYGameManager>();
            gm.ChangeReelToLuckyBoost(4);
            gm.RecoverFgCounter();
            gm.RecoverPreviousPotFeatureState();
            gm.RecoverTempIngotDigit();
            gm.ForceResetAnimationBitMask();

            base.RecoverState();
        }
    }
}