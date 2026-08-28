using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYReelManager : WkReelManager
    {
        public bool haveNudge = false, doingNudge = false, doneNudge = false;
        public bool[] useDefaultSpinDir = new bool[5];
        public uint[] nudgeSteps = new uint[5];
        public bool nudgeDoneThisSpin = false;
        // also, using 3 instead of 2 for testing

        private bool _shouldSpeedUp = false;

        public JIXRYReelManager() : base()
        {
            reelData = new JIXRYReelData[]
            {
                new JIXRYReelData(),
                new JIXRYReelData(),
                new JIXRYReelData(),
                new JIXRYReelData(),
                new JIXRYReelData()
            };
            reelManagerDataModel = new JIXRYReelManagerDataModel();
        }

        public void TriggerPreSpinAnimation()
        {
            foreach (WkReelData r in reelData)
            {
                r.loopReelSpin = true;
            }
        }

        public void FinishPreSpinAnimation(float delay = 0)
        {
            WkCoroutine.instance.StartTrackedCoroutine(StopReelSpinOneByOne(delay));
        }

        private IEnumerator StopReelSpinOneByOne(float delay)
        {
            // delay after small ingot anim
            yield return new WaitForSeconds(2f);

            int n = reelData.Length;
            for (int i = 0; i < n; i++)
            {
                reelData[i].flags &= (byte)~WkReelDataFlag.WantToSpin;
                reelData[i].flags |= (byte)WkReelDataFlag.PostSpinStop;
                yield return new WaitForSeconds(delay);
            }

            yield return null;
        }

        public override void ChangeToFgReelStrip(int fgAmount, byte rtp, uint playOption, uint reelStripIndex = 255, bool isCombi = false)
        {
            string gameType = string.Empty;

            // handle different game manager type
            JIXRYGameManager gm = _gameManager as JIXRYGameManager;
            JIXRYHistoryGameManager gmHist = _gameManager as JIXRYHistoryGameManager;
            JIXRYCombiGameManager gmCombi = _gameManager as JIXRYCombiGameManager;
            if (gm is not null)
                gameType = gm.GetUpcomingGameType();
            if (gmHist is not null)
            {
                // additional handling for historyGM if there's retrigger
                gameType = gmHist.GetGameType();
                if (gameType != gmHist.GetPrevGameType() && gmHist.GetPrevGameType() != "MAIN_GAME")
                    // use previous game type for reel strip selection
                    gameType = gmHist.GetPrevGameType();
            }
            if (gmCombi is not null)
                gameType = gmCombi.GetGameType();

            if (reelStripIndex != 0xff)
            {
                reelManagerDataModel.fgReelStripIndex = (byte)reelStripIndex;
                SetReelStrip(rtp, gameType, playOption, reelStripIndex, isCombi);
                return;
            }

            reelManagerDataModel.fgReelStripIndex = (byte)GenerateRandomReelStripIndex(gameType, rtp, playOption);
            SetReelStrip(rtp, gameType, playOption, reelManagerDataModel.fgReelStripIndex, isCombi);
        }

        /// <summary>
        /// Enable/Disable rd.wantToPlayNudgeAnim
        /// </summary>
        /// <param name="allow"></param>
        public void AllowNudgeAnimation(bool allow)
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[];
            int n = reelData.Length;
            for (int i = 0; i < n; i++)
                rd[i].wantToPlayNudgeAnim = allow;
        }

        #region Simulation
        private bool PerformImmediateNudge()
        {
            if (!WkGameInstance.instance!.isSimulation)
                return false;

            JIXRYSimulationGameManager? gm = _gameManager as JIXRYSimulationGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = gm.freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            int totalRows = rd[0].numRows + rd[0].numDummy;
            int fgIngotValueIndex = 0;
            uint[] reelIngotValue = new uint[totalRows];
            for (int i = 0; i < dm.maxIngotWayWin; i++)
            {
                // Get subset of fgdm that corrosponds to that reel from uint[35] into uint[7]
                for (int j = 0; j < totalRows; j++)
                {
                    reelIngotValue[j] = fgdm.fgIngotValue[fgIngotValueIndex++];
                }

                if (rm.useDefaultSpinDir[i])
                {
                    // Nudge RNG
                    rm.reelManagerDataModel.rng[i] += rm.nudgeSteps[i];

                    // Nudge Ingot Values
                    while (rm.nudgeSteps[i]-- > 0)
                    {
                        // Store first element
                        uint firstElement = reelIngotValue[0];

                        // Shift elements to the left
                        Array.Copy(reelIngotValue, 1, reelIngotValue, 0, reelIngotValue.Length - 1);

                        // Place the first element at the end
                        reelIngotValue[reelIngotValue.Length - 1] = firstElement;
                    }
                }
                else
                {
                    rm.reelManagerDataModel.rng[i] -= rm.nudgeSteps[i];
                    while (rm.nudgeSteps[i]-- > 0)
                    {
                        // Same as above, but shift right
                        uint lastElement = reelIngotValue[reelIngotValue.Length - 1];
                        Array.Copy(reelIngotValue, 0, reelIngotValue, 1, reelIngotValue.Length - 1);
                        reelIngotValue[0] = lastElement;
                    }
                }

                // Update gm.dm.ingotValue
                Array.Copy(reelIngotValue, 0, fgdm.fgTempIngotValue, fgIngotValueIndex, totalRows);
            }

            return true;
        }
        #endregion

        public void SpinReelsWithIndividualControl(bool[] useDefaultSpinDir, uint[] nudgeSteps)
        {
            if (PerformImmediateNudge()) return;

            if (reelManagerDataModel.reelSet is not { } reelSet)
            {
                Debug.LogWarning("Reel set is not available for reverse spin");
                return;
            }

            WkSlotMainGameManager? gm = _gameManager as WkSlotMainGameManager;
            gm!.StopWinAnimation();

            if (reelSpinning > 0)
            {
                Debug.LogWarning("Cannot spin while reels are still spinning");
                return;
            }

            int n = reelData.Length;

            // Validate input arrays
            if (useDefaultSpinDir != null && useDefaultSpinDir.Length != n)
            {
                Debug.LogWarning($"reverseSpinDirections array length ({useDefaultSpinDir.Length}) doesn't match reel count ({n})");
                return;
            }

            if (nudgeSteps != null && nudgeSteps.Length != n)
            {
                Debug.LogWarning($"targetRngs array length ({nudgeSteps.Length}) doesn't match reel count ({n})");
                return;
            }

            // Save current RNG as previous
            uint[] currentRngCopy = new uint[n];
            currentRngCopy = reelManagerDataModel.rng.ToArray();
            reelManagerDataModel.prevRng = currentRngCopy.ToArray();

            List<byte[]> list = reelSet.reelStrips.ToList();
            uint[] newRng = new uint[n];

            Debug.Log($"Before spin - Current RNG: [{string.Join(",", currentRngCopy)}]");

            JIXRYReelData[] rd = reelData as JIXRYReelData[];
            // Calculate new RNG values based on parameters
            for (int i = 0; i < n; i++)
            {
                byte[] reelStrip = list[i];
                int newStop;

                // Nudge rng by nudgeSteps
                if (useDefaultSpinDir![i])
                {
                    newStop = (int)currentRngCopy[i] + (int)nudgeSteps![i];
                }
                else
                {
                    newStop = (int)currentRngCopy[i] - (int)nudgeSteps![i];
                }

                // Wrap around reel strip length
                newStop = (newStop % reelStrip!.Length + reelStrip!.Length) % reelStrip!.Length;
                newRng[i] = (uint)newStop;

                // Update reel data
                reelData[i].finalReelStop = (int)newStop;
                rd[i].nudgeSteps = (int)nudgeSteps[i];
                rd[i].wantToNudgeInDefaultDir = useDefaultSpinDir[i];

                // Clear WantToSpin flag for nudges
                reelData[i].flags &= (byte)~WkReelDataFlag.WantToSpin;

                // Trigger JIXRYReel.UpdateUI to play nudge anim
                AllowNudgeAnimation(true);

                Debug.Log($"Reel {i}: {currentRngCopy[i]} -> {newStop}, Reverse: {useDefaultSpinDir[i]}");
            }

            // Update the RNG in data model
            reelManagerDataModel.rng = newRng;
            reelSpinning = 0; // Start at 0, will be incremented for reels that actually nudge
            // Instead of using spin animation, we'll use hard-coded RNG update for nudges
            bool[] hasNudgeMovement = new bool[n];

            for (int i = 0; i < n; i++)
            {
                if (rd[i].nudgeSteps > 0)
                {
                    hasNudgeMovement[i] = true;
                    // Ensure that WantToSpin is false
                    reelData[i].flags &= (byte)~WkReelDataFlag.WantToSpin;
                    reelSpinning++; // Increment spinning count for reels that will nudge
                }
                else
                {
                    // If not nudging, just update the symbol position immediately
                    reelData[i].flags &= (byte)~WkReelDataFlag.WantToSpin;
                    hasNudgeMovement[i] = false;
                }
            }

            Debug.Log($"After nudge - New RNG: [{string.Join(",", newRng)}]");

            haveNudge = true;
            doingNudge = true;
        }

        public override void OnReelStopped(WkReelData wkReelData)
        {
            base.OnReelStopped(wkReelData);

            JIXRYReelData reel = wkReelData as JIXRYReelData;

            // Reset Nudge Flags
            reel.wantToNudgeInDefaultDir = true;
            reel.nudgeSteps = 0;

            if (reelSpinning <= 0)
            {
                if (doingNudge && !doneNudge)
                {
                    doneNudge = true;
                }
            }
        }

        #region Reel Nudge
        public void NudgeIngotValues(WkReel wkReel)
        {
            JIXRYReel reel = wkReel as JIXRYReel;
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            if (reel.nudgeSteps == 0) return;

            int nudgeCounter = reel.nudgeSteps;
            int reelNo = reel.reelNo - 1; // -1 to account for 0-indexed array

            if (reel.wantToNudgeInDefaultDir)
            {
                while (nudgeCounter-- > 0)
                {
                    // Store first element
                    uint firstElement = rd[reelNo].reelIngotValue[0];

                    // Shift elements to the left
                    Array.Copy(rd[reelNo].reelIngotValue, 1, rd[reelNo].reelIngotValue, 0, rd[reelNo].reelIngotValue.Length - 1);

                    // Place the first element at the end
                    rd[reelNo].reelIngotValue[rd[reelNo].reelIngotValue.Length - 1] = firstElement;
                }
            }
            else
            {
                while (nudgeCounter-- > 0)
                {
                    // Same as above, but shift right
                    uint lastElement = rd[reelNo].reelIngotValue[rd[reelNo].reelIngotValue.Length - 1];
                    Array.Copy(rd[reelNo].reelIngotValue, 0, rd[reelNo].reelIngotValue, 1, rd[reelNo].reelIngotValue.Length - 1);
                    rd[reelNo].reelIngotValue[0] = lastElement;
                }
            }

            // Reverse reelIngotValue before updating gm.dm.ingotValue
            int numRows = rd[reelNo].numRows + rd[reelNo].numDummy;
            uint[] newValues = new uint[numRows];
            uint[] slice = rd[reelNo].reelIngotValue;
            for (int i = 0; i < numRows; i++)
            {
                newValues[i] = slice[(numRows - 1) - i];
            }

            // Update gm.dm.ingotValue
            JIXRYGameManager gm = _gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = gm.freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            int start = reelNo * numRows;

            uint[] tempArray = new uint[fgdm.fgTempIngotValue.Length];
            Array.Copy(fgdm.fgTempIngotValue, tempArray, fgdm.fgTempIngotValue.Length);
            Array.Copy(newValues, 0, tempArray, start, numRows);
            fgdm.fgTempIngotValue = tempArray.ToArray();
        }

        /// <summary>
        /// Set all reel nudge border to SetActive(false)
        /// </summary>
        public void DisableReelNudgeBorder()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[];
            for(int i = 0; i < reelData.Length; ++i)
            {
                rd[i].playReelNudgeBorderExitAnim = true;
                rd[i].playReelNudgeBorderExitAnim = false;
            }

            AllowNudgeAnimation(false);
        }

        /// <summary>
        /// Override w/o base to use centerPoint in determining symbol pos
        /// </summary>
        /// <param name="reelIndex"></param>
        /// <param name="symbolIndex"></param>
        public override void GenerateReelIconIndex(int reelIndex, uint symbolIndex)
        {
            {
                WkReelSet reelSet = reelManagerDataModel.reelSet;
                if (reelSet is null)
                {
                    return;
                }

                int n = reelData[reelIndex].numRows;
                byte[] reelStripCollection = reelSet.reelStrips.ElementAt(reelIndex);
                int centerPoint = Mathf.CeilToInt((float)n / 2);
                for (int i = 0; i < n; i++)
                {
                    int pos = (int)(symbolIndex + centerPoint - i);

                    if (pos < 0)
                    {
                        pos += reelStripCollection.Length;
                    }

                    pos = pos % reelStripCollection.Length;
                    pos = reelStripCollection[pos];
                    reelIconIndexList[reelIndex * n + i] = (byte)pos;
                }
            }
        }
        #endregion


        #region Update Data

        public void ForceUpdateRecoverTransform()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            byte maxIngotWayWin = GetPreviousMaxIngotWayWin();
            for (int reel = 0; reel < maxIngotWayWin; reel++)
            {
                rd[reel].updateRecoverTransform = true;
                rd[reel].updateRecoverTransform = false;
            }
        }

        /// <summary>
        /// Called in JIXRYReel.OnNudgeComplete to udpate ingot values after nudging.
        /// </summary>
        /// <param name="reel"></param>
        /// <exception cref="InvalidCastException"></exception>
        public void UpdateIngotValueData(ReadOnlySpan<uint> ingotValue)
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            int numRows = rd[0].numRows + rd[0].numDummy;

            uint[] newValues = new uint[numRows];
            for (int reel = 0; reel < reelData.Length; reel++)
            {
                int start = reel * numRows;
                ReadOnlySpan<uint> slice = ingotValue.Slice(start, numRows);

                // Copy in reverse order
                for (int i = 0; i < numRows; i++)
                {
                    newValues[i] = slice[(numRows - 1) - i];
                }
                Array.Copy(newValues, rd[reel].reelIngotValue, numRows);
                rd[reel].updateRecoverData = true;
                rd[reel].updateRecoverData = false;
            }
        }

        public void UpdateExtraPrizeMultiplierType(int reelIndex, byte extraPrizeMultiplierType)
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            rd[reelIndex].extraPrizeMultiplierType = extraPrizeMultiplierType;
        }

        public void UpdateExtraJackpotType(byte extraJackpotType)
        {
            int reel5 = 4;
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            rd[reel5].extraJackpotType = extraJackpotType;
        }

        public void UpdateFreeGameCheatData()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            rd[2].extraPrizeMultiplierType = 2;
            rd[4].extraJackpotType = 1;

            for (int reel = 0; reel < reelData.Length; reel++)
            {
                rd[reel].updateFgCheatData = true;
                rd[reel].updateFgCheatData = false;
            }        
        }

        public void SetShouldSpeedUp(bool speedUp)
        {
            _shouldSpeedUp = speedUp;
        }

        public float GetPrizeAnimSpeed()
        {
            if (_shouldSpeedUp)
            {
                return JIXRYGameManager.fastPrizeSpeed;
            }
            else
            {
                return JIXRYGameManager.normalPrizeSpeed;
            }
        }

        public void UpdateRecoverIngotData()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            for (int i = 0; i < reelData.Length; i++)
            {
                rd[i].updateRecoverData = true;
                rd[i].updateRecoverData = false;
            }
        }
        public override void StartSpin()
        {
            base.StartSpin();
            _shouldSpeedUp = false;
            _reelScatterDone = 0;
            doneNudge = false;
        }

        #region Recovery
        public override void RecoverPreviousRng()
        {
            base.RecoverPreviousRng();

            RecoverPreviousTransformedIngot();
        }

        public void UpdatePreviousIngotData(ReadOnlySpan<uint> previousIngotValue, uint previousExtraPrizeMultiplier, byte previousExtraJackpotType)
        {
            UpdateIngotValueData(previousIngotValue);

            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            rd[2].extraPrizeMultiplierType = (byte)previousExtraPrizeMultiplier;
            rd[4].extraJackpotType = previousExtraJackpotType;

            for (int i = 0; i < reelData.Length; i++)
            {
                rd[i].updatePreviousIngotData = true;
                rd[i].updatePreviousIngotData = false;
            }
        }

        public void ResetTransformedSymbol()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            for (int i = 0; i < reelData.Length; i++)
            {
                rd[i].resetTransformedSymbol = true;
                rd[i].resetTransformedSymbol = false;
            }
        }

        public void RecoverPreviousTransformedIngot()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            for (int i = 0; i < reelData.Length; i++)
            {
                if(i <= GetPreviousMaxIngotWayWin())
                {
                    rd[i].updateRecoverTransform = true;
                    rd[i].updateRecoverTransform = false;
                }                
            }
        }
        #endregion

        #endregion

        #region Trigger Animation
        private int _reelScatterDone = 0;

        /// <summary>
        /// Force reels to re-get their animation data.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void ForceResetAnimationBitMask(int maxIngotWayWin)
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            for (int reel = 0; reel < maxIngotWayWin; reel++)
            {
                rd[reel].getAnimationData = true;
                rd[reel].getAnimationData = false;
            }                
        }

        public void ReelScatterAnimDone(int blueScatter, int redScatter, int greenScatter)
        {
            // Prevent from being called when scene has switched
            if (!gameObject.activeInHierarchy) return;

            _reelScatterDone++;

            JIXRYGameManager gm = _gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            
            // Only update amount in main game
            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                gm.UpdatePotCoinValue(blueScatter, redScatter, greenScatter);
            }

            // Play Coin Burst
            gm.PlayCoinBurstAnim(blueScatter, redScatter, greenScatter);
            
            if (_reelScatterDone == reelData.Length)
            {
                gm.PlayFeatureTriggerCoinAnim();
            }
        }

        // Prize Addition & Multiplier
        public void GatherAnimationData(int maxIngotWayWin)
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            for (int i = 0; i < maxIngotWayWin; i++)
            {
                rd[i].getAnimationData = true;
                rd[i].getAnimationData = false;
            }

            HandlePrizeAnimation();
        }

        public void HandlePrizeAnimation()
        {
            JIXRYReelData[] rData = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            
            if (rData[2].haveMultiplierIngot)
            {
                if (rData[2].haveMultiplierIngot)
                {
                    rData[2].playExtraPrizeMultiplierAnimation = true;
                    rData[2].playExtraPrizeMultiplierAnimation = false;
                }
            }          
        }

        private readonly Vector2[,] predefinedPositions = new Vector2[5, 3]
        {
            { new Vector2(-6.68f, -2.42f), new Vector2(-6.68f, -4.92f), new Vector2(-6.68f, -7.42f) },
            { new Vector2(-3.34f, -2.42f), new Vector2(-3.34f, -4.92f), new Vector2(-3.34f, -7.42f) },
            { new Vector2(0.00f,  -2.42f), new Vector2(0.00f,  -4.92f), new Vector2(0.00f,  -7.42f) },
            { new Vector2(3.34f,  -2.42f), new Vector2(3.34f,  -4.92f), new Vector2(3.34f,  -7.42f) },
            { new Vector2(6.68f,  -2.42f), new Vector2(6.68f,  -4.92f), new Vector2(6.68f,  -7.42f) }
        };


        public List<Vector2> GetAnimationData()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            byte maxIngotWayWin = GetMaxIngotWayWin();

            List<Vector2> result = new List<Vector2>();

            for (int reel = 0; reel < maxIngotWayWin; reel++)
            { 
                byte mask = rd[reel].animationBitmask;

                for (int row = 0; row < 3; row++)
                {
                    if ((mask & (1 << row)) != 0)
                    {
                        result.Add(predefinedPositions[reel, row]);
                    }
                }
            }
            return result;
        }

        public void PlayIngotTransform()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            for (int i = 0; i < reelData.Length; i++)
            {
                if (rd[i].animationBitmask != 0)
                {
                    rd[i].playIngotTransformation = true;
                    rd[i].playIngotTransformation = false;
                    break;
                }
            }
        }

        #region History

        public void PlayPrizeMultiplierTransformationWithoutAnimation()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            byte maxIngotWayWin = GetPreviousMaxIngotWayWin();
            if (rd[2].haveMultiplierIngot && maxIngotWayWin > 2)
            {
                rd[2].playExtraPrizeMultiplierTransformationWithoutAnimation = true;
                rd[2].playExtraPrizeMultiplierTransformationWithoutAnimation = false;
            }
        }
        #endregion

        public void PlayPrizeMultiplierTransformation()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            byte maxIngotWayWin = GetMaxIngotWayWin();
            if (rd[2].haveMultiplierIngot && maxIngotWayWin > 2)
            {
                rd[2].playExtraPrizeMultiplierTransformation = true;
                rd[2].playExtraPrizeMultiplierTransformation = false;
            }
        }

        public void PrizeMultiplyAnimDone()
        {
            JIXRYGameManager gm = _gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            SetShouldSpeedUp(false);
            gm.RunFinishPrizeMultiplierCommand();
        }
        #endregion

        public override void StopAnimation()
        {
            JIXRYReelData[] rd = reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            for (int i = 0; i < reelData.Length; i++)
            {
                rd[i].playIngotTransformation = false;
                rd[i].playExtraPrizeMultiplierAnimation = false;
            }

            base.StopAnimation();
        }

        public byte GetMaxIngotWayWin()
        {
            JIXRYGameManager gm = _gameManager as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            return dm.maxIngotWayWin;
        }

        /// <summary>
        /// Gets the number of reels that won in the previous spin.
        /// </summary>
        /// <returns>dm.previousMaxWayWin</returns>
        public byte GetPreviousMaxIngotWayWin()
        {
            if (_gameManager is null) return 0;

            switch (_gameManager)
            {
                case JIXRYGameManager gm when gm.dataModel is JIXRYGameDataModel dm:
                    return dm.previousMaxWayWin;
                case JIXRYHistoryGameManager gmHist:
                    return gmHist.GetPreviousMaxIngotWayWin();
                default:
                    return 0;
            }
        }
    }
}