using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameTrasition : WkSlotStateCore
    {
        private const float Delay = 0.5f;
        public JIXRYStateFreeGameTrasition(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.UpdateWin();
            InvokeMethod(gm.PlayFgTrasition, Delay);
            gm.SetCurrentFeature();
        }

        public override void PIError()
        {
            base.PIError();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            CancelInvokeMethod(gm.PlayFgTrasition);
        }

        public override void RecoverState()
        {
            base.RecoverState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RecoverPreviousIngotData(true);
            gm.RecoverReel();
            gm.UpdateBetDisplay();
            InvokeMethod(gm.PlayFgTrasition, Delay);
        }
    }
}