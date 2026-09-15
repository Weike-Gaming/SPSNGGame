using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Weike.Common;
using Weike.LobbyManagement;
using Weike.MachineInterface;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiGameManager : WkSlotCombiGameManager
    {
        /// <summary>
        /// List of Upcoming Pot Features based on scatter check's Extra___Scatter
        /// Used by CombiGM.ToggleFeatureSelected to set display possible features in CombiButton "FeatureSelcted"
        /// </summary>
        private List<JIXRYStateDataFlag> _listOfPossibleUpcomingPots = new List<JIXRYStateDataFlag>();
        private List<JIXRYStateDataFlag> _listOfPossibleUpcomingPotsPrevious = new List<JIXRYStateDataFlag>();
        
        /// <summary>
        /// Previous flag for use when there's Fg retrigger
        /// </summary>
        private byte _prevUpcomingPot;

        /// <summary>
        /// Stored versions for use in making _listOfPossibleUpcomingPots
        /// </summary>
        private byte _storedReelNudgeScatter = 0;
        private byte _storedExtraJackpotScatter = 0;
        private byte _storedReelUpgradeScatter = 0;

        /// <summary>
        /// Control RandomJackpot button display
        /// </summary>
        private int _randomJackpotType = 0;

        /// <summary>
        /// Index for list of available features
        /// </summary>
        private byte _toggleFeatureSelectedIndex = 0;

        /// <summary>
        /// Force skip MG ingot generation to allow for changing ingot values.
        /// Resets to false at end of CheckWin()
        /// </summary>
        private bool _skipIngotGeneration = false;

        private JIXRYWeightage _gameWeightageHandler;
        private bool _loadOnceWeightage = false;
        private byte _numScatter = 0;

        private byte _minReelStripSetIndex, _maxReelStripSetIndex, _reelStripSetIndex;
        private byte _minBetMultiplierIndex, _maxBetMultiplierIndex, _betMultiplierIndex;
        private byte _minPlayOptionIndex, _maxPlayOptionIndex, _playOptionIndex;
        private uint _betMultiplier, _playOption;

        private uint[] _cachedJackpotPrizes = new uint[4];

		private bool _isInFreeGame = false;

        /// <summary>
        /// Set to true to use _debugLogStr to output stuff
        /// </summary>
        private bool _enableDebugLogging = false;
		private string _debugLogStr = string.Empty;

        /// <summary>
        /// bool const for CheckIngotTrigger's isHistory param.
        /// </summary>
        private const bool isHistory = false;

        protected override Type winManagerType => typeof(JIXRYWinManager);

        public JIXRYCombiGameManager() : base()
        {
            dataModel = new JIXRYGameDataModel();
            freeGameDataModel = new JIXRYFreeGameDataModel();
            _gameWeightageHandler = new JIXRYWeightage();
        }

        #region INITIALIZATION

        /// <summary>
        /// Called by Unity when the object is enabled
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            LoadWeightage();
        }

        private void LoadWeightage()
        {
            if (_loadOnceWeightage)
            {
                return;
            }
            _loadOnceWeightage = true;
            string path;
            _gameWeightageHandler = new JIXRYWeightage();
            path = $"{WkApplication.GetGameEncryptedDataXML(gameCode)}/Weightage.xml";
            _gameWeightageHandler.ReadXML(path);
        }

        /// <summary>
        /// After InitializeJackpot(), Before game start
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public override void OnSelected()
        {
            base.OnSelected();

            WkSlotGameDataModel slotGameDataModel = dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();

            _minReelStripSetIndex = _reelStripSetIndex = 1;
            _maxReelStripSetIndex = (byte)(GetNumOfReelSetsFromXML());

            _minBetMultiplierIndex = _betMultiplierIndex = slotGameDataModel.minimumBetMultiplierProfile;
            _maxBetMultiplierIndex = (byte)(slotGameDataModel.getSelectedBetMultipliers.Length - 1);
            _betMultiplier = slotGameDataModel.getSelectedBetMultipliers[_betMultiplierIndex];

            _minPlayOptionIndex = _playOptionIndex = slotGameDataModel.minimumPlayOptionProfile;
            _maxPlayOptionIndex = (byte)(slotGameDataModel.getSelectedPlayOptions.Length - 1);
            _playOption = slotGameDataModel.getSelectedPlayOptions[_playOptionIndex];

            slotGameDataModel.selectedPlayOption = (byte)_playOptionIndex;
            slotGameDataModel.selectedBetMultiplier = (byte)_betMultiplierIndex;

            StartCoroutine(ChangeReelStrip());
            StartCoroutine(InitializeWinManager());

            UpdateHud();
        }

        private IEnumerator ChangeReelStrip()
        {
            while (reelManager is null)
                yield return new WaitForEndOfFrame();

            JIXRYGameDataModel slotGameDataModel = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            byte rtp = slotGameDataModel.variation;
            reelManager.ChangeToMgReelStrip(rtp, _playOption, _reelStripSetIndex, true);
            reelManager.HardCodeReelSymbol(predetermineRng);
        }

        private IEnumerator InitializeWinManager()
        {
            while (winManager is null)
                yield return new WaitForEndOfFrame();

            JIXRYWinManager slotWinManager = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            slotWinManager.InitWinManager(gameCode, GameInfoWrapper.EnumType.TYPE_WAYS);
        }

        public void GameSpecificOnFgSelectedSetFGReelStrip()
        {
            if (!WkAssert.EnsureMsgf(reelManager, "Reel Manager Missing in OnFGSelectedSetFGReelStrip"))
                return;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkFreeGameDataModel fgdm = freeGameDataModel as WkFreeGameDataModel ?? throw new InvalidCastException();
            reelManager!.ChangeToFgReelStrip(fgdm.fgMultiplier, dm.variation, dm.getPlayOption, _reelStripSetIndex, true);
        }

        #endregion

        #region CORE LOGIC

        /// <summary>
        /// Called in StartSpin()
        /// </summary>
        public override void CheckWin()
        {
            if (reelManager is null)
                return;
            
            UpdateBet();

            Array.Clear(_cachedJackpotPrizes, 0, _cachedJackpotPrizes.Length);

            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            // Reset data
            JIXRYWinManagerModel slotWinManagerModel = winManager.modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();
            slotWinManagerModel.winAmount = 0;
            slotWinManagerModel.totalFgWinAmount = 0;
            slotWinManagerModel.totalFgIngotWinAmount = 0;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.jackpotLevel1Prize = 0;
            dm.jackpotLevel2Prize = 0;
            dm.jackpotLevel3Prize = 0;
            dm.jackpotLevel4Prize = 0;
            dm.jackpotWinAmount = 0;
            WkFreeGameDataModel fgdm = freeGameDataModel;
            fgdm.fgJackpotLevel1Prize = 0;
            fgdm.fgJackpotLevel2Prize = 0;
            fgdm.fgJackpotLevel3Prize = 0;
            fgdm.fgJackpotLevel4Prize = 0;

            // MAIN GAME
            StopWinAnimation();
            reelManager!.GenerateSpinRng(predetermineRng);

            // Ingot generation
            if (!_skipIngotGeneration)
            {
                GenerateIngotsDigit();
            }

            // Saving dm.ingotValue for to restore free game
            uint[] temp = new uint[dm.ingotValue.Length];
            if (!_skipIngotGeneration)
            {
                for (int i = 0; i < dm.ingotValue.Length; i++)
                {
                    temp[i] = dm.ingotValue[i];
                }
                dm.ingotValue = temp.ToArray();
            }
            else
            {
                temp = dm.ingotValue.ToArray();
            }

            // "PreSpin" stuff
            UpdateReelIngotData();
            rm.HardCodeReelSymbol(predetermineRng);
            CheckIsScatter();

            // Actual check win
            MgCheckWin();

            // Debug
            if (_enableDebugLogging)
            {
                _debugLogStr = string.Empty;
                _debugLogStr += "MAIN GAME -----------------------------------------\n";
                _debugLogStr += DebugPrintReels();
                _debugLogStr += "\n---------------------------------------------------\n";
                Debug.Log(_debugLogStr);
            }

            // Save prev in case of retrigger
            _prevUpcomingPot = dm.upcomingPotFeatureGameFlag;

            // FREE GAMES (only if triggered)
            if (dm.featureGameFlag == (byte)WkFeatureGameTriggerFlag.FreeGameTrigger ||
                dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                _isInFreeGame = true;

                rm!.ChangeToFgReelStrip(fgdm.fgMultiplier, dm.variation, dm.getPlayOption, _reelStripSetIndex, true);
                InitiateFreeGame();

                while (freeGameDataModel.currentAmountOfFreeGame++ < freeGameDataModel.totalFreeGameAmount)
                {
                    // Generate stuff
                    reelManager!.GenerateSpinRng();
                    GenerateIngotsDigit(true);
                    FgGenerateExtraPrizeMultiplier();
                    FgGenerateExtraJackpotIngot();

                    // "PreSpin" stuff
                    UpdateReelIngotData(true);
                    FgCheckIsScatter();

                    // Actual check win
                    FgCheckWin();
                    CheckFgExtraJackpot();

                    // Debug
                    if (_enableDebugLogging)
                    {
                        _debugLogStr = string.Empty;
                        _debugLogStr += "FREE GAME -----------------------------------------\n";
                        _debugLogStr += DebugPrintReels();
                        _debugLogStr += "\n---------------------------------------------------\n";
                        Debug.Log(_debugLogStr);
                    }
                }

                // Ingot restore to MainGame and update display
                dm.ingotValue = temp;
                UpdateReelIngotData();
                rm!.ChangeToMgReelStrip(dm.variation, dm.getPlayOption, rm!.reelManagerDataModel.mgReelStripIndex, true);
            }

            _isInFreeGame = false;

            rm!.GenerateSpinRng(predetermineRng);
            rm.UpdateRecoverIngotData();
            rm.HardCodeReelSymbol(predetermineRng);
            MgCheckWin();

            // Restore prev in case of retrigger
            dm.upcomingPotFeatureGameFlag = _prevUpcomingPot;

            PlayWinAnimation();
            UpdateHud();

            // Reset _skipIngotGeneration
            _skipIngotGeneration = false;
        }

        public uint GetPrizeCount(int index)
        {
            return _cachedJackpotPrizes[index];
        }

        public void MgCheckWin(bool modifyWinAmount = true)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            bool isFgTriggered = dm.previousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;
            WkWinCheckingInfo info = new WkWinCheckingInfo()
            {
                betMultiplier = dm.getBetMultiplier,
                playOption = dm.getPlayOption,
                reelManagerRef = reelManager
            };
            wm.CheckReelWayWin(info, modifyWinAmount);

            if (isFgTriggered)
            {
                dm.featureGameFlag |= (byte)WkFeatureGameTriggerFlag.FreeGameTrigger;
            }
            else
            {
                dm.featureGameFlag &= (byte)~WkFeatureGameTriggerFlag.FreeGameTrigger;
            }

            if (!WkAssert.Ensure(winManager) ||
                winManager is not JIXRYWinManager JIXRYWinManager ||
                !WkAssert.EnsureMsgf(reelManager, "ReelManager is missing during check win"))
                return;

            // Isolate only visible ingots from dm.ingotValue to pass into CheckIngotTrigger
            int totalNumRows = rd[0].numRows + rd[0].numDummy;
            int startRow = rd[0].numDummy / 2;
            int endRow = totalNumRows - rd[0].numDummy / 2;
            uint[] ingotValues = new uint[rd.Length * rd[0].numRows];
            for (int reel = 0; reel < rd.Length; reel++)
            {
                for (int row = 0; row < rd[0].numRows; row++)
                {
                    int sourceIndex = reel * totalNumRows + startRow + row;
                    int destIndex = reel * rd[0].numRows + row;
                    ingotValues[destIndex] = dm.ingotValue[sourceIndex];
                }
            }

            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                reelManager,
                ingotValues,
                0,
                0,
                false,
                modifyWinAmount,
                false,
                isHistory
            );

            // Extra Prize Data
            dm.maxIngotWayWin = maxIngotWayWin;
            if (extraIngotPosition.HasFlag(JIXRYExtraIngotPosition.MultiplierIn2))
            {
                rd[2].haveMultiplierIngot = true;
            }

            // Jackpot Ingot Data
            if (maxIngotWayWin == 5)
            {
                rd[4].possibleJackpotIngotWin = true;
            }
            else
            {
                rd[4].possibleJackpotIngotWin = false;
            }

            // Update Scatter Win Statement
            bool isMainGameToFeature = dm.previousPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME && dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;
            bool isFeatureEnd = dm.previousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            if (isMainGameToFeature || isFeatureEnd)
            {
                long betValue = (long)dm.betAmount;
                string potFeature = GetGameType();
                wm.UpdateScatterStatement(reelManager, betValue, potFeature);
            }
        }

        public void FgCheckWin(bool modifyWinAmount = true)
        {
            // Reset rm nudge flags
            JIXRYReelManager rm = reelManager as JIXRYReelManager;            
            rm.haveNudge = false;
            rm.doingNudge = false;
            rm.doneNudge = false;
            rm.useDefaultSpinDir = new bool[5];
            rm.nudgeSteps = new uint[5];
            rm.nudgeDoneThisSpin = false;

            FgCheckWinIcon();

            WkSlotWinManagerModel slotWinManagerModel = winManager.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
            long iconWin = slotWinManagerModel.winAmount - slotWinManagerModel.totalFgWinAmount;

            if (rm.haveNudge)
            {
                PerformNudge();
            }

            FgCheckWinIngot();

            long ingotWin = slotWinManagerModel.winAmount - slotWinManagerModel.totalFgWinAmount - iconWin;
            long currentFreeGameWinAmount = slotWinManagerModel.winAmount - slotWinManagerModel.totalFgWinAmount;
            slotWinManagerModel.totalFgWinAmount = slotWinManagerModel.winAmount;

            Debug.Log($"subGameSequence {freeGameDataModel.currentAmountOfFreeGame}/{freeGameDataModel.totalFreeGameAmount}: " +
                $"FG current win {currentFreeGameWinAmount}, " +
                $"FG current Icon win {iconWin}, " +
                $"FG current Ingot win {ingotWin}, " +
                $"FG total win {slotWinManagerModel.winAmount}");
        }

        public void FgCheckWinIcon(bool modifyWinAmount = true)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            WkSlotWinManagerModel slotWinManagerModel = winManager.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();

            bool isFgTriggered = dm.previousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;
            uint freeGameMultiplier = 1;
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;

            if (feature.ToString().Contains("LW"))
            {
                // First call after spin
                (byte maxWayWin, _) = wm.CheckIngotTrigger(
                    reelManager,
                    fgDm.fgIngotValue,
                    fgDm.extraPrizeMultiplier,
                    fgDm.extraPrizeMultiplierIngotValue,
                    true,
                    modifyWinAmount,
                    true,
                    isHistory
                );
                dm.maxIngotWayWin = maxWayWin;
                (rm.useDefaultSpinDir, rm.nudgeSteps) = GetNudgeData(maxWayWin);
                rm.haveNudge = (maxWayWin > 1) ? true : false;
            }

            WkWinCheckingInfo info = new WkWinCheckingInfo()
            {
                betMultiplier = dm.getBetMultiplier,
                freeGameMultiplier = freeGameMultiplier,
                playOption = dm.getPlayOption,
                reelManagerRef = reelManager,
                isFreeGame = true
            };

            wm.CheckReelWayWin(info, modifyWinAmount);

            if (isFgTriggered)
            {
                dm.featureGameFlag |= (byte)WkFeatureGameTriggerFlag.FreeGameTrigger;
            }
            else
            {
                dm.featureGameFlag &= (byte)~WkFeatureGameTriggerFlag.FreeGameTrigger;
            }
        }

        /// <summary>
        /// Will need to be called in after Spin if FG is RU/RUJP/RURN/RUJPRN
        /// </summary>
        /// <param name="modifyWinAmount"></param>
        /// <exception cref="InvalidCastException"></exception>
        public void FgCheckWinIngot(bool modifyWinAmount = true)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            if (!WkAssert.Ensure(winManager) ||
                winManager is not JIXRYWinManager JIXRYWinManager ||
                !WkAssert.EnsureMsgf(reelManager, "ReelManager is missing during check win"))
                return;

            // Check if in nudge state and have nudge
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            bool isInReelNudge = feature.ToString().Contains("LW") ? true : false;
            if (isInReelNudge && !rm.nudgeDoneThisSpin)
            {
                return;
            }

            // Isolate only visible ingots from dm.ingotValue to pass into CheckIngotTrigger
            int totalNumRows = rd[0].numRows + rd[0].numDummy;
            int startRow = rd[0].numDummy / 2;
            uint[] ingotValues = new uint[rd.Length * rd[0].numRows];
            for (int reel = 0; reel < rd.Length; reel++)
            {
                int sourceIndex = reel * totalNumRows + startRow;
                int destIndex = reel * rd[0].numRows;
                for (int row = 0; row < rd[0].numRows; row++)
                {
                    ingotValues[destIndex + row] = fgDm.fgIngotValue[sourceIndex + row];
                }
            }

            // Second call after nudge or no nudge
            // Do normal ingot win check
            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                reelManager,
                ingotValues,
                fgDm.extraPrizeMultiplier,
                fgDm.extraPrizeMultiplierIngotValue,
                true,
                modifyWinAmount,
                false,
                isHistory
            );

            // Extra Prize Data
            dm.maxIngotWayWin = maxIngotWayWin;

            if (extraIngotPosition.HasFlag(JIXRYExtraIngotPosition.MultiplierIn2))
            {
                rd[2].haveMultiplierIngot = true;
            }

            // Jackpot Ingot Data
            if (maxIngotWayWin == 5)
            {
                rd[4].possibleJackpotIngotWin = true;
            }
            else
            {
                rd[4].possibleJackpotIngotWin = false;
            }
        }

        public void InitiateFreeGame()
        {
            ushort initialFreeGameCount = 8;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            freeGameDataModel.currentAmountOfFreeGame = 0;
            
            WkMachineInstance.currentSubGameSequence = 0;
            freeGameDataModel.subGameSequence = 0;           
            freeGameDataModel.totalFreeGameAmount = initialFreeGameCount;
            AddExtraFreegame(dm.upcomingPotFeatureGameFlag);
            freeGameDataModel.initialFreeGameAmount = freeGameDataModel.totalFreeGameAmount;
            freeGameDataModel.savedCurrentAmountOfFreeGame = 0;
            freeGameDataModel.savedTotalFreeGameAmount = freeGameDataModel.totalFreeGameAmount;
        }

        private void AddExtraFreegame(byte currentFeatureGame)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            ushort extraFreeGame = 0;
            byte allPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB;
            byte twoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_LWJP;
            byte onePotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_LB;

            if (currentFeatureGame == allPotFeature)
            {
                if (dm.previousPotFeatureGameFlag < twoPotFeature) //RN,JP,RU
                {
                    extraFreeGame = 8;
                }
                else if (dm.previousPotFeatureGameFlag < allPotFeature) //RUJP,RUEP,JPEP
                {
                    extraFreeGame = 4;
                }
            }
            else if (dm.previousPotFeatureGameFlag < twoPotFeature &&
                    currentFeatureGame > onePotFeature && currentFeatureGame < allPotFeature)
            {
                extraFreeGame = 4;
            }

            freeGameDataModel.totalFreeGameAmount += extraFreeGame;
        }

        #endregion

        #region REEL NUDGE
        private (bool[], uint[]) GetNudgeData(byte maxIngotWayWin)
        {
            bool[] useDefaultSpinDir = { false, false, false, false, false };
            uint[] nudgeSteps = { 0, 0, 0, 0, 0 };

            if (maxIngotWayWin < 2) return (useDefaultSpinDir, nudgeSteps);

            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            for (int reel = 0; reel < maxIngotWayWin; reel++)
            {
                // Check the 3 visible reels and set nudge steps
                int indexTop = rm.GetReelIconIndex(reel, 2) - 1;
                int indexCenter = rm.GetReelIconIndex(reel, 1) - 1;
                int indexBottom = rm.GetReelIconIndex(reel, 0) - 1;

                bool isIngotTop = rm.symbolInfo.symbolTemplate.symbols[indexTop].symbolType.Contains("INGOT");
                bool isIngotCenter = rm.symbolInfo.symbolTemplate.symbols[indexCenter].symbolType.Contains("INGOT");
                bool isIngotBottom = rm.symbolInfo.symbolTemplate.symbols[indexBottom].symbolType.Contains("INGOT");

                if (isIngotTop && isIngotCenter && isIngotBottom)
                {
                    continue;
                }

                // Set spin direction
                if (isIngotTop)
                    useDefaultSpinDir[reel] = false;
                if (isIngotBottom)
                    useDefaultSpinDir[reel] = true;

                // Set nudge steps
                if (isIngotCenter)
                    nudgeSteps[reel] = 1;
                else
                    nudgeSteps[reel] = 2;
            }

            return (useDefaultSpinDir, nudgeSteps);
        }
        #endregion

        #region Generate Probability Before Spin
        private void GenerateIngotsDigit(bool isFreeGame = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte symbolIndex = 16;
            JIXRYCoinValueRoot coinValueRoot = _gameWeightageHandler.GetCoinValueRoot(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Credit");
            uint[] temp = new uint[dm.ingotValue.Length];

            long totalWeight = coinValueRoot.totalWeightCount;

            IEnumerable<JIXRYCoinValueProbabilityRoot> coinValueProbability;
            int index = 0;
            int numOfReels = reelManager.reelData.Length;
            int numOfRows = reelManager.reelData[0].numRows + (reelManager.reelData[0] as JIXRYReelData).numDummy; // Use first reel num of rows
            for (int i = 0; i < numOfReels; i++)
            {
                for (int y = 0; y < numOfRows; y++)
                {
                    uint rng = (uint)UnityEngine.Random.Range(0,totalWeight);
                    byte reelIndex = (byte)(i + 1);
                    coinValueProbability = _gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Credit", reelIndex, symbolIndex);

                    long weight = 0;
                    foreach (JIXRYCoinValueProbabilityRoot prob in coinValueProbability)
                    {
                        weight += prob.weight;
                        if (rng < weight)
                        {
                            temp[index] = (uint)(prob.value * dm.getBetMultiplier);
                            index++;
                            break;
                        }
                    }
                }
            }

            if (isFreeGame)
            {
                fgDm.fgIngotValue = temp.ToArray();
            }
            else
            {
                dm.ingotValue = temp.ToArray();
            }

            // Update ingots
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.UpdateIngotValueData(temp);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Original Ingot");
            for (int reelNo = 0; reelNo < 5; reelNo++)
            {
                for (int rowNo = 0; rowNo < 7; rowNo++)
                {
                    sb.Append($"{temp[reelNo * rowNo + rowNo]}\t");
                }
                sb.Append($"\n");
            }
            Debug.Log(sb.ToString());
        }

        private void CheckIsScatter()
        {
            _storedReelNudgeScatter = 0;
            _storedExtraJackpotScatter = 0;
            _storedReelUpgradeScatter = 0;

            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int reelAmount = reelManager!.reelData.Length;
            for (int i = 0; i < reelAmount; i++)
            {
                int numRows = reelManager.reelData[i].numRows;
                for (int j = 0; j < numRows; j++)
                {
                    int index = reelManager.GetReelIconIndex(i, j) - 1;
                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols![index];

                    string symbolType = symbolDetail.symbolType;

                    switch (symbolType)
                    {
                        case "BLUE_SCATTER":
                            _storedReelNudgeScatter++;
                            break;
                        case "RED_SCATTER":
                            _storedExtraJackpotScatter++;
                            break;
                        case "GREEN_SCATTER":
                            _storedReelUpgradeScatter++;
                            break;
                    }
                }
            }

            UpdateListOfPossibleUpcomingPots();

            _numScatter = (byte)(_storedReelNudgeScatter + _storedExtraJackpotScatter + _storedReelUpgradeScatter);
        }

        /// <summary>
        /// Fg version of CheckIsScatter
        /// using local extra___Scatter variables instead of class variables
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        /// <exception cref="Exception"></exception>
        private void FgCheckIsScatter()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte reelNudgeScatter = 0;
            byte extraJackpotScatter = 0;
            byte reelUpgradeScatter = 0;

            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int reelAmount = reelManager!.reelData.Length;
            for (int i = 0; i < reelAmount; i++)
            {
                int numRows = reelManager.reelData[i].numRows;
                for (int j = 0; j < numRows; j++)
                {
                    int index = reelManager.GetReelIconIndex(i, j) - 1;
                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols![index];

                    string symbolType = symbolDetail.symbolType;

                    switch (symbolType)
                    {
                        case "BLUE_SCATTER":
                            reelNudgeScatter++;
                            break;
                        case "RED_SCATTER":
                            extraJackpotScatter++;
                            break;
                        case "GREEN_SCATTER":
                            reelUpgradeScatter++;
                            break;
                    }
                }
            }

            CheckTriggerPotFeature(reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter);

            _numScatter = (byte)(reelNudgeScatter + extraJackpotScatter + reelUpgradeScatter);

            if (_numScatter > 0 && dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                GenerateRandomJackpotProbability();
                RandomJackpotQualifyingThreshold();
            }
        }

        public void UpdateReelIngotData(bool isFreeGame = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.UpdateIngotValueData(isFreeGame ? fgDm.fgIngotValue : dm.ingotValue);
        }

        private void CheckTriggerPotFeature(byte reelNudgeScatter, byte extraJackpotScatter, byte reelUpgradeScatter)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB)
            {
                return;
            }

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;

            JIXRYPotFeatureRoot potFeatureRoot = _gameWeightageHandler.GetPotFeatureRoot(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "PotFeature_TriggerProb");
            IEnumerable<JIXRYPotFeatureProbabilityRoot> potFeatureProbability;

            long totalWeight = potFeatureRoot.totalWeightCount;

            long value = 0;

            uint rng = (uint)UnityEngine.Random.Range(0, totalWeight);
            potFeatureProbability = _gameWeightageHandler.GetPotFeatureProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter);

            long weight = 0;

            foreach (JIXRYPotFeatureProbabilityRoot prob in potFeatureProbability)
            {
                weight += prob.weight;
                if (rng < weight)
                {
                    value = prob.value;
                    break;
                }
            }

            SetFeatureFlag(StackFeatureValue(value));
        }

        private void GenerateRandomJackpotProbability()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            byte gameId = 0;
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte jackpotSet = machInfo.gameDef[gameId].m_byJPSet;
            byte jackpotGroup = machInfo.gameDef[gameId].m_byJPGrp;
            byte jackpotOption = machInfo.gameDef[gameId].m_byJPOpt;
            byte betMultiplier = (byte)dm.getBetMultiplier;
            uint denominationRatio = WkLobbySceneManager.instance.GetGameDenomRatioValue(gameId);

            JIXRYMysteryJackpotRoot mysteryJackpotRoot = _gameWeightageHandler.GetMysteryJackpotRoot(rtpFk, betFk, "MAIN_GAME", _reelStripSetIndex, "Mystery_Jackpot");

            long totalWeight = mysteryJackpotRoot.totalWeightCount;
            uint rng1 = (uint)UnityEngine.Random.Range(0, totalWeight);
            uint rng2 = (uint)UnityEngine.Random.Range(0, totalWeight);

            IEnumerable<JIXRYMysteryJackpotProbabilityRoot> mysteryJackpotProbability;
            mysteryJackpotProbability = _gameWeightageHandler.GetMysteryJackpotProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier, denominationRatio);

            long mappingValue = 0;
            long weight = 0;
            foreach (JIXRYMysteryJackpotProbabilityRoot prob in mysteryJackpotProbability)
            {
                weight += prob.weight * betMultiplier;
                if (rng1 < weight && rng2 < weight)
                {
                    mappingValue = prob.value;
                    break;
                }
            }

            switch (mappingValue)
            {
                case 1999:
                    dm.jackpotType = 1;
                    break;
                case 2999:
                    dm.jackpotType = 2;
                    break;
                case 3999:
                    dm.jackpotType = 3;
                    break;
                case 4999:
                    dm.jackpotType = 4;
                    break;
                default:
                    dm.jackpotType = 0;
                    break;
            }
        }

        private void RandomJackpotQualifyingThreshold()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkSlotConfigurationData config = configDataRecover as WkSlotConfigurationData ?? throw new InvalidCastException();
            ulong betAmountInCurrency = dm.betAmount * (ulong)dm.denomValue;
            bool qualifyGrand = machineContext.platformInterface.ShowProgressiveMeters() ? betAmountInCurrency >= (ulong)(config.qualifyingBet) : false;
            bool isGrandPrize = GetWinManagerChecked<WkSlotWinManager>().GetJackpotPrizeType(dm.jackpotType).Contains("GRAND");

            if (!qualifyGrand && isGrandPrize)
            {
                const byte maxGrandCounter = 3;
                dm.grandCounter = dm.grandCounter >= maxGrandCounter ? dm.grandCounter : ++dm.grandCounter;
                AwardGrandReplacement();
            }

            // If meet qualify bet
            if (qualifyGrand && dm.grandCounter > 0 && !isGrandPrize)
            {
                if (GetWinManagerChecked<WkSlotWinManager>().GetJackpotPrizeType(dm.jackpotType).Contains("MAJOR"))
                {
                    AwardReservedGrand();
                }
            }
        }

        protected void AwardReservedGrand()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.grandCounter--;
            byte prize = GetWinManagerChecked<WkSlotWinManager>().GetJackpotPrizeIndex("GRAND");
            dm.jackpotType = prize;
        }

        protected void AwardGrandReplacement()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            dm.jackpotType = wm.GetJackpotPrizeIndex("MAJOR");
        }

        private void FgGenerateExtraPrizeMultiplier()
        {
            if (!GetGameType().Contains("RU")) return;

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            byte reel3 = 3;
            byte ingotMultiplierIndex = 17;

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            uint betMultiplier = dm.getBetMultiplier;

            // Reel 3
            {
                JIXRYExtraPrizeMultiplierRoot extraPrizeMultiplierRoot = _gameWeightageHandler.GetExtraPrizeMultiplierRoot(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Multiplier", reel3, ingotMultiplierIndex);
                IEnumerable<JIXRYExtraPrizeMultiplierProbabilityRoot> extraPrizeMultiplierProbabilityRoot = _gameWeightageHandler.GetExtraPrizeMultiplierProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Multiplier", reel3, ingotMultiplierIndex);
                long totalWeight = extraPrizeMultiplierRoot.totalWeightCount;

                long weight = 0;
                uint rng = (uint)UnityEngine.Random.Range(0, totalWeight);

                foreach (JIXRYExtraPrizeMultiplierProbabilityRoot prob in extraPrizeMultiplierProbabilityRoot)
                {
                    weight += prob.weight;
                    if (rng < weight)
                    {
                        uint tempPrizeMul = fgdm.extraPrizeMultiplier;
                        tempPrizeMul = (uint)prob.value;
                        fgdm.extraPrizeMultiplier = tempPrizeMul;
                        rm.UpdateExtraPrizeMultiplierType(reel3 - 1, (byte)fgdm.extraPrizeMultiplier);
                        break;
                    }
                }

                //Generate Extra Prize Multiplier Ingot Value
                uint extraPrizeMultiplierIngotValue = GetIngotValue(reel3, ingotMultiplierIndex);
                uint tempPrizeMulValue = fgdm.extraPrizeMultiplierIngotValue;
                tempPrizeMulValue = extraPrizeMultiplierIngotValue * betMultiplier;
                fgdm.extraPrizeMultiplierIngotValue = tempPrizeMulValue;
            }
        }

        private uint GetIngotValue(byte reelNumber, byte symbolIndex)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;

            JIXRYCoinValueRoot ingotCreditRoot = _gameWeightageHandler.GetIngotCreditRoot(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Credit", reelNumber, symbolIndex);
            long totalWeight = ingotCreditRoot.totalWeightCount;

            IEnumerable<JIXRYCoinValueProbabilityRoot> ingotCreditProbabilityRoot = _gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Credit", reelNumber, symbolIndex);

            long weight = 0;
            uint rng = (uint)UnityEngine.Random.Range(0, totalWeight);

            foreach (JIXRYCoinValueProbabilityRoot prob in ingotCreditProbabilityRoot)
            {
                weight += prob.weight;
                if (rng < weight)
                {
                    return (uint)prob.value;
                }
            }
            return 0;
        }

        /// <summary>
        /// Condensed version of normal GM doing reel nudge.
        /// Adjusts rm.rmdm.rng & fgdm.fgIngotValue based on what was set by GetNudgeData().
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        private void PerformNudge()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            int totalRows = rd[0].numRows + rd[0].numDummy;
            int fgIngotValueIndex = 0;
            for (int i = 0; i < dm.maxIngotWayWin; i++)
            {
                // Get subset of fgdm that corrosponds to that reel from uint[35] into uint[7]
                uint[] reelIngotValue = new uint[totalRows];
                fgIngotValueIndex = totalRows * i;
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
                fgIngotValueIndex = totalRows * i;
                Array.Copy(reelIngotValue, 0, fgdm.fgIngotValue, fgIngotValueIndex, totalRows);
            }
        }

        private void FgGenerateExtraJackpotIngot()
        {
            if (!GetGameType().Contains("JP")) return;

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            byte gameId = 0;
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte jackpotSet = machInfo.gameDef[gameId].m_byJPSet;
            byte jackpotGroup = machInfo.gameDef[gameId].m_byJPGrp;
            byte jackpotOption = machInfo.gameDef[gameId].m_byJPOpt;
            byte betMultiplier = (byte)dm.getBetMultiplier;

            byte reel5 = 5;
            byte jackpotIngotIndex = 18;

            JIXRYExtraJackpotRoot extraJackpotRoot = _gameWeightageHandler.GetExtraJackpotRoot(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Jackpot", reel5, jackpotIngotIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier);

            long totalWeight = extraJackpotRoot.totalWeightCount;
            uint rng = (uint)UnityEngine.Random.Range(0, totalWeight);

            IEnumerable<JIXRYExtraJackpotProbabilityRoot> extraJackpotProbability;
            extraJackpotProbability = _gameWeightageHandler.GetExtraJackpotProbability(rtpFk, betFk, GetGameType(), _reelStripSetIndex, "Jackpot", reel5, jackpotIngotIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier);

            long mappingValue = 0;
            long weight = 0;
            foreach (JIXRYExtraJackpotProbabilityRoot prob in extraJackpotProbability)
            {
                weight += prob.weight;
                if (rng < weight)
                {
                    mappingValue = prob.value;
                    break;
                }
            }

            switch (mappingValue)
            {
                case 1999:
                    dm.jackpotType = 1;
                    fgdm.extraJackpotType = 1;
                    break;
                case 2999:
                    dm.jackpotType = 2;
                    fgdm.extraJackpotType = 2;
                    break;
                case 3999:
                    dm.jackpotType = 3;
                    fgdm.extraJackpotType = 3;
                    break;
                case 4999:
                    dm.jackpotType = 4;
                    fgdm.extraJackpotType = 4;
                    break;
                default:
                    dm.jackpotType = 0;
                    break;
            }
            RandomJackpotQualifyingThreshold();
            rm.UpdateExtraJackpotType(fgdm.extraJackpotType);
        }
        #endregion

        #region HELPER FUNCTIONS

        public string GetGameType()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.upcomingPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "FREE_GAME_LW";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "FREE_GAME_LB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "FREE_GAME_LWJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "FREE_GAME_LWLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "FREE_GAME_JPLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "FREE_GAME_LWJPLB";
                default:
                    return "MAIN_GAME";
            }
        }

        public long StackFeatureValue(long value)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            long stackValue = value;

            if (value == 1) //RN
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JP)
                {
                    stackValue = 12;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LB)
                {
                    stackValue = 13;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPLB)
                {
                    stackValue = 123;
                }
            }
            else if (value == 2) //JP
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LW)
                {
                    stackValue = 12;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LB)
                {
                    stackValue = 23;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWLB)
                {
                    stackValue = 123;
                }
            }
            else if (value == 3) //RU
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LW)
                {
                    stackValue = 13;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JP)
                {
                    stackValue = 23;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LWJP)
                {
                    stackValue = 123;
                }
            }
            else if (value == 12) //RUJP
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LB)
                {
                    stackValue = 123;
                }
            }
            else if (value == 13) //RNRU
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JP)
                {
                    stackValue = 123;
                }
            }
            else if (value == 23) //JPRU
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_LW)
                {
                    stackValue = 123;
                }
            }

            return stackValue;
        }

        public void SetFeatureFlag(long value)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (value)
            {
                case 1:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_LW;
                    break;
                case 2:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_JP;
                    break;
                case 3:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_LB;
                    break;
                case 12:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_LWJP;
                    break;
                case 13:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_LWLB;
                    break;
                case 23:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_JPLB;
                    break;
                case 123:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB;
                    break;
                default:
                    break;
            }
        }

        private byte GetVariationIndex()
        {
            byte rtpIndex = 1;
            if (dataModel is WkSlotGameDataModel slotGameDataModel)
            {
                rtpIndex = (byte)(slotGameDataModel.variation + 1);
            }

            return rtpIndex;
        }

        public string GetCurrentGameType()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.potFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "FREE_GAME_LW";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "FREE_GAME_LB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "FREE_GAME_LWJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "FREE_GAME_LWLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "FREE_GAME_JPLB";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "FREE_GAME_LWJPLB";
                default:
                    return "MAIN_GAME";
            }
        }

        #endregion

        #region HELPER FUNCTIONS - EXTERNAL USE (eg: buttons)

        public void SetSkipIngotGeneration()
        {
            _skipIngotGeneration = true;
        }

        public uint GetBetMultiplier()
        {
            WkSlotGameDataModel slotGameDataModel = dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            return _betMultiplier = slotGameDataModel.getSelectedBetMultipliers[_betMultiplierIndex];
        }

        public void ToggleBetMultiplier()
        {
            WkSlotGameDataModel slotGameDataModel = dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            ++_betMultiplierIndex;
            if (_betMultiplierIndex > _maxBetMultiplierIndex)
            {
                _betMultiplierIndex = _minBetMultiplierIndex;
            }

            slotGameDataModel.selectedBetMultiplier = (byte)_betMultiplierIndex;
            _betMultiplier = slotGameDataModel.getSelectedBetMultipliers[_betMultiplierIndex];
            slotGameDataModel.betAmount = slotGameDataModel.getPlayOption * slotGameDataModel.getBetMultiplier;
            UpdateHud();
        }

        public uint GetPlayOption()
        {
            return _playOption;
        }

        public void TogglePlayOption()
        {
            WkSlotGameDataModel slotGameDataModel = dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            ++_playOptionIndex;
            if (_playOptionIndex > _maxPlayOptionIndex)
                _playOptionIndex = _minPlayOptionIndex;

            _playOption = slotGameDataModel.getSelectedPlayOptions[_playOptionIndex];
            UpdateHud();
        }

        #region FEATURE SELECTED

        /// <summary>
        /// Get dm.upcomingPotFeatureGameFlag
        /// </summary>
        /// <returns>JIXRYStateDataFlag upcomingPotFeatureGameFlag</returns>
        /// <exception cref="InvalidCastException"></exception>
        public JIXRYStateDataFlag GetFeatureFlag()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            return (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;
        }

        /// <summary>
        /// Update _listOfPossibleUpcomingPots.
        /// Called by CheckScatter(), which is called in CheckWin().
        /// </summary>
        public void UpdateListOfPossibleUpcomingPots()
        {
            if (_listOfPossibleUpcomingPotsPrevious != _listOfPossibleUpcomingPots)
            {
                _listOfPossibleUpcomingPotsPrevious = _listOfPossibleUpcomingPots;
                _toggleFeatureSelectedIndex = 0;
            }

            _listOfPossibleUpcomingPots.Clear();

            // Add MainGame to the list as it's always an option
            _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.MAIN_GAME);

            // Check for additional Flags to add to the list
            if (_storedReelNudgeScatter > 0 && _storedExtraJackpotScatter > 0 && _storedReelUpgradeScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_LWJPLB);
            if (_storedReelNudgeScatter > 0 && _storedExtraJackpotScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_LWJP);
            if (_storedExtraJackpotScatter > 0 && _storedReelUpgradeScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_JPLB);
            if (_storedReelNudgeScatter > 0 && _storedReelUpgradeScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_LWLB);
            if (_storedReelNudgeScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_LW);
            if (_storedExtraJackpotScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_JP);
            if (_storedReelUpgradeScatter > 0)
                _listOfPossibleUpcomingPots.Add(JIXRYStateDataFlag.FREE_GAME_LB);
        }

        /// <summary>
        /// Cycle through _listOfPossibleUpcomingPots.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void ToggleFeatureSelected()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            if (_randomJackpotType != 0 && dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;
                _toggleFeatureSelectedIndex = 0;
            }
            else
            {
                // Cycle through possible indices
                _toggleFeatureSelectedIndex = (++_toggleFeatureSelectedIndex < (byte)(_listOfPossibleUpcomingPots.Count)) ?
                    _toggleFeatureSelectedIndex : (byte)0;

                // Set upcoming flag
                if (_listOfPossibleUpcomingPots.Count > 0)
                    dm.upcomingPotFeatureGameFlag = (byte)_listOfPossibleUpcomingPots[_toggleFeatureSelectedIndex];
                else
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;

                string _debugLogStr = $"DEBUG LOG: ToggleFeatureSelected -- " +
                    $"Index is \"{GetGameType(_toggleFeatureSelectedIndex)}\" and is out of " +
                    $"UpcomingFeature(s): ";
                foreach(byte flag in _listOfPossibleUpcomingPots)
                    _debugLogStr += $" {GetGameType(flag)}";
                Debug.Log(_debugLogStr);
                ToggleRandomJackpot(true);
            }
        }

        /// <summary>
        /// Set upcomingPotFeatureFlag to 0 (aka MAIN_GAME).
        /// Used by CombiButtonDO.ResetToDeafult()
        /// </summary>
        public void ResetUpcomingFeatureFlag()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel;
            dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;
        }

        #endregion

        #region RANDOM JACKPOT

        public int GetRandomPrizeType()
        {
            return _randomJackpotType;
        }

        /// <summary>
        /// Cycle through TRUE/FALSE for enabling RandomJackpot.
        /// Additional logic to only allow switching in MAIN_GAME
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        /// <param name="disable">Disable jackpot</param>
        public void ToggleRandomJackpot(bool disable = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (disable)
            {
                _randomJackpotType = 0;
                dm.jackpotType = 0;
                return;
            }

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME &&
                (_storedReelNudgeScatter > 0 ||
                _storedExtraJackpotScatter > 0 ||
                _storedReelUpgradeScatter > 0) )
            {
                _randomJackpotType++;
                _randomJackpotType %= 3;
                dm.jackpotType = (byte)_randomJackpotType;
            }                
            else
            {
                _randomJackpotType = 0;
            }
        }

        #endregion

        public void UpdateHud()
        {
            playerController?.hud?.OnSelected();
        }

        /// <summary>
        /// Called by Reel Buttons to reset data of buttons and statistics
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void ResetBottomRowButtons()
        {
            _randomJackpotType = 0;
            _toggleFeatureSelectedIndex = 0;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.jackpotType = (byte)_randomJackpotType;
            dm.upcomingPotFeatureGameFlag = dm.featureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;
        }


        /// <summary>
        /// Convert flag to string
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public string GetGameType(byte flag)
        {
            switch (flag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_LW:
                    return "Green Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LB:
                    return "Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJP:
                    return "Green + Red Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWLB:
                    return "Green + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPLB:
                    return "Red + Purple Pot";
                case (byte)JIXRYStateDataFlag.FREE_GAME_LWJPLB:
                    return "Green + Red + Purple Pot";
                default:
                    return "MAIN_GAME";
            }
        }

        /// <summary>
        /// Gets list of possible ingot values from XML file
        /// Note: Careful with what the reelIndex start val is. XML is 1.
        /// </summary>
        /// <param name="reelIndex"></param>
        /// <returns>List of possible ingot values</returns>
        /// <exception cref="InvalidCastException"></exception>
        public List<long> GetIngotValuesFromXML(byte reelIndex)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            IEnumerable<JIXRYCoinValueProbabilityRoot> coinValueProbability;
            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte symbolIndex = 16;
            JIXRYCoinValueRoot coinValueRoot = _gameWeightageHandler.GetCoinValueRoot(rtpFk, betFk, "MAIN_GAME", _reelStripSetIndex, "Credit");
            uint[] temp = new uint[dm.ingotValue.Length];
            coinValueProbability = _gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, "MAIN_GAME", _reelStripSetIndex, "Credit", reelIndex, symbolIndex);

            List<long> ingotValues = new List<long>();
            foreach(JIXRYCoinValueProbabilityRoot prob in coinValueProbability)
            {
                ingotValues.Add(prob.value * dm.getBetMultiplier);
            }
            return ingotValues;
        }

        public void ToggleReelStripSet()
        {
            ++_reelStripSetIndex;
            if (_reelStripSetIndex > _maxReelStripSetIndex)
                _reelStripSetIndex = _minReelStripSetIndex;
            StartCoroutine(ChangeReelStrip());
            UpdateHud();
        }

        public byte GetReelStripSetIndex()
        {
            return _reelStripSetIndex;
        }

        public byte GetNumOfReelSetsFromXML()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            IEnumerable<JIXRYReelSetRoot> tmp;
            tmp = _gameWeightageHandler.GetReelSets(rtpFk, betFk, "MAIN_GAME");
            return (byte)tmp.Count();
        }

        #endregion

        #region CHECK WIN

        public void CheckFgExtraJackpot()
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            int reel5 = 4;
            int numRows = reelManager!.reelData[reel5].numRows;
            bool hasJackpotIngot = false;

            if (dm.maxIngotWayWin < 5)
            {
                return;
            }

            for (int i = 0; i < numRows; i++)
            {
                int index = reelManager.GetReelIconIndex(reel5, i) - 1;
                WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols![index];
                string symbolType = symbolDetail.symbolType;

                if (symbolType == "INGOT_JACKPOT")
                {
                    hasJackpotIngot = true;
                    break;
                }
            }

            if (hasJackpotIngot)
            {
                _cachedJackpotPrizes[dm.jackpotType - 1]++;
            }
        }
        #endregion

        #region DEBUG LOGGING

        /// <summary>
        /// Debug.Log FG reels for debugging.
        /// </summary>
        private string DebugPrintReels()
        {
            StringBuilder sb = new StringBuilder();

            for (int row = 2; row >= 0; row--) // rows (top to bottom)
            {
                for (int reel = 0; reel < 5; reel++) // reels left to right
                {
                    sb.Append(DebugGetSymbolName(reel, row, reelManager.GetReelIconIndex(reel, row)));
                    if (reel < 4) sb.Append(" | ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string DebugGetSymbolName(int reel, int row, int id)
        {
            string tmp = string.Empty;
            switch (id)
            {
                case 1:
                    tmp = "9";
                    break;
                case 2:
                    tmp = "10";
                    break;
                case 3:
                    tmp = "Jack";
                    break;
                case 4:
                    tmp = "Queen";
                    break;
                case 5:
                    tmp = "King";
                    break;
                case 6:
                    tmp = "Ace";
                    break;
                case 7:
                    tmp = "Flag";
                    break;
                case 8:
                    tmp = "Hat";
                    break;
                case 9:
                    tmp = "Green";
                    break;
                case 10:
                    tmp = "Scroll";
                    break;
                case 11:
                    tmp = "Throne";
                    break;
                case 12:
                    tmp = "RN Coin";
                    break;
                case 13:
                    tmp = "JP Coin";
                    break;
                case 14:
                    tmp = "RU Coin";
                    break;
                case 15:
                    tmp = "Elephant";
                    break;
                case 16:
                    tmp = $"Ingot {DebugGetIngotValue(reel, row)}";
                    break;
                case 17:
                    tmp = $"Ingot x{DebugGetMultiplier()}";
                    break;
                case 18:
                    tmp = "Ingot JP";
                    break;
                default:
                    tmp = "ERROR";
                    break;
            }
            int pad = 20;
            pad -= tmp.Length;
            tmp = pad > 0 ? tmp.PadRight(pad) : tmp;
            return tmp;
        }

        private string DebugGetIngotValue(int reel, int row)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel;
            JIXRYFreeGameDataModel fgdm = freeGameDataModel as JIXRYFreeGameDataModel;
            JIXRYReelManager rm = reelManager as JIXRYReelManager;
            if (rm == null) return "N/A";

            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[];
            if (rd == null || rd.Length == 0) return "N/A";

            int totalRows = rd[0].numRows + rd[0].numDummy;
            int startRow = rd[0].numDummy / 2;

            // Index in the flattened ingot array (includes dummy rows)
            int index = reel * totalRows + startRow + row;

            if (_isInFreeGame)
            {
                if (fgdm != null && index >= 0 && index < fgdm.fgIngotValue.Length)
                    return fgdm.fgIngotValue[index].ToString();
            }
            else
            {
                if (dm != null && index >= 0 && index < dm.ingotValue.Length)
                    return dm.ingotValue[index].ToString();
            }
            return "0";
        }

        private string DebugGetMultiplier()
        {
            return freeGameDataModel.fgMultiplier.ToString();
        }
        #endregion
    }
}
