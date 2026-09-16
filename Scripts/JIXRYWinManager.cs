using System;
using System.Collections.Generic;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYWinManager : WkSlotWinManager
    {
        public JIXRYWinManager(IWkObject owner) : base(owner)
        {
            modelData = new JIXRYWinManagerModel();
        }

        /// <summary>
        /// Check ingot trigger, calc win and save ingot win amounts. Pass isHistory to CalculateIngotPrize()
        /// </summary>
        /// <param name="reelManager"></param>
        /// <param name="ingotValue"></param>
        /// <param name="extraPrizeMultiplier"></param>
        /// <param name="extraPrizeMultiplierIngotValue"></param>
        /// <param name="isFreeGame"></param>
        /// <param name="modifyWinAmount"></param>
        /// <param name="resetWinStatement"></param>
        /// <param name="isHistory"></param>
        /// <returns></returns>
        public (byte maxMayWin, JIXRYExtraIngotPosition extraIngotPosition) CheckIngotTrigger(
            WkReelManager reelManager, ReadOnlySpan<uint> ingotValue,
            uint extraPrizeMultiplier, uint extraPrizeMultiplierIngotValue, bool isFreeGame, bool modifyWinAmount,
            bool resetWinStatement = false, bool isHistory = false)
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int totalReels = reelManager.reelData.Length;
            int[] winPosTemp = new int[totalReels];

            JIXRYExtraIngotPosition extraFlags = JIXRYExtraIngotPosition.None;

            uint normalIngotCount = 0;
            uint normalIngotValue = 0;
            byte consecutiveReelsWithIngot = 0;
            uint totalIngotPrize = 0;

            for (int reel = 0; reel < totalReels; reel++) //5
            {
                int numRows = reelManager.reelData[reel].numRows;
                bool hasIngotInThisReel = false;

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
                            case "INGOT":
                                {
                                    hasIngotInThisReel = true;
                                    winPosTemp[reel] |= (byte)(1 << row);

                                    normalIngotCount++;
                                    normalIngotValue += ingotValue[ingotPrizeIndex];
                                    break;
                                }
                            case "INGOT_PRIZEMULTIPLIER":
                                {
                                    hasIngotInThisReel = true;
                                    winPosTemp[reel] |= (byte)(1 << row);

                                    if (reel == 2)
                                    {
                                        extraFlags |= JIXRYExtraIngotPosition.MultiplierIn2;
                                    }
                                    break;
                                }
                            case "INGOT_JACKPOT":
                                {
                                    winPosTemp[reel] |= (byte)(1 << row);
                                    hasIngotInThisReel = true;
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }

                if (hasIngotInThisReel)
                {
                    consecutiveReelsWithIngot++;
                }
                else
                {
                    break;
                }
            }


            JIXRYWinManagerModel dm = modelData as JIXRYWinManagerModel;

            if(resetWinStatement)
            {
                if(winCheckingData.winStatements == null)
                {
                    winCheckingData.winStatements = new List<WinStatement>();
                }
                else
                {
                    winCheckingData.winStatements?.Clear();
                }                
            }

            // Get and save ingot win amount
            if (consecutiveReelsWithIngot > 1)
            {
                totalIngotPrize = CalculateIngotPrize(normalIngotCount, normalIngotValue, extraFlags,
                                    extraPrizeMultiplier, extraPrizeMultiplierIngotValue,
                                    isHistory);
                if (isFreeGame)
                {
                    dm.totalFgIngotWinAmount = totalIngotPrize;
                }
                else
                {
                    dm.totalMgIngotWinAmount = normalIngotValue;
                }
                UpdateWinStatement(totalIngotPrize, winPosTemp);
            }

            UpdateIngotWinAmount(totalIngotPrize, isFreeGame, modifyWinAmount);

            return (consecutiveReelsWithIngot, extraFlags);
        }

        /// <summary>
        /// Calculate ingot win amount. Special handling if isHistory.
        /// </summary>
        /// <param name="normalIngotCount"></param>
        /// <param name="normalIngotValue"></param>
        /// <param name="extraFlags"></param>
        /// <param name="extraPrizeMultiplier"></param>
        /// <param name="extraPrizeMultiplierIngotValue"></param>
        /// <param name="isHistory"></param>
        /// <returns></returns>
        private uint CalculateIngotPrize(
            uint normalIngotCount, uint normalIngotValue, JIXRYExtraIngotPosition extraFlags,
            uint extraPrizeMultiplier, uint extraPrizeMultiplierIngotValue,
            bool isHistory = false)
        {
            uint ingotWinAmount = 0;

            uint multiplier2 = 1;
            uint multiplier2IngotValue = 0;


            if (true)
            {
                multiplier2 = extraPrizeMultiplier;
                multiplier2IngotValue = extraPrizeMultiplierIngotValue;
            }

            ingotWinAmount = normalIngotValue * multiplier2 + multiplier2IngotValue;
           
            if(isHistory)
            {
                // Using unmodified histGameData.fgPreviousIngotValue saving is done with modified values already.
                ingotWinAmount = normalIngotValue + multiplier2IngotValue;
            }

            return ingotWinAmount;
        }

        private void UpdateWinStatement(uint ingotWinAmount, ReadOnlySpan<int> winPosition)
        {
            int ingotSymbolIndex = 16;

            WinStatement rawStatement = new WinStatement
            {
                symbolIndex = ingotSymbolIndex,
                winCount = 0,
                winAmount = ingotWinAmount,
                winPosCollection = winPosition.ToArray(),
                wayCount = 1
            };
    
            if (winCheckingData.winStatements is not null)
                winCheckingData.winStatements.Add(rawStatement);
        }

        private void UpdateIngotWinAmount(uint totalIngotPrize, bool isFreeGame, bool modifyWinAmount)
        {
            WkSlotWinManagerModel dm = modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
            if (modifyWinAmount)
            {
                dm.winAmount += totalIngotPrize;

                if (!isFreeGame)
                {
                    dm.mgWinAmount += totalIngotPrize;
                }
            }
            dm.tempWinAmount += totalIngotPrize;
        }

        public void UpdateScatterStatement(WkReelManager reelManager, long betValue, string potFeature)
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int totalReels = reelManager.reelData.Length;
            int[] winPosTemp = new int[5];

            for (int reel = 0; reel < totalReels; reel++) //5
            {
                int numRows = reelManager.reelData[reel].numRows;

                for (int row = 0; row < numRows; row++) //3
                {
                    int index = reelManager.GetReelIconIndex(reel, row) - 1;
                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols[index];

                    string ingotType = symbolDetail.symbolType;

                    if (potFeature.Contains("RN"))
                    {
                        if (ingotType == "BLUE_SCATTER")
                        {
                            winPosTemp[reel] |= (byte)(1 << row);
                        }
                    }

                    if (potFeature.Contains("JP"))
                    {
                        if (ingotType == "RED_SCATTER")
                        {
                            winPosTemp[reel] |= (byte)(1 << row);
                        }
                    }

                    if (potFeature.Contains("RU"))
                    {
                        if (ingotType == "GREEN_SCATTER")
                        {
                            winPosTemp[reel] |= (byte)(1 << row);
                        }
                    }
                }
            }

            int scatterSymbolIndex = 12;

            WinStatement rawStatement = new WinStatement
            {
                symbolIndex = scatterSymbolIndex,
                winCount = 0,
                winAmount = betValue * 2,
                winPosCollection = winPosTemp,
                wayCount = 1
            };

            winCheckingData.winStatements.Add(rawStatement);

            WkSlotWinManagerModel dm = modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
            long winAmount = betValue * 2;
            dm.winAmount += winAmount;
            dm.tempWinAmount += winAmount;
            dm.mgWinAmount += winAmount;
        }

        public override string GetJackpotPrizeType(byte index)
        {
            if (index == 1)
            {
                return "GRAND";
            }
            else if (index == 2)
            {
                return "MAJOR";
            }
            else if (index == 3)
            {
                return "MINOR";
            }
            else if (index == 4)
            {
                return "MINI";
            }
            else
            {
                return "NONE";
            }
        }

        public override byte GetJackpotPrizeIndex(string prizeType)
        {
            if (prizeType == "GRAND")
            {
                return 1;
            }
            else if (prizeType == "MAJOR")
            {
                return 2;
            }
            else if (prizeType == "MINOR")
            {
                return 3;
            }
            else if (prizeType == "MINI")
            {
                return 4;
            }
            else
            {
                return 0;
            }
        }

        public void CacheFirstIncrementAmount()
        {
            JIXRYWinManagerModel dm = modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();
            dm.fgFirstIncrementAmount = dm.tempWinAmount;
        }
    }
}