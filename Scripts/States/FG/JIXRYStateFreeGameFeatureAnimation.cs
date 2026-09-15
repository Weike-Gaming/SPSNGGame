using System;
using UnityEditor;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameFeatureAnimation : WkSlotStateCore
    {
        public JIXRYStateFreeGameFeatureAnimation(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PlayHitNewFeaturePanel();
        }

        public override void ExitState()
        {
            base.ExitState();
            JIXRYGameManager gm = gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            if (!feature.ToString().Contains("LB"))
                gm.ChangeReelToLuckyBoostinFeature(4);
        }
    }
}