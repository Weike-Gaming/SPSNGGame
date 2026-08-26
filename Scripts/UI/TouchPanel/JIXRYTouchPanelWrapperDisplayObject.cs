using System;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYTouchPanelWrapperDisplayObject : WkTouchPanelWrapperDisplayObject
    {
        /// <inheritdoc/>
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            Func<string, WkStateCore> getState = gm.gameState!.GetState<WkStateCore>;

            string[] states = new string[] { "fg-win-increment", "fg-end-panel", "fg-win-increment-from-jackpot", "win-increment-from-jp", "take-win-from-jp" };
            foreach (string state in states)
            {
                getState(state).onEnterState += () =>
                {
                    takeWinBtn.SetActive(true);
                };
                getState(state)!.onExitState += () =>
                {
                    takeWinBtn.SetActive(false);
                };
            }
            getState("fg-end-panel").onRecoverState += () =>
            {
                takeWinBtn.SetActive(true);
            };
        }
    }
}
