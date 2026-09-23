using System;
using System.Linq;
using Weike.Common;
using Weike.LobbyManagement;
using Weike.MachineInterface;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{

    public class JIXRYHistoryGameManager : WkSlotHistoryGameManager
    {
        /// <summary>
        /// Bool that is true if entered FreeGame, false if entered MainGame
        /// </summary>
        public bool currentlyInSubGame = false;
        public bool currentlyInPreNudge = false;

        protected override Type winManagerType => typeof(JIXRYWinManager);

        private uint _currentBetMultiplier;
        private uint _currentPlayOption;
        private bool _currenrlyIsMultiplier;

        public JIXRYHistoryGameManager() : base()
        {
            historyMainRecoverData = new JIXRYHistoryMainGameData();
            historySubRecoverData = new JIXRYHistorySubGameData();
            winStatementDataModel = new WkWinLineStatementDataModel();
        }

        /// <inheritdoc />
        protected override void InitializedGameStates()
        {
            if (!WkAssert.EnsureMsgf(gameCode, "Game Code is null"))
                return;

            WkLobbySceneManager.instance!.showServiceInProgressTopScreen = true;
        }

        /// <inheritdoc />
        public override void OnSelected()
        {
            currentlyInSubGame = false;
            currentlyInPreNudge = false;
            base.OnSelected();           
            currentFgIndex = 0;
            OnStatisticUpdate?.Invoke();
            JIXRYReelManager rm = reelManager as JIXRYReelManager;
            if (rm != null)
            {
                rm.ChangeNumRows(3);
            }
            StartHistorySpin();
           
        }
        /// <inheritdoc />
        protected override void GameSpecificStartHistorySpin()
        {
            if (historyMainRecoverData is not WkSlotHistoryMainGameData historyData ||
                winManager is null ||
                winManager is not JIXRYWinManager slotWinManager)
            {
                return;
            }
            slotWinManager.InitWinManager(gameCode, GameInfoWrapper.EnumType.TYPE_WAYS);
            RecoverIngotData();

            _currentPlayOption = GetSelectedPlayOptions()[historyData.selectedPlayOption];
            _currentBetMultiplier = (uint)(historyData.betAmount / _currentPlayOption);
            WkWinCheckingInfo info = new WkWinCheckingInfo()
            {
                betMultiplier = _currentBetMultiplier,
                playOption = _currentPlayOption,
                reelManagerRef = reelManager
            };
            slotWinManager.CheckReelWayWin(info);

            JIXRYHistoryMainGameData histGameData = historyMainRecoverData as JIXRYHistoryMainGameData;

            JIXRYWinManager JIXRYWinManager = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                reelManager,
                histGameData.ingotValue,
                new uint[20],
                0,
                0,
                false,
                true,
                false,
                true
            );

            // Update Scatter Win Statement
            if (HasSubGame())
            {
                long betValue = (long)histGameData.betAmount;
                string potFeature = GetTriggeringGameType();
                JIXRYWinManager.UpdateScatterStatement(reelManager, betValue, potFeature);
            }

            reelManager.HardCodeReelSymbol(histGameData.rng);
            PlayWinAnimation();

            OnStatisticUpdate?.Invoke();
        }

        /// <inheritdoc />
        public override uint[] GetSelectedPlayOptions()
        {
            WkSlotGameDataModel slotDataModel = dataModel as WkSlotGameDataModel;
            return slotDataModel.getSelectedPlayOptions;
        }

        /// <inheritdoc />
        public override void StartHistoryFgSpin()
        {
            RecoverFgJpIngotData();

            // Check if showing Nudge or not
            subGameData = replayHistorySubRecoverData[currentFgIndex] as WkSlotHistorySubGameData ?? throw new NullReferenceException();
            JIXRYHistorySubGameData thisSubGameData = subGameData as JIXRYHistorySubGameData ?? throw new InvalidCastException();

            JIXRYHistorySubGameData prevSubGamesData = new JIXRYHistorySubGameData();
            


            // Store local vars for rng
            // Will set subGameData.rng to pre-nudge rng, then immeditately reset, as base class uses .rng
            uint[] postNudgeRng = thisSubGameData.rng.ToArray(); 

            base.StartHistoryFgSpin();

            playerController?.hud?.OnSelected();

            if (winManager is null ||
                winManager is not JIXRYWinManager slotWinManager)
            {
                return;
            }
            winManager!.ResetWinAmount();

            WkWinCheckingInfo info = new WkWinCheckingInfo()
            {
                betMultiplier = _currentBetMultiplier,
                playOption = _currentPlayOption,
                reelManagerRef = reelManager
            };
            slotWinManager.CheckReelWayWin(info);

            winStatementDataModel.multiplier = freeGameDataModel.fgMultiplier;
            
            JIXRYHistorySubGameData histGameData = subGameData as JIXRYHistorySubGameData;
            JIXRYWinManager JIXRYWinManager = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            // Isolate only visible ingots from dm.ingotValue to pass into CheckIngotTrigger
            int totalNumRows = rd[0].numRows + rd[0].numDummy;
            int startRow = rd[0].numDummy / 2;
            uint[] ingotValues = new uint[rd.Length * rd[0].numRows];
            for (int reel = 0; reel < rd.Length; reel++)
            {
                int sourceIndex = reel * totalNumRows + startRow;
                int destIndex = reel * rd[0].numRows;
                for (int row = 0; row < rd[0].numRows; row++)
                {
                    ingotValues[destIndex + row] = histGameData.fgPreviousIngotValue[sourceIndex + row];
                }
            }

            // Do ingot win check
            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                    reelManager,
                    ingotValues,
                    histGameData.fgIsMultiply,
                    histGameData.extraPrizeMultiplier,
                    histGameData.extraPrizeMultiplierIngotValue,
                    true,
                    true,
                    false,
                    true
                );
            _currenrlyIsMultiplier = false;
            if (maxIngotWayWin > 1)
            {
                _currenrlyIsMultiplier = true;
            }
            reelManager.HardCodeReelSymbol(subGameData.rng);
            PlayWinAnimation();
            // Force history to 'skip' animation for special ingots
            rm.RecoverPreviousTransformedIngot();
            subGameData.rng = postNudgeRng.ToArray();

            OnStatisticUpdate.Invoke();
        }

        #region History Navigation

        public int GetCurrentFgIndex()
        {
            return currentFgIndex;
        }
     
        public bool HasPrevSubGame()
        {
            return currentFgIndex < replayHistorySubRecoverData.Count - 1;
        }

        public bool HasNextSubGame()
        {
            return currentFgIndex > 0;
        }

        public bool IsInSubGame()
        {
            return freeGameDataModel.initialFreeGameAmount > 0;
        }

        public bool HasSubGame()
        {
            return replayHistorySubRecoverData.Count <= 0 ? false : true;
        }

        public override void EnterSubGame()
        {
            if (replayHistorySubRecoverData.Count <= 0) return;
            subGameData = replayHistorySubRecoverData[replayHistorySubRecoverData.Count - 1] as JIXRYHistorySubGameData;
            bool isFreeGame = subGameData.initialFreeGameAmount > 0;
            currentFgIndex = 0;
            subGameData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData;
            winStatementDataModel.showStatement = false;
            ChangeReelToLuckyBoost(4);
            StartHistoryFgSpin();
            OnStatisticUpdate.Invoke();
            currentlyInSubGame = true;
        }

        public override void PreviousSubGame()
        {
            if (currentFgIndex >= replayHistorySubRecoverData.Count - 1) return;
            currentFgIndex++;
            subGameData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData;
            currentlyInPreNudge = false;
            JIXRYHistorySubGameData subGamesData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData; 
            if (subGamesData.potFeatureGameFlag != subGamesData.savedPreviousPotFeatureGameFlag &&
                       subGamesData.savedPreviousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME &&
                      GetGameType(subGamesData.potFeatureGameFlag,true).ToString().Contains("Purple"))
            {
                ChangeReelToLuckyBoost(3);
            }
            StartHistoryFgSpin();
            OnStatisticUpdate.Invoke();
        }

        public override void NextSubGame()
        {
            if (currentFgIndex <= 0) return;

            currentFgIndex--;
            subGameData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData;
            currentlyInPreNudge = false;
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYHistorySubGameData subGamesData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData;
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            if (GetGameType(subGamesData.potFeatureGameFlag, true).ToString().Contains("Purple") &&
                       GetGameType(subGamesData.savedPreviousPotFeatureGameFlag, true).ToString().Contains("Purple")&& rd[0].numRows == 3
                      )
            {
                ChangeReelToLuckyBoost(4);
            }
            StartHistoryFgSpin();
            OnStatisticUpdate.Invoke();
        }
        public override void BackToMainGame()
        {
            ChangeReelToLuckyBoost(3);
            StartHistorySpin();
            OnStatisticUpdate.Invoke();
            currentlyInSubGame = false;
        }

        public override void ReturnToHistoryLobby()
        {
           
            base.ReturnToHistoryLobby();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
                rm.ChangeNumRows(3);

        }

        public void ToggleNudge()
        {
            currentlyInPreNudge = !currentlyInPreNudge;
            StartHistoryFgSpin();
        }

        #endregion

        #region Helper

        public string GetTriggeringGameType()
        {
            JIXRYHistoryMainGameData dm = historyMainRecoverData as JIXRYHistoryMainGameData ?? throw new InvalidCastException();

            switch (dm.savedTriggerPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "FREE_GAME_LW";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "FREE_GAME_LB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "FREE_GAME_LWJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "FREE_GAME_LWLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "FREE_GAME_JPLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "FREE_GAME_LWJPLB";
                default:
                    return "MAIN_GAME";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>SubGameData.savedPreviousPotFeatureGameFlag</returns>
        public string GetPrevGameType()
        {
            JIXRYHistorySubGameData gameData = subGameData as JIXRYHistorySubGameData;
            switch (gameData.savedPreviousPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "FREE_GAME_LW";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "FREE_GAME_LB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "FREE_GAME_LWJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "FREE_GAME_LWLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "FREE_GAME_JPLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "FREE_GAME_LWJPLB";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetGameType()
        {
            JIXRYHistorySubGameData gameData = subGameData as JIXRYHistorySubGameData;
            
            switch (gameData.potFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "FREE_GAME_LW";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "FREE_GAME_LB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "FREE_GAME_LWJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "FREE_GAME_LWLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "FREE_GAME_JPLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "FREE_GAME_LWJPLB";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetGameType(byte flag, bool returnAsPotText)
        {
            switch (flag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "Green Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "Green + Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "Green + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "Red + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "Green + Red + Purple Pot";
                default:
                    return "MAIN_GAME";
            }
        }

        /// <summary>
        /// Determines the next game type based on the current and previous game state flags.
        /// </summary>
        /// <remarks>This method evaluates the saved previous and current game state flags to determine
        /// the next game type. The returned value corresponds to a color or a combination of colors representing the
        /// game type. If the game state flags do not match any known combination, the method returns "ERROR".</remarks>
        /// <returns>A string representing the next game type. Possible values include: "Red", "Purple", "Green", "Red + Purple",
        /// "Green + Purple", "Green + Red", or "ERROR" if the state is invalid.</returns>
        public string GetNextGameType()
        {
            JIXRYHistorySubGameData gameData = subGameData as JIXRYHistorySubGameData;
            switch (gameData.savedPreviousPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWJP)
                            return "Red";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWLB)
                            return "Purple";
                        else
                            return "Red + Purple";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWJP)
                            return "Green";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPLB)
                            return "Purple";
                        else
                            return "Green + Purple";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWLB)
                            return "Green";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPLB)
                            return "Red";
                        else
                            return "Green + Red";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "Purple";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "Red";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                     return "Green";
                default:
                    return "ERROR";
            }
        }

        public string GetBonus(long winAmount)
        {
            JIXRYHistorySubGameData sd = subGameData as JIXRYHistorySubGameData;
            // loop through tempList and stitch together return string
            string returnStr = string.Empty;
            if (winAmount == 0 && GetGameType().ToString().Contains("LW"))
                return "Lucky Win +1 spin";
            if(_currenrlyIsMultiplier && GetGameType().ToString().Contains("LB"))
                return "Lucky Boost x" + sd.extraPrizeMultiplier;
            else
                return "N/A";
        }
        #endregion

        #region RECOVER INGOT STUFFS

        public void RecoverFgJpIngotData()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYHistorySubGameData jixrySubGameData = subGameData as JIXRYHistorySubGameData;
            rm.UpdateIngotValueData(jixrySubGameData.fgPreviousIngotValue);
            rm.UpdateExtraPrizeMultiplierType(2, (byte)jixrySubGameData.extraPrizeMultiplier);
            rm.UpdateExtraJackpotType(jixrySubGameData.extraJackpotType);

            rm.UpdateRecoverIngotData();
        }

        public void RecoverIngotData()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYHistoryMainGameData mainGameData = historyMainRecoverData as JIXRYHistoryMainGameData;
            rm.UpdateIngotValueData(mainGameData.ingotValue);
            rm.UpdateRecoverIngotData();
        }

        /// <summary>
        /// Retrieves the maximum way win value from the previous ingot sub-game.
        /// Used by ReelManager.GetPreviousMaxIngotWayWin()
        /// </summary>
        /// <returns>The maximum way win value from the previous ingot sub-game.</returns>
        /// <exception cref="InvalidCastException">Thrown if the <see cref="reelManager"/> is not of type <see cref="JIXRYReelManager"/>.</exception>
        public byte GetPreviousMaxIngotWayWin()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYHistorySubGameData jixrySubGameData = subGameData as JIXRYHistorySubGameData;
            return jixrySubGameData.previousMaxWayWin;
        }

        #endregion

        public void ChangeReelToLuckyBoost(int index)
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            if (GetGameType().ToString().Contains("LB"))
                rm.ChangeNumRows(index);
        }
    }
}
