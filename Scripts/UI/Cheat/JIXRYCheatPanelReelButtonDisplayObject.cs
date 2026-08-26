using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatPanelReelButtonDisplayObject : WkCheatPanelReelButtonDisplayObject
    {
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) { return; }

            gm.gameState.GetState<WkSlotStateCore>("fg-cheat").onEnterState += () =>
            {
                CloneReelManagerModelData();
                SetupFgReel();
            };
        }

        private void SetupFgReel()
        {
            CloneReelManagerModelData();
            index = 0;
            UpdateReel();
        }
    }
}
