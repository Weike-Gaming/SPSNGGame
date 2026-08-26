using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.HORSE
{
    public class WKUtils : MonoBehaviour
    {
        public void ToggleDebugCanvas()
        {
            WkSlotMainGameManager gm = WkCommon.GetActiveGameManager() as WkSlotMainGameManager;
            if (gm is null) return;
            gm.ToggleDebugShowCanvas();
        }
    }
}
