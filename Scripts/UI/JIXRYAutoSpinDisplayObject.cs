using System;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYAutoSpinDisplayObject : WkAutoSpinDisplayObject
    {
        ///<inheritdoc/>
        protected override void OnClickDown()
        {
            base.OnClickDown();
            getActiveAudioManager.PlayAudio("BtnInfo");
        }

        ///<inheritdoc/>
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager;
            Func<string, WkSlotStateCore> getState = gm.gameState!.GetState<WkSlotStateCore>;

            getState("fg-init")!.onEnterState += () => ShowButton(false);

            string[] recoverStates = new string[] { "fg-init", "fg-spin", "fg-end", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot", "fg-end-panel" };
            foreach (string state in recoverStates)
            {
                getState(state)!.onRecoverState += () =>
                {
                    ShowButton(false);
                };
            }
        }
    }
}
