using Weike.Core;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYTriggerFreeGameSpinAction : WkActionCore
    {
        public JIXRYTriggerFreeGameSpinAction(IWkController controller, params object[] parameters) : base(controller)
        {
        }

        public override void Execute(params object[] parameters)
        {
            if (myGameManager is JIXRYGameManager gm)
            {
                gm.ContinueFgSpinOrEnd();
            }
        }
    }
}
