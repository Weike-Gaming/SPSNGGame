using System.Linq;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYBigWinParticleEffectDisplayObject : WkBigWinParticleEffectDisplayObject
    {
        public JIXRYBigWinParticleEffectDisplayObject() : base()
        {
            playStateCollection = playStateCollection
                .Concat(new string[] { "win-increment-non-skip-from-jp", "fg-win-increment-non-skip", "fg-win-increment-non-skip-from-jackpot" })
                .ToArray();
        }
    }
}
