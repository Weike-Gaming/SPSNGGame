using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYReelNudgeDisplayObject : WkButtonDisplayObject
    {

        protected override void OnButtonPress()
        {
            base.OnButtonPress();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager;
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;

            // spin up, down, down, down, up
            bool[] useDefaultSpinDir = { false, true, false, false, true};

            // nudge by 1,1,0,2,2 steps
            // only non-zero will nudge
            uint[] nudgeSteps = {1,1,0,2,2 };

            rm.SpinReelsWithIndividualControl(useDefaultSpinDir, nudgeSteps);
        }

        ///<inheritdoc/>
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
        }
    }
}
