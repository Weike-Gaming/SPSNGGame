using System;
using UnityEngine;
using UnityEngine.UI;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiStatisticsDisplayObject : WkCombiDisplayObject
    {
        [SerializeField] private Text gameDenomination;
        private string _currency;

        [SerializeField] private Text bet;

        [SerializeField] private Text totalWin;

        [SerializeField] private Text randomJackpotHit;

        [SerializeField] private Text totalMainGameWin;

        [SerializeField] private GameObject parentFeature;
        [SerializeField] private Text feature;

        [SerializeField] private GameObject parentTotalFeatureWin;
        [SerializeField] private Text totalFeatureWin;

        [SerializeField] private GameObject parentFeatureSpins;
        [SerializeField] private Text featureSpins;

        [SerializeField] private GameObject parentTotalJackpotAndBonusWin;
        [SerializeField] private Text totalJackpotAndBonusWin;

        public byte upcomingPotFeatureGameFlag { get; set; }
        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYCombiGameManager? gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            wkModelCollection.AddModel(dm);
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            UpdateStatisticsDisplay();
        }

        protected override void SafeSelected()
        {
            base.SafeSelected();

            _currency = "USD";
            if (WkLobbySceneManager.instance is not null && WkLobbySceneManager.instance.currencyData != null)
            {
                _currency = WkLobbySceneManager.instance.dataModel.currency;
            }

            UpdateStatisticsDisplay();
        }

        public void UpdateStatisticsDisplay()
        {
            if (owningPlayerController?.owner is null) { return; }
            JIXRYCombiGameManager gm = owningPlayerController.owner as JIXRYCombiGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;

            if (!(WkLobbySceneManager.instance is { } lobbySceneManager)) { return; }

            #region GAMEDENOM
            ulong denomValue = lobbySceneManager.dataModel.currentDenomValue;
            WkAssert.EnsureMsgf(denomValue > 0, "Denominator not set!");
            gameDenomination.text = $"{WkCoreCurrencyUtils.GetCurrencyInString(_currency, denomValue)}";
            #endregion

            #region BET
            uint betAmount = gm.GetBetMultiplier() * gm.GetPlayOption();
            bet.text = $"{betAmount} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, betAmount * denomValue)})";
            #endregion

            JIXRYWinManager winManager = gm.winManager as JIXRYWinManager ?? throw new InvalidCastException();
            JIXRYWinManagerModel slotWinManagerModel = winManager.modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();

            #region TOTAL WIN
            ulong totalWinAmount = (ulong)(slotWinManagerModel.mgWinAmount + slotWinManagerModel.totalFgWinAmount);
            totalWin.text = $"{totalWinAmount} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, totalWinAmount * denomValue)})";
            #endregion

            #region RANDOM JACKPOT HIT

            string randomJackpot = dm.jackpotType switch
            {
                1 => "GRAND",
                2 => "MAJOR",
                _ => "N/A"
            };

            randomJackpotHit.text = gm.GetRandomPrizeType() > 0 ? randomJackpot : "N/A";
            #endregion

            #region TOTAL MAIN GAME WIN
            long totalMainGameWinAmount = slotWinManagerModel.mgWinAmount;
            totalMainGameWin.text = $"{totalMainGameWinAmount} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)totalMainGameWinAmount * denomValue)})";
            #endregion

            if (gm.GetFeatureFlag() != JIXRYStateDataFlag.MAIN_GAME)
            {
                #region FEATURE
                parentFeature.SetActive(true);
                feature.text = gm.GetGameType((byte)gm.GetFeatureFlag());
                #endregion

                #region TOTAL FEATURE WIN
                parentTotalFeatureWin.SetActive(true);
                long featureWin = slotWinManagerModel.totalFgWinAmount;
                totalFeatureWin.text = $"{(ulong)featureWin} ({WkCoreCurrencyUtils.GetCurrencyInString(_currency, (ulong)featureWin * denomValue)})";
                #endregion

                #region FEATURE SPINS
                parentFeatureSpins.SetActive(true);
                int retriggerAmount = gm.freeGameDataModel.totalFreeGameAmount - gm.freeGameDataModel.initialFreeGameAmount;
                featureSpins.text = (retriggerAmount > 0)
                    ? $"{gm.freeGameDataModel.totalFreeGameAmount} ({gm.freeGameDataModel.initialFreeGameAmount} + {retriggerAmount})"
                    : $"{gm.freeGameDataModel.totalFreeGameAmount}";
                #endregion

                #region TOTAL JACKPOT AND BONUS WIN
                parentTotalJackpotAndBonusWin.SetActive(true);
                string prize = string.Empty;
                uint total = 0;
                for(int i = 0; i < 4; i++)
                {
                    string type = string.Empty;
                    switch(i)
                    {
                        case 0:
                            type += "GRAND";
                            break;
                        case 1:
                            type += "MAJOR";
                            break;
                        case 2:
                            type += "MINOR";
                            break;
                        case 3:
                            type += "MINI";
                            break;
                    }

                    uint count = gm.GetPrizeCount(i);
                    total += count;
                    type += $" (X{count})";

                    if(count > 0)
                    {
                        prize += type;
                    }
                }

                if(total <= 0)
                {
                    prize = "N/A";
                }
                totalJackpotAndBonusWin.text = prize;
                #endregion                
            }
            else
            {
                parentFeature.SetActive(false);
                parentTotalFeatureWin.SetActive(false);
                parentFeatureSpins.SetActive(false);
                parentTotalJackpotAndBonusWin.SetActive(false);
            }
        }
    }
}
