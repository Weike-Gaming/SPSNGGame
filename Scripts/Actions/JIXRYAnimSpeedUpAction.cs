using Weike.Core;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYAnimSpeedUpAction : WkActionCore
    {
        public JIXRYAnimSpeedUpAction(IWkController controller) : base(controller)
        {
        }

        public override void Execute(params object[] parameters)
        {
            if (myGameManager is JIXRYGameManager gm)
            {
                gm.SetAnimSpeedUp();
            }
        }
    }
}
