using System;
using Weike.Core;
using UnityEngine;
using System.Collections.Generic;
using Weike.SlotCore;
using System.Collections;

namespace Weike.Games.JIXRY
{
    public class JIXRYFreeGameCounterDisplayObject : WkDisplayObject
    {
        [SerializeField] protected GameObject counterRef = null!;
        [SerializeField] protected GameObject lanternRef = null!;

        [Header("Renderer")]
        [SerializeField] private WkAdvanceRenderer currentRenderer = null!;
        [SerializeField] private WkAdvanceRenderer slashRenderer = null!;
        [SerializeField] private WkAdvanceRenderer totalRenderer = null!;
        [SerializeField] private WkTexture2DFramesInfo textureInfo;
        private bool _isInfoReady = false;
        private const int SortingOrder = 3;

        private int _totalFgCount = 0;

        #region Binding
        public ushort totalFreeGameAmount { get; set; }
        public ushort currentAmountOfFreeGame { get; set; }
        #endregion

        protected override void OnAllowedEnable()
        {

        }

        protected override void ResetToDefault()
        {
            counterRef.SetActive(false);
            lanternRef.SetActive(false);
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController!.owner as JIXRYGameManager ?? throw new InvalidCastException();
            WkFreeGameDataModel fgDm = gm.freeGameDataModel;
            wkModelCollection.AddModel(fgDm);

            Func<string, WkStateCore?> getState = gm!.gameState!.GetState<WkStateCore>;

            getState("idle").onRecoverState += ResetToDefault;         
            getState("idle").onEnterState += ResetToDefault;
            getState("spin").onRecoverState += ResetToDefault;
            getState("fg-session-end").onEnterState += HandleClose;

            getState("fg-init").onExitState += () =>
            {
                UpdateSlashText();
                counterRef.SetActive(true);
                lanternRef.SetActive(true);
            };

            string[] recoverStates = new string[] {"fg-spin", "fg-end", "fg-end-panel", "fg-jp-announcement", "fg-jp-session-end", "fg-end-from-jackpot" };
            foreach (string state in recoverStates)
            {
                getState(state).onRecoverState += () =>
                {
                    UpdateSlashText();
                    counterRef.SetActive(true);
                    lanternRef.SetActive(true);
                    totalFreeGameAmount = fgDm.totalFreeGameAmount;
                    currentAmountOfFreeGame = fgDm.currentAmountOfFreeGame;
                };
            }

            currentRenderer.SetSortingOrder(SortingOrder);
            slashRenderer.SetSortingOrder(SortingOrder);
            totalRenderer.SetSortingOrder(SortingOrder);
            _isInfoReady = true;
        }

        private void HandleClose()
        {
            if(hasAutoSpin())
            {
                ResetToDefault();
            }
            else
            {
                WkCoroutine.instance.StartTrackedCoroutine(TweenDelay());
            }
        }

        private IEnumerator TweenDelay()
        {
            yield return new WaitForSeconds(2f);
            ResetToDefault();
        }

        private bool hasAutoSpin()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            return gm.configDataRecover.GetRecoverDataCasted<WkSlotConfigurationData>().hasAutoSpin;
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            UpdateCounter();         
        }

        private void UpdateCounter()
        {
            UpdateCounterText(currentRenderer, currentAmountOfFreeGame);

            if (totalFreeGameAmount != _totalFgCount)
            {
                _totalFgCount = totalFreeGameAmount;
                UpdateCounterText(totalRenderer, totalFreeGameAmount);
            }
        }

        private void UpdateCounterText(WkAdvanceRenderer renderer, int number)
        {
            if (!_isInfoReady) return;
            renderer.ClearAllDrawInfo();
            (Texture2D[], SSFrame[]) value = TryGetInfo(number);
            DrawTextInfo info = new DrawTextInfo();
            SSFrame[] frames = value.Item2;
            Texture2D[] textures = value.Item1;
            Vector2[] offset = new Vector2[frames.Length];
            Array.Fill(offset, Vector2.zero);

            info.textures = textures;
            info.frames = frames;
            info.offsets = offset;

            renderer.AddDrawInfo("Digit", info);
            renderer.Draw("UI", SortingOrder);
        }
        
        private void UpdateSlashText()
        {
            if (!_isInfoReady) return;
            _totalFgCount = 0;
            slashRenderer.ClearAllDrawInfo();
            (Texture2D, SSFrame) value =  textureInfo.GetTexture2DAndFrame("/");
            SSFrame[] frames = new SSFrame[] { value.Item2 };
            Texture2D[] textures = new Texture2D[] { value.Item1 };

            Vector2[] offset = new Vector2[frames.Length];
            Array.Fill(offset, Vector2.zero);

            DrawTextInfo info = new DrawTextInfo();
            info.textures = textures;
            info.frames = frames;
            info.offsets = offset;
            slashRenderer.AddDrawInfo("Digit", info);
            slashRenderer.Draw("UI", SortingOrder);
        }

        private ReadOnlySpan<long> GetDigits(long num)
        {
            List<long> digits = new List<long>();

            if (num == 0)
            {
                digits.Add(0);
            }
            else
            {
                // Extract digits from right to left
                while (num > 0)
                {
                    digits.Add(num % 10);
                    num /= 10;
                }

                digits.Reverse(); // Reverse to get original order
            }

            return digits.ToArray();
        }

        private (Texture2D[], SSFrame[]) TryGetInfo(long amount)
        {
            List<Texture2D> textures = new List<Texture2D>();
            List<SSFrame> frames = new List<SSFrame>();

            ReadOnlySpan<long> digits = GetDigits(amount);
            for (int i = 0; i < digits.Length; i++)
            {
                (Texture2D, SSFrame) value = textureInfo.GetTexture2DAndFrame(digits[i].ToString());
                textures.Add(value.Item1);
                frames.Add(value.Item2);
            }

            return (textures.ToArray(), frames.ToArray());
        }
    }
}