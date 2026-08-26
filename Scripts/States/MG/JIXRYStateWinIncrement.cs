using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateWinIncrement : WkStateWinIncrement
    {
        public JIXRYStateWinIncrement(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.RunFinishScatterAnimationCommand();
            gm.CheckFeatureGame();
        }

        protected override bool IsTriggerFeatureGame()
        {
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            return gm.CheckCurrentHitNewFeature();
        }
    }
}