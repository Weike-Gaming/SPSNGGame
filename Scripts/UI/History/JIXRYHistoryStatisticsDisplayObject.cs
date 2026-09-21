using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYHistoryStatisticsDisplayObject : WkHistoryDisplayObject
    {
        [Header("SEQUENCE")]
        [SerializeField] private GameObject parentGameSequence;
        [SerializeField] private Text gameSequence;
        private byte _maxGameSequence;

        [Header("SUB-SEQUENCE")]
        [SerializeField] private GameObject parentFeature; // enable in sub game
        [SerializeField] private Text feature;
            // Blue Pot (JIXRYHistorySubGameData.potFeatureGameFlag) Enum (JIXRYStateDataFlag)
        
        [SerializeField] private GameObject parentSubGameSequence; // enable in sub game
        [SerializeField] private Text subGameSequence;

        [Header("GENERIC")]
        private long _denomValue;
        private string _currency;

        [SerializeField] private GameObject parentRtpVariation; // disable in sub game
        [SerializeField] private Text rtpVariation;

        [SerializeField] private GameObject parentGameDenomination;
        [SerializeField] private Text gameDenomination;

        [SerializeField] private GameObject parentCreditsAtStart; // disable in sub game
        [SerializeField] private Text creditsAtStart;

        [SerializeField] private GameObject parentCreditsAtEnd; // disable in sub game
        [SerializeField] private Text creditsAtEnd;
        private ulong _creditsAtEndAmount;

        [SerializeField] private GameObject parentBet;
        [SerializeField] private Text bet;
            // == WkCoreHistoryMainGameData.betAmount

        [Header("GENERIC CONT")]
        [SerializeField] private GameObject parentBetOption;
        [SerializeField] private Text betOption;
            // == WkCoreHistoryMainGameData.betAmount / WkSlotHistoryMainGameData.selectedPlayOptionValue 
        
        [SerializeField] private GameObject parentPlayOption;
        [SerializeField] private Text playOption;
            // == WkSlotHistoryMainGameData.selectedPlayOptionValue

        // toggle enable/disable to opposite of "FREE GAME" enable/disable
        [Header("MAIN GAME")]
        [SerializeField] private GameObject parentTotalMainGameWin;
        [SerializeField] private Text totalMainGameWin;
            // == WkSlotHistoryMainGameData.savedMgWinAmount
       
        [SerializeField] private GameObject parentTotalFreeGameFeatureWin;
        [SerializeField] private Text totalFreeGameFeatureWin;
            // == WkSlotHistorySubGameData.savedTotalFgWinAmount 
        
        [SerializeField] private GameObject parentTotalJackpotAndBonusWinFromFreeGame;
        [SerializeField] private Text totalJackpotAndBonusWinFromFreeGame;
            // == (WkSlotHistorySubGameData[LastGame].jackpotLevel1Prize + WkSlotHistorySubGameData[LastGame].jackpotLevel2Prize +
            // WkSlotHistorySubGameData[LastGame].jackpotLevel3Prize + WkSlotHistorySubGameData[LastGame].jackpotLevel4Prize)
            // ?(WkSlotHistoryMainGameData.jackpotLevel1Prize + WkSlotHistoryMainGameData.jackpotLevel2Prize +
            // WkSlotHistoryMainGameData.jackpotLevel3Prize+ WkSlotHistoryMainGameData.jackpotLevel4Prize) 
        
        [SerializeField] private GameObject parentTotalRandomJackpotWin;
        [SerializeField] private Text totalRandomJackpotWin;
            // == WkSlotHistoryMainGameData.jackpotLevel1Prize + WkSlotHistoryMainGameData.jackpotLevel2Prize +
            // WkSlotHistoryMainGameData.jackpotLevel3Prize + WkSlotHistoryMainGameData.jackpotLevel4Prize

        [SerializeField] private GameObject parentRandomJackpotHit;
        [SerializeField] private Text randomJackpotHit;

        // toggle enable/disable to opposite of "MAIN GAME" enable/disable
        [Header("FREE GAME")]
        [SerializeField] private GameObject parentCurrentSpinIngotBonus;
        [SerializeField] private Text currentSpinIngotBonus;

        [SerializeField] private GameObject parentCurrentSpinWin;
        [SerializeField] private Text currentSpinWin;

        [SerializeField] private GameObject parentCumulativeWinAfterSpin;
        [SerializeField] private Text cumulativeWinAfterSpin;

        [SerializeField] private GameObject parentCurrentSpinIngotJackpotAndBonusWin;
        [SerializeField] private Text currentSpinIngotJackpotAndBonusWin;

        [SerializeField] private GameObject parentCumulativeIngotJackpotAndBonusAfterSpin;
        [SerializeField] private Text cumulativeIngotJackpotAndBonusAfterSpin;

        [SerializeField] private GameObject parentRetrigger;
        [SerializeField] private Text retrigger;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYHistoryGameManager gameManager = owningPlayerController?.owner as JIXRYHistoryGameManager;
            if (gameManager is null) return;

            gameManager.OnStatisticUpdate += UpdateStatisticsDisplay;
        }

        protected override void SafeSelected()
        {
            base.SafeSelected();

            WkHistoryManager? historyManager = machineContext?.historyManager;

            _maxGameSequence = 0;
            foreach (HistMainGame data in historyManager.cacheHistMainGames)
            {
                if (data.gameSequence >= 2)
                {
                    ++_maxGameSequence;
                }
            }

            _currency = "USD";
            if (WkLobbySceneManager.instance is not null && WkLobbySceneManager.instance.currencyData != null)
            {
                _currency = WkLobbySceneManager.instance.dataModel.currency;
            }

            UpdateStatisticsDisplay();
        }

        public void UpdateStatisticsDisplay()
        {
            JIXRYHistoryGameManager? gameManager = owningPlayerController?.owner as JIXRYHistoryGameManager;
            if (gameManager is not null)
            {
                #region FETCHDATA
                WkHistoryManager? historyManager = machineContext?.historyManager;

                HistMainGame currentGame = historyManager.GetHistoryData(WkMachineInstance.selectedHistory);
                JIXRYHistoryMainGameData historyMainRecoverData = new();
                historyManager.DeserializeMainGameData(currentGame, historyMainRecoverData);

                HistSubGame[] subGames = historyManager.GetHistorySubData((byte)(WkMachineInstance.selectedHistory + 1));
                JIXRYHistorySubGameData lastSubGameData = null;
                JIXRYHistorySubGameData subGamesData = null;
                bool hasFreeGame = false;
                ushort totalFreeGameCount = 0;

                HistSubGame? lastSubGame = subGames!.Where(game => historyMainRecoverData.gameSequence == game.gameSequence).FirstOrDefault();
                if (lastSubGame is not null)
                {
                    hasFreeGame = true;
                    lastSubGameData = new JIXRYHistorySubGameData();
                    subGamesData = new JIXRYHistorySubGameData();
                    historyManager.DeserializeSubGameData(subGames[gameManager.GetCurrentFgIndex()], subGamesData);
                    historyManager.DeserializeSubGameData(lastSubGame, lastSubGameData);
                    totalFreeGameCount = gameManager.freeGameDataModel.totalFreeGameAmount = lastSubGameData.savedTotalFreeGameAmount;
                }

                long mgWinAmount = historyMainRecoverData.savedWinAmount;
                long totalWinAmount = hasFreeGame ? lastSubGameData.savedWinAmount : mgWinAmount;
                long tmp = 0;
                ulong utmp = 0;

                // Check if there's retrigger
                bool hasRetrigger = false;
                if (lastSubGame is not null) {                    
                    if (subGamesData.potFeatureGameFlag != subGamesData.savedPreviousPotFeatureGameFlag &&
                        subGamesData.savedPreviousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME)
                    {
                        hasRetrigger = true;
                    }
                }
                #endregion

                #region SEQUENCE
                parentGameSequence.SetActive(true);
                byte sequenceNumber = (byte)(_maxGameSequence - WkMachineInstance.selectedHistory);
                gameSequence.text = (sequenceNumber == _maxGameSequence) ? "Latest" : sequenceNumber.ToString();
                #endregion

                #region SUBSEQUENCE
                if (gameManager.currentlyInSubGame)
                {
                    parentFeature.SetActive(true);

                    // Feature text need to show 'previous' if there's a retrigger.
                    if (hasRetrigger)
                    {
                        feature.text = $"{gameManager.GetGameType(subGamesData.savedPreviousPotFeatureGameFlag, true)}";
                    }
                    else
                    {
                        feature.text = $"{gameManager.GetGameType(subGamesData.potFeatureGameFlag, true)}";
                    }

                    parentSubGameSequence.SetActive(true);
                    byte subSequenceNumber = (byte)(totalFreeGameCount - gameManager.GetCurrentFgIndex());
                    subGameSequence.text = $"{subSequenceNumber}";
                }
                else
                {
                    parentFeature.SetActive(false);
                    parentSubGameSequence.SetActive(false);
                }
                #endregion

                #region GENERIC
                #region RTPVARIATION
                if (gameManager.currentlyInSubGame)
                    parentRtpVariation.SetActive(false);
                else
                {
                    parentRtpVariation.SetActive(true);
                    if (gameManager.reelManager is not null)
                        rtpVariation.text = gameManager.reelManager.reelStripHandler.GetRTPvariationName(historyMainRecoverData.variation);
                }
                #endregion
                #region GAMEDENOM
                parentGameDenomination.SetActive(true);
                _denomValue = historyMainRecoverData.denomValue;
                gameDenomination.text = _denomValue > 0 ? $"{WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)_denomValue)}" : "Error: Denominator not set!";
                #endregion
                #region CREDITS START/END
                if (gameManager.currentlyInSubGame)
                {
                    parentCreditsAtStart.SetActive(false);
                    parentCreditsAtEnd.SetActive(false);
                }
                else
                {
                    parentCreditsAtStart.SetActive(true);
                    utmp = historyMainRecoverData.previousTotalCredit - historyMainRecoverData.betAmount * (ulong)_denomValue;
                    creditsAtStart.text = _denomValue > 0 ? $"{utmp / (ulong)_denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, utmp)})" : "Error: Denominator not set!";

                    parentCreditsAtEnd.SetActive(true);
                    // Get base Credits At End amount
                    _creditsAtEndAmount = historyMainRecoverData.previousTotalCredit - historyMainRecoverData.betAmount * (ulong)_denomValue;   
                    // Add win amount
                    if (hasFreeGame)
                        _creditsAtEndAmount += (ulong)(lastSubGameData.savedTotalFgWinAmount * _denomValue);
                    else
                        _creditsAtEndAmount += (ulong)(historyMainRecoverData.savedMgWinAmount * _denomValue);
                    creditsAtEnd.text = _denomValue > 0 ? $"{_creditsAtEndAmount / (ulong)_denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, _creditsAtEndAmount)})" : "Error: Denominator not set!";
                }
                #endregion
                #region BET
                parentBet.SetActive(true);
                utmp = historyMainRecoverData.betAmount * (ulong)_denomValue;
                bet.text = _denomValue > 0 ? $"{utmp / (ulong)_denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, utmp)})" : "Error: Denominator not set!";
                #endregion
                #endregion

                #region GENERIC CONT
                #region PLAY OPTION
                parentPlayOption.SetActive(true);
                uint currentPlayOption = gameManager.GetSelectedPlayOptions()[historyMainRecoverData.selectedPlayOption];
                playOption.text = $"{currentPlayOption}";
                #endregion

                #region BET MULTIPLIER
                parentBetOption.SetActive(true);
                utmp = historyMainRecoverData.betAmount;
                uint currentBetMultiplier = (uint)(utmp / currentPlayOption);
                betOption.text = currentBetMultiplier.ToString();
                #endregion
                #endregion

                #region MAIN GAME
                if (gameManager.currentlyInSubGame)
                {
                    parentTotalMainGameWin.SetActive(false);
                    parentTotalFreeGameFeatureWin.SetActive(false);
                    parentTotalJackpotAndBonusWinFromFreeGame.SetActive(false);
                    parentTotalRandomJackpotWin.SetActive(false);
                    parentRandomJackpotHit.SetActive(false);
                }
                else
                {
                    #region TOTAL MAIN GAME WIN
                    parentTotalMainGameWin.SetActive(true);
                    tmp = historyMainRecoverData.savedMgWinAmount;
                    totalMainGameWin.text = _denomValue > 0 ? $"{tmp} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)(tmp * _denomValue))})" : "Error: Denominator not set!";

                    #endregion

                    #region TOTAL FREE GAME FEATURE WIN
                    parentTotalFreeGameFeatureWin.SetActive(true);
                    tmp = (lastSubGameData is not null) ? lastSubGameData.savedTotalFgWinAmount - historyMainRecoverData.savedMgWinAmount : 0;
                    totalFreeGameFeatureWin.text = _denomValue > 0 ? $"{tmp} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)(tmp * _denomValue))})" : "Error: Denominator not set!";
                    #endregion

                    #region TOTAL JACKPOT AND BONUS WIN FROM FREE GAME
                    parentTotalJackpotAndBonusWinFromFreeGame.SetActive(true);
                    if (lastSubGameData is not null)
                    {
                        tmp = lastSubGameData.jackpotLevel1Prize + lastSubGameData.jackpotLevel2Prize + lastSubGameData.jackpotLevel3Prize + lastSubGameData.jackpotLevel4Prize
                            - historyMainRecoverData.mgJackpotLevel1Prize - historyMainRecoverData.mgJackpotLevel2Prize - historyMainRecoverData.mgJackpotLevel3Prize - historyMainRecoverData.mgJackpotLevel4Prize;
                    }
                    else
                    {
                        tmp = 0;
                    }
                    totalJackpotAndBonusWinFromFreeGame.text = _denomValue > 0 ? $"{tmp / _denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)tmp)})" : "Error: Denominator not set!";
                    #endregion

                    #region updating CREDITS AT END if got jackpot
                    if (tmp > 0)
                    {
                        // Add jackpot amount
                        _creditsAtEndAmount += (ulong)(tmp);
                        creditsAtEnd.text = _denomValue > 0 ? $"{_creditsAtEndAmount / (ulong)_denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, _creditsAtEndAmount)})" : "Error: Denominator not set!";
                    }
                    #endregion

                    #region TOTAL RANDOM JACKPOT WIN
                    parentTotalRandomJackpotWin.SetActive(true);
                    tmp = historyMainRecoverData.mgJackpotLevel1Prize + historyMainRecoverData.mgJackpotLevel2Prize + historyMainRecoverData.mgJackpotLevel3Prize + historyMainRecoverData.mgJackpotLevel4Prize;
                    totalRandomJackpotWin.text = _denomValue > 0 ? $"{tmp / _denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)tmp)})" : "Error: Denominator not set!";
                    #endregion

                    #region updating TOTAL MAIN GAME WIN if got random jackpot
                    if (tmp > 0)
                    {
                        _creditsAtEndAmount += (ulong)(tmp);
                        creditsAtEnd.text = _denomValue > 0 ? $"{_creditsAtEndAmount / (ulong)_denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, _creditsAtEndAmount)})" : "Error: Denominator not set!";
                    }
                    #endregion

                    #region RANDOM JACKPOT HIT
                    parentRandomJackpotHit.SetActive(true);
                    if (historyMainRecoverData.mgJackpotLevel1Prize > 0)
                        randomJackpotHit.text = "Grand";
                    else if (historyMainRecoverData.mgJackpotLevel2Prize > 0)
                        randomJackpotHit.text = "Major";
                    else if (historyMainRecoverData.mgJackpotLevel3Prize > 0)
                        randomJackpotHit.text = "Minor";
                    else if (historyMainRecoverData.mgJackpotLevel4Prize > 0)
                        randomJackpotHit.text = "Mini";
                    else
                        randomJackpotHit.text = "N/A";
                    #endregion

                }
                #endregion

                #region FREE GAME
                if (gameManager.currentlyInSubGame)
                {
                    #region CURRENT SPIN INGOT BONUS
                    parentCurrentSpinIngotBonus.SetActive(true);
                    currentSpinIngotBonus.text = gameManager.GetBonus();
                    #endregion

                    #region CURRENT SPIN WIN
                    parentCurrentSpinWin.SetActive(true);
                    JIXRYHistorySubGameData prevSubGamesData = new JIXRYHistorySubGameData();
                    if (gameManager.HasPrevSubGame())
                    {
                        historyManager.DeserializeSubGameData(subGames[gameManager.GetCurrentFgIndex()+1], prevSubGamesData);
                        tmp = subGamesData.savedTotalFgWinAmount - prevSubGamesData.savedTotalFgWinAmount;
                    }
                    else
                        tmp = subGamesData.savedTotalFgWinAmount - historyMainRecoverData.savedMgWinAmount;
                    currentSpinWin.text = _denomValue > 0 ? $"{tmp} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)(tmp * _denomValue))})" : "Error: Denominator not set!";
                    #endregion

                    #region CUMULATIVE WIN AFTER SPIN
                    parentCumulativeWinAfterSpin.SetActive(true);
                    tmp = subGamesData.savedTotalFgWinAmount;
                    cumulativeWinAfterSpin.text = _denomValue > 0 ? $"{tmp} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)(tmp * _denomValue))})" : "Error: Denominator not set!";
                    #endregion

                    #region CURRENT SPIN INGOT JACKPOT AND BONUS WIN
                    parentCurrentSpinIngotJackpotAndBonusWin.SetActive(true);
                    tmp = subGamesData.fgJackpotLevel1Prize + subGamesData.fgJackpotLevel2Prize +
                        subGamesData.fgJackpotLevel3Prize + subGamesData.fgJackpotLevel4Prize;
                    currentSpinIngotJackpotAndBonusWin.text = _denomValue > 0 ? $"{tmp / _denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)tmp)})" : "Error: Denominator not set!";
                    #endregion

                    #region CUMULATIVE INGOT JACKPOT AND BONUS AFTER SPIN
                    parentCumulativeIngotJackpotAndBonusAfterSpin.SetActive(true);
                    tmp = subGamesData.jackpotLevel1Prize + subGamesData.jackpotLevel2Prize +
                        subGamesData.jackpotLevel3Prize + subGamesData.jackpotLevel4Prize;
                    cumulativeIngotJackpotAndBonusAfterSpin.text = _denomValue > 0 ? $"{tmp / _denomValue} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)tmp)})" : "Error: Denominator not set!";
                    #endregion

                    #region RETRIGGER
                    parentRetrigger.SetActive(true);
                    if (hasRetrigger)
                    {
                        retrigger.text = gameManager.GetNextGameType();
                    }
                    else
                        retrigger.text = "N/A";
                    #endregion
                }
                else
                {
                    parentCurrentSpinIngotBonus.SetActive(false);
                    parentCurrentSpinWin.SetActive(false);
                    parentCumulativeWinAfterSpin.SetActive(false);
                    parentCurrentSpinIngotJackpotAndBonusWin.SetActive(false);
                    parentCumulativeIngotJackpotAndBonusAfterSpin.SetActive(false);
                    parentRetrigger.SetActive(false);
                }
                #endregion
            }
        }

        ///<inheritdoc/>
        protected override void OnAllowedEnable()
        {
            parentGameSequence.SetActive(true);
            parentFeature.SetActive(true);
            parentSubGameSequence.SetActive(true);
            parentRtpVariation.SetActive(true);
            parentGameDenomination.SetActive(true);
            parentCreditsAtStart.SetActive(true);
            parentCreditsAtEnd.SetActive(true);
            parentBet.SetActive(true);
            parentBetOption.SetActive(true);
            parentPlayOption.SetActive(true);
            parentTotalMainGameWin.SetActive(true);
            parentTotalFreeGameFeatureWin.SetActive(true);
            parentTotalJackpotAndBonusWinFromFreeGame.SetActive(true);
            parentTotalRandomJackpotWin.SetActive(true);
            parentCurrentSpinIngotBonus.SetActive(true);
            parentCurrentSpinWin.SetActive(true);
            parentCumulativeWinAfterSpin.SetActive(true);
            parentCurrentSpinIngotJackpotAndBonusWin.SetActive(true);
            parentCumulativeIngotJackpotAndBonusAfterSpin.SetActive(true);
            parentRetrigger.SetActive(true);
            base.OnAllowedEnable();
        }

        ///<inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();
            parentGameSequence.SetActive(false);
            parentFeature.SetActive(false);
            parentSubGameSequence.SetActive(false);
            parentRtpVariation.SetActive(false);
            parentGameDenomination.SetActive(false);
            parentCreditsAtStart.SetActive(false);
            parentCreditsAtEnd.SetActive(false);
            parentBet.SetActive(false);
            parentBetOption.SetActive(false);
            parentPlayOption.SetActive(false);
            parentTotalMainGameWin.SetActive(false);
            parentTotalFreeGameFeatureWin.SetActive(false);
            parentTotalJackpotAndBonusWinFromFreeGame.SetActive(false);
            parentTotalRandomJackpotWin.SetActive(false);
            parentCurrentSpinIngotBonus.SetActive(false);
            parentCurrentSpinWin.SetActive(false);
            parentCumulativeWinAfterSpin.SetActive(false);
            parentCurrentSpinIngotJackpotAndBonusWin.SetActive(false);
            parentCumulativeIngotJackpotAndBonusAfterSpin.SetActive(false);
            parentRetrigger.SetActive(false);
        }

        ///<inheritdoc/>
        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            parentGameSequence.SetActive(false);
            parentFeature.SetActive(false);
            parentSubGameSequence.SetActive(false);
            parentRtpVariation.SetActive(false);
            parentGameDenomination.SetActive(false);
            parentCreditsAtStart.SetActive(false);
            parentCreditsAtEnd.SetActive(false);
            parentBet.SetActive(false);
            parentBetOption.SetActive(false);
            parentPlayOption.SetActive(false);
            parentTotalMainGameWin.SetActive(false);
            parentTotalFreeGameFeatureWin.SetActive(false);
            parentTotalJackpotAndBonusWinFromFreeGame.SetActive(false);
            parentTotalRandomJackpotWin.SetActive(false);
            parentCurrentSpinIngotBonus.SetActive(false);
            parentCurrentSpinWin.SetActive(false);
            parentCumulativeWinAfterSpin.SetActive(false);
            parentCurrentSpinIngotJackpotAndBonusWin.SetActive(false);
            parentCumulativeIngotJackpotAndBonusAfterSpin.SetActive(false);
            parentRetrigger.SetActive(false);
        }
    }
}
