using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameFirstPopAnimation : WkStateFreeGameFirstPopAnimation
    {
        public JIXRYStateFreeGameFirstPopAnimation(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            GetGameManagerChecked<JIXRYGameManager>().CacheFirstIncrementAmount();
        }
    }
}