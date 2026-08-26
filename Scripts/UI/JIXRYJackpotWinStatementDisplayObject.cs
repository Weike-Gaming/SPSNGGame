using System;
using TMPro;
using UnityEngine;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYJackpotWinStatementDisplayObject : WkDisplayObject
    {
        [Header("Feature & Ingot")]
        [SerializeField] GameObject jpWinStatement;
        [SerializeField] TMP_Text jpText;
        [SerializeField] TMP_Text jpAmount;
        private bool _showJpStatement = false;

        public WkGameLanguage language { get; set; }

        protected override void OnAllowedEnable()
        {
        }

        protected override void ResetToDefault()
        {
            jpWinStatement.SetActive(false);
            _showJpStatement = false;
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            wkModelCollection.AddModel(WkLobbySceneManager.instance!.dataModel);

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("jp-session-end")!.onEnterState += ShowJpWinStatement;
            getState("jp-session-end")!.onRecoverState += ShowJpWinStatement;
            getState("main-game-end")!.onEnterState += ResetToDefault;

            getState("fg-jp-session-end")!.onEnterState += ShowJpWinStatement;
            getState("fg-jp-session-end")!.onRecoverState += ShowJpWinStatement;
            getState("fg-end-from-jackpot")!.onEnterState += ResetToDefault;

            gm.SavePotCoinValue();

            ResetToDefault();
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            if (_showJpStatement)
            {
                ShowJpWinStatement();
            }
            else
            {
                ResetToDefault();
            }
        }

        private void ShowJpWinStatement()
        {
            _showJpStatement = true;
            jpWinStatement.SetActive(true);
            UpdateJackpotWinAmount();
        }

        private void UpdateJackpotWinAmount()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkFreeGameDataModel fgdm = gm.freeGameDataModel;

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                if (dm.jackpotType == 1)
                {
                    jpText.text = language == WkGameLanguage.EN ? $"RANDOM GRAND JACKPOT WON" : $"随机巨奖积宝赢得";
                }
                else if (dm.jackpotType == 2)
                {
                    jpText.text = language == WkGameLanguage.EN ? $"RANDOM MAJOR JACKPOT WON" : $"随机大奖积宝赢得";
                }
            }
            else
            {
                switch (dm.jackpotType)
                {
                    case 1:
                        jpText.text = language == WkGameLanguage.EN ? $"GRAND JACKPOT WON" : $"巨奖积宝赢得";
                        break;
                    case 2:
                        jpText.text = language == WkGameLanguage.EN ? $"MAJOR JACKPOT WON" : $"大奖积宝赢得";
                        break;
                    case 3:
                        jpText.text = language == WkGameLanguage.EN ? $"MINOR BONUS WON" : $"中奖红利赢得";
                        break;
                    case 4:
                        jpText.text = language == WkGameLanguage.EN ? $"MINI BONUS WON" : $"小奖红利赢得";
                        break;
                }
            }

            long totalAmount = 0;
            if (IsFreeGame())
            {
                totalAmount = fgdm.fgJackpotLevel1Prize + fgdm.fgJackpotLevel2Prize + fgdm.fgJackpotLevel3Prize + fgdm.fgJackpotLevel4Prize;
            }
            else
            {
                totalAmount = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
            }

            string currency = WkLobbySceneManager.instance.dataModel.currency;

            jpAmount.text = WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)totalAmount);
        }

        private bool IsFreeGame()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            return dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;
        }
    }
}
