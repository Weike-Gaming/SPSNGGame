using System;
using System.Collections;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYReel : WkReel
    {
        [Header("Pot Position")]
        [SerializeField] private Transform fgCounter;
        [SerializeField] private Transform spinPot;
        [SerializeField] private Transform jackpotPot;
        [SerializeField] private Transform prizePot;

        private int _symbolScatterDone = 0;
        private int _prizeAnimationDone = 0;
        private Vector2[] _extraPrizeTargets;

        private float _animSpeed = 0;

        private int visibleIndex = 1;
        #region Reel Nudge Vars
        public int nudgeSteps { get; set; }
        public bool wantToNudgeInDefaultDir { get; set; }
        public bool playReelNudgeBorderExitAnim { get; set; }
        public bool wantToPlayNudgeAnim { get; set; }

        private bool _isReelRebuilding;

        private bool _postSpinStop2 = false;    // 'duplicate' var from base due to need for overriding CheckBoundBack() and PlayReelStopSound()
        private bool _playCustomSfx2 = false;   // 'duplicate' var from base due to need for overriding CheckBoundBack() and PlayReelStopSound()
        #endregion


        #region Binding
        public int numDummy { get; set; }
        public bool getAnimationData { get; set; }
        public bool playIngotTransformation { get; set; }
        public bool playExtraSpinAnimation { get; set; }
        public bool playExtraSpinTransformation { get; set; }
        public bool playExtraPrizeAdditionAnimation { get; set; }
        public bool playExtraPrizeAdditionTransformation { get; set; }
        public bool playExtraPrizeMultiplierAnimation { get; set; }
        public bool playExtraPrizeMultiplierTransformation { get; set; }
        public bool updateFgCheatData { get; set; }
        public bool updatePreviousIngotData { get; set; }
        public bool updateRecoverData { get; set; }
        public bool updateRecoverTransform { get; set; }
        public bool resetTransformedSymbol { get; set; }
        public bool playExtraPrizeAdditionTransformationWithoutAnimation { get; set; }
        public bool playExtraPrizeMultiplierTransformationWithoutAnimation { get; set; }
        public bool redrawSymbols { get; set; }

        public bool isLuckyBoost { get; set; }

        public bool noLuckyBoost { get; set; }
        #endregion

        #region Base Overrides

        /// <summary>
        /// Copied from WkReel but changed from using fixed "numRows+2" to "numRows+numDummy".
        /// </summary>
        protected override void InitReel()
        {
            int totalRows = numRows + numDummy;
            holder = 1f / totalRows;
            holderOffset = holder;

            symbolIndex = new int[totalRows];
            foreach (int i in symbolIndex)
                symbolIndex[i] = i;

            symbols = new GameObject[totalRows];
            int nextSymbolReelStop = ClampToReelLength(finalReelStop - numDummy / 2);
            float pos = ((totalRows + 1) / 2.0f) * containerSize; // container size  = 2.94

            if (reelManager == null)
            {
                return;
            }

            for (int i = 0; i < totalRows; i++) // instantiate (num rows + newRows) of the wkSymbol prefab
            {
                symbols[i] = Instantiate(symbolPrefab, transform, false);

                WkSymbol wkSymbol = symbols[i].GetComponent<WkSymbol>();
                WkSymbolProperty symbolProperty = new WkSymbolProperty(reelManager.symbolInfo!, mask.backSortingOrder, mask.frontSortingOrder);
                wkSymbol.InitSymbol(symbolProperty);

                Transform symbolTransform = symbols[i].GetComponent<Transform>();

                symbolTransform.localPosition = new Vector3(0, pos, 0);
                if(!_isReelRebuilding)
                {
                    wkSymbol.SetSymbol(reelStrip![nextSymbolReelStop]);
                }
                wkSymbol.SetOrder(i + 1 + reelLayerOffset);
                wkSymbol.SpinLayer();

                pos -= containerSize;
                nextSymbolReelStop = ClampToReelLength(nextSymbolReelStop + 1);
            }
            if (_isReelRebuilding)
            {
                StartCoroutine(DelaySetAllSymbols(nextSymbolReelStop));
            }
            if (isNormalGame)
            {
                reelManager.OnReelFinishInitialized();
                Debug.Log($"Init Reel {name} DONE");
            }
        }
        private IEnumerator DelaySetAllSymbols(int startIndex)
        {
            yield return null;

            int num = startIndex;
            for (int i = 0; i < symbols.Length; i++)
            {
                WkSymbol wkSymbol = symbols[i].GetComponent<WkSymbol>();
                wkSymbol.SetSymbol(reelStrip[num]);
                num = ClampToReelLength(num + 1);
            }
        }
        /// <summary>
        /// Only update reelLayerOffset
        /// </summary>
        protected override void InitReelMask()
        {
            base.InitReelMask();
            mask = GetComponentInChildren<SpriteMask>();
            if (mask is not null)
                mask.frontSortingOrder = reelLayerOffset + 9 + 2;
        }

        /// <summary>
        /// Copied from WkReel to replace the loop with use of centerRow
        /// </summary>
        protected override void PlayWinAnimation()
        {
            if (isFirstLoop && playedFirstLoopOnces)
            {
                return;
            }
            float duration = reelData!.animationLoopDuration;
            int index = 0;
            for (int i = centerRow + visibleIndex; i >= centerRow - 1; i--)
            {
                WkSymbol symbol = symbols[i].GetComponent<WkSymbol>();
                if ((winPos & (1 << index)) > 0)
                {
                    symbol.PlayAnimation(duration, isFirstLoop);
                }
                index++;
            }
        }

        /// <summary>
        /// Copied from WkReel to replace "_numRow - centerRow" with "numDummy/2"
        /// </summary>
        /// <param name="reelStop"></param>
        protected override void UpdateSymbol(int reelStop)
        {
            int nextSymbolReelStop = ClampToReelLength(reelStop - numDummy / 2);

            for (int i = 0; i < symbolIndex.Length; i++)
            {
                WkSymbol symbol = symbols[i].GetComponent<WkSymbol>();
                OnSymbolChanged(i);
                symbol.SetSymbol(reelStrip![nextSymbolReelStop]);
                symbolIndex[i] = reelStrip[nextSymbolReelStop];
                nextSymbolReelStop = ClampToReelLength(nextSymbolReelStop + 1);
            }

            SetSymbolStopLayer();
        }

        /// <summary>
        /// Update setting of "_holder"
        /// Replace "_numRow-centerRow" with "numDummy/2"
        /// </summary>
        protected override void UpdateCurrStop()
        {
            if (inBounceback)
            {
                return;
            }

            while (offset >= holder)
            {
                if (curStop == rng)
                {
                    break;
                }

                int size = symbolIndex.Length;
                holder += holderOffset;
                holder = Mathf.Round(holder);
                index = ((index - 1) % size + size) % size;
                OnSymbolChanged(index);
                curStop--;
                symbolIndex[index] = reelStrip[ClampToReelLength(curStop - (numDummy / 2))];
                RefreshSymbolImage();
            }
            return;
        }

        /// <summary>
        /// Add "&& _inBounceback" to "else if (_onHold)"
        /// </summary>
        protected override void SpinReel()
        {
            if (onHold && inBounceback && !onHoldOnce) return;
            if (!spin)
            {
                return;
            }

            if (loopReelSpin)
            {
                Loop();
                return;
            }

            if (elapstime > 0.0f && elapstime >= duration && !loopReelSpin)
            {
                OnReelStopped();
                return;
            }
            else if (onHold && !inBounceback)
            {
                return;
            }
            elapstime += Time.deltaTime;
            float alpha = elapstime / duration;
            float blendWeight = newCurve.Evaluate(alpha);
            offset = blendWeight * targetSpin;
        }

        /// <summary>
        /// Copied from base. Replace "_numRow - centerRow" with "numDummy / 2"
        /// </summary>
        protected override void HardSetReelIndex()
        {
            for (int i = 0; i < symbolIndex.Length; i++)
            {
                symbolIndex[i] = reelStrip![ClampToReelLength(curStop - (numDummy / 2) + i)];
                symbols[i].GetComponent<WkSymbol>().SetSymbol((byte)symbolIndex[i]);
            }
        }

        /// <summary>
        /// Replace for loop logic with centerRow logic
        /// </summary>
        protected override void SetSymbolStopLayer()
        {
            for (int i = centerRow - 1; i <= centerRow + visibleIndex; i++)
            {
                symbols[i].GetComponent<WkSymbol>().StopSpinLayer();
            }
        }

        protected override void OnReelStopped()
        {
            // From Base
            offset = 0;
            elapstime = 0;
            holder = holderOffset;
            index = symbols.Length;
            spin = false;
            inBounceback = false;

            curStop = rng;
            UpdateSymbol(rng);
            doOnce = false;

            for (int i = centerRow - 1; i <= centerRow + visibleIndex; i++)
            {
                symbols[i].GetComponent<WkSymbol>().StopSpinLayer();
            }

            reelManager?.OnReelStopped(reelData!);
            isPressedFastStop = false;
            holdPlayStopSnd = false;
            // End of base

            PlayIngotLandingSfx();
            PlayScatterLanding();

            PlayScatterAnimation();
        }

        /// <summary>
        /// Use base but use MCQNG values
        /// </summary>
        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            holder = 0.143f;
            index = symbols.Length;
        }

        #endregion
        protected override void UpdateUI()
        {
            _postSpinStop2 = (flags & (byte)WkReelDataFlag.PostSpinStop) == (byte)WkReelDataFlag.PostSpinStop;
            _playCustomSfx2 = (flags & (byte)WkReelDataFlag.PlayCustomSfx) == (byte)WkReelDataFlag.PlayCustomSfx;

            base.UpdateUI();
            if(isLuckyBoost)
            {
                ChangeToLuckyBoost();
            }
            if (noLuckyBoost)
            {
                ChangeToMGReel();
            }
            if (getAnimationData)
            {
                GatherAnimationData();
            }
            if (playIngotTransformation)
            {
                IngotTransform();
            }
            if (playExtraPrizeMultiplierAnimation)
            {
                PlayExtraPrizeMultiplierAnimation();
            }
            if (playExtraPrizeMultiplierTransformation)
            {
                //PlayExtraPrizeMultiplierTransformation();
            }
            if (updateFgCheatData)
            {
                UpdateFgCheatData();
            }
            if (updatePreviousIngotData)
            {
                RecoverPreviousIngotData();
            }
            if (updateRecoverData)
            {
                SetIngotInfo();
            }
            if (updateRecoverTransform)
            {
                RecoverIngotTransformValue();
            }
            if (resetTransformedSymbol)
            {
                SetSymbolTransformed(false);
            }
            if (playExtraPrizeMultiplierTransformationWithoutAnimation)
            {
                PlayExtraPrizeMultiplierTransformationWithoutAnimation();
            }
            if (redrawSymbols)
            {
                RedrawSymbols();
            }
           
            bool changeSymbol = (flags & (byte)WkReelDataFlag.HardCodeRng) == (byte)WkReelDataFlag.HardCodeRng;
            if (changeSymbol)
            {
                PlayRecoverLoopAnim();
            }
        }

        public override void Spin(int rng)
        {
            SetIngotInfo();
            base.Spin(rng);
        }

        public void RedrawSymbols()
        {
            for (int i = 0; i < symbolIndex.Length; i++)
            {
                symbolIndex[i] = reelStrip![ClampToReelLength(finalReelStop - (numDummy / 2) + i)];
                symbols[i].GetComponent<WkSymbol>().SetSymbol((byte)symbolIndex[i]);
            }
        }

        public void SetIngotInfo()
        {
            int index = 0;

            for (int row = 0; row < numRows + numDummy; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                JIXRYReelData rd = reelData.GetModelDataChecked<JIXRYReelData>();
                uint reelIngotValue = rd.reelIngotValue[index];
                reelIngotValue = reelIngotValue > 0 ? reelIngotValue : 1234;
                symbol.SetIngotValue((int)reelIngotValue);
                index++;

                if (row >= centerRow - numDummy / 2 && row <= centerRow + numDummy / 2)
                {
                    byte extraPrizeMultiplierType = rd.extraPrizeMultiplierType;
                    byte extraJackpotType = rd.extraJackpotType;

                    symbol.SetExtraPrizeMultuplierType(extraPrizeMultiplierType);
                    extraJackpotType = (byte)(extraJackpotType == 0 || extraJackpotType > 4 ? 1 : extraJackpotType); // Dummy data if not valid
                    symbol.SetExtraJackpotType(extraJackpotType);
                }
                else
                {
                    //dummy data
                    symbol.SetExtraSpinType(1);
                    symbol.SetExtraPrizeMultuplierType(2);
                    symbol.SetExtraPrizeAdditionType(10);
                    symbol.SetExtraJackpotType(1);
                }
            }
        }

        private void PlayScatterAnimation()
        {
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                symbol.PlayScatterAnim(spinPot, jackpotPot, prizePot, OnSymbolScatterComplete);
            }
        }

        /// <summary>
        /// Override to use time directly for checking inBounceback instead of getting frame value based on time.
        /// </summary>
        protected override void CheckBoundBack()
        {
            float alpha = elapstime / duration;
            float threshold = 0.95f;
            if (alpha >= threshold)
            {
                inBounceback = true;
                if (doOnce) return;
                if (!isPressedFastStop || lastReel || holdPlayStopSnd || _postSpinStop2 || _playCustomSfx2)
                {
                    PlayReelStopSound();
                    doOnce = true;
                }
            }
        }

        protected override void PlayReelStopSound()
        {
            if (IsPressedFastStop() && lastReel && !_postSpinStop2)
            {
                if (reelManager.reelManagerDataModel.customSfx.isEmpty)
                {
                    PlayDefaultReelStopSfx();
                }
                else
                {
                    string sfx = reelManager.reelManagerDataModel.customSfx.RemoveFront();
                    getActiveAudioManager?.PlayAudioUnique(sfx);
                }
            }
            else
            {
                if (!PlayCustomLandingSfx())
                {
                    PlayDefaultReelStopSfx();
                }
            }
        }
        protected override void OnFastStop()
        {
            base.OnFastStop();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelManagerDataModel rmdm = reelManager!.reelManagerDataModel.GetModelDataChecked<JIXRYReelManagerDataModel>();

            // Play SFX
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();

                // Play Scatter landing
                if (symbol.CheckBlueScatter() || symbol.CheckRedScatter() || symbol.CheckGreenScatter())
                {
                    rmdm.customSfx.Add($"ScatterLandingReel_{rmdm.numOfScatterHitInSpin}");
                    break;
                }

                // Play Ingot landing
                if (reelNo > rm.GetMaxIngotWayWin())
                    continue;
                else if (symbol.CheckNormalIngot() || symbol.CheckPrizeMultiplierIngot() || symbol.CheckJackpotIngot())
                {
                    rmdm.customSfx.Add($"IngotLandingReel_{reelNo}");
                    break;
                }
            }
        }

        private void UpdateFgCheatData()
        {
            for (int row = 0; row < symbolIndex.Length; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (row >= centerRow - 1 && row <= centerRow + visibleIndex)
                {
                    JIXRYReelData rd = reelData.GetModelDataChecked<JIXRYReelData>();

                    byte extraPrizeMultiplierType = rd.extraPrizeMultiplierType;
                    byte extraJackpotType = rd.extraJackpotType;

                    symbol.SetExtraPrizeMultuplierType(extraPrizeMultiplierType);
                    extraJackpotType = (byte)(extraJackpotType == 0 || extraJackpotType > 4 ? 1 : extraJackpotType); // Dummy data if not valid
                    symbol.SetExtraJackpotType(extraJackpotType);
                }
                else
                {
                    //dummy data
                    symbol.SetExtraSpinType(1);
                    symbol.SetExtraPrizeMultuplierType(2);
                    symbol.SetExtraPrizeAdditionType(10);
                    symbol.SetExtraJackpotType(1);
                }
            }

            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            _animSpeed = rm.GetPrizeAnimSpeed();
        }

        #region Recovery
        private void PlayRecoverLoopAnim()
        {
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                symbol.PlayLoopAnimation();
            }
        }

        public void RecoverIngotTransformValue()
        {
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckPrizeMultiplierIngot())
                {
                    symbol.IngotValueTransform((int)reelData.GetModelDataChecked<JIXRYReelData>().reelIngotValue[row]);
                    break;
                }
            }
        }

        private void RecoverPreviousIngotData()
        {
            SetIngotInfo();

            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            for (int row = 1; row < symbolIndex.Length - 1; row++)
            {
                if (reelNo <= rm.GetPreviousMaxIngotWayWin())
                {
                    JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                    symbol.SetTransformed(true);
                }
            }
        }

        private void SetSymbolTransformed(bool isTransformed)
        {
            for (int row = 1; row < symbolIndex.Length - 1; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                symbol.SetTransformed(isTransformed);
            }
        }
        #endregion

        #region Animation

        #region Scatter Complete
        private void OnSymbolScatterComplete(JIXRYSymbol symbol)
        {
            _symbolScatterDone++;

            if (_symbolScatterDone == numRows)
            {
                _symbolScatterDone = 0;
                int blueScatter = 0;
                int redScatter = 0;
                int greenScatter = 0;

                JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

                CheckPotCoinCount(out blueScatter, out redScatter, out greenScatter);
                rm.ReelScatterAnimDone(blueScatter, redScatter, greenScatter);
            }
        }

        private void CheckPotCoinCount(out int blueScatter, out int redScatter, out int greenScatter)
        {
            blueScatter = 0;
            redScatter = 0;
            greenScatter = 0;

            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckBlueScatter())
                {
                    blueScatter++;
                    break;
                }
                else if (symbol.CheckRedScatter())
                {
                    redScatter++;
                    break;
                }
                else if (symbol.CheckGreenScatter())
                {
                    greenScatter++;
                    break;
                }
            }
        }
        #endregion

        #region Extra Prize
        private void GatherAnimationData()
        {
            JIXRYReelData rd = reelData.GetModelDataChecked<JIXRYReelData>();
            byte bitmask = 0;

            int index = 0;
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckNormalIngot() || symbol.CheckPrizeMultiplierIngot())
                {
                    if (!rd.haveMultiplierIngot)
                    {
                        bitmask += (byte)(1 << index);
                    }
                    else if (!symbol.CheckPrizeMultiplierIngot())
                    {
                        bitmask += (byte)(1 << index);
                    }
                }
                index++;
            }

            rd.animationBitmask = bitmask;
        }

        private void IngotTransform()
        {
            JIXRYReelData rd = reelData.GetModelDataChecked<JIXRYReelData>();
            int reelIndex = -1;

            // Check rows in order: 1 (top), 2 (middle), 3 (bottom)
            int index = 0;
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                if ((rd.animationBitmask & (1 << index)) != 0)
                {
                    reelIndex = row;
                    break;
                }
                index++;
            }

            // Get symbol and value
            JIXRYSymbol symbol = symbols[reelIndex].GetComponent<JIXRYSymbol>();
            int value = (int)rd.reelIngotValue[reelIndex];
            symbol.IngotValueTransform(value);

            // Clear the bit for this row (mark as done)
            rd.animationBitmask = (byte)(rd.animationBitmask & ~(1 << (index)));
        }

        #region Prize Multiplier
        private void PlayExtraPrizeMultiplierAnimation()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            _extraPrizeTargets = rm.GetAnimationData().ToArray();
            _prizeAnimationDone = 0;
            DelayAction(() => PlayNextPrizeMultiplier(), 0.67f);
        }


        private void PlayNextPrizeMultiplier()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            if (_prizeAnimationDone >= _extraPrizeTargets.Length)
            {
                rm.PrizeMultiplyAnimDone();
                return;
            }

            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (row == 3)
                {
                    _animSpeed = rm.GetPrizeAnimSpeed();
                    symbol.PlayPrizeMultiplierAnim(_extraPrizeTargets[_prizeAnimationDone], _animSpeed, OnSymbolPrizeMultiplierComplete);
                    break;
                }
            }
        }

        private void OnSymbolPrizeMultiplierComplete()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.PlayIngotTransform();
            _prizeAnimationDone++;
            getActiveAudioManager.PlayAudio($"SpinIngotCounter");
            PlayNextPrizeMultiplier();
        }

        private void PlayExtraPrizeMultiplierTransformation()
        {
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (row == 3)
                {
                    symbol.StartIngotTransform((int)reelData.GetModelDataChecked<JIXRYReelData>().reelIngotValue[row]);
                    break;
                }
            }
        }
        #endregion

        #endregion

        public void DelayAction(Action callback, float delay)
        {
            WkCoroutine.instance.StartTrackedCoroutine(DelayActionCoroutine(delay, callback));
        }

        private IEnumerator DelayActionCoroutine(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
        #endregion

        #region SFX
        private void PlayIngotLandingSfx()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            int jackpotIngotIndex = int.MaxValue;
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckNormalIngot() || symbol.CheckPrizeMultiplierIngot() || symbol.CheckJackpotIngot())
                {
                    if (reelNo > rm.GetMaxIngotWayWin()) continue;
                    symbol.PlayLandingAnim();
                    if (symbol.CheckJackpotIngot())
                    {
                        jackpotIngotIndex = row;
                    }
                }
            }

            if (reelNo == 5 && rm.GetMaxIngotWayWin() == 5 && !IsPressedFastStop())
            {
                if (jackpotIngotIndex != int.MaxValue)
                {
                    WkCoroutine.instance.StartTrackedCoroutine(DelayPlayJackpotAwardAnimation(jackpotIngotIndex));
                }
                else
                {
                    WkCoroutine.instance.StartTrackedCoroutine(PlayMaxIngotAwardAudio());
                }
            }
        }

        private void PlayScatterLanding()
        {
            for (int row = centerRow + 1; row >= centerRow - 1; row--)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckScatter())
                {
                    symbol.PlayLandingAnim();
                }
            }
        }

        private IEnumerator DelayPlayJackpotAwardAnimation(int index)
        {
            yield return new WaitForSeconds(0.5f);
            JIXRYSymbol symbol = symbols[index].GetComponent<JIXRYSymbol>();
            symbol.PlayAwardAnimation();
        }

        private IEnumerator PlayMaxIngotAwardAudio()
        {
            yield return new WaitForSeconds(0.25f);
            PlayAwardSfx();
        }

        protected override bool PlayCustomLandingSfx()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            // priority play scatter sfx
            JIXRYReelManagerDataModel rmdm = reelManager.reelManagerDataModel.GetModelDataChecked<JIXRYReelManagerDataModel>();
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckBlueScatter() || symbol.CheckRedScatter() || symbol.CheckGreenScatter())
                {
                    getActiveAudioManager.PlayAudioUnique($"ScatterLandingReel_{++rmdm.scatterCount}");
                    return true;
                }

                if (reelNo > rm.GetMaxIngotWayWin())
                    continue;
                else if (symbol.CheckNormalIngot() || symbol.CheckPrizeMultiplierIngot() || symbol.CheckJackpotIngot())
                {
                    getActiveAudioManager.PlayAudioUnique($"IngotLandingReel_{reelNo}");
                    return true;
                }
            }
            return base.PlayCustomLandingSfx();
        }

        private void PlayAwardSfx()
        {
            getActiveAudioManager.PlayAudioUnique("IngotAward");
        }
        #endregion

        public override void OnPIError()
        {
            base.OnPIError();

            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckPrizeMultiplierIngot())
                {
                    symbol.StopAllTween();
                }
            }

        }

        #region History
        private void PlayExtraPrizeMultiplierTransformationWithoutAnimation()
        {
            for (int row = centerRow - 1; row <= centerRow + visibleIndex; row++)
            {
                JIXRYSymbol symbol = symbols[row].GetComponent<JIXRYSymbol>();
                if (symbol.CheckPrizeMultiplierIngot())
                {
                    int prizeMultiplierValue = (int)reelData.GetModelDataChecked<JIXRYReelData>().reelIngotValue[row - 1];
                    symbol.StartIngotTransformWithoutAnimation(prizeMultiplierValue);
                    break;
                }
            }
        }
        #endregion

        public void ChangeToLuckyBoost()
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i].GetComponent<JIXRYSymbol>().StopAnimation();
            }
            ReBuildReel();
            visibleIndex = 2;
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i].GetComponent<JIXRYSymbol>().SetSymbolSize(0.77f);
                scale.y = 1.9f;
            }
            
        } 
        public void ChangeToMGReel()
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i].GetComponent<JIXRYSymbol>().StopAnimation();
            }
            int extraIndex = symbols.Length - 1;
            if (symbols[extraIndex] != null)
            {
                WkSymbol sym = symbols[extraIndex].GetComponent<WkSymbol>();
                sym?.StopAnimation();
                Destroy(symbols[extraIndex]);
            }

            // 必须新建更短数组，复制前面有效元素，截断末尾
            int newLen = symbols.Length - 1;
            GameObject[] newSymbols = new GameObject[newLen];
            int[] newSymbolIndex = new int[newLen];
            Array.Copy(symbols, newSymbols, newLen);
            Array.Copy(symbolIndex, newSymbolIndex, newLen);

            symbols = newSymbols;
            symbolIndex = newSymbolIndex;

            // 重新调整剩下所有符号的Y位置
            float pos = ((newLen + 1) / 2.0f) * containerSize;
            for (int i = 0; i < newLen; i++)
            {
                symbols[i].transform.localPosition = new Vector3(0, pos, 0);
                pos -= containerSize;
            }
            holder = 1f / newLen;
            holderOffset = holder;


            visibleIndex = 1;
            for (int i = 0; i < symbols.Length; i++)
            {
                symbols[i].GetComponent<JIXRYSymbol>().SetSymbolSize(1f);
                scale.y = 2.5f;
            }
            GameObject[] array = symbols;
            foreach (GameObject gameObject in array)
            {
                gameObject.GetComponent<WkSymbol>().SpinLayer();
                gameObject.GetComponent<WkSymbol>().ShowSymbol();
            }
        }
        public void ClearSymbols()
        {
            if (symbols != null)
            {
                foreach (GameObject go in symbols)
                {
                    if (go != null)
                    {
                        
                       Destroy(go);
                    }
                }
                symbols = null;
            }
        }
        protected override void Update()
        {
            if (_isReelRebuilding)
                return; 
            base.Update();
        }
        public void ReBuildReel()
        {
            _isReelRebuilding = true;
            ClearSymbols();

            InitReel();
            _isReelRebuilding = false;
        }

    }
}