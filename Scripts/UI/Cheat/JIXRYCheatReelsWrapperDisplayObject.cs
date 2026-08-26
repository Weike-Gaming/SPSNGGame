using System;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatReelsWrapperDisplayObject : WkCheatReelsWrapperDisplayObject
    {
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("fg-cheat")!.onEnterState += SetupFgCheat;
            getState("fg-cheat")!.onExitState += () =>
            {
                reelsWrapper?.SetActive(false);
            };
        }

        private void SetupFgCheat()
        {
            CloneReelManagerModelData();
            SetupPredetermineRng();
            reelsWrapper?.SetActive(true);
        }
    }
}