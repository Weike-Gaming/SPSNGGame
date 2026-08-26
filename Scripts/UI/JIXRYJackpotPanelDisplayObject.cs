using System;
using System.Collections.Generic;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;
using DigitalRuby.Tween;
using System.Collections;
using Weike.LobbyManagement;
using System.Linq;
using Weike.MachineInterface;

namespace Weike.Games.JIXRY
{
    public class JIXRYJackpotPanelDisplayObject : WkMultiLanguageDisplayObject
    {
        [SerializeField] private bool isTopScreen;
        [SerializeField] private SpriteRenderer panelBlurImg;
        [SerializeField] private SpriteRenderer jackpotPanelImg;

        [Header("Random Jackpot")]
        [SerializeField] private GameObject randomJackpotGO;
        [SerializeField] private SpriteRenderer jackpotTextImg;
               
        [Header("Free Game Jackpot")]
        [SerializeField] private GameObject fgJackpotGO;
        [SerializeField] private SpriteRenderer fgJackpotTextImg;
        [SerializeField] private SpriteRenderer fgJackpotTypeImg;

        [Header("Renderer")]
        [SerializeField] private WkAdvanceRenderer randomJackpotRenderer = null!;
        [SerializeField] private WkAdvanceRenderer fgJackpotRenderer = null!;

        [SerializeField] private WkTexture2DFramesInfo info;
        private Animator _animator;
        private const int SortingLayer = 10;
        private const float FeatureAudioDelay = 2.464f;
        private readonly Vector2 _maxScale = new Vector2(1200, 148);

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<Animator>();
        }

        protected override void OnAllowedEnable()
        {
            
        }

        public override void OnPIError()
        {
            base.OnPIError();
            StopAllCoroutines();
        }

        protected override void ResetToDefault()
        {
            panelBlurImg.enabled = false;
            jackpotPanelImg.enabled = false;
            randomJackpotGO.SetActive(false);
            fgJackpotGO.SetActive(false);
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("jp-announcement")!.onEnterState += () =>
            {
                ShowBottomPanel();
                PlayLed();
            };
            getState("jp-announcement")!.onRecoverState += () =>
            {
                ShowBottomPanel();
                PlayLed();
            };
            getState("fg-jp-announcement")!.onEnterState += DelayPop;
            getState("fg-jp-announcement")!.onRecoverState += DelayPop;

            getState("jp-session-end")!.onEnterState += ShowTopPanel;
            getState("jp-session-end")!.onRecoverState += ShowTopPanel;
            getState("take-win-from-jp")!.onExitState += HideTopPanel;
        }

        protected override void OnLanguageChange()
        {
            if(IsMainGame())
            {
                ChangeJackpotTypeImage();
            }
            else
            {
                ChangeFgJackpotSprite();
            }
        }

        private void ShowTopPanel()
        {
            if (!isTopScreen) return;
            UpdatePanel();
        }
        
        private void HideTopPanel()
        {
            if (!isTopScreen) return;
            HideTween();
        }

        private void ShowBottomPanel()
        {
            if (isTopScreen) return;
            StartCoroutine(FeatureTriggerAudio());
        }

        private IEnumerator FeatureTriggerAudio()
        {
            getActiveAudioManager.PlayAudio("FeatureTrigger");
            yield return new WaitForSeconds(FeatureAudioDelay);
            UpdatePanel();
        }

        #region Delay
        private IEnumerator PanelDelayCoroutine()
        {
            yield return new WaitForSeconds(4f);
            HideTween();
        }

        //Free Game
        private void DelayPop()
        {
            if (isTopScreen) return;
            StartCoroutine(HandleFgJackpotCoroutine());       
        }

        private IEnumerator HandleFgJackpotCoroutine()
        {
            getActiveAudioManager.PlayAudioUnique("FeatureTrigger");
            yield return new WaitForSeconds(2.8f);
            UpdatePanel();
        }
        #endregion

        private void UpdatePanel()
        {
            if (IsMainGame())
            {
                ChangeJackpotTypeImage();
            }
            else
            {
                ChangeFgJackpotSprite();
            }
            
            ShowPanel();
            UpdateTotalWinText();
            
            ShowTween();

            if (!isTopScreen)
            {
                getActiveAudioManager.PlayAudioUnique("JackpotPanelOpen");
                StartCoroutine(PanelDelayCoroutine());
            }
        }

        #region Change Sprite
        private void ChangeJackpotTypeImage()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel slotGameDataModel = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte jackpotType = slotGameDataModel.jackpotType;
            if (jackpotType == 1)
            {
                string animClip = language == WkGameLanguage.EN ? "Grand_en" : "Grand_zh";
                _animator.Play(animClip);
            }
            else
            {
                string animClip = language == WkGameLanguage.EN ? "Major_en" : "Major_zh";
                _animator.Play(animClip);
            }
            jackpotPanelImg.sprite = collection["randomJpBg"];
        }

        private void ChangeFgJackpotSprite()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
			JIXRYGameDataModel slotGameDataModel = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte jackpotType = slotGameDataModel.jackpotType;

            if (jackpotType > 2)
            {
                fgJackpotTextImg.gameObject.transform.localPosition = new Vector2(0, 2.6f);
            }
            else
            {
                fgJackpotTextImg.gameObject.transform.localPosition = new Vector2(0, 2.4f);
            }

            switch (jackpotType)
            {
                case 1:
                    jackpotPanelImg.sprite = collection["fgJpBgGrand"];
                    fgJackpotTextImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTextGrand_en"] : collection["fgJpTextGrand_zh"];
                    fgJackpotTypeImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTypeGrand_en"] : collection["fgJpTypeGrand_zh"];
                    break;
                case 2:
                    jackpotPanelImg.sprite = collection["fgJpBgMajor"];
                    fgJackpotTextImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTextMajor_en"] : collection["fgJpTextMajor_zh"];
                    fgJackpotTypeImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTypeMajor_en"] : collection["fgJpTypeMajor_zh"];
                    break;
                case 3:
                    jackpotPanelImg.sprite = collection["fgJpBgMinor"];
                    fgJackpotTextImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTextMinor_en"] : collection["fgJpTextMinor_zh"];
                    fgJackpotTypeImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTypeMinor_en"] : collection["fgJpTypeMinor_zh"];
                    break;
                case 4:
                    jackpotPanelImg.sprite = collection["fgJpBgMini"];
                    fgJackpotTextImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTextMini_en"] : collection["fgJpTextMini_zh"];
                    fgJackpotTypeImg.sprite = language == WkGameLanguage.EN ? collection["fgJpTypeMini_en"] : collection["fgJpTypeMini_zh"];
                    break;
            }
        }
        #endregion

        private void ShowPanel()
        {
            panelBlurImg.enabled = true;
            jackpotPanelImg.enabled = true;

            if (IsMainGame())
            {
                randomJackpotGO.SetActive(true);
            }
            else
            {
                fgJackpotGO.SetActive(true);
            }
        }

        private void UpdateTotalWinText()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkFreeGameDataModel fgdm = gm.freeGameDataModel;
            
            WkAdvanceRenderer advanceRenderer;
            long jackpotPrize;

            if (IsMainGame())
            {
                jackpotPrize = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
                advanceRenderer = randomJackpotRenderer;
            }
            else
            {
                jackpotPrize = fgdm.fgJackpotLevel1Prize + fgdm.fgJackpotLevel2Prize + fgdm.fgJackpotLevel3Prize + fgdm.fgJackpotLevel4Prize;
                advanceRenderer = fgJackpotRenderer;
            }

            advanceRenderer.ClearAllDrawInfo();
            (Texture2D[], SSFrame[]) value = TryGetInfo(jackpotPrize);
            SSFrame[] frames = value.Item2;
            Vector2[] offset = new Vector2[frames.Length];
            Array.Fill(offset, Vector2.zero);
            DrawTextInfo info = new DrawTextInfo();
            info.textures = value.Item1;
            info.frames = frames;
            info.offsets = offset;
            info.minScale = new Vector2(1, 1);
            info.maxScale = _maxScale;
            advanceRenderer.AddDrawInfo("Digit", info);
            advanceRenderer.Draw("UI", SortingLayer);
        }

        private bool IsMainGame()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private (Texture2D[], SSFrame[]) TryGetInfo(long amount)
        {
            string currency = WkLobbySceneManager.instance.dataModel.currency;
            List<Texture2D> textures = new List<Texture2D>();
            List<SSFrame> frames = new List<SSFrame>();

            {
                (Texture2D, SSFrame) value;

                if(currency.Equals("GEN"))
                {
                    // Generic currency

                    MachInfoWrapper machInfo = new();
                    MachInfoWrapper.PlExtGetMachInfo(machInfo);
                    ReadOnlySpan<char> genericCurrency = machInfo.genericSymbol.AsSpan();

                    foreach (char c in genericCurrency)
                    {
                        value = info.GetTexture2DAndFrame(c.ToString());
                        textures.Add(value.Item1);
                        frames.Add(value.Item2);
                    }
                }
                else
                {
                    value = info.GetTexture2DAndFrame(currency);
                    textures.Add(value.Item1);
                    frames.Add(value.Item2);
                }               
            }

            string a = WkCoreCurrencyUtils.GetCurrencyInStringNoSymbol(currency, (ulong)amount);

            for (int i = 0; i < a.Length; i++)
            {
                (Texture2D, SSFrame) value = info.GetTexture2DAndFrame(a.ElementAt(i).ToString());
                textures.Add(value.Item1);
                frames.Add(value.Item2);
            }

            return (textures.ToArray(), frames.ToArray());
        }
        #region DeserializeJson
        private ReadOnlySpan<long> GetDigits(long num)
        {
            List<long> digits = new List<long>();

            // Extract digits from right to left
            while (num > 0)
            {
                digits.Add(num % 10);
                num /= 10;
            }

            digits.Reverse(); // Reverse to get original order
            return digits.ToArray();
        }
        #endregion

        #region Animation
        private void ShowTween()
        {
            transform.localScale = Vector2.zero;

            TweenFactory.Tween(
                "JackpotPanelShow" + GetInstanceID(),
                0f, 1.1f,
                0.45f,
                TweenScaleFunctions.QuadraticEaseOut,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ =>
                {
            TweenFactory.Tween(
                        "JackpotPanelSettle" + GetInstanceID(),
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
            getActiveAudioManager.PlayAudioUnique("JackpotPanelClose");
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            
            TweenFactory.Tween(
                "JackpotPanelHide" + GetInstanceID(),
                1.0f, 0f,
                0.35f,
                TweenScaleFunctions.QuadraticEaseIn,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ =>
                {
                    ResetToDefault();
                }
            );
        }
        #endregion

        #region play led

        private void PlayLed()
        {
            if (isTopScreen) return;
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            gm.PlayLEDJackpot();
        }
        #endregion
    }
}