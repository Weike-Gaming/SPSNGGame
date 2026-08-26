using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameFeatureAnimationFromJackpot : WkSlotStateCore
    {
        public JIXRYStateFreeGameFeatureAnimationFromJackpot(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }
        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PlayHitNewFeaturePanel();
        }
    }
}