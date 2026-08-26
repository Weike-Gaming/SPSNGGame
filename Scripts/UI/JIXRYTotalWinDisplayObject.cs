using DigitalRuby.Tween;
using System;
using System.Collections.Generic;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYTotalWinDisplayObject : WkDisplayObject
    {
        [SerializeField] private SpriteRenderer panelBlurImg;
        [SerializeField] private SpriteRenderer totalWinPanelBg;
        [SerializeField] private SpriteRenderer leftAnim;
        [SerializeField] private SpriteRenderer rightAnim;
        [SerializeField] private SpriteRenderer totalWinTitle;
        [SerializeField] private SpriteRenderer creditsImg;
         private const int SortingLayer = 12;

        [Header("Renderer")]
        [SerializeField] private WkAdvanceRenderer advanceRenderer = null!;

        [SerializeField] private WkTexture2DFramesInfo info;

        protected override void OnAllowedEnable()
        {
            
        }

        protected override void ResetToDefault()
        {
            panelBlurImg.enabled = false;
            leftAnim.enabled = false;
            rightAnim.enabled = false;
            totalWinPanelBg.enabled = false;
            totalWinTitle.enabled = false;
            creditsImg.enabled = false;
            advanceRenderer.ClearAllDrawInfo();
            advanceRenderer.Draw();
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();       

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;
            getState("idle")!.onRecoverState += ResetToDefault;
            getState("idle")!.onEnterState += ResetToDefault;
            getState("fg-end-panel")!.onEnterState += () =>
            {
                UpdatePanel();
                gm.PlayLEDFgEndPanel();
            };
            getState("fg-end-panel")!.onExitState += HideTween;
            getState("fg-end-panel")!.onRecoverState += () =>
            {
                UpdatePanel();
                gm.PlayLEDFgEndPanel();
            };
        }

        private void UpdatePanel()
        {
            UpdateTotalWinText();
            ShowPanel();
            getActiveAudioManager.PlayAudioUnique("TotalWinPanel");
            ShowTween();
        }

        private void ShowPanel()
        {
            panelBlurImg.enabled = true;
            totalWinPanelBg.enabled = true;
            rightAnim.enabled = true;
            leftAnim.enabled = true;
            totalWinTitle.enabled = true;
            creditsImg.enabled = true;
        }

        private void UpdateTotalWinText()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManagerModel winManagerModel  = gm.winManager.modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();

            gm.RecoverJackpotAmount();

            long totalWin = (dm.jackpotWinAmount / dm.denomValue) + winManagerModel.savedTotalFgWinAmount;

            advanceRenderer.ClearAllDrawInfo();
            (Texture2D[], SSFrame[]) value = TryGetInfo(totalWin);
            SSFrame[] frames = value.Item2;
            DrawTextInfo info = new DrawTextInfo();
            Vector2[] offset = new Vector2[frames.Length];
            Array.Fill(offset, Vector2.zero);
            info.textures = value.Item1;
            info.frames = frames;
            info.offsets = offset;
            advanceRenderer.AddDrawInfo("Digit", info);
            advanceRenderer.Draw("UI", SortingLayer);
        }

        private (Texture2D[], SSFrame[]) TryGetInfo(long amount)
        {
            List<Texture2D> textures = new List<Texture2D>();
            List<SSFrame> frames = new List<SSFrame>();

            ReadOnlySpan<long> digits = GetDigits(amount);
            for (int i = 0; i < digits.Length; i++)
            {
                (Texture2D, SSFrame) value = info.GetTexture2DAndFrame(digits[i].ToString());
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
                "TotalWinPanelShow",
                0f, 1.1f,
                0.45f,
                TweenScaleFunctions.QuadraticEaseOut,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ =>
                {
                    TweenFactory.Tween(
                                "TotalWinPanelSettle",
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
            TweenFactory.Tween(
                "TotalWinPanelHide",
                1.0f, 0f,
                0.35f,
                TweenScaleFunctions.QuadraticEaseIn,
                t => transform.localScale = Vector2.one * t.CurrentValue,
                _ => ResetToDefault()
            );
        }
        #endregion
    }
}