using DigitalRuby.Tween;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYFeaturePanelDisplayObject : WkMultiLanguageDisplayObject
    {
        [SerializeField] private SpriteRenderer panelBlur;
        [SerializeField] private SpriteRenderer featurePanel;       
        [SerializeField] private SpriteRenderer leftAnim;
        [SerializeField] private SpriteRenderer rightAnim;
        [SerializeField] private SpriteRenderer featureTitle;

        [Header("Feature Text")]
        [SerializeField] private JIXRYFeatureTextController textControllerVerticalBox;
        [SerializeField] private SpriteRenderer pressImage;
        [SerializeField] private GameObject spinText;
        [SerializeField] private GameObject jackpotText;
        [SerializeField] private GameObject prizeText;
        [SerializeField] private GameObject spinTextSolo;
        [SerializeField] private GameObject jackpotTextSolo;
        [SerializeField] private GameObject prizeTextSolo;

        protected override void OnAllowedEnable()
        {

        }

        protected override void ResetToDefault()
        {
            panelBlur.enabled = false;
            featurePanel.enabled = false;
            featureTitle.enabled = false;
            leftAnim.enabled = false;
            rightAnim.enabled = false;
            spinText.SetActive(false);
            jackpotText.SetActive(false);
            prizeText.SetActive(false);
            spinTextSolo.SetActive(false);
            jackpotTextSolo.SetActive(false);
            prizeTextSolo.SetActive(false);
            pressImage.enabled = false;
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("idle")!.onRecoverState += () =>
            {
                ResetToDefault();
                gm.PlayLED(JIXRYStateDataFlag.MAIN_GAME);
            };
            getState("idle")!.onEnterState += () =>
            {
                ResetToDefault();
                gm.PlayLED(JIXRYStateDataFlag.MAIN_GAME);
            };
            getState("fg-init")!.onEnterState += () =>
            {
                UpdateImage();
                getActiveAudioManager.PlayAudio("FreeGameBanner");
                gm.PlayLED((JIXRYStateDataFlag)gm.dataModel.GetModelDataChecked<JIXRYGameDataModel>().potFeatureGameFlag);
            };
            getState("fg-init")!.onExitState += () => 
            {
                getActiveAudioManager.StopAudio("FreeGameBanner");
                getActiveAudioManager.PlayAudio("FgBannerToFg");
                HideTween();
            };
            getState("fg-feature-animation")!.onEnterState += () =>
            {
                WkCoroutine.instance.StartTrackedCoroutine(HandleFgFeatureCoroutine());
            };

            getState("fg-feature-animation")!.onExitState += HideTween;
            getState("fg-cheat")!.onEnterState += ResetToDefault;

            getState("fg-init")!.onRecoverState += () =>
            {
                getActiveAudioManager.PlayAudio("FreeGameBanner");
                UpdateImage();
                gm.PlayLED((JIXRYStateDataFlag)gm.dataModel.GetModelDataChecked<JIXRYGameDataModel>().potFeatureGameFlag);
            };
            
        }

        private IEnumerator HandleFgFeatureCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            UpdateImage();
        }

        private void UpdateImage()
        {
            ChangePanelTitle();
            ChangePanelText();

            ShowPanel();
        }

        protected override void OnLanguageChange()
        {
            ChangePanelTitle();
        }

        private void ChangePanelTitle()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            JIXRYStateDataFlag potFeatureGameFlag = (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;
            bool fromMainGame = dm.previousPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME && 
                                dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            byte allPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU;
            byte twoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJP;

            if (fromMainGame)
            {

                if (dm.upcomingPotFeatureGameFlag < twoPotFeature)
                {
                    featureTitle.sprite = language == WkGameLanguage.EN ? collection["free8_en"] :collection["free8_zh"];
                }
                else if (dm.upcomingPotFeatureGameFlag < allPotFeature)
                {
                    featureTitle.sprite = language == WkGameLanguage.EN ? collection["free12_en"] : collection["free12_zh"];
                }
                else
                {
                    featureTitle.sprite = language == WkGameLanguage.EN ? collection["free16_en"] : collection["free16_zh"];
                }
            }
            else
            {
                if (dm.upcomingPotFeatureGameFlag == allPotFeature)
                {
                    if (dm.previousPotFeatureGameFlag < twoPotFeature) //RN,JP,RU
                    {
                        featureTitle.sprite = language == WkGameLanguage.EN ? collection["extra8_en"] : collection["extra8_zh"];
                    }
                    else if (dm.previousPotFeatureGameFlag < allPotFeature) //ESJP,ESEP,JPEP
                    {
                        featureTitle.sprite = language == WkGameLanguage.EN ? collection["extra4_en"] : collection["extra4_zh"];
                    }
                }
                else
                {
                    featureTitle.sprite = language == WkGameLanguage.EN ? collection["extra4_en"] : collection["extra4_zh"];
                }
            }           
        }

        private void ChangePanelText()
        {
            // Hide everything first
            spinText.SetActive(false);
            jackpotText.SetActive(false);
            prizeText.SetActive(false);
            spinTextSolo.SetActive(false);
            jackpotTextSolo.SetActive(false);
            prizeTextSolo.SetActive(false);

            // Get game data
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            List<JIXRYStateDataFlag> newParts = gm.GetNewFeatureList();

            if(newParts.Count() == 1 )
            {
                // Display only the newly hit feature(s)
                foreach (JIXRYStateDataFlag part in newParts)
                {
                    switch (part)
                    {
                        case JIXRYStateDataFlag.FREE_GAME_RN:
                            spinTextSolo.SetActive(true);
                            break;
                        case JIXRYStateDataFlag.FREE_GAME_JP:
                            jackpotTextSolo.SetActive(true);
                            break;
                        case JIXRYStateDataFlag.FREE_GAME_RU:
                            prizeTextSolo.SetActive(true);
                            break;
                    }
                }
            }
            else
            {
                // Display only the newly hit feature(s)
                foreach (JIXRYStateDataFlag part in newParts)
                {
                    switch (part)
                    {
                        case JIXRYStateDataFlag.FREE_GAME_RN:
                            spinText.SetActive(true);
                            break;
                        case JIXRYStateDataFlag.FREE_GAME_JP:
                            jackpotText.SetActive(true);
                            break;
                        case JIXRYStateDataFlag.FREE_GAME_RU:
                            prizeText.SetActive(true);
                            break;
                    }
                }
            }           
        }


        private void ShowPanel()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            bool fromMainGame = dm.previousPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME &&
                                dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            panelBlur.enabled = true;
            featurePanel.enabled = true;
            featureTitle.enabled = true;
            leftAnim.enabled = true;
            rightAnim.enabled = true;

            if (fromMainGame)
            {
                pressImage.enabled = true;
            }

            textControllerVerticalBox.UpdateVerticalBox();

            ShowTween();
        }

        private void ShowTween()
        {
            transform.localScale = Vector2.zero;

            TweenFactory.Tween(
                "FeaturePanelShow",
                0f, 1.1f,
                0.45f,
                TweenScaleFunctions.QuadraticEaseOut,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ =>
                {
                    TweenFactory.Tween(
                                "FeaturePanelSettle",
                                1.1f, 1.0f,
                                0.2f,
                                TweenScaleFunctions.QuadraticEaseInOut,
                                tt => transform.localScale = Vector2.one * tt.CurrentValue
                            );
                }
            );
        }

        private void HideTween()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            TweenFactory.Tween(
                "FeaturePanelHide",
                1.0f, 0f,
                0.35f,
                TweenScaleFunctions.QuadraticEaseIn,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ => ResetToDefault()
            );
        }
    }
}