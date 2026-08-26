using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateIdle : WkStateIdle
    {
        public JIXRYStateIdle(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void ExitToLobby()
        {
            base.ExitToLobby();
            GetGameManagerChecked<JIXRYGameManager>().PassPotScatterNumToLobbyModel();
        }

        public override void RecoverState()
        {
            base.RecoverState();
            GetGameManagerChecked<JIXRYGameManager>().PlayWinAnimation();
        }

        public override void PITxn()
        {
            base.PITxn();
            GetGameManagerChecked<JIXRYGameManager>().PlayWinAnimation();
        }
    }
}