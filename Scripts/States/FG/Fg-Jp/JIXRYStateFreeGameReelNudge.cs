using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameReelNudge : WkSlotStateCore
    {
        public JIXRYStateFreeGameReelNudge(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.SpinReelsWithIndividualControl(rm.useDefaultSpinDir, rm.nudgeSteps);
            gm.GenerateReelDataIndex();
            base.EnterState();
            JIXRYReelManagerDataModel rmdm = rm.reelManagerDataModel as JIXRYReelManagerDataModel;

            uint maxNudgeStep = 0;
            foreach (uint i in rm.nudgeSteps)
            {
                if (maxNudgeStep < i)
                {
                    maxNudgeStep = i;
                }
            }

            float totalNudgeDuration = (maxNudgeStep > 0) ? rmdm.durationReelNudgeAnim + rmdm.durationReelNudgeLinger : 0;
            //InvokeMethod(gm.CmdFinishReelNudge, totalNudgeDuration);
        }

        public override void ExitState()
        {
            base.ExitState();

            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.DisableReelNudgeBorder();
        }

        public override void PIError()
        {
            base.PIError();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.DisableReelNudgeBorder();
            //CancelInvokeMethod(gm.CmdFinishReelNudge);
        }
    }
}
