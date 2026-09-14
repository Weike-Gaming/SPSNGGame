using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.LobbyManagement;
using Weike.MachineInterface;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYGameManager : WkSlotMainGameManager
    {
        private JIXRYWeightage _gameWeightageHandler;
        private bool _loadOnceWeightage = false;
        private const byte ReelSetId = 1;
        private byte numScatter = 0;

        public const float normalPrizeSpeed = 0.5f;
        public const float fastPrizeSpeed = 0.25f;

        protected float fgPreSpinDelay = 0;
        private Coroutine _potAnimCoroutine;
        private int _finishPotAnimationCounter = 0;

        private const int InitialFreeGameCount = 8;
        public ushort modifiedInitialFreeGameCount = 8;
        private const int ExtraFreeGameCount = 4;

        #region Reel Nudge
        public bool nudgeChecked = false;
        #endregion

        /// <summary>
        /// For handling Feature Hit PreSpin animation
        /// </summary>
        public List<JIXRYStateDataFlag> newFeatureParts { get; private set; }

        protected override Type winManagerType => typeof(JIXRYWinManager);

        public JIXRYGameManager() : base()
        {
            dataModel = new JIXRYGameDataModel();
            cheatDataModel = new JIXRYCheatDataModel();
            gamePlayDataRecover = new JIXRYGamePlayData();
            gameplayMessage = new WkGameplayMessage(dataModel);
            freeGameDataModel = new JIXRYFreeGameDataModel();
        }

        public JIXRYWeightage gameWeightageHandler
        {
            get => _gameWeightageHandler;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadWeightage();
        }

        protected override string LoadGameSpecificChecksum()
        {
            StringBuilder temp = new StringBuilder(base.LoadGameSpecificChecksum());
            temp.Append($"<tr><td>{_gameWeightageHandler.fileName}</td><td>{_gameWeightageHandler.checksum}</td></tr>");

            return temp.ToString();
        }

        protected override WkSlotConfigurationData GetConfigurationData(bool resetDefault)
        {
            WkSlotConfigurationData config = base.GetConfigurationData(resetDefault);
            config.minimumPlayOptionIndex = 4;
            return config;
        }

        protected override void InitializeJackpot()
        {
            base.InitializeJackpot();

            if (WkGameInstance.instance!.isSimulation)
            {
                return;
            }
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);
            bool isDemo = !string.IsNullOrEmpty(machInfo.demo);
            if (isDemo)
            {
                return;
            }
            bool validateDemo = jackpotInfo.ValidateDemo(isDemo);
            WkAssert.EnsureMsgf(validateDemo, "Mismatch in jackpotInfo.xml");
            if (!validateDemo)
            {
                OnGameMalfunction();
            }
        }
        public override void RecoverPreviousRng()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            uint[] ingotValue = dm.potFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME ?
                dm.previousIngotValue.ToArray() : fgDm.fgPreviousIngotValue.ToArray();
            rm.UpdatePreviousIngotData(ingotValue, fgDm.previousExtraPrizeMultiplier, fgDm.previousExtraJackpotType);
            base.RecoverPreviousRng();
        }

        public void CacheFirstIncrementAmount()
        {
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            wm.CacheFirstIncrementAmount();
        }

        protected void LoadWeightage()
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

            if (WkGameInstance.instance!.isSimulation)
            {
                return;
            }
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);
            bool isDemo = !string.IsNullOrEmpty(machInfo.demo);
            if (isDemo)
            {
                return;
            }
            bool validateDemo = gameWeightageHandler.ValidateDemo(isDemo);
            WkAssert.EnsureMsgf(validateDemo, "Mismatch in Weightage.xml");
            if (!validateDemo)
            {
                OnGameMalfunction();
            }
        }

        public override void FinishIncrementing()
        {
            base.FinishIncrementing();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            if (dm.previousPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                CheckFeatureGame();
            }
            else
            {
                CheckCurrentHitNewFeature();
            }
        }

        protected override void GenerateGameSpecific(bool isRecovery)
        {
            base.GenerateGameSpecific(isRecovery);

            if (!isRecovery)
            {
                GenerateIngotsDigit();
            }
        }

        protected override void PreSpin(bool isRecovery)
        {
            if (!isRecovery)
            {
                ResetJackpotData();
                CheckIsScatter();
            }
            base.PreSpin(isRecovery);
            _finishPotAnimationCounter = 0;
            UpdateReelIngotData();
        }

        protected override void GenerateFgGameSpecific(bool isRecovery = false)
        {
            base.GenerateFgGameSpecific(isRecovery);

            if (!isRecovery)
            {
                GenerateIngotsDigit(true);
                //SavePreNudgeIngotValue();
                FgGenerateExtraPrizeMultiplier();
                FgGenerateExtraJackpotIngot();
            }
        }

        protected override void FGPreSpin(bool isRecovery)
        {
            if (!isRecovery)
            {
                CheckIsScatter();
            }
            base.FGPreSpin(isRecovery);
            _finishPotAnimationCounter = 0;
            UpdateReelIngotData(true);
        }

        public void UpdateReelIngotData(bool isFreeGame = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.UpdateIngotValueData(isFreeGame ? fgDm.fgIngotValue : dm.ingotValue);
        }

        #region Generate Probability Before Spin
        private void GenerateIngotsDigit(bool isFreeGame = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            if (cdm.mgCheatEnable)
            {
                dm.ingotValue = cdm.predetermineIngotValue.ToArray();
                return;
            }

            if (cdm.fgCheatEnable)
            {
                fgDm.fgIngotValue = cdm.predetermineIngotValue.ToArray();
                fgDm.fgTempIngotValue = cdm.predetermineIngotValue.ToArray();
                return;
            }

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte symbolIndex = 16;
            JIXRYCoinValueRoot coinValueRoot = _gameWeightageHandler.GetCoinValueRoot(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Credit");
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
                    uint rng = machineContext.platformInterface.GetRng((uint)totalWeight);
                    byte reelIndex = (byte)(i + 1);
                    coinValueProbability = _gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Credit", reelIndex, symbolIndex);

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
                fgDm.fgTempIngotValue = temp.ToArray();
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
            for (int i = 0; i < temp.Length; i++)
            {
                sb.AppendLine($"Index {i + 1}: {temp[i]}");
            }
            Debug.Log(sb.ToString());
        }

        private void CheckIsScatter()
        {
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            dm.savedPreviousPotFeatureGameFlag = dm.potFeatureGameFlag;
            dm.previousPotFeatureGameFlag = dm.potFeatureGameFlag;
            if (cdm.demoEnable)
            {
                dm.jackpotType = cdm.predetermineJackpotType;
                cdm.predetermineJackpotType = 0;
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.MAIN_GAME;
                cdm.demoEnable = false;
                RandomJackpotQualifyingThreshold();
                return;
            }

            if (cdm.demoPotFeature != (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                dm.upcomingPotFeatureGameFlag = cdm.demoPotFeature;
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.MAIN_GAME;
                return;
            }

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

            numScatter = (byte)(reelNudgeScatter + extraJackpotScatter + reelUpgradeScatter);

            if (numScatter > 0 && dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                GenerateRandomJackpotProbability();
                RandomJackpotQualifyingThreshold();
            }
        }

        private void CheckTriggerPotFeature(byte reelNudgeScatter, byte extraJackpotScatter, byte reelUpgradeScatter)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU)
            {
                return;
            }

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;

            JIXRYPotFeatureRoot potFeatureRoot = _gameWeightageHandler.GetPotFeatureRoot(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "PotFeature_TriggerProb");
            IEnumerable<JIXRYPotFeatureProbabilityRoot> potFeatureProbability;

            long totalWeight = potFeatureRoot.totalWeightCount;

            long value = 0;

            uint rng = machineContext.platformInterface.GetRng((uint)totalWeight);
            potFeatureProbability = _gameWeightageHandler.GetPotFeatureProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter);

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
            byte gameId = recoveredDataRaw?.id ?? 0;
            MachInfoWrapper machInfo = new();
            MachInfoWrapper.PlExtGetMachInfo(machInfo);

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte jackpotSet = machInfo.gameDef[gameId].m_byJPSet;
            byte jackpotGroup = machInfo.gameDef[gameId].m_byJPGrp;
            byte jackpotOption = machInfo.gameDef[gameId].m_byJPOpt;
            byte betMultiplier = (byte)dm.getBetMultiplier;
            uint denominationRatio = WkLobbySceneManager.instance.GetGameDenomRatioValue(gameId);

            JIXRYMysteryJackpotRoot mysteryJackpotRoot = _gameWeightageHandler.GetMysteryJackpotRoot(rtpFk, betFk, "MAIN_GAME", ReelSetId, "Mystery_Jackpot");

            long totalWeight = mysteryJackpotRoot.totalWeightCount;
            uint rng1 = machineContext!.platformInterface!.GetRng((uint)(totalWeight));
            uint rng2 = machineContext!.platformInterface!.GetRng((uint)(totalWeight));

            long mappingValue1 = 9999;
            long mappingValue2 = 9999;

            IEnumerable<JIXRYMysteryJackpotProbabilityRoot> mysteryJackpotProbability;
            mysteryJackpotProbability = _gameWeightageHandler.GetMysteryJackpotProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, jackpotSet, jackpotGroup, jackpotOption, betMultiplier, denominationRatio);

            long mappingValue = 9999;
            long weight = 0;
            foreach (JIXRYMysteryJackpotProbabilityRoot prob in mysteryJackpotProbability)
            {
                weight += prob.weight;
                if (rng1 < weight)
                {
                    mappingValue1 = prob.value;
                    break;
                }
            }

            weight = 0;
            foreach (JIXRYMysteryJackpotProbabilityRoot prob in mysteryJackpotProbability)
            {
                weight += prob.weight;
                if (rng2 < weight)
                {
                    mappingValue2 = prob.value;
                    break;
                }
            }

            if (mappingValue1 == mappingValue2)
            {
                mappingValue = mappingValue1;
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
            bool qualifyGrand = ShowProgressiveMeters() ? betAmountInCurrency >= (ulong)(config.qualifyingBet) : false;
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
                if (GetWinManagerChecked<WkSlotWinManager>().GetJackpotPrizeType(dm.jackpotType).Contains(GetPrizeNameForQualifyBet()))
                {
                    AwardReservedGrand();
                }
            }
        }

        protected override void AwardGrandReplacement()
        {
            base.AwardGrandReplacement();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            dm.jackpotType = wm.GetJackpotPrizeIndex("MAJOR");
        }

        private void FgGenerateExtraPrizeMultiplier()
        {
            if (!GetUpcomingGameType().Contains("RU")) return;

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();

            byte reel3 = 3;
            byte ingotMultiplierIndex = 17;

            if (cdm.fgCheatEnable)
            {
                // Multiplier
                uint tempPrizeMul = fgDm.extraPrizeMultiplier;
                tempPrizeMul = cdm.predetermineExtraPrizeMultiplierType;
                fgDm.extraPrizeMultiplier = tempPrizeMul;

                rm.UpdateExtraPrizeMultiplierType(reel3 - 1, (byte)fgDm.extraPrizeMultiplier);

                cdm.predetermineExtraPrizeMultiplierType = 2;

                // IngotValue
                uint extraPrizeMultiplierIngotValue3 = GetIngotValue(reel3, ingotMultiplierIndex);
                uint tempPrizeMulValue = fgDm.extraPrizeMultiplierIngotValue;
                tempPrizeMulValue = extraPrizeMultiplierIngotValue3;
                fgDm.extraPrizeMultiplierIngotValue = tempPrizeMulValue;
                return;
            }

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            uint betMultiplier = dm.getBetMultiplier;

            // Reel 3
            {
                JIXRYExtraPrizeMultiplierRoot extraPrizeMultiplierRoot = gameWeightageHandler.GetExtraPrizeMultiplierRoot(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Multiplier", reel3, ingotMultiplierIndex);
                IEnumerable<JIXRYExtraPrizeMultiplierProbabilityRoot> extraPrizeMultiplierProbabilityRoot = gameWeightageHandler.GetExtraPrizeMultiplierProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Multiplier", reel3, ingotMultiplierIndex);
                long totalWeight = extraPrizeMultiplierRoot.totalWeightCount;

                long weight = 0;
                uint rng = machineContext.platformInterface.GetRng((uint)totalWeight);

                foreach (JIXRYExtraPrizeMultiplierProbabilityRoot prob in extraPrizeMultiplierProbabilityRoot)
                {
                    weight += prob.weight;
                    if (rng < weight)
                    {
                        uint tempPrizeMul = fgDm.extraPrizeMultiplier;
                        tempPrizeMul = (uint)prob.value;
                        fgDm.extraPrizeMultiplier = tempPrizeMul;
                        rm.UpdateExtraPrizeMultiplierType(reel3 - 1, (byte)fgDm.extraPrizeMultiplier);
                        break;
                    }
                }

                //Generate Extra Prize Multiplier Ingot Value
                uint extraPrizeMultiplierIngotValue = GetIngotValue(reel3, ingotMultiplierIndex);
                uint tempPrizeMulValue = fgDm.extraPrizeMultiplierIngotValue;
                tempPrizeMulValue = extraPrizeMultiplierIngotValue;
                fgDm.extraPrizeMultiplierIngotValue = tempPrizeMulValue;
            }
        }

        private uint GetIngotValue(byte reelNumber, byte symbolIndex)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;

            JIXRYCoinValueRoot ingotCreditRoot = gameWeightageHandler.GetIngotCreditRoot(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Credit", reelNumber, symbolIndex);
            long totalWeight = ingotCreditRoot.totalWeightCount;

            IEnumerable<JIXRYCoinValueProbabilityRoot> ingotCreditProbabilityRoot = gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Credit", reelNumber, symbolIndex);

            long weight = 0;
            uint rng = machineContext.platformInterface.GetRng((uint)totalWeight);

            foreach (JIXRYCoinValueProbabilityRoot prob in ingotCreditProbabilityRoot)
            {
                weight += prob.weight;
                if (rng < weight)
                {
                    return (uint)prob.value * dm.getBetMultiplier;
                }
            }
            return 0;
        }

        private void FgGenerateExtraJackpotIngot()
        {
            if (!GetUpcomingGameType().Contains("JP")) return;

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            if (cdm.fgCheatEnable)
            {
                dm.jackpotType = cdm.predetermineJackpotType;
                fgDm.extraJackpotType = cdm.predetermineJackpotType;
                RandomJackpotQualifyingThreshold();
                rm.UpdateExtraJackpotType(dm.jackpotType);
                cdm.predetermineJackpotType = 1;
                return;
            }

            byte gameId = recoveredDataRaw?.id ?? 0;
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

            JIXRYExtraJackpotRoot extraJackpotRoot = _gameWeightageHandler.GetExtraJackpotRoot(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Jackpot", reel5, jackpotIngotIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier);

            long totalWeight = extraJackpotRoot.totalWeightCount;
            uint rng = machineContext!.platformInterface!.GetRng((uint)(totalWeight));

            IEnumerable<JIXRYExtraJackpotProbabilityRoot> extraJackpotProbability;
            extraJackpotProbability = _gameWeightageHandler.GetExtraJackpotProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Jackpot", reel5, jackpotIngotIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier);

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
                    fgDm.extraJackpotType = 1;
                    break;
                case 2999:
                    dm.jackpotType = 2;
                    fgDm.extraJackpotType = 2;
                    break;
                case 3999:
                    dm.jackpotType = 3;
                    fgDm.extraJackpotType = 3;
                    break;
                case 4999:
                    dm.jackpotType = 4;
                    fgDm.extraJackpotType = 4;
                    break;
                default:
                    dm.jackpotType = 0;
                    break;
            }
            RandomJackpotQualifyingThreshold();
            rm.UpdateExtraJackpotType(fgDm.extraJackpotType);
        }
        #endregion

        protected override void AwardReservedGrand()
        {
            base.AwardReservedGrand();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.grandCounter--;
            byte prize = GetWinManagerChecked<WkSlotWinManager>().GetJackpotPrizeIndex("GRAND");
            dm.jackpotType = prize;
        }

        public override void MgCheckWin(bool modifyWinAmount = true)
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
            uint[] ingotValues = new uint[rd.Length * rd[0].numRows];
            for (int reel = 0; reel < rd.Length; reel++)
            {
                int sourceIndex = reel * totalNumRows + startRow;
                int destIndex = reel * rd[0].numRows;
                for (int row = 0; row < rd[0].numRows; row++)
                {
                    ingotValues[destIndex + row] = dm.ingotValue[sourceIndex + row];
                }
            }

            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                reelManager,
                ingotValues,
                0,
                0,
                false,
                modifyWinAmount
            );
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
                string potFeature = isFeatureEnd ? GetTriggeringGameType() : GetUpcomingGameType();
                wm.UpdateScatterStatement(reelManager, betValue, potFeature);
            }

            CheckAndSetScatterHitSfx();
        }

        public void FgCheckWinIcon(bool modifyWinAmount = true)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManager wm = winManager as JIXRYWinManager ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            bool isFgTriggered = dm.previousPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;
            uint freeGameMultiplier = 1;
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
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

            CheckAndSetScatterHitSfx();
        }

        /// <summary>
        /// FgEndPanel.OnRecover re-checking of wins so that winStatement has correct winCounts & ingots have value.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void FgEndPanelCheckWin()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();

            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            bool isInReelNudge = feature.ToString().Contains("RN") ? true : false;
            FgEndPanelCheckWinIngot(true);
        }

        /// <summary>
        /// Specifically for fg-end-panel, cannot use default FgCheckIngotWin called by InternalFgBet() on recovery as nudge related info is lost.
        /// </summary>
        /// <param name="modifyWinAmount"></param>
        /// <exception cref="InvalidCastException"></exception>
        public void FgEndPanelCheckWinIngot(bool resetWinStatements)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            if (!WkAssert.Ensure(winManager) ||
                winManager is not JIXRYWinManager JIXRYWinManager ||
                !WkAssert.EnsureMsgf(reelManager, "ReelManager is missing during check win"))
                return;

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

            // Do normal ingot win check
            (byte maxIngotWayWin, JIXRYExtraIngotPosition extraIngotPosition) = JIXRYWinManager.CheckIngotTrigger(
                reelManager,
                ingotValues,
                fgDm.extraPrizeMultiplier,
                fgDm.extraPrizeMultiplierIngotValue,
                true,
                false,
                resetWinStatements
            );

            dm.maxIngotWayWin = maxIngotWayWin;

            // Extra Prize Data
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
                    ingotValues[destIndex + row] = fgDm.fgTempIngotValue[sourceIndex + row];
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
                modifyWinAmount
                //isInReelNudge
            );

            dm.maxIngotWayWin = maxIngotWayWin;

            // Extra Prize Data
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

            CheckAndSetScatterHitSfx();
        }


        /// <summary>
        /// Spin will call this.
        /// </summary>
        /// <param name="modifyWinAmount"></param>
        /// <exception cref="InvalidCastException"></exception>
        public override void FgCheckWin(bool modifyWinAmount = true)
        {
            // Reset rm flags
            nudgeChecked = false;
            JIXRYReelManager rm = reelManager as JIXRYReelManager;
            rm.nudgeDoneThisSpin = false;
            rm.haveNudge = false;
            rm.doingNudge = false;
            rm.doneNudge = false;
            FgCheckWinIcon(modifyWinAmount);
            FgCheckWinIngot(modifyWinAmount);
        }

        public void DoneNudge()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager;
            rm.doneNudge = true;
            rm.nudgeDoneThisSpin = true;
        }

        public override void SaveFreeGameRandomJackpotWin()
        {
            base.SaveFreeGameRandomJackpotWin();
            int symbolCount = 3;
            byte position = 13;
            for (int i = 0; i < symbolCount; i++)
            {
                byte index = reelManager.GetReelIconIndex(4, i);
                if (reelManager.symbolInfo.symbolTemplate.symbols[index - 1].symbolType.Contains("JP"))
                {
                    position += (byte)i;
                    break;
                }
            }

            SaveFgRandomJackpotWin(position);
        }

        public void CheckFeatureGame()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoFeatureGame");
            }
            else
            {
                gameState!.AddInputAtomAndRunState("Cmd_GotFeatureGame");
            }
        }

        public bool CheckJackpotTrigger()
        {
            if (!ShowPreanimation()) return false; // if config disable remove

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            bool isJackpotTrigger = dm.jackpotType != 0;

            if (isJackpotTrigger)
            {
                int weightage = UnityEngine.Random.Range(0, 10);
                if (weightage > 2)
                {
                    dm.extremeBigWinPreSpinAnim = true;
                    dm.extremeBigWinPreSpinAnim = false;
                    if (reelManager is JIXRYReelManager rm)
                    {
                        rm.TriggerPreSpinAnimation();
                    }
                    return true;
                }
            }
            return false;
        }

        public bool CheckFreeGameJackpotTrigger()
        {
            if (!ShowPreanimation() || PlayHitFeatureAnimation(true)) return false; // if config disable remove
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            bool hasJackpotIngot = false;
            int numRows = 3;
            int reelIndex = 4;
            for (int i = 0; i < numRows; i++)
            {
                int index = reelManager.GetReelIconIndex(reelIndex, i) - 1;
                WkSymbolDetail symbolDetail = reelManager.symbolInfo.symbolTemplate.symbols![index];
                string symbolType = symbolDetail.symbolType;

                if (symbolType == "INGOT_JACKPOT")
                {
                    hasJackpotIngot = true;
                    break;
                }
            }

            if (rm.GetMaxIngotWayWin() >= 5 && hasJackpotIngot)
            {
                int weightage = UnityEngine.Random.Range(0, 10);
                if (weightage > 2)
                {
                    dm.extremeBigWinPreSpinAnim = true;
                    dm.extremeBigWinPreSpinAnim = false;
                    rm.TriggerPreSpinAnimation();
                    return true;
                }
            }

            return false;
        }

        public bool CheckFeatureGameTrigger(bool isInFreeGame = false)
        {
            if (!ShowPreanimation() || PlayHitFeatureAnimation(isInFreeGame)) return false; // if config disable remove

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            bool isFeatureTriggered = dm.upcomingPotFeatureGameFlag != dm.previousPotFeatureGameFlag;

            if (isFeatureTriggered)
            {
                int weightage = UnityEngine.Random.Range(0, 10);
                if (weightage > 2)
                {
                    dm.extremeBigWinPreSpinAnim = true;
                    dm.extremeBigWinPreSpinAnim = false;
                    if (reelManager is JIXRYReelManager rm)
                    {
                        rm.TriggerPreSpinAnimation();
                    }
                    return true;
                }
            }
            return false;
        }

        public bool CheckForExtremeBigWinPreSpin(bool isInFreeGame = false)
        {
            if (!ShowPreanimation() || PlayHitFeatureAnimation(isInFreeGame)) return false; // if config disable remove

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            if (!WkAssert.Ensure(winManager)) return false;
            WkSlotWinManager win = winManager! as WkSlotWinManager ?? throw new InvalidCastException();
            WkSlotWinManagerModel winModel = win.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
            bool isFreeGame = dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            ulong currentWinamount = isFreeGame ? (ulong)(winModel.winAmount - winModel.totalFgWinAmount) : winManager.GetWinAmount();
            win.GetWinLevel(currentWinamount, dm.getBetMultiplier, dm.getPlayOption, out int winLevel, out float nonSkipDuration);
            int weightage = UnityEngine.Random.Range(0, 10);

            if ((winLevel == 2) && (weightage > 2))
            {
                dm.extremeBigWinPreSpinAnim = true;
                dm.extremeBigWinPreSpinAnim = false;
                if (reelManager is JIXRYReelManager rm)
                {
                    rm.TriggerPreSpinAnimation();
                }
                return true;
            }
            return false;
        }

        public override void ReelStopSpinning()
        {
            gameState!.AddInputAtomAndRunState("Cmd_ReelStopSpinning");
            RunCheckWinCommands();
            predetermineRng = null;
        }

        public override void RunCheckWinCommands()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            WkWinManagerModel winModel = winManager!.modelData ?? throw new InvalidCastException();
            bool inFreeGame = dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            long winAmount = 0;

            if (inFreeGame)
            {
                if (dm.maxIngotWayWin != 5)
                {
                    dm.jackpotType = 0;
                }

                winAmount = freeGameDataModel.fgJackpotLevel1Prize + freeGameDataModel.fgJackpotLevel2Prize +
                           freeGameDataModel.fgJackpotLevel3Prize + freeGameDataModel.fgJackpotLevel4Prize + winModel.tempWinAmount;
            }
            else
            {
                winAmount = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize +
                            dm.jackpotLevel3Prize + dm.jackpotLevel4Prize + winModel.tempWinAmount;
            }

            if (winAmount > 0)
            {
                
                if(inFreeGame)
                {
                    fgDm.isLuckyWin = false;
                }
                gameState!.AddInputAtomAndRunState("Cmd_GotWin");
            }
            else
            {
                if (inFreeGame)
                {
                    fgDm.isLuckyWin = true;
                }
                gameState!.AddInputAtomAndRunState("Cmd_NoWin");
            }

            CheckSwitchStatesInSpin(winAmount);
        }

        public override void ResetFGDataOnFgSessionEnd()
        {
            ResetDataForMainGame();
            base.ResetFGDataOnFgSessionEnd();
        }

        public void ResetDataForMainGame()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            dm.previousPotFeatureGameFlag = dm.potFeatureGameFlag;
            dm.savedPreviousPotFeatureGameFlag = dm.potFeatureGameFlag;
            dm.potFeatureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;
            dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.MAIN_GAME;

            dm.jackpotWinAmount = 0;
            dm.jackpotType = 0;

            cheatDataModel.fgCheatEnable = false;
            cheatDataModel.fgCheatEnabledInCurrentRound = false;
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.UpdatePreviousIngotData(dm.ingotValue, fgDm.previousExtraPrizeMultiplier, fgDm.previousExtraJackpotType);
        }

        public void ResetFeatureCheat()
        {
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            cdm.demoEnable = false;
            cdm.demoPotFeature = (byte)JIXRYStateDataFlag.MAIN_GAME;
        }

        public void InitiateFgCheatData()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            
                rm.UpdateFreeGameCheatData();

            uint betMultiplier = dm.getBetMultiplier;

            cdm.mgCheatEnable = false;
            cdm.predetermineJackpotType = 1;
            cdm.predetermineExtraPrizeMultiplierType = 2;
            uint[] baseIngotValues;
            //if (feature.ToString().Contains("RU"))
            //{
                baseIngotValues = new uint[]
                {
            10, 10, 10, 10, 10, 10, 10, 10,
            20, 20, 20, 20, 20, 20, 20, 20,
            30, 30, 30, 30, 30, 30, 30, 30,
            50, 50, 50, 50, 50, 50, 50, 40,
            80, 80, 80, 80, 80, 80, 80, 80
                };
            //}
            //else
            //{
            //    baseIngotValues = new uint[]
            //    {
            //10, 10, 10, 10, 10, 10, 10,
            //20, 20, 20, 20, 20, 20, 20,
            //30, 30, 30, 30, 30, 30, 30,
            //50, 50, 50, 50, 50, 50, 50,
            //80, 80, 80, 80, 80, 80, 80
            //    };
            //}
            // Base values


            uint[] baseExtraPrizeAdditions = { 30, 80 };

            // Apply multiplier
            for (int i = 0; i < cdm.predetermineIngotValue.Length; i++)
            {
                cdm.predetermineIngotValue[i] = baseIngotValues[i] * betMultiplier;
            }
        }

        public IReadOnlyList<uint> GetPossibleIngotValue(byte reelIndex)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            byte rtpFk = GetVariationIndex();
            uint betFk = dm.getPlayOption;
            byte symbolIndex = 16;

            IEnumerable<JIXRYCoinValueProbabilityRoot> coinValueProbability = _gameWeightageHandler.GetCoinValueProbability(rtpFk, betFk, GetUpcomingGameType(), ReelSetId, "Credit", reelIndex, symbolIndex);

            List<uint> values = new List<uint>();
            foreach (JIXRYCoinValueProbabilityRoot prob in coinValueProbability)
            {
                values.Add((uint)prob.value);
            }

            return values.ToArray();
        }

        public void ResetRngForFirstFgSpin()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            WkReelManagerDataModel rdm = rm.reelManagerDataModel;

            int n = rm.reelData.Length;
            uint[] defaultRng = new uint[n];

            rdm.prevRng = defaultRng.ToArray();
            rdm.rng = defaultRng.ToArray();

            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            rm.ChangeToFgReelStrip(freeGameDataModel.initialFreeGameAmount, dm.variation, dm.getPlayOption);
            rm.HardCodeReelSymbol(rdm.rng);
        }
       
        public void FinishPotAnimation()
        {
            _finishPotAnimationCounter++;

            if (_finishPotAnimationCounter < 3) return;

            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            dm.triggerFinalAnim = true;
            dm.triggerFinalAnim = false;
        }

        #region Pot Coin
        public void UpdatePotCoinValue(int blueScatter, int redScatter, int greenScatter)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            dm.tempBluePotScatter += blueScatter;
            dm.tempRedPotScatter += redScatter;
            dm.tempGreenPotScatter += greenScatter;
        }

        public void SavePotCoinValue()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            dm.savedBluePotScatter = dm.tempBluePotScatter;
            dm.savedRedPotScatter = dm.tempRedPotScatter;
            dm.savedGreenPotScatter = dm.tempGreenPotScatter;
        }

        public void RunFinishScatterAnimationCommand()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishScatterAnimation");
        }

        public void PlayFeatureTriggerCoinAnim()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            bool isHitFeature = dm.upcomingPotFeatureGameFlag != dm.previousPotFeatureGameFlag;

            if (isHitFeature)
            {
                _potAnimCoroutine = StartCoroutine(PotAnimCoroutine());
            }
            else
            {
                RunFinishScatterAnimationCommand();
            }
        }
        
        private float GetPotDelayDuration()
        {
            int lowestLevel = int.MaxValue;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            if (GetUpcomingGameType().Contains("JP"))
            {
                if (dm.savedRedPotScatter < lowestLevel)
                {
                    lowestLevel = dm.savedRedPotScatter;
                }
            }

            if(GetUpcomingGameType().Contains("RN"))
            {
                if(dm.savedBluePotScatter < lowestLevel)
                {
                    lowestLevel = dm.savedBluePotScatter;
                }
            }

            if (GetUpcomingGameType().Contains("RU"))
            {
                if (dm.savedGreenPotScatter < lowestLevel)
                {
                    lowestLevel = dm.savedGreenPotScatter;
                }
            }

            float duration = 4.0f;
            if (lowestLevel < 4)
            {
                duration = 9.0f;
            }
            else if (lowestLevel < 7)
            {
                duration = 8.0f;
            }
            else if (lowestLevel < 10)
            {
                duration = 7.0f;
            }
            else if(lowestLevel < 13)
            {
                duration = 6.0f;
            }
            else if(lowestLevel < 16)
            {
                duration = 5.0f;
            }

            return duration;
        }

        private IEnumerator PotAnimCoroutine()
        {
            yield return new WaitForSeconds(0.2f);
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.playFeatureTriggerCoinAnim = true;
            dm.playFeatureTriggerCoinAnim = false;
            yield return new WaitForSeconds(GetPotDelayDuration());
            gameState.AddInputAtomAndRunState("Cmd_FinishScatterAnimation");
        }

        public void PlayCoinBurstAnim(int blueScatter, int redScatter, int greenScatter)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            if (blueScatter > 0)
            {
                dm.blueCoinInAnim = true;
                dm.blueCoinInAnim = false;
            }
            if (redScatter > 0)
            {
                dm.redCoinInAnim = true;
                dm.redCoinInAnim = false;
            }
            if (greenScatter > 0)
            {
                dm.greenCoinInAnim = true;
                dm.greenCoinInAnim = false;
            }
        }

        public void PotCoinFeatureAwarded(string potType)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (GetUpcomingGameType().Contains(potType))
            {
                if (potType == "RN")
                {
                    dm.tempBluePotScatter = 0;
                    dm.savedBluePotScatter = 0;
                }
                if (potType == "JP")
                {
                    dm.tempRedPotScatter = 0;
                    dm.savedRedPotScatter = 0;
                }
                if (potType == "RU")
                {
                    dm.tempGreenPotScatter = 0;
                    dm.savedGreenPotScatter = 0;
                }
            }
        }
        #endregion

        #region Recovery

        /// <summary>
        /// Recover the digits for curr and total FreeGameAmount
        /// </summary>
        public void RecoverFgCounter()
        {
            freeGameDataModel.currentAmountOfFreeGame = freeGameDataModel.savedCurrentAmountOfFreeGame;
            freeGameDataModel.totalFreeGameAmount = freeGameDataModel.savedTotalFreeGameAmount;
        }

        public void RecoverPotCoinLevel()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            dm.tempBluePotScatter = dm.savedBluePotScatter;
            dm.tempGreenPotScatter = dm.savedGreenPotScatter;
            dm.tempRedPotScatter = dm.savedRedPotScatter;
        }

        public void RecoverPreviousIngotData(bool isFreeGame)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            rm.UpdateIngotValueData(isFreeGame ? fgDm.fgPreviousIngotValue : dm.previousIngotValue);

            if (GetUpcomingGameType().Contains("JP"))
            {
                rm.UpdateExtraJackpotType(fgDm.previousExtraJackpotType);
            }

            rm.UpdateRecoverIngotData();
        }

        public override void RecoverReel()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            rm.UpdatePreviousIngotData(dm.potFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME ? dm.ingotValue : fgDm.fgIngotValue, fgDm.extraPrizeMultiplier, fgDm.extraJackpotType);
            base.RecoverReel();
        }

        /// <summary>
        /// Modified RecoverReel() specific for recovering to fg-jp-session-end.
        /// Which will do the same thing EXCEPT not transform multiplier ingot.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void RecoverReelJackpot()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            rm.UpdatePreviousIngotData(dm.potFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME ? dm.ingotValue : fgDm.fgIngotValue, fgDm.extraPrizeMultiplier, fgDm.extraJackpotType);

            rm.ResetTransformedSymbol();
            base.RecoverReel();
        }
        protected override IEnumerator StartRecoverFgSpin()
        {
            UpdateBetDisplay();
            RecoverPrevReelStrip();
            RecoverPreviousRng();
            yield return new WaitForEndOfFrame();
            RecoverReelStrip();
            RecoverFgIngotData();

            yield return new WaitForEndOfFrame();
            RecoverFgBet();

            CheckCurrentHitNewFeature(true);
            CheckFgExtraJackpot();
            CheckFgExtraPrize();
        }

        public void RecoverFgIngotData()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            fgDm.fgTempIngotValue = fgDm.fgIngotValue.ToArray();
            fgDm.fgPreviousIngotValue = fgDm.fgIngotValue.ToArray();
            rm.UpdateIngotValueData(fgDm.fgIngotValue);
            rm.UpdateExtraPrizeMultiplierType(2, (byte)fgDm.extraPrizeMultiplier);
            rm.UpdateExtraJackpotType(fgDm.extraJackpotType);

            rm.UpdateRecoverIngotData();
        }

        public void RecoverWinAmount()
        {
            JIXRYWinManagerModel dm = winManager.modelData.GetModelDataChecked<JIXRYWinManagerModel>();
            dm.winAmount = dm.savedWinAmount;
        }

        protected override long GetRecoverTargetWinAmount()
        {
            HashSet<string> zeroStates = new HashSet<string> {"spin", "jp-announcement", "jp-session-end" };

            HashSet<string> withoutJp = new HashSet<string> { "fg-jp-announcement", "fg-jp-session-end" };
            if (zeroStates.Contains(gameState!.fsm!.activeStateId))
            {
                return 0;
            }
            else if (withoutJp.Contains(gameState!.fsm!.activeStateId))
            {
                WkSlotWinManagerModel slotWinManagerModel = winManager!.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
                JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
                JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
                long accumulated = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
                long currentRound = fgDm.fgJackpotLevel1Prize + fgDm.fgJackpotLevel2Prize + fgDm.fgJackpotLevel3Prize + fgDm.fgJackpotLevel4Prize;
                long fgWinInCurrency = slotWinManagerModel.savedTotalFgWinAmount * dm.denomValue;
                return fgWinInCurrency + accumulated - currentRound;
            }
            else
            {
                WkSlotWinManagerModel slotWinManagerModel = winManager!.modelData as WkSlotWinManagerModel ?? throw new InvalidCastException();
                JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
                long fgWinInCurrency = slotWinManagerModel.savedTotalFgWinAmount * dm.denomValue;
                return fgWinInCurrency + dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
            }
        }

        public void RecoverJackpotAmount()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.jackpotWinAmount = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
        }

        public void RecoverJackpotPrize()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYWinManagerModel winModel = winManager.modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();
            bool isFreeGame = dm.upcomingPotFeatureGameFlag != (byte)JIXRYStateDataFlag.MAIN_GAME;

            if (isFreeGame)
            {
                dm.jackpotWinAmount = freeGameDataModel.fgJackpotLevel1Prize + freeGameDataModel.fgJackpotLevel2Prize + freeGameDataModel.fgJackpotLevel3Prize + freeGameDataModel.fgJackpotLevel4Prize;
                winManager.modelData.tempWinAmount = winModel.winAmount + dm.jackpotWinAmount;
            }
            else
            {
                dm.jackpotWinAmount = dm.jackpotLevel1Prize + dm.jackpotLevel2Prize + dm.jackpotLevel3Prize + dm.jackpotLevel4Prize;
            }
        }

        public void UpdateFreeGameWinAmount()
        {
            JIXRYWinManagerModel winModel = winManager.modelData as JIXRYWinManagerModel ?? throw new InvalidCastException();
            winModel.savedWinAmount += winModel.fgFirstIncrementAmount;
            winModel.savedTotalFgWinAmount += winModel.fgFirstIncrementAmount;
        }

        public void SaveFreeGameCount()
        {
            freeGameDataModel.savedCurrentAmountOfFreeGame = freeGameDataModel.currentAmountOfFreeGame;
            freeGameDataModel.savedTotalFreeGameAmount = freeGameDataModel.totalFreeGameAmount;
        }

        public void SavePreviousPotFeatureState()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.savedPreviousPotFeatureGameFlag = dm.previousPotFeatureGameFlag;
        }

        public void SetCurrentFeature()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.potFeatureGameFlag = dm.upcomingPotFeatureGameFlag;
        }

        public void RecoverPreviousPotFeatureState()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.previousPotFeatureGameFlag = dm.savedPreviousPotFeatureGameFlag;
        }

        public void RecoverTempIngotDigit()
        {
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            fgDm.fgTempIngotValue = fgDm.fgIngotValue.ToArray();
        }

        public void SaveFinalIngotAmount(bool isFreeGame = false)
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            uint[] temp = (isFreeGame) ? fgDm.fgTempIngotValue.ToArray() : dm.ingotValue.ToArray();

            // Update multiplier ingot - only check reel 3 (or reel index 2)
            bool skipMultiplierCheck = true; // True means skip the check for ingot as it's not to be changed
            int reelIndex = 2; // Center reel

            if (dm.maxIngotWayWin > 2)
                skipMultiplierCheck = false;

            // Loop to find and update multiplierIngot value w/ early break if alr modified OR not supposed to change
            int numRows = reelManager.reelData[reelIndex].numRows + (reelManager.reelData[reelIndex] as JIXRYReelData).numDummy;
            int centerRow = reelManager.reelData[reelIndex].centerRow;

            for (int row = centerRow - 1; row <= centerRow + 1; row++)
            {
                if (skipMultiplierCheck)
                    break;

                // Adjust local var to account for GetReelIconIndex() only using visible rows to check
                int firstVisibleRow = centerRow - 1; // bottom visible row in full‑row coordinates
                int visibleRow = row - firstVisibleRow; // 0, 1, or 2
                int iconIndex = reelManager.GetReelIconIndex(reelIndex, visibleRow) - 1;

                WkSymbolDetail d = symbolInfoTemplate.symbols![iconIndex];

                if (d.symbolType.Contains("INGOT_PRIZEMULTIPLIER"))
                {
                    int tempIndex = reelIndex * numRows + row; // convert index back into full-row index
                    temp[tempIndex] = fgDm.extraPrizeMultiplierIngotValue;
                    break;
                }
            }

            if (isFreeGame)
            {
                fgDm.fgPreviousIngotValue = temp.ToArray();
            }
            else
            {
                dm.previousIngotValue = temp.ToArray();
            }

            //Debug
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Previous Ingot");
            for (int i = 0; i < temp.Length; i++)
            {
                sb.AppendLine($"Index {i + 1}: {temp[i]}");
            }
            Debug.Log(sb.ToString());
        }

        public void SavePreviousIngotData()
        {
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            fgDm.previousExtraPrizeMultiplier = fgDm.extraPrizeMultiplier;
            fgDm.previousExtraJackpotType = fgDm.extraJackpotType;
            dm.previousMaxWayWin = dm.maxIngotWayWin;
        }

        public void PassPotScatterNumToLobbyModel()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;

            if (lobby is null)
            {
                return;
            }

            lobby.dataModel.customData[0] = dm.savedBluePotScatter;
            lobby.dataModel.customData[1] = dm.savedRedPotScatter;
            lobby.dataModel.customData[2] = dm.savedGreenPotScatter;
        }

        public override void OnSelected()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkLobbySceneManager lobby = WkLobbySceneManager.instance;

            if (lobby is null)
            {
                return;
            }
            dm.savedBluePotScatter = lobby.dataModel.customData[0];
            dm.savedRedPotScatter = lobby.dataModel.customData[1];
            dm.savedGreenPotScatter = lobby.dataModel.customData[2];
            base.OnSelected();
        }
        #endregion

        #region Jackpot Transition (Cmd)

        public void FinishFirstPop()
        {
            gameState.AddInputAtomAndRunState("Cmd_FinishFirstPop");
        }

        public void CheckSymbolWinInJackpot()
        {
            WkSlotWinManager slotWinManager = winManager as WkSlotWinManager ?? throw new InvalidCastException();
            if (!slotWinManager.HasWinStatement())
            {
                gameState.AddInputAtomAndRunState("Cmd_FinishFirstPop");
            }
        }

        public void CheckHaveJackpot()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.jackpotType != 0)
            {
                gameState!.AddInputAtomAndRunState("Cmd_GotJackpot");
            }
            else
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoJackpot");
            }
        }

        protected override void ResetJackpotData()
        {
            base.ResetJackpotData();
            WkSlotGameDataModel jpDataModel = dataModel as WkSlotGameDataModel ?? throw new InvalidCastException();
            jpDataModel.jackpotType = 0;
        }

        protected override long SaveFgJackpotPrize()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            freeGameDataModel.fgJackpotLevel1Prize = 0;
            freeGameDataModel.fgJackpotLevel2Prize = 0;
            freeGameDataModel.fgJackpotLevel3Prize = 0;
            freeGameDataModel.fgJackpotLevel4Prize = 0;

            int type = dm.jackpotType;
            long amount = machineContext!.platformInterface!.GetStdProgressiveAmount(type);
            uint betMultiplier = dm.getBetMultiplier;
            switch (type)
            {
                case 1:
                    freeGameDataModel.fgJackpotLevel1Prize = amount;
                    dm.jackpotLevel1Prize += amount;
                    break;
                case 2:
                    freeGameDataModel.fgJackpotLevel2Prize = amount;
                    dm.jackpotLevel2Prize += amount;
                    break;
                case 3:
                    freeGameDataModel.fgJackpotLevel3Prize = amount * betMultiplier;
                    dm.jackpotLevel3Prize += amount * betMultiplier;
                    amount = freeGameDataModel.fgJackpotLevel3Prize;
                    break;
                case 4:
                    freeGameDataModel.fgJackpotLevel4Prize = amount * betMultiplier;
                    dm.jackpotLevel4Prize += amount * betMultiplier;
                    amount = freeGameDataModel.fgJackpotLevel4Prize;
                    break;
                default:
                    break;
            }

            return amount;
        }

        public void RunFinishJackpotAnnouncementCommand()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishAnnouncement");
        }

        public void CheckJackpotPrizeReady()
        {
            WkPlatformInterface? pi = machineContext?.platformInterface;
            if (pi.IsStdProgressiveAmountReady())
            {
                gameState!.AddInputAtomAndRunState("Cmd_JackpotPrizeReady");
            }
        }

        public void RunJackpotSessionEndCommand()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishJackpot");
        }

        public void RunJackpotBigWinCommand()
        {
            gameState!.AddInputAtomAndRunState("Cmd_BigWin");
        }
        #endregion

        #region Free Game Transition (Cmd)

        public void InitiateFreeGame()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();

            byte gameIndex = recoveredDataRaw?.id ?? 0;
            WkHistorySaveType saveType = WkHistorySaveType.MainGame;
            Debug.Log($"Saving History {gameIndex} - Save Type : {saveType}");
            IWkRootActor rootOwner = owner as IWkRootActor ?? throw new InvalidCastException();
            rootOwner.SaveHistory(modelList.ToArray(), saveType, true);

            cdm.mgCheatEnable = false;
            freeGameDataModel.currentAmountOfFreeGame = 0;
            freeGameDataModel.totalFreeGameAmount = modifiedInitialFreeGameCount;
            modifiedInitialFreeGameCount = InitialFreeGameCount;
            WkMachineInstance.currentSubGameSequence = 0;
            freeGameDataModel.subGameSequence = 0;
            AddExtraFreegame(dm.upcomingPotFeatureGameFlag);
            freeGameDataModel.savedCurrentAmountOfFreeGame = 0;
            freeGameDataModel.savedTotalFreeGameAmount = freeGameDataModel.totalFreeGameAmount;
            dm.savedTriggerPotFeatureGameFlag = dm.upcomingPotFeatureGameFlag;
        }

        public void ResetFgJackpotPrize()
        {
            freeGameDataModel.fgJackpotLevel1Prize = 0;
            freeGameDataModel.fgJackpotLevel2Prize = 0;
            freeGameDataModel.fgJackpotLevel3Prize = 0;
            freeGameDataModel.fgJackpotLevel4Prize = 0;
        }

        public void ResetCacheAmount()
        {
            JIXRYWinManagerModel wm = winManager.modelData as JIXRYWinManagerModel;
            wm.fgFirstIncrementAmount = 0;
        }

        /// <summary>
        /// Elephant Animation (?)
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public void PlayFgTrasition()  //Elephant
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.upcomingPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    Debug.Log("State : Play RN Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    Debug.Log("State : Play JP Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    Debug.Log("State : Play RU Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    Debug.Log("State : Play RNJP Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    Debug.Log("State : Play RNRU Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    Debug.Log("State : Play JPRU Transition From Main Game");
                    break;
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    Debug.Log("State : Play RNJPRU Transition From Main Game");
                    break;
            }
            gameState.AddInputAtomAndRunState("Cmd_FinishFeatureGameTransition");
        }

        public void CheckFgExtraJackpot()
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            int reel5 = 4;
            int numRows = reelManager!.reelData[reel5].numRows;
            bool hasJackpotIngot = false;

            if (dm.maxIngotWayWin < 5)
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoJackpot");
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
                gameState!.AddInputAtomAndRunState("Cmd_GotJackpot");
            }
            else
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoJackpot");
            }
        }

        protected override bool RunJackpotCommands()
        {
            return false;
        }

        public void CheckFgExtraPrize()
        {
            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();

            if (dm.maxIngotWayWin < 2 || (!rd[2].haveMultiplierIngot) || (rm.haveNudge && !rm.doneNudge))
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoPrizeBoostMultiplier");
                return;
            }

            int[] reelsToCheck = dm.maxIngotWayWin > 4 ? new[] { 2, 4 } : new[] { 2 };

            bool foundAddition = false;

            // Only check for multiplier if no addition was found
            bool hasMultiplierInReels = reelsToCheck.Any(i => rd[i].haveMultiplierIngot);

            if (!foundAddition)
            {
                if (hasMultiplierInReels)
                {
                    bool foundMultiplier = CheckForSymbol("INGOT_PRIZEMULTIPLIER", reelsToCheck, symbolInfoTemplate, "Cmd_GotPrizeBoostMultiplier");

                    if (!foundMultiplier)
                    {
                        gameState!.AddInputAtomAndRunState("Cmd_NoPrizeBoostMultiplier");
                    }
                }
                else
                {
                    // Multiplier check skipped - treat as "not found"
                    gameState!.AddInputAtomAndRunState("Cmd_NoPrizeBoostMultiplier");
                }
            }
        }

        #region Hit New Feature
        public bool CheckCurrentHitNewFeature(bool isRecovery = false)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (isRecovery)
            {
                dm.previousPotFeatureGameFlag = dm.potFeatureGameFlag;
            }

            bool hitNewFeature = dm.upcomingPotFeatureGameFlag != dm.previousPotFeatureGameFlag;
            gameState.AddInputAtomAndRunState(hitNewFeature ? "Cmd_GotHitNewFeature" : "Cmd_NoHitNewFeature");

            return hitNewFeature;
        }

        public readonly Queue<JIXRYStateDataFlag> pendingSmallIngotAnim = new Queue<JIXRYStateDataFlag>();

        private List<JIXRYStateDataFlag> GetParts(JIXRYStateDataFlag state)
        {
            switch (state)
            {
                case JIXRYStateDataFlag.MAIN_GAME: return new();
                case JIXRYStateDataFlag.FREE_GAME_RN: return new() { JIXRYStateDataFlag.FREE_GAME_RN };
                case JIXRYStateDataFlag.FREE_GAME_JP: return new() { JIXRYStateDataFlag.FREE_GAME_JP };
                case JIXRYStateDataFlag.FREE_GAME_RU: return new() { JIXRYStateDataFlag.FREE_GAME_RU };
                case JIXRYStateDataFlag.FREE_GAME_RNJP: return new() { JIXRYStateDataFlag.FREE_GAME_RN, JIXRYStateDataFlag.FREE_GAME_JP };
                case JIXRYStateDataFlag.FREE_GAME_RNRU: return new() { JIXRYStateDataFlag.FREE_GAME_RN, JIXRYStateDataFlag.FREE_GAME_RU };
                case JIXRYStateDataFlag.FREE_GAME_JPRU: return new() { JIXRYStateDataFlag.FREE_GAME_JP, JIXRYStateDataFlag.FREE_GAME_RU };
                case JIXRYStateDataFlag.FREE_GAME_RNJPRU: return new() { JIXRYStateDataFlag.FREE_GAME_RN, JIXRYStateDataFlag.FREE_GAME_JP, JIXRYStateDataFlag.FREE_GAME_RU };
                default: return new();
            }
        }

        public List<JIXRYStateDataFlag> GetNewFeatureList()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            JIXRYStateDataFlag currentFlag = (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;
            JIXRYStateDataFlag previousFlag = (JIXRYStateDataFlag)dm.previousPotFeatureGameFlag;

            List<JIXRYStateDataFlag> currentParts = GetParts(currentFlag);
            List<JIXRYStateDataFlag> previousParts = GetParts(previousFlag);

            List<JIXRYStateDataFlag> newParts = currentParts
                .Where(p => !previousParts.Contains(p))
                .ToList();

            Debug.Log($"Current feature = {currentFlag}");
            Debug.Log($"Previous feature = {previousFlag}");

            return newParts;
        }

        public virtual void PlayHitNewFeatureAnimation()
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
                StartCoroutine(WaitFeaturePanelClose());
                dm.shouldPlayHitFeatureAnim = false;
            }
        }

        public bool PlayHitFeatureAnimation(bool isInFreeGame)
        {
            if (!isInFreeGame) return false;
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            return dm.shouldPlayHitFeatureAnim;
        }

        private IEnumerator WaitFeaturePanelClose()
        {
            yield return new WaitForSeconds(0.4f);

            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.playHitFeatureAnim = true;
            dm.playHitFeatureAnim = false;
            yield return new WaitForSeconds(fgPreSpinDelay);

            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.FinishPreSpinAnimation(0.3f);
            fgPreSpinDelay = 0;
        }

        public void SetPlayAnimFlag()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            if (dm.previousPotFeatureGameFlag != dm.upcomingPotFeatureGameFlag)
            {
                dm.shouldPlayHitFeatureAnim = true;
                HandleHitNewAnimationData();
            }
        }

        protected virtual void HandleHitNewAnimationData()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            JIXRYStateDataFlag prev = (JIXRYStateDataFlag)dm.previousPotFeatureGameFlag;
            JIXRYStateDataFlag cur = (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;

            List<JIXRYStateDataFlag> prevParts = GetParts(prev);
            List<JIXRYStateDataFlag> currParts = GetParts(cur);

            newFeatureParts = currParts.Where(p => !prevParts.Contains(p)).ToList();

            if (prev == cur)
            {
                foreach (JIXRYStateDataFlag part in currParts)
                    pendingSmallIngotAnim.Enqueue(part);
            }
            else
            {
                foreach (JIXRYStateDataFlag part in currParts)
                    if (!prevParts.Contains(part))
                        pendingSmallIngotAnim.Enqueue(part);
            }

            List<JIXRYStateDataFlag> newParts = GetNewFeatureList();

            foreach (JIXRYStateDataFlag part in newParts)
            {
                string newFeature = part.ToString();

                if (newFeature.Contains("RN"))
                {
                    fgPreSpinDelay += 5f;
                }
                if (newFeature.Contains("JP"))
                {
                    fgPreSpinDelay += 6f;
                }
                if (newFeature.Contains("RU"))
                {
                    fgPreSpinDelay += 9f;
                }
            }
        }

        public void ResetPreviousPotFeatureFlag()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            dm.previousPotFeatureGameFlag = dm.upcomingPotFeatureGameFlag;
        }

        public void PlayHitNewFeaturePanel()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            AddExtraFreegame(dm.upcomingPotFeatureGameFlag);
            DelayAtom("Cmd_FinishFeatureAnimation", 5.5f);
        }

        private void AddExtraFreegame(byte currentFeatureGame)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            ushort extraFreeGame = 0;
            byte allPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU;
            byte twoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJP;
            byte onePotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RU;

            if (currentFeatureGame == allPotFeature)
            {
                if (dm.previousPotFeatureGameFlag < twoPotFeature) //RN,JP,RU
                {
                    extraFreeGame = ExtraFreeGameCount * 2;
                }
                else if (dm.previousPotFeatureGameFlag < allPotFeature) //ESJP,ESEP,JPEP
                {
                    extraFreeGame = ExtraFreeGameCount;
                }
            }
            else if (dm.previousPotFeatureGameFlag < twoPotFeature &&
                    currentFeatureGame > onePotFeature && currentFeatureGame < allPotFeature)
            {
                extraFreeGame = ExtraFreeGameCount;
            }

            freeGameDataModel.totalFreeGameAmount += extraFreeGame;
        }
        #endregion

        private bool CheckForSymbol(string symbolTypeToFind, ReadOnlySpan<int> reelIndices, WkSymbolInfoTemplate symbolTemplate, string successCommand)
        {
            foreach (int reelIndex in reelIndices)
            {
                foreach (int row in Enumerable.Range(0, reelManager.reelData[reelIndex].numRows))
                {
                    int index = reelManager.GetReelIconIndex(reelIndex, row) - 1;

                    string symbolType = symbolTemplate.symbols[index].symbolType;

                    if (symbolType == symbolTypeToFind)
                    {
                        gameState!.AddInputAtomAndRunState(successCommand);
                        return true;
                    }
                }
            }

            return false;
        }

        public void CheckFgIngotWin()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            WkWinManagerModel winModel = winManager!.modelData ?? throw new InvalidCastException();
            if (dm.maxIngotWayWin > 1 || winModel.tempWinAmount > 0)
            {
                gameState!.AddInputAtomAndRunState("Cmd_GotWin");
            }
            else
            {
                gameState!.AddInputAtomAndRunState("Cmd_NoWin");
            }
        }

        public override void OnPIError()
        {
            base.OnPIError();

            if (_potAnimCoroutine != null)
            {
                StopCoroutine(_potAnimCoroutine);
                _potAnimCoroutine = null;
            }

            CloseFgInitHelpPage(); // Selection Help Page
        }

        public void PlayFgIngotMultiplierAnimation()
        {
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            uint[] tempValue = new uint[fgDm.fgTempIngotValue.Length];
            tempValue = fgDm.fgTempIngotValue.ToArray();

            int maxIndex = dm.maxIngotWayWin <= 3 ? 21 : fgDm.fgIngotValue.Length;

            if (rd[2].haveMultiplierIngot)
            {
                for (int i = 0; i < maxIndex; i++)
                {
                    tempValue[i] *= fgDm.extraPrizeMultiplier;
                }
            }

            rm.UpdateIngotValueData(tempValue);
            rm.GatherAnimationData(dm.maxIngotWayWin);

            fgDm.fgTempIngotValue = tempValue.ToArray();

            //Debug
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Multiply");
            for (int i = 0; i < fgDm.fgTempIngotValue.Length; i++)
            {
                sb.AppendLine($"Index {i + 1}: {fgDm.fgTempIngotValue[i]}");
            }
            Debug.Log(sb.ToString());
        }

        public void ForceResetAnimationBitMask()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager;
            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            rm.ForceResetAnimationBitMask(dm.maxIngotWayWin);
        }

        /// <summary>
        /// Modified function for SimGM to call Cmd_FinishPrizeBoostMultiplierAnimation
        /// </summary>
        public void RunFinishPrizeMultiplierCommand()
        {
            gameState.AddInputAtomAndRunState("Cmd_FinishPrizeBoostMultiplierAnimation");
        }

        /// <summary>
        /// Original function to run Cmd_FinishPrizeBoostMultiplierAnimation
        /// </summary>
        public void DelayRunFinishPrizeMultiplierCommand()
        {
            DelayAtom("Cmd_FinishPrizeBoostMultiplierAnimation", 0.5f);
        }

        public void PlayFgIngotMultiplierTransformation()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgDm = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();

            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int numRowsShowOnReel = reelManager!.reelData[0].numRows;
            int totalNumRows = reelManager!.reelData[0].numRows + (reelManager!.reelData[0] as JIXRYReelData).numDummy;
            byte reel = 0;
            if (rd[2].haveMultiplierIngot)
            {
                reel = 2;

                for (int row = 0; row < numRowsShowOnReel; row++)
                {
                    int index = reelManager.GetReelIconIndex(reel, row) - 1;
                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols![index];
                    string symbolType = symbolDetail.symbolType;

                    if (symbolType == "INGOT_PRIZEMULTIPLIER")
                    {
                        int prizeIngotIndex = reel * totalNumRows + row + 2;
                        fgDm.fgTempIngotValue[prizeIngotIndex] = (ushort)fgDm.extraPrizeMultiplierIngotValue;
                        break;
                    }
                }
            }

            rm.UpdateIngotValueData(fgDm.fgTempIngotValue);
            rm.PlayPrizeMultiplierTransformation();

            if (reel == 2)
            {
                rd[2].haveMultiplierIngot = false;
            }
            else if (reel == 4)
            {
                rd[4].haveMultiplierIngot = false;
            }

            //Debug
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Multiply Trans");
            for (int i = 0; i < fgDm.fgTempIngotValue.Length; i++)
            {
                sb.AppendLine($"Index {i + 1}: {fgDm.fgTempIngotValue[i]}");
            }
            Debug.Log(sb.ToString());

            DelayAtom("Cmd_FinishPrizeBoostMultiplierTransformation", 1.2f);
        }

        public override void ContinueFgSpinOrEnd()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            base.ContinueFgSpinOrEnd();

            if (freeGameDataModel.currentAmountOfFreeGame >= freeGameDataModel.totalFreeGameAmount)
            {
                dataModel.isGameCompleted = true;
                gameState!.AddInputAtomAndRunState("Cmd_FinishFeatureGame");
            }
            else
            {
                if (cdm.fgCheatEnable)
                {
                    gameState!.AddInputAtomAndRunState("Cmd_EnableCheatPanel");
                    return;
                }

                gameState!.AddInputAtomAndRunState("Cmd_Spin");
            }


        }

        public void RunFgSessionEndCommand()
        {
            DelayAtom("Cmd_FGSessionEnd", 2f);
        }

        public void ResetToMgJackpotData()
        {
            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            dm.jackpotType = 0;
        }

        public void SetAnimSpeedUp()
        {
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            rm.SetShouldSpeedUp(true);
        }
        #endregion

        #region Helper

        private void CheckAndSetScatterHitSfx()
        {
            // Reset rmdm data used for playing scatter sounds
            JIXRYReelManagerDataModel rmdm = reelManager.reelManagerDataModel as JIXRYReelManagerDataModel;
            rmdm.scatterCount = 0;
            rmdm.numOfScatterHitInSpin = 0;

            WkSymbolInfo symbolInfo = reelManager.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;

            int totalReels = reelManager.reelData.Length;

            for (int reel = 0; reel < totalReels; reel++) //5
            {
                int numRows = reelManager.reelData[reel].numRows;

                for (int row = 0; row < numRows; row++) //3
                {
                    int index = reelManager.GetReelIconIndex(reel, row) - 1;
                    WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols[index];

                    string ingotType = symbolDetail.symbolType;

                    if (ingotType == "BLUE_SCATTER")
                    {
                        rmdm.numOfScatterHitInSpin++;
                    }
                    if (ingotType == "RED_SCATTER")
                    {
                        rmdm.numOfScatterHitInSpin++;
                    }
                    if (ingotType == "GREEN_SCATTER")
                    {
                        rmdm.numOfScatterHitInSpin++;
                    }
                }
            }
        }

        public string GetUpcomingGameType()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.upcomingPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetCurrentGameType()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.potFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
                default:
                    return "MAIN_GAME";
            }
        }

        public string GetTriggeringGameType()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            switch (dm.savedTriggerPotFeatureGameFlag)
            {
                case (byte)JIXRYStateDataFlag.FREE_GAME_RN:
                    return "FREE_GAME_RN";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JP:
                    return "FREE_GAME_JP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RU:
                    return "FREE_GAME_RU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJP:
                    return "FREE_GAME_RNJP";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNRU:
                    return "FREE_GAME_RNRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_JPRU:
                    return "FREE_GAME_JPRU";
                case (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    return "FREE_GAME_RNJPRU";
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
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RU)
                {
                    stackValue = 13;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JPRU)
                {
                    stackValue = 123;
                }
            }
            else if (value == 2) //JP
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RN)
                {
                    stackValue = 12;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RU)
                {
                    stackValue = 23;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNRU)
                {
                    stackValue = 123;
                }
            }
            else if (value == 3) //RU
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RN)
                {
                    stackValue = 13;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_JP)
                {
                    stackValue = 23;
                }
                else if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RNJP)
                {
                    stackValue = 123;
                }
            }
            else if (value == 12) //RUJP
            {
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RU)
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
                if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.FREE_GAME_RN)
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
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_RN;
                    break;
                case 2:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_JP;
                    break;
                case 3:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_RU;
                    break;
                case 12:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_RNJP;
                    break;
                case 13:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_RNRU;
                    break;
                case 23:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_JPRU;
                    break;
                case 123:
                    dm.upcomingPotFeatureGameFlag = (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU;
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

        void DelayAtom(string atom, float delay)
        {
            WkCoroutine.instance.StartTrackedCoroutine(DelayAtomCoroutine(atom, delay));
        }

        IEnumerator DelayAtomCoroutine(string atom, float delay)
        {
            yield return new WaitForSeconds(delay);
            gameState!.AddInputAtomAndRunState(atom);
        }

        public void RunPrizeMultiplierAnimationCmd()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishPrizeBoostMultiplierAnimation");
        }

        public void RunPrizeMultiplierTransformationCmd()
        {
            gameState!.AddInputAtomAndRunState("Cmd_FinishPrizeBoostMultiplierTransformation");
        }
        #endregion

        #region FG-INIT'S HELP PAGE
        // This is different from normal help page as it is it's own game object that is enabled/disabled within fg-selection state
        public void ToggleFgInitStateHelpPage()
        {
            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            dm.enableSelectionHelpPage = !dm.enableSelectionHelpPage;
        }

        public void CloseFgInitHelpPage()
        {
            JIXRYGameDataModel dm = dataModel.GetModelDataChecked<JIXRYGameDataModel>();
            dm.enableSelectionHelpPage = false;
        }
        #endregion

        #region Demo Option
        private const int BlueScatter = 12;
        private const int RedScatter = 13;
        private const int GreenScatter = 14;
        private const int ElephantSub = 15;

        [WkDemoSelection(nameof(TriggerNudgeFeature))]
        public void TriggerNudgeFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { BlueScatter, BlueScatter, BlueScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RN;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");

                JIXRYReelNudgePreSpinDisplayObject tmp = FindObjectOfType<JIXRYReelNudgePreSpinDisplayObject>();
                tmp.TryToEnableMenu();
            }
        }

        [WkDemoSelection(nameof(TriggerUpgradeFeature))]
        public void TriggerUpgradeFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { GreenScatter, GreenScatter, GreenScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RU;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerJackpotFeature))]
        public void TriggerJackpotFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { RedScatter, RedScatter, RedScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_JP;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerNudgeJackpotFeature))]
        public void TriggerNudgeJackpotFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { BlueScatter, BlueScatter, BlueScatter, RedScatter, RedScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJP;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerUpgradeNudgeFeature))]
        public void TriggerUpgradeNudgeFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { BlueScatter, BlueScatter, GreenScatter, GreenScatter, GreenScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNRU;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerUpgradeJackpotFeature))]
        public void TriggerUpgradeJackpotFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { RedScatter, RedScatter, RedScatter, GreenScatter, GreenScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_JPRU;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerAllFeature))]
        public void TriggerAllFeature()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { BlueScatter, RedScatter, GreenScatter }));
                cdm.demoPotFeature = (byte)JIXRYStateDataFlag.FREE_GAME_RNJPRU;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerGrandRandom))]
        public void TriggerGrandRandom()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { RedScatter }));
                cdm.demoEnable = true;
                cdm.predetermineJackpotType = 1;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        [WkDemoSelection(nameof(TriggerMajorRandom))]
        public void TriggerMajorRandom()
        {
            if (dataModel.totalCredit > 0)
            {
                ResetCheat();
                JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
                SetupCheat(GenerateCheatRng(new uint[] { BlueScatter }));
                cdm.demoEnable = true;
                cdm.predetermineJackpotType = 2;
                gameState!.AddInputAtomAndRunState("Cmd_DisableDemoPanel");
            }
        }

        private void ResetCheat()
        {
            JIXRYCheatDataModel cdm = cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();
            cdm.demoEnable = false;
            cdm.demoPotFeature = (byte)JIXRYStateDataFlag.MAIN_GAME;
        }

        public uint[] GenerateCheatRng(params uint[] inputNumbers)
        {
            List<uint> finalList = new List<uint>();

            if (inputNumbers.Length > 0)
            {
                finalList.AddRange(inputNumbers);
            }

            // 1 - 16
            while (finalList.Count < 5)
            {
                finalList.Add((uint)UnityEngine.Random.Range(1, 17));
            }

            // Shuffle the list
            for (int i = finalList.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                uint temp = finalList[i];
                finalList[i] = finalList[j];
                finalList[j] = temp;
            }

            // Prevent ElephantSub on reels 1 and 5
            for (int i = 0; i < finalList.Count; i++)
            {
                if ((i == 0 || i == 4) && finalList[i] == ElephantSub)
                {
                    uint newNum;
                    do
                    {
                        newNum = (uint)UnityEngine.Random.Range(1, 17);
                    } while (newNum == ElephantSub);

                    finalList[i] = newNum;
                }
            }

            return finalList.ToArray();
        }

        private void SetupCheat(uint[] scatterCombination)
        {
            Debug.Log($"[CHEAT] Scatter Combination Input: {string.Join(", ", scatterCombination)}");

            List<byte[]> reelStrips = reelManager.reelManagerDataModel.reelSet.reelStrips.ToList();
            uint[] rng = new uint[reelStrips.Count];

            for (int i = 0; i < reelStrips.Count; i++)
            {
                uint targetScatter = scatterCombination[i];
                int reelLength = reelStrips[i].Length;

                // Collect all valid positions
                List<int> validPositions = new List<int>();
                for (int j = 0; j < reelLength; j++)
                {
                    if (reelStrips[i][j] == targetScatter)
                    {
                        validPositions.Add(j);
                    }
                }

                // Pick one of the valid positions at random
                int foundPos = validPositions[UnityEngine.Random.Range(0, validPositions.Count)];

                // Apply a small offset for variability
                int offset = UnityEngine.Random.Range(0, 3);
                int finalPos = (foundPos - offset + reelLength) % reelLength;

                rng[i] = (uint)finalPos;
            }

            Debug.Log($"[CHEAT] Final RNG: {string.Join(", ", rng)}");
            SetupPredetermineRng(rng);
        }
        #endregion

        #region play led

        /// <summary>
        /// Play LED effect
        /// </summary>
        /// <param name="potState"></param>
        public virtual void PlayLED(JIXRYStateDataFlag potState)
        {
            byte[] staticColor = new byte[] { 80, 0, 0 };
            byte[] greenColor = new byte[] { 30, 60, 30 };
            byte[] redColor = new byte[] { 90, 0, 0 };
            byte[] purpleColor = new byte[] { 40, 0, 40 };
            byte[] empty = new byte[] { 0, 0, 0 };
            switch (potState)
            {
                case JIXRYStateDataFlag.FREE_GAME_RN:
                    Debug.Log("Play LED: greenColor");
                    PlayLedStatic(greenColor);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_JP:
                    Debug.Log("Play LED: redColor");
                    PlayLedStatic(redColor);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RU:
                    Debug.Log("Play LED: purpleColor");
                    PlayLedStatic(purpleColor);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNRU:
                    Debug.Log("Play LED: greenColor, purpleColor");
                    PlayLedTransition(greenColor, purpleColor, empty);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNJP:
                    Debug.Log("Play LED: greenColor, redColor");
                    PlayLedTransition(greenColor, redColor, empty);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_JPRU:
                    Debug.Log("Play LED: redColor, purpleColor");
                    PlayLedTransition(redColor, purpleColor, empty);
                    break;
                case JIXRYStateDataFlag.FREE_GAME_RNJPRU:
                    Debug.Log("Play LED: greenColor, redColor, purpleColor");
                    const uint interval = 7500;
                    PlayLedTransition(greenColor, redColor, purpleColor, interval);
                    break;
                case JIXRYStateDataFlag.MAIN_GAME:
                    Debug.Log("Play LED: staticColor");
                    PlayLedStatic(staticColor);
                    break;
            }
        }

        /// <summary>
        /// Play LED effect for jackpot
        /// </summary>
        public virtual void PlayLEDJackpot()
        {
            byte jackpotType = dataModel.GetModelDataChecked<JIXRYGameDataModel>().jackpotType;
            if (jackpotType == 1)
            {
                Debug.Log("Play LED blink: GRAND");
                byte[] firstColor = new byte[] { 70, 50, 20 };
                byte[] nextColor = new byte[] { 80, 70, 30 };
                PlayLedBlink(firstColor, nextColor);
            }
            else if (jackpotType == 2)
            {
                Debug.Log("Play LED blink: MAJOR");
                byte[] firstColor = new byte[] { 60, 10, 60 };
                byte[] nextColor = new byte[] { 75, 30, 75 };
                PlayLedBlink(firstColor, nextColor);
            }
        }

        /// <summary>
        /// Play LED effect for free game end panel
        /// </summary>
        public virtual void PlayLEDFgEndPanel()
        {
            Debug.Log("Play LED blink: FgEndPanel");
            byte[] firstColor = new byte[] { 70, 50, 20 };
            byte[] nextColor = new byte[] { 80, 70, 30 };
            PlayLedBlink(firstColor, nextColor);
        }
        #endregion

        #region Lucky Win
        public void CheckisLuckyWin()
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fd = freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            if (feature.ToString().Contains("RN"))
            {
                if(fd.isLuckyWin && !rm.haveNudge)
                {
                    fd.totalFreeGameAmount += 1;
                } 
            }
        }
        #endregion
        #region Lucky Boost
        public void ChangeReelToLuckyBoost(int index)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.potFeatureGameFlag;
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            if (feature.ToString().Contains("RU"))
                rm.ChangeNumRows(index);
        }
        public void ChangeReelToLuckyBoostinFeature(int index)
        {
            JIXRYGameDataModel dm = dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYStateDataFlag feature = (JIXRYStateDataFlag)dm.upcomingPotFeatureGameFlag;
            JIXRYReelManager rm = reelManager as JIXRYReelManager ?? throw new InvalidCastException();
            if (feature.ToString().Contains("RU"))
                rm.ChangeNumRows(index);
        }
        #endregion

    }
}
