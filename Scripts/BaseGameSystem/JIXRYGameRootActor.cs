using System.Collections.Generic;
using Weike.LobbyManagement;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYGameRootActor : WkSlotRootActor
    {
        protected override Dictionary<SelectedGameMode, string> gameManagerTypes => new()
        {
            { SelectedGameMode.NormalGame, nameof(JIXRYGameManager)},
            { SelectedGameMode.History, nameof(JIXRYHistoryGameManager)},
            { SelectedGameMode.Combi, nameof(JIXRYCombiGameManager)},
            { SelectedGameMode.SimulationGame, nameof(JIXRYSimulationGameManager)},
        };
    }
}
