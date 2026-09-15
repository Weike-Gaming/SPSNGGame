using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;
using Weike.SlotCore;
using Weike.StateMachine;

namespace Weike.Games.JIXRY
{
    public class JIXRYSimulationGameManager : JIXRYGameManager
    {
        /// <summary>
        /// DEBUG LOG var to check FSM state switching.
        /// </summary>
        private string _debugLog = string.Empty;
        private string _debugLogPrev = string.Empty;

        private bool _startedInvoke = false;

        /// <summary>
        /// Updated and reset in fg-session-end.
        /// </summary>
        private ulong _currentTotalFgWin = 0;

        /// <summary>
        /// Cumulative FgIngotWinAmount for current batch of FreeGames (cause dm.TotalFgIngotWinAmount is only for 1 spin).
        /// Accumulated over fg-spins, used in fg-session-end to update dm.potWinIngot and dm.totalFgWin, reset in fg-session-end.
        /// </summary>
        private ulong _currentFgIngotWinAmount = 0;

        private const int StepsPerFrame = 200;

        public JIXRYSimulationGameManager()
        {
            dataModel = new JIXRYSimDataModel();
        }

        #region Unity Interface
        protected override void OnEnable()
        {
            if (WkGameInstance.instance!.isSimulation)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Debug.unityLogger.logEnabled = false;
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_debugLog != "")
            {
                _debugLogPrev = _debugLog;
                _debugLog = string.Empty;
            }
            else
            {
                // Breakpoint here to see where Sim gets stuck
                _debugLog = _debugLogPrev;
                IReadOnlyCollection<string> tmp = gameState.fsm.inputAtoms;
            }

            JIXRYSimDataModel dm = dataModel as JIXRYSimDataModel;
            if (!dm.isSimulationStarted)
            {
                _startedInvoke = false;
                return;
            }

            dm.simElapsedTime += Time.deltaTime;

            if (_startedInvoke) return;
            _startedInvoke = true;
            StartCoroutine(DelayNextFrame(() =>
            {
                playerController?.InvokeActions("Key_Spin");
            }));
        }

        #endregion

        public override void RegisterObject(IWkObject obj)
        {
            base.RegisterObject(obj);

            if (obj is not WkReelManager rm) return;

            StartCoroutine(TryToInitializeRtp(rm));
        }

        protected override void OnModelListPropertyChanges(object sender, PropertyChangedEventArgs args)
        {
            base.OnModelListPropertyChanges(sender, args);

            if (!(args.PropertyName.Equals("betAmount") || args.PropertyName.Equals("denomValue")))
            {
                return;
            }

            CanStartBet();
        }

        protected override void InitializedGameStates()
        {
            base.InitializedGameStates();
            gameState!.fsm!.onActiveStateUpdate += OnGameStateUpdate;

            gameState.fsm.GetAllState(out IEnumerable<IWkStateBase> states);

            foreach (IWkStateBase state in states)
            {
                state.SetDisableInvocation(true);
            }
        }

        protected override void Update()
        {
            for (int i = 0; i < StepsPerFrame; i++)
            {
                gameState?.Update(Time.deltaTime);
                GetCreditAmountFromPI();
            }         
        }

        protected override void SaveDataIfNeeded()
        {
        }

        /// <inheritdoc />
        public override void EndBet()
        {
            if (!CanEndBet()) return;

            long modelDataWinAmount = winManager!.modelData.winAmount * dataModel.denomValue;
            byte gameIndex = recoveredDataRaw?.id ?? 0;

            machineContext!.platformInterface!.OnGameEnd(gameIndex, modelDataWinAmount, 0 ,0);
        }

        private void FinishFirstPopAnimation()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishFirstPop", false);
        }

        private void OnGameStateUpdate(IWkStateBase current)
        {
            JIXRYSimDataModel dm = dataModel as JIXRYSimDataModel;
            JIXRYWinManagerModel winModel = winManager!.modelData as JIXRYWinManagerModel;

            _debugLog += $"|{current.id}|";

            switch (current.id)
            {
                case "idle":
                    if (dm.isSimulationStarted)
                    {
                        TryStartSpin();
                    }
                    break;
                case "spin":
                    ReelStopSpinning();
                    RunFinishScatterAnimationCommand();
                    break;
                case "first-pop-animation":
                    FinishFirstPopAnimation();
                    break;
                case "win-increment-non-skip":
                    gameState!.fsm!.ForceStateChange("win-increment");
                    break;
                case "win-increment":
                    UpdateMgWinAmounts(dm, 0);
                    FinishIncrementing();
                    break;
                case "take-win":
                    TakeWin();
                    break;
                case "main-game-end":
                    // Reset Ingot Win Amounts
                    winModel.totalFgIngotWinAmount = 0;
                    winModel.totalMgIngotWinAmount = 0;
                    FinishMainGameEndDelay();
                    break;
                case "jp-announcement":
                    if (dm.mgJackpotLevel1Prize > 0)
                    {
                        ++dm.potJackpotHit[0, 0]; // Grand
                        dm.potJackpotWin[0, 0] += (ulong)dm.mgJackpotLevel1Prize;
                    }
                    if (dm.mgJackpotLevel2Prize > 0)
                    {
                        ++dm.potJackpotHit[0, 1]; // Major
                        dm.potJackpotWin[0, 1] += (ulong)dm.mgJackpotLevel2Prize;
                    }
                    if (dm.mgJackpotLevel3Prize > 0)
                    {
                        ++dm.potJackpotHit[0, 2]; // Minor
                        dm.potJackpotWin[0, 2] += (ulong)dm.mgJackpotLevel3Prize;
                    }
                    if (dm.mgJackpotLevel4Prize > 0)
                    {
                        ++dm.potJackpotHit[0, 3]; // Mini
                        dm.potJackpotWin[0, 3] += (ulong)dm.mgJackpotLevel4Prize;
                    }
                    RunFinishJackpotAnnouncementCommand();
                    break;
                case "jp-session-end":
                    RunJackpotSessionEndCommand();
                    break;
                case "first-pop-animation-from-jp":
                    FinishFirstPopAnimation();
                    break;
                case "win-increment-non-skip-from-jp":
                    gameState!.fsm!.ForceStateChange("win-increment-from-jp");
                    break;
                case "win-increment-from-jp":
                    UpdateMgWinAmounts(dm, 0);
                    FinishIncrementing();
                    break;
                case "take-win-from-jp":
                    TakeWin();
                    break;
                case "fg-transition":
                    FgTransition();
                    break;
                case "fg-init":
                    dm.isInFreeGame = true;
                    dm.currentSimDataIndex = ConvertPotFeatureFlagToSimDataIndex((JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag);
                    ++dm.totalFgTriggered;
                    ++dm.potTriggered[dm.currentSimDataIndex];
                    InitiateFreeGame();
                    gameState!.AddInputAtomAndRunState("Cmd_Spin");
                    break;
                case "fg-spin":
                    ++dm.totalFreeGamePlayed;
                    ++dm.potGamesPlayed[dm.currentSimDataIndex];
                    ReelStopSpinning();
                    RunFinishScatterAnimationCommand();
                    break;
                case "fg-reel-nudge":
                    gameState!.AddInputAtomAndRunState("Cmd_FinishReelNudge");
                    break;
                case "fg-reel-nudge-end":
                    break;
                case "fg-prize-boost-multiplier":
                    // Animation of coloured ingots modifying non-colored ingots
                    RunPrizeMultiplierAnimationCmd();
                    break;
                case "fg-prize-boost-multiplier-transformation":
                    // Transforming the ingots that triggered animation
                    RunPrizeMultiplierTransformationCmd();
                    break;
                case "fg-first-pop-animation":
                    FinishFirstPopAnimation();
                    break;
                case "fg-win-increment-non-skip":
                    gameState!.fsm!.ForceStateChange("fg-win-increment");
                    break;
                case "fg-win-increment":
                    ++dm.totalFgHit;
                    ++dm.potHit[dm.currentSimDataIndex];
                    FinishIncrementing();
                    break;
                case "fg-feature-animation":
                    // Retrigger state
                    ++dm.totalFgRetriggered;
                    ++dm.potRetriggered[dm.currentSimDataIndex];
                    gameState!.fsm!.AddInputAtomAndRunState("Cmd_FinishFeatureAnimation");
                    break;
                case "fg-jp-announcement":
                    JIXRYFreeGameDataModel fgdm = freeGameDataModel as JIXRYFreeGameDataModel;
                    if (fgdm.fgJackpotLevel1Prize > 0)
                    {
                        ++dm.potJackpotHit[dm.currentSimDataIndex, 0]; // Grand
                        dm.potJackpotWin[dm.currentSimDataIndex, 0] += (ulong)fgdm.fgJackpotLevel1Prize;
                    }
                    if (fgdm.fgJackpotLevel2Prize > 0)
                    {
                        ++dm.potJackpotHit[dm.currentSimDataIndex, 1]; // Major
                        dm.potJackpotWin[dm.currentSimDataIndex, 1] += (ulong)fgdm.fgJackpotLevel2Prize;
                    }
                    if (fgdm.fgJackpotLevel3Prize > 0)
                    {
                        ++dm.potJackpotHit[dm.currentSimDataIndex, 2]; // Minor
                        dm.potJackpotWin[dm.currentSimDataIndex, 2] += (ulong)fgdm.fgJackpotLevel3Prize;
                    }
                    if (fgdm.fgJackpotLevel4Prize > 0)
                    {
                        ++dm.potJackpotHit[dm.currentSimDataIndex, 3]; // Mini
                        dm.potJackpotWin[dm.currentSimDataIndex, 3] += (ulong)fgdm.fgJackpotLevel4Prize;
                    }
                    RunFinishJackpotAnnouncementCommand();
                    break;
                case "fg-jp-session-end":
                    break;
                case "fg-prize-boost-multiplier-from-jackpot":
                    RunPrizeMultiplierAnimationCmd();
                    break;
                case "fg-prize-boost-multiplier-transformation-from-jackpot":
                    RunPrizeMultiplierTransformationCmd();
                    break;
                case "fg-first-pop-animation-from-jackpot":
                    FinishFirstPopAnimation();
                    break;
                case "fg-win-increment-non-skip-from-jackpot":
                    gameState!.fsm!.ForceStateChange("fg-win-increment-from-jackpot");
                    break;
                case "fg-win-increment-from-jackpot":
                    ++dm.totalFgHit;
                    ++dm.potHit[dm.currentSimDataIndex];
                    FinishIncrementing();
                    break;
                case "fg-feature-animation-from-jackpot":
                    ++dm.totalFgRetriggered;
                    ++dm.potRetriggered[dm.currentSimDataIndex];
                    gameState!.fsm!.AddInputAtomAndRunState("Cmd_FinishFeatureAnimation");
                    break;
                case "fg-end-from-jackpot":
                    UpdateFgWinAmounts(dm, dm.currentSimDataIndex);
                    ContinueFgSpinOrEnd();
                    break;
                case "fg-end":
                    UpdateFgWinAmounts(dm, dm.currentSimDataIndex);
                    ContinueFgSpinOrEnd();
                    break;
                case "fg-end-panel":
                    CloseFgPanel(FgPanelAction.Takewin);
                    FinishTakeWin();
                    break;
                case "fg-session-end":
                    dm.isInFreeGame = false;
                    dm.totalFgWin += _currentTotalFgWin;
                    dm.potWinTotal[dm.currentSimDataIndex] += _currentTotalFgWin;
                    dm.potWinIcon[dm.currentSimDataIndex] += _currentTotalFgWin - (ulong)winModel.scatterWinAmount - _currentFgIngotWinAmount;
                    _currentFgIngotWinAmount = 0;
                    dm.currentSimDataIndex = (byte)JIXRYStateDataFlag.MAIN_GAME;
                    UpdateSimDataModelWithNewArrayValues(dm);
                    FGSessionEnd();
                    break;
            }
        }

        public override void ContinueFgSpinOrEnd()
        {
            WkSlotWinManagerModel slotWinManagerModel = winManager!.modelData.GetModelDataChecked<WkSlotWinManagerModel>();
            if (WkAssert.Ensure(winManager))
            {
                slotWinManagerModel.totalFgWinAmount = slotWinManagerModel.winAmount;
            }
            base.ContinueFgSpinOrEnd();
        }

        protected override void HandleHitNewAnimationData()
        {
            base.HandleHitNewAnimationData();
            fgPreSpinDelay = 0;
        }

        /// <summary>
        /// Use dm.currentSimDataIndex to get save into SimDM's win amount vars & arrays.
        /// Called in any fg-end state (aka end of current fg spin)
        /// </summary>
        /// <param name="dm"></param>
        /// <param name="currentSimIndex"></param>
        private void UpdateFgWinAmounts(JIXRYSimDataModel dm, byte currentSimIndex)
        {
            JIXRYWinManager wm = winManager as JIXRYWinManager;
            JIXRYWinManagerModel wmdm = wm.modelData as JIXRYWinManagerModel;
            _currentFgIngotWinAmount += (ulong)wmdm.totalFgIngotWinAmount;
            dm.potWinIngot[currentSimIndex] += (ulong)wmdm.totalFgIngotWinAmount;

            // Reset Ingot Win Amounts
            wmdm.totalFgIngotWinAmount = 0;
        }

        /// <summary>
        /// Use dm.currentSimDataIndex to get save into SimDM's win amount vars & arrays.
        /// Called in any mg-end state (aka end of current mg spin)
        /// </summary>
        /// <param name="dm"></param>
        /// <param name="currentSimIndex"></param>
        private void UpdateMgWinAmounts(JIXRYSimDataModel dm, byte currentSimIndex)
        {
            JIXRYWinManager wm = winManager as JIXRYWinManager;
            JIXRYWinManagerModel wmdm = wm.modelData as JIXRYWinManagerModel;

            ++dm.totalMgHit;
            ++dm.potHit[currentSimIndex];

            dm.totalMgWin += winManager!.GetWinAmount();
            dm.potWinTotal[0] += winManager!.GetWinAmount();
            dm.potWinIcon[0] += winManager!.GetWinAmount() - (ulong)wmdm.totalMgIngotWinAmount;
            dm.potWinIngot[0] += (ulong)wmdm.totalMgIngotWinAmount;

            // Reset Ingot Win Amounts
            wmdm.totalMgIngotWinAmount = 0;
        }

        /// <summary>
        /// Used by the UI counters. Not important to info dumping.
        /// </summary>
        private void UpdateSimDataModelWithNewArrayValues(JIXRYSimDataModel simData)
        {
            ulong[] tmp = new ulong[8];

            tmp = simData.potTriggered;
            Array.Copy(simData.potTriggered, tmp, 8);
            tmp[0]++;
            simData.potTriggered = tmp;

            tmp = simData.potRetriggered;
            Array.Copy(simData.potRetriggered, tmp, 8);
            tmp[0]++;
            simData.potRetriggered = tmp;
        }

        public override void PlayHitNewFeatureAnimation()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            freeGameDataModel.currentAmountOfFreeGame++;
            SaveFreeGameCount();
            if (!dm.shouldPlayHitFeatureAnim)
            {
                return;
            }
            else
            {
                rm.TriggerPreSpinAnimation();
                dm.playHitFeatureAnim = true;
                dm.playHitFeatureAnim = false;
                rm.FinishPreSpinAnimation(0.3f);
                fgPreSpinDelay = 0;
                dm.shouldPlayHitFeatureAnim = false;
            }
        }

        #region HELPER

        /// <summary>
        /// Convert JIXRYStateDataFlag to SimDataModel's potFeature index
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>Val to save in dm.currentSimDataIndex</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private byte ConvertPotFeatureFlagToSimDataIndex(JIXRYStateDataFlag flag)
        {
            switch (flag)
            {
                case JIXRYStateDataFlag.MAIN_GAME:
                    return 0;
                case JIXRYStateDataFlag.FREE_GAME_LW: // Blue
                    return 1;
                case JIXRYStateDataFlag.FREE_GAME_JP: // Red
                    return 2;
                case JIXRYStateDataFlag.FREE_GAME_LB: // Green
                    return 3;
                case JIXRYStateDataFlag.FREE_GAME_LWJP: // Blue Red
                    return 4;
                case JIXRYStateDataFlag.FREE_GAME_LWLB: // Blue Green
                    return 5;
                case JIXRYStateDataFlag.FREE_GAME_JPLB: // Red Green
                    return 6;
                case JIXRYStateDataFlag.FREE_GAME_LWJPLB: // Blue Red Green
                    return 7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }

        /// <summary>
        /// Use dm.currentSimDataIndex to get named version of the index for logging
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private string ConvertSimDataIndexToText(byte index)
        {
            switch (index)
            {
                case 0: // MainGame
                    return "MAIN GAME ---------------------------------------";
                case 1: // GREEN
                    return "GREEN --------------------------------------------";
                case 2: // Red
                    return "RED ---------------------------------------------";
                case 3: // PURPLE
                    return "PURPLE -------------------------------------------";
                case 4: // GREEN Red
                    return "GREEN RED ----------------------------------------";
                case 5: // GREEN PURPLE
                    return "GREEN PURPLE --------------------------------------";
                case 6: // Red PURPLE
                    return "RED PURPLE ---------------------------------------";
                case 7: // GREEN Red PURPLE
                    return "GREEN RED PURPLE ----------------------------------";
                default:
                    return "INVALID FLAG";
            }
        }

        private ulong GetCredit(ulong amount)
        {
            return (amount * (ulong)dataModel.denomValue);
        }

        #endregion

        #region Unchanged Stuff

        public override bool CanStartBet()
        {
            if (!finishSettingMachineId || GetDataModel().betAmount <= 0)
            {
                return false;
            }

            while (!base.CanStartBet())
            {
                ulong topUpAmount = 9999 * (ulong)GetDataModel().denomValue * GetDataModel().betAmount;
                TopUp(topUpAmount);
            }

            return base.CanStartBet();
        }

        public override void StartBet()
        {
            base.StartBet();

            JIXRYSimDataModel simData = GetDataModel();
            ulong betVal = simData.betAmount * (ulong)simData.denomValue;
            ++(simData.totalMainGamePlayed);
            ++(simData.totalBetCount);
            simData.totalBet += betVal;
        }

        /// <summary>
        /// Called in fg-end, mg-end
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public override void UpdateWin()
        {
            base.UpdateWin();

            WkSlotWinManagerModel winModel = winManager!.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
            if (GetDataModel().isInFreeGame)
            {
                winModel.totalFgWinAmount -= (long)GetDataModel().singleMgWin;
                _currentTotalFgWin = (ulong)winModel.totalFgWinAmount;
            }
            else
            {
                GetDataModel().singleMgWin = (ulong)winModel.winAmount;
            }
        }

        public void ToggleSimulation()
        {
            UpdateBet();
            bool bStarted = GetDataModel().isSimulationStarted;
            GetDataModel().isSimulationStarted = !bStarted;
            uint timeStamp = WkDateTimeConvert.ToDosDateTime(DateTime.Now);
            GetDataModel().timeStart = WkDateTimeConvert.FromCtmToDateTime(timeStamp).ToString();
        }

        public void FgTransition()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishFeatureGameTransition");
        }

        public override void PlayFirstLoopAnimation()
        {

        }

        public JIXRYSimDataModel GetDataModel()
        {
            return (JIXRYSimDataModel)dataModel;
        }

        private IEnumerator TryToInitializeRtp(WkReelManager rm)
        {
            while (rm.reelManagerDataModel.reelSet is null)
            {
                yield return null;
            }

            List<WkRtpReelStrip> rtpReelStrip = rm.reelStripHandler.rtpReelStrip;
            int n = rtpReelStrip.Count;
            string[] names = new string[n];
            for (int i = 0; i < n; ++i)
            {
                names[i] = rtpReelStrip[i].rtpName;
            }

            dataModel.GetModelDataChecked<JIXRYSimDataModel>().variationNames = names;

            LoadWeightage();
        }
        private static IEnumerator DelayNextFrame(Action fn)
        {
            yield return new WaitForEndOfFrame();

            fn.Invoke();
        }

        public override bool GameFinishInitialize()
        {
            return true;
        }

        public override bool LoadGameConfiguration()
        {
            return false;
        }

        public override bool LoadGameMachineSummaryDICJ()
        {
            return false;
        }

        public override bool LoadGameMachineIdentification()
        {
            return false;
        }

        public override bool LoadGameProperty()
        {
            return false;
        }

        protected override void OnAuditStateChanges(AuditModeState prev, AuditModeState current)
        {

        }

        protected override void RecoverState()
        {
            IWkStateMachine fsm = gameState?.fsm as IWkStateMachine ?? throw new InvalidCastException();
            fsm.ForceStateChange("idle");
            ushort index = fsm.GetState<WkStateCore>(fsm.activeStateId).stateIndex;

            fsm.Recover(index);
        }

        protected override void ResetToDefault()
        {

        }

        public void DumpLog()
        {
            string pcDebugPath = $"{WkApplication.GetFrameworkXMLPath()}/PCDebugData.xml";

            XElement xmlRoot = XElement.Load(pcDebugPath);
            XElement tagPath = xmlRoot.Descendants("SimDumpPath").First();
            string path = tagPath.Value;

            Directory.CreateDirectory(path);

            DumpDataModelLogs($"{path}/{WkDateTimeConvert.ToDosDateTime(DateTime.Now)}.txt");
        }
        #endregion

        #region play led
        public override void PlayLED(JIXRYStateDataFlag potState)
        {
        }

        public override void PlayLEDJackpot()
        {
        }

        public override void PlayLEDFgEndPanel()
        {
        }
        #endregion

        private void DumpDataModelLogs(string path)
        {
            JIXRYSimDataModel simData = GetDataModel();
            StreamWriter sw = new(path);
            string _currency = WkLobbySceneManager.instance.dataModel.currency;
            WkLobbySceneManager.instance.GetCurrencyData(_currency, out WkCurrency? outCurrency);
            // Header
            {
                // totalWin is updated with totalGameWin and is only called in mg win-increment
                // finalTotalWin includes: MG + MG Random Jackpot & FG & FG Jackpot
                ulong totalMgJackpotWin = simData.potJackpotWin[0, 0] + simData.potJackpotWin[0, 1];
                ulong totalFgJackpotWin = 0;
                for (int i = 1; i < 8; ++i)
                    for (int j = 0; j < 4; ++j)
                        totalFgJackpotWin += simData.potJackpotWin[i, j];
                ulong finalTotalWin = ((simData.totalMgWin + simData.totalFgWin) * (ulong)simData.denomValue) + totalFgJackpotWin + totalMgJackpotWin;
                decimal rtp = (decimal)finalTotalWin / simData.totalBet;

                sw.WriteLine($"Game Code: {gameCode}");
                sw.WriteLine($"RTP Code: {simData.variationNames[simData.variation]}");
                sw.WriteLine($"Bet Amount: {simData.betAmount}");
                sw.WriteLine($"Denom: {simData.denomValue}");
                sw.WriteLine($"Total Bet (SGD): {outCurrency.GetSymbol()} {(simData.totalBet / 100).ToString("F2")}");
                sw.WriteLine($"Total Bet (Count): {simData.totalBetCount}");
                sw.WriteLine($"Total Win (SGD): {outCurrency.GetSymbol()} {(finalTotalWin / 100).ToString("F2")}");
                sw.WriteLine($"Total RTP: {rtp}");
            }
            // End of Header

            sw.WriteLine(sw.NewLine);

            sw.WriteLine(sw.NewLine);

            // Main Game
            {
                if (simData.totalBet <= 0) return;
                decimal mgRtp = (decimal)simData.totalMgWin * simData.denomValue / simData.totalBet;
                decimal mgHitRate = simData.totalMainGamePlayed <= 0 ?
                    0 : (decimal)simData.totalMainGamePlayed / simData.totalMgHit;

                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("MAIN GAME ---------------------------------------");
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine($"MG Total Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.totalMgWin))}");
                sw.WriteLine($"MG Total Win (Count): {simData.totalMgHit}");
                sw.WriteLine($"MG RTP: {mgRtp}");
                sw.WriteLine($"MG Total Play: {simData.totalGamePlayed}");
                sw.WriteLine($"MG Hit Rate: {mgHitRate}");
                sw.WriteLine($"Total Icons Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.potWinIcon[0]))}");
                sw.WriteLine($"Total Ingots Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.potWinIngot[0]))}");
                sw.WriteLine(sw.NewLine);
            }

            // Free Game
            {
                ulong totalPotWin = 0;
                ulong totalIconsWin = 0;
                ulong totalIngotsWin = 0;
                for (byte i = 1; i < 8; ++i)
                {
                    totalPotWin += simData.potWinTotal[i];
                    totalIconsWin += simData.potWinIcon[i];
                    totalIngotsWin += simData.potWinIngot[i];
                }

                decimal fgRtp = (decimal)simData.totalFgWin * simData.denomValue / simData.totalBet;

                decimal fgHitRate = simData.totalFgHit <= 0 ?
                    0 : (decimal)simData.totalFreeGamePlayed / simData.totalFgHit;

                // Note: For all the following, if there's retrigger, keep updating initiating featurePot
                // eg: MG trigger Blue, FG retrigger Red, all stats stay in Blue

                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("FREE GAME ---------------------------------------");
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine($"FG Total Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.totalFgWin))}");
                sw.WriteLine($"FG Total Win (Count): {simData.totalFgHit}");
                sw.WriteLine($"FG RTP: {fgRtp}");
                sw.WriteLine($"FG Total Play: {simData.totalFreeGamePlayed}");
                sw.WriteLine($"FG Hit Rate: {fgHitRate}");
                sw.WriteLine($"Total Icons Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(totalIconsWin))}");
                sw.WriteLine($"Total Ingots Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(totalIngotsWin))}");
                sw.WriteLine(sw.NewLine);

                // Free Game - Blue
                PrintFgPotWinData(sw, simData, 1);

                // Free Game - Red
                PrintFgPotWinData(sw, simData, 2);

                // Free Game - Green
                PrintFgPotWinData(sw, simData, 3);

                // Free Game - Blue Red
                PrintFgPotWinData(sw, simData, 4);

                // Free Game - Blue Green
                PrintFgPotWinData(sw, simData, 5);

                // Free Game - Red Green
                PrintFgPotWinData(sw, simData, 6);

                // Free Game - Blue Red Green
                PrintFgPotWinData(sw, simData, 7);
            }

            // Jackpot
            {
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("MAIN GAME JP ------------------------------------");
                sw.WriteLine("-------------------------------------------------");

                // MG Random Jackpot
                ulong totalMgJackpot = simData.potJackpotWin[0, 0] + simData.potJackpotWin[0, 1];
                decimal mgJackpotRtp = simData.totalBet <= 0 ?
                    0 : (decimal)totalMgJackpot / simData.totalBet;

                sw.WriteLine($"MG Random Jackpot RTP: {mgJackpotRtp}");
                sw.WriteLine($"MG Random Jackpot Win (SGD): {outCurrency.GetSymbol()} {(totalMgJackpot / 100).ToString("F2")}");
                sw.WriteLine(sw.NewLine);

                // MG Individual Jackpot
                PrintJpData(sw, simData, 0);

                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("FREE GAME JP ------------------------------------");
                sw.WriteLine("-------------------------------------------------");

                // FG Total Jackpot
                ulong totalFgJackpot = 0;
                for (int i = 1; i < 8; ++i)
                    for (int j = 0; j < 4; ++j)
                        totalFgJackpot += simData.potJackpotWin[i, j];
                decimal fgJackpotRtp = simData.totalBet <= 0 ?
                    0 : (decimal)totalFgJackpot / simData.totalBet;

                sw.WriteLine($"FG Jackpot RTP: {fgJackpotRtp}");
                sw.WriteLine($"FG Jackpot Win (SGD): {outCurrency.GetSymbol()} {(totalFgJackpot / 100).ToString("F2")}");
                sw.WriteLine(sw.NewLine);

                // Blue Jackpot
                PrintJpData(sw, simData, 1);

                // Red Jackpot
                PrintJpData(sw, simData, 2);

                // Green Jackpot
                PrintJpData(sw, simData, 3);

                // Blue Green Jackpot
                PrintJpData(sw, simData, 4);

                // Blue Red Jackpot
                PrintJpData(sw, simData, 5);

                // Red Green Jackpot
                PrintJpData(sw, simData, 6);

                // Blue Red Green Jackpot
                PrintJpData(sw, simData, 7);
            }

            // Highest
            {
                sw.WriteLine($"Main Game Highest Win: {simData.mgHighestWin}");
                sw.WriteLine($"Free Game Highest Win: {simData.fgHighestWin}");
            }

            sw.WriteLine(sw.NewLine);

            // Performance
            {
                sw.WriteLine($"Elapsed Time: {simData.simElapsedTime}");
                sw.WriteLine($"Games per second: {simData.totalGamePlayed / simData.simElapsedTime}");
            }

            sw.Flush();
            sw.Close();
            sw.Dispose();
        }

        /// <summary>
        /// sw.WriteLine Jackpot Data using ingo in arrays
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="simData"></param>
        /// <param name="currentPotIndex"></param>
        private void PrintFgPotWinData(StreamWriter sw, JIXRYSimDataModel simData, byte currentPotIndex)
        {
            string _currency = WkLobbySceneManager.instance.dataModel.currency;
            decimal fgRtp = simData.totalBet <= 0 ?
                0 : (decimal)simData.potWinTotal[currentPotIndex] * simData.denomValue / simData.totalBet;
            decimal fgHitRate = simData.potHit[currentPotIndex] <= 0 ?
                0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potHit[currentPotIndex];
            decimal avgSpins = simData.potTriggered[currentPotIndex] <= 0 ?
                0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potTriggered[currentPotIndex];
            ulong totalFgWin = simData.potWinIcon[currentPotIndex] + simData.potWinIngot[currentPotIndex];
            ulong featureCount = simData.potGamesPlayed[currentPotIndex];
            sw.WriteLine(ConvertSimDataIndexToText(currentPotIndex));
            sw.WriteLine("-------------------------------------------------");

            sw.WriteLine($"Trigger Count: {simData.potTriggered[currentPotIndex]}");
            sw.WriteLine($"FG Total Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.potWinTotal[currentPotIndex]))}");
            sw.WriteLine($"FG Total Win (Count): {simData.potHit[currentPotIndex]}");

            sw.WriteLine($"FG RTP: {fgRtp}");

            sw.WriteLine($"FG Total Play: {simData.potGamesPlayed[currentPotIndex]}");
            sw.WriteLine($"FG Hit: {simData.potHit[currentPotIndex]}");
            sw.WriteLine($"FG Hit Rate: {fgHitRate}");

            sw.WriteLine($"Total Icons Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.potWinIcon[currentPotIndex]))}");
            sw.WriteLine($"Total Ingots Win (SGD): {WkCoreCurrencyUtils.GetCurrencyInString(_currency, GetCredit(simData.potWinIngot[currentPotIndex]))}");

            sw.WriteLine($"Total FG Retrigger: {simData.potRetriggered[currentPotIndex]}");

            sw.WriteLine($"Average Number of Free Spins: {avgSpins}");

            sw.WriteLine(sw.NewLine);
        }

        /// <summary>
        /// sw.WriteLine Jackpot Data using ingo in arrays
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="simData"></param>
        /// <param name="currentPotIndex"></param>
        private void PrintJpData(StreamWriter sw, JIXRYSimDataModel simData, byte currentPotIndex)
        {
            string _currency = WkLobbySceneManager.instance.dataModel.currency;
            WkLobbySceneManager.instance.GetCurrencyData(_currency, out WkCurrency? outCurrency);
            if (currentPotIndex == 0) // When MainGame
            {
                decimal hitrateGrand = simData.potJackpotHit[0, 0] <= 0 ?
                    0 : (decimal)simData.totalGamePlayed / simData.potJackpotHit[0, 0];
                decimal hitrateMajor = simData.potJackpotHit[0, 1] <= 0 ?
                    0 : (decimal)simData.totalGamePlayed / simData.potJackpotHit[0, 1];

                sw.WriteLine(ConvertSimDataIndexToText(0));

                ulong utmp = 0;

                sw.WriteLine($"Grand Hit: {simData.potJackpotHit[0, 0]}");
                sw.WriteLine($"Grand Hit Rate: {hitrateGrand}");
                utmp = simData.potJackpotWin[0, 0];
                sw.WriteLine($"Total Grand Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");

                sw.WriteLine($"Major Hit: {simData.potJackpotHit[0, 1]}");
                sw.WriteLine($"Major Hit Rate: {hitrateMajor}");
                utmp = simData.potJackpotWin[0, 1];
                sw.WriteLine($"Total Major Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");
            }
            else // When NOT MainGame
            {
                decimal hitrateGrand = simData.potJackpotHit[currentPotIndex, 0] <= 0 ?
                    0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potJackpotHit[currentPotIndex, 0];
                decimal hitrateMajor = simData.potJackpotHit[currentPotIndex, 1] <= 0 ?
                    0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potJackpotHit[currentPotIndex, 1];
                decimal hitrateMinor = simData.potJackpotHit[currentPotIndex, 2] <= 0 ?
                    0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potJackpotHit[currentPotIndex, 2];
                decimal hitrateMini = simData.potJackpotHit[currentPotIndex, 3] <= 0 ?
                    0 : (decimal)simData.potGamesPlayed[currentPotIndex] / simData.potJackpotHit[currentPotIndex, 3];

                //dm.totalGameWin += (ulong)(fgdm.fgJackpotLevel3Prize / GetDataModel().denomValue);

                ulong utmp = 0;
;
                sw.WriteLine(ConvertSimDataIndexToText(currentPotIndex));
                sw.WriteLine("-------------------------------------------------");

                sw.WriteLine($"Grand Hit: {simData.potJackpotHit[currentPotIndex, 0]}");
                sw.WriteLine($"Grand Hit Rate: {hitrateGrand}");
                utmp = simData.potJackpotWin[currentPotIndex, 0];
                sw.WriteLine($"Total Grand Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");

                sw.WriteLine($"Major Hit: {simData.potJackpotHit[currentPotIndex, 1]}");
                sw.WriteLine($"Major Hit Rate: {hitrateMajor}");
                utmp = simData.potJackpotWin[currentPotIndex, 1];
                sw.WriteLine($"Total Major Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");

                sw.WriteLine($"Minor Hit: {simData.potJackpotHit[currentPotIndex, 2]}");
                sw.WriteLine($"Minor Hit Rate: {hitrateMinor}");
                utmp = simData.potJackpotWin[currentPotIndex, 2];
                sw.WriteLine($"Total Minor Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");

                sw.WriteLine($"Mini Hit: {simData.potJackpotHit[currentPotIndex, 3]}");
                sw.WriteLine($"Mini Hit Rate: {hitrateMini}");
                utmp = simData.potJackpotWin[currentPotIndex, 3];
                sw.WriteLine($"Total Mini Win (SGD): {outCurrency.GetSymbol()} {(utmp / 100).ToString("F2")}");
            }

            sw.WriteLine(sw.NewLine);
        }
    }
}