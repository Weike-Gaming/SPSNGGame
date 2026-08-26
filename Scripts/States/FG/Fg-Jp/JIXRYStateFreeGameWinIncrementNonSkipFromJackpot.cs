using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYStateFreeGameWinIncrementNonSkipFromJackpot : WkStateFreeGameWinIncrementNonSkip
    {
        public JIXRYStateFreeGameWinIncrementNonSkipFromJackpot(string id, string fallBackStateId, IWkObject owner) : base(id, fallBackStateId, owner)
        {
        }
    }
}
