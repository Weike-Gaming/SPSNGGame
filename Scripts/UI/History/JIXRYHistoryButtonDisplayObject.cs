using System;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYHistoryButtonDisplayObject : WkHistoryButtonDisplayObject
    {
        protected override void UpdateButtonStatus()
        {
            base.UpdateButtonStatus();
            JIXRYHistoryGameManager gm = owningPlayerController?.owner as JIXRYHistoryGameManager ?? throw new InvalidCastException();

            prevBtn.gameObject.SetActive(!gm.currentlyInSubGame);
            nextBtn.gameObject.SetActive(!gm.currentlyInSubGame);
            subBtn.gameObject.SetActive(!gm.currentlyInSubGame);

            prevSubBtn.gameObject.SetActive(gm.currentlyInSubGame);
            nextSubBtn.gameObject.SetActive(gm.currentlyInSubGame);
            mainBtn.gameObject.SetActive(gm.currentlyInSubGame);

            prevSubBtn.interactable = gm.HasPrevSubGame();
            nextSubBtn.interactable = gm.HasNextSubGame();

            subBtn.interactable = gm.replayHistorySubRecoverData.Count > 0;
        }

        protected void OnClickNudgeButton()
        {
            JIXRYHistoryGameManager gm = owningPlayerController?.owner as JIXRYHistoryGameManager ?? throw new InvalidCastException();
            gm.ToggleNudge();
        }
    }
}
