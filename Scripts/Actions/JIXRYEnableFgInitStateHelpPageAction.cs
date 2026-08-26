using System;
using Weike.Core;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYEnableFgInitStateHelpPageAction : WkActionCore
    {
        public JIXRYEnableFgInitStateHelpPageAction(IWkController controller) : base(controller)
        {
        }

        public override void Execute(params object[] parameters)
        {
            JIXRYGameManager gm = myGameManager as JIXRYGameManager ?? throw new InvalidCastException();
            gm.ToggleFgInitStateHelpPage();
            WkAudioManager.instance!.PlayAudio("BtnInfo");
        }
    }
}
