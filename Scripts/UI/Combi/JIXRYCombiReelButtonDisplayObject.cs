using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiReelButtonDisplayObject : WkCombiReelButtonDisplayObject
    {
        protected override void UpdateReel()
        {
            JIXRYCombiGameManager? gameManager = owningPlayerController?.owner as JIXRYCombiGameManager;
            gameManager.ResetBottomRowButtons();
            base.UpdateReel();
        }
    }
}
