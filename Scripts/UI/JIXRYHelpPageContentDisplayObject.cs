using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYHelpPageContentDisplayObject : WkHelpPageContentDisplayObject
    {
        protected override bool ShouldInclude(string key)
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;

            if (gm is null)
            {
                return base.ShouldInclude(key);
            }

            switch (key)
            {
                case "ingot_feature":
                case "purple_pot":
                case "red_pot":
                case "green_pot":
                case "red_purple_pot":
                case "green_purple_pot":
                case "red_green_pot":
                case "green_red_purple_pot":
                case "free_spin_feature":
                case "free_spin_bonus_1":
                case "free_spin_bonus_2":
                case "random_jackpot":          
                case "general_rules":
                case "progressive_jackpot_bonus_prize_pool":
                    return true;
            }

            return base.ShouldInclude(key);
        }
    }
}
