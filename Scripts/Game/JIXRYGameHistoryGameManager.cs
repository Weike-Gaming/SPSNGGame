using System;
using System.Collections.Generic;
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

            // Store local vars for rng
            // Will set subGameData.rng to pre-nudge rng, then immeditately reset, as base class uses .rng
            uint[] preNudgeRng = thisSubGameData.savedPreNudgeRng.ToArray();
            uint[] postNudgeRng = thisSubGameData.rng.ToArray();
            bool hasNudge = HasNudge();  

            if (hasNudge)
            {
                // set subGameData.rng to prenudge/final rng
                subGameData.rng = currentlyInPreNudge ? preNudgeRng.ToArray() : postNudgeRng.ToArray();
            }

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

            // Overwrite ingotValues with pre-nudge ingot values if in pre-nudge state, otherwise use post-nudge ingot values for win check
            if (hasNudge && IsInPreNudge()) 
            {
                int numRows = rd[0].numRows;
                // If in pre-nudge, use pre-nudge ingot values instead of previous ingot values for win check
                for (int reel = 0; reel < rd.Length; reel++)
                {
                    int sourceIndex = reel * numRows;
                    int destIndex = reel * numRows;
                    for (int row = 0; row < rd[0].numRows; row++)
                    {
                        ingotValues[destIndex + row] = histGameData.savedPreNudgeIngotValue[sourceIndex + row];
                    }
                }
            }

            // Do ingot win check
            if (hasNudge && !IsInPreNudge() || !hasNudge)
            {
                (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                    reelManager,
                    ingotValues,
                    histGameData.extraPrizeMultiplier,
                    histGameData.extraPrizeMultiplierIngotValue,
                    true,
                    true,
                    hasNudge,
                    true
                );
            }

            // Update ingot value visual digits via reelIngotValue if in prenudge
            if (hasNudge && IsInPreNudge())
            {
                JIXRYReelManager jixryReelManager = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

                // Convert preNudgeIngotValue[15] to uint[35] to use in rm.UpdateIngotValueData
                int totalRows = rd[0].numRows + rd[0].numDummy;
                int start = rd[0].numDummy / 2;
                int end = start + rd[0].numRows;
                uint[] preNudgeIngotValueExpanded = new uint[rd.Length * totalRows];
                for (int reel = 0; reel < rd.Length; reel++)
                {
                    int uint15Index = reel * rd[0].numRows;
                    int uint35Index = reel * totalRows;
                    for (int row = 0; row < totalRows; row++)
                    {
                        preNudgeIngotValueExpanded[uint35Index] = (start <= row && row < end) ?
                            histGameData.savedPreNudgeIngotValue[uint15Index++] : (uint)1;
                        ++uint35Index;
                    }
                }
                // Sets rd.reelIngotInfo but doesn't change symbol.ingotValueToAdd
                jixryReelManager.UpdateIngotValueData(preNudgeIngotValueExpanded);
            }

            reelManager.HardCodeReelSymbol(subGameData.rng);
            PlayWinAnimation();

            // Force history to 'skip' animation for special ingots
            if (hasNudge && !IsInPreNudge() || !hasNudge)
                rm.RecoverPreviousTransformedIngot();
            else
            // Force ingot to update visual
                rm.ForceUpdateIngotValueVisual(subGameData.rng);

            // Revert subGameData.rng back to post-nudge rng if it was changed for the spin
            subGameData.rng = postNudgeRng.ToArray();

            OnStatisticUpdate.Invoke();
        }

        #region History Navigation

        public int GetCurrentFgIndex()
        {
            return currentFgIndex;
        }

        public void SetCurrentFgIndex(int index)
        {
            currentFgIndex = index;
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
            StartHistoryFgSpin();
            OnStatisticUpdate.Invoke();
        }

        public override void NextSubGame()
        {
            if (currentFgIndex <= 0) return;

            currentFgIndex--;
            subGameData = replayHistorySubRecoverData[currentFgIndex] as JIXRYHistorySubGameData;
            currentlyInPreNudge = false;
            StartHistoryFgSpin();
            OnStatisticUpdate.Invoke();
        }

        public override void BackToMainGame()
        {
            StartHistorySpin();
            OnStatisticUpdate.Invoke();
            currentlyInSubGame = false;
        }

        public bool HasNudge()
        {
            subGameData = replayHistorySubRecoverData[currentFgIndex] as WkSlotHistorySubGameData ?? throw new NullReferenceException();
            JIXRYHistorySubGameData thisSubGameData = subGameData as JIXRYHistorySubGameData ?? throw new InvalidCastException();
            return !thisSubGameData.savedPreNudgeRng.SequenceEqual(thisSubGameData.rng);
        }

        public bool IsInPreNudge()
        {
            return currentlyInPreNudge;
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
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
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
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetGameType()
        {
            JIXRYHistorySubGameData gameData = subGameData as JIXRYHistorySubGameData;
            
            switch (gameData.potFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetGameType(byte flag, bool returnAsPotText)
        {
            switch (flag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "Green Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "Green + Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "Green + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "Red + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
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
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNJP)
                            return "Red";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNRU)
                            return "Purple";
                        else
                            return "Red + Purple";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNJP)
                            return "Green";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPRU)
                            return "Purple";
                        else
                            return "Green + Purple";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    {
                        if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNRU)
                            return "Green";
                        else if (gameData.potFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPRU)
                            return "Red";
                        else
                            return "Green + Red";
                    }
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "Purple";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "Red";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                     return "Green";
                default:
                    return "ERROR";
            }
        }

        public string GetBonus()
        {
            string tempStr = string.Empty;
            List<string> tempList = new List<string>();

            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            int totalReels = reelManager.reelData.Length;
            int[] winPosTemp = new int[totalReels];

            JIXRYHistorySubGameData sd = subGameData as JIXRYHistorySubGameData;

            // loop through visible symbols and identify what was won
            for (int reel = 0; reel < totalReels; reel++) //5
            {
                if (sd.previousMaxWayWin < reel + 1) break;
                
                int numRows = reelManager.reelData[reel].numRows;

                for (int row = 0; row < numRows; row++) //3
                {
                    int index = reelManager.GetReelIconIndex(reel, row) - 1;
                    if (index < 0 || index >= symbolInfoTemplate.symbols.Length) continue;

                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols[index];

                    if (!string.IsNullOrEmpty(symbolDetail.symbolType))
                    {
                        string ingotType = symbolDetail.symbolType;
                        int ingotPrizeIndex = reel * numRows + row;

                        switch (ingotType)
                        {
                            case "INGOT_PRIZEMULTIPLIER":
                                {
                                    winPosTemp[reel] |= (byte)(1 << row);

                                    tempStr = string.Empty;
                                    if (reel == 2)
                                    {
                                        tempStr = $"x{sd.extraPrizeMultiplier} (reel 3)";
                                        tempList.Add(tempStr);
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
            }

            // loop through tempList and stitch together return string
            string returnStr = string.Empty;

            if (tempList.Count == 0)
                return "N/A";
            else
                return string.Join(", ", tempList);
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
    }
}
