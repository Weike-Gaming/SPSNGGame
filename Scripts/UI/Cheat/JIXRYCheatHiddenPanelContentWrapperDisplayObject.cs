using System;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenPanelContentWrapperDisplayObject : WkCheatHiddenPanelContentWrapperDisplayObject
    {
        [SerializeField] private WkButton demoButton;
        private bool _isMoreSettings = false;

        [Header("More Settings")]
        [SerializeField] private GameObject featureSettingsContent;
        [SerializeField] private GameObject ingotSettingsContent;
        [SerializeField] private GameObject extraSpinContent;
        [SerializeField] private GameObject extraPrize2AddContent;
        [SerializeField] private GameObject extraPrize2MulContent;
        [SerializeField] private GameObject extraPrize4AddContent;
        [SerializeField] private GameObject extraPrize4MulContent;
        [SerializeField] private GameObject jackpotContent;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm == null || button == null) { return; }

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            getState("fg-cheat")!.onEnterState += SetUpFgHiddenSettings;
            getState("fg-cheat")!.onExitState += ResetCheat;

            getState("idle")!.onEnterState += () => demoButton.interactable = true;
            getState("idle")!.onRecoverState += () => demoButton.interactable = true;
            getState("fg-init")!.onEnterState += () => demoButton.interactable = true;

            getState("fg-spin").onEnterState += () =>
            {
                demoButton.interactable = false;
            };

            button.onClickDown.AddListener(ToggleMoreSettings);
        }

        private void SetUpFgHiddenSettings()
        {
            demoButton.interactable = true;
            button?.gameObject.SetActive(true);
        }

        private void ResetCheat()
        {
            button.gameObject.SetActive(false);
            ResetHiddenSettingsToDefault();
            isOpen = false;
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            featureSettingsContent.SetActive(false);
            ingotSettingsContent.SetActive(false);
        }

        private void HandleContentVisibility()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            extraSpinContent.SetActive(false);
            extraPrize2AddContent.SetActive(false);
            extraPrize2MulContent.SetActive(false);
            extraPrize4AddContent.SetActive(false);
            extraPrize4MulContent.SetActive(false);
            jackpotContent.SetActive(false);

            if (gm.GetUpcomingGameType().Contains("LB"))
            {
                //extraPrize2AddContent.SetActive(true);
                extraPrize2MulContent.SetActive(true);
                //extraPrize4AddContent.SetActive(true);
                //extraPrize4MulContent.SetActive(true);
            }
            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME || gm.GetUpcomingGameType().Contains("JP"))
            {
                jackpotContent.SetActive(true);
            }        
        }

        private void ToggleMoreSettings()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (_isMoreSettings)
            {
                ingotSettingsContent.SetActive(false);
                featureSettingsContent.SetActive(false);
                _isMoreSettings = false;
            }
            else
            {
                _isMoreSettings = true;
                ingotSettingsContent.SetActive(true);
                if (dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME && dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.FREE_GAME_JP)
                {
                    featureSettingsContent.SetActive(true);
                }

                HandleContentVisibility();
            }        
        }

        public void CloseMoreSettings()
        {
            ingotSettingsContent.SetActive(false);
            featureSettingsContent.SetActive(false);
            _isMoreSettings = false;
        }
    }
}
