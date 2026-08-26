using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Weike.Games.JIXRY
{
    public class JIXRYWeightage
    {
        private JIXRYWeightRoot _gameRoot;
        private List<JIXRYRtpRoot> _gameRtpCollections = new List<JIXRYRtpRoot>();
        private List<JIXRYBetRoot> _gameBetCollection = new List<JIXRYBetRoot>();
        private List<JIXRYGameRoot> _gameTypeCollection = new List<JIXRYGameRoot>();
        private List<JIXRYReelSetRoot> _reelSetCollection = new List<JIXRYReelSetRoot>();

        private List<JIXRYPotFeatureRoot> _potFeatureCollection = new List<JIXRYPotFeatureRoot>();
        private List<JIXRYPotFeatureProbabilityRoot> _potFeatureProbabilityCollection = new List<JIXRYPotFeatureProbabilityRoot>();
               
        private List<JIXRYCoinValueRoot> _coinValueCollection = new List<JIXRYCoinValueRoot>();
        private List<JIXRYCoinValueProbabilityRoot> _coinValueProbabilityCollection = new List<JIXRYCoinValueProbabilityRoot>();

        private List<JIXRYMysteryJackpotRoot> _mysteryJackpotCollection = new List<JIXRYMysteryJackpotRoot>();
        private List<JIXRYMysteryJackpotProbabilityRoot> _mysteryJackpotProbabilityCollection = new List<JIXRYMysteryJackpotProbabilityRoot>();

        private List<JIXRYExtraSpinRoot> _extraSpinCollection = new List<JIXRYExtraSpinRoot>();
        private List<JIXRYExtraSpinProbabilityRoot> _extraSpinProbabilityCollection = new List<JIXRYExtraSpinProbabilityRoot>();

        private List<JIXRYExtraPrizeMultiplierRoot> _extraPrizeMultiplierCollection = new List<JIXRYExtraPrizeMultiplierRoot>();
        private List<JIXRYExtraPrizeMultiplierProbabilityRoot> _extraPrizeMultiplierProbabilityCollection = new List<JIXRYExtraPrizeMultiplierProbabilityRoot>();

        private List<JIXRYExtraPrizePayerRoot> _extraPrizePayerCollection = new List<JIXRYExtraPrizePayerRoot>();
        private List<JIXRYExtraPrizePayerProbabilityRoot> _extraPrizePayerProbabilityCollection = new List<JIXRYExtraPrizePayerProbabilityRoot>();

        private List<JIXRYExtraJackpotRoot> _extraJackpotCollection = new List<JIXRYExtraJackpotRoot>();
        private List<JIXRYExtraJackpotProbabilityRoot> _extraJackpotProbabilityCollection = new List<JIXRYExtraJackpotProbabilityRoot>();

        private bool _docLoaded = false;
        private XmlDocument _doc = new();
        private string _checksum = string.Empty;
        private string _filename = string.Empty;
        private bool _demo = false;

        /// <summary>
        /// File checksum
        /// </summary>
        public string checksum
        {
            get { return _checksum; }
        }

        /// <summary>
        /// Name of file 
        /// </summary>HORSEProbabilityRoot
        public string fileName
        {
            get { return _filename; }
        }

        #region Get Data

        #region Collections
        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="reelNudgeScatterFk"></param>
        /// <param name="extraJackpotScatterFk"></param>
        /// <param name="reelUpgradeScatterFk"></param>
        /// <returns></returns>
        public IEnumerable<JIXRYPotFeatureProbabilityRoot> GetPotFeatureProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte reelNudgeScatterFk, byte extraJackpotScatterFk, byte reelUpgradeScatterFk)
        {
            IEnumerable<JIXRYPotFeatureProbabilityRoot> returnVal = from p in _potFeatureProbabilityCollection 
                                                                    where p.rtpFk.Equals(rtpFk) && 
                                                                        p.betFk.Equals(betFk) && 
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.reelNudgeScatter.Equals(reelNudgeScatterFk) &&
                                                                        p.extraJackpotScatter.Equals(extraJackpotScatterFk) &&
                                                                        p.reelUpgradeScatter.Equals(reelUpgradeScatterFk)
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="coinTypeFk"></param>
        /// <param name="reelNumberFk"></param>
        /// <param name="symbolIndexFk"></param>
        /// <returns></returns>
        public IEnumerable<JIXRYCoinValueProbabilityRoot> GetCoinValueProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string coinTypeFk, byte reelNumberFk, byte symbolIndexFk)
        {
            IEnumerable<JIXRYCoinValueProbabilityRoot> returnVal = from p in _coinValueProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.coinType.Equals(coinTypeFk) &&
                                                                        p.reelNumber.Equals(reelNumberFk) &&
                                                                        p.symbolIndex.Equals(symbolIndexFk)
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="jackpotSetFk"></param>
        /// <param name="jackpotGroupFk"></param>
        /// <param name="jackpotOptionFk"></param>
        /// <param name="betMultiplierFk"></param>
        /// <param name="denominationRatioFk"></param>
        /// <returns></returns>
        public IEnumerable<JIXRYMysteryJackpotProbabilityRoot> GetMysteryJackpotProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte jackpotSetFk, byte jackpotGroupFk, byte jackpotOptionFk, byte betMultiplierFk, uint denominationRatioFk)
        {
            IEnumerable<JIXRYMysteryJackpotProbabilityRoot> returnVal = from p in _mysteryJackpotProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.jackpotSet.Equals(jackpotSetFk) &&
                                                                        p.jackpotGroup.Equals(jackpotGroupFk) &&
                                                                        p.jackpotOption.Equals(jackpotOptionFk) &&
                                                                        p.betMultiplier.Equals(betMultiplierFk) &&
                                                                        p.denominationRatio.Equals(denominationRatioFk)
                                                                        select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraTypeFk"></param>
        /// <param name="reelNumberFk"></param>
        /// <param name="symbolIndexFk"></param>
        /// <returns></returns>
        public IEnumerable<JIXRYExtraSpinProbabilityRoot> GetExtraSpinProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraTypeFk, byte reelNumberFk, byte symbolIndexFk)
        {
            IEnumerable<JIXRYExtraSpinProbabilityRoot> returnVal = from p in _extraSpinProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.extraType.Equals(extraTypeFk) &&
                                                                        p.reelNumber.Equals(reelNumberFk) &&
                                                                        p.symbolIndex.Equals(symbolIndexFk)
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraTypeFk"></param>
        /// <param name="reelNumberFk"></param>
        /// <param name="symbolIndexFk"></param>
        public IEnumerable<JIXRYExtraPrizeMultiplierProbabilityRoot> GetExtraPrizeMultiplierProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraTypeFk, byte reelNumberFk, byte symbolIndexFk)
        {
            IEnumerable<JIXRYExtraPrizeMultiplierProbabilityRoot> returnVal = from p in _extraPrizeMultiplierProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.extraType.Equals(extraTypeFk) &&
                                                                        p.reelNumber.Equals(reelNumberFk) &&
                                                                        p.symbolIndex.Equals(symbolIndexFk)
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraTypeFk"></param>
        /// <param name="reelNumberFk"></param>
        /// <param name="symbolIndexFk"></param>
        public IEnumerable<JIXRYExtraPrizePayerProbabilityRoot> GetExtraPrizePayerProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraTypeFk, byte reelNumberFk, byte symbolIndexFk)
        {
            IEnumerable<JIXRYExtraPrizePayerProbabilityRoot> returnVal = from p in _extraPrizePayerProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.extraType.Equals(extraTypeFk) &&
                                                                        p.reelNumber.Equals(reelNumberFk) &&
                                                                        p.symbolIndex.Equals(symbolIndexFk)
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get selected reel probability
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraTypeFk"></param>
        /// <param name="reelNumberFk"></param>
        /// <param name="symbolIndexFk"></param>
        /// <param name="jackpotSetFk"></param>
        /// <param name="jackpotGroupFk"></param>
        /// <param name="jackpotOptionFk"></param>
        /// <param name="betMultiplierFk"></param>
        public IEnumerable<JIXRYExtraJackpotProbabilityRoot> GetExtraJackpotProbability(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraTypeFk, byte reelNumberFk, byte symbolIndexFk, byte jackpotSetFk, byte jackpotGroupFk, byte jackpotOptionFk, byte betMultiplierFk)
        {
            IEnumerable<JIXRYExtraJackpotProbabilityRoot> returnVal = from p in _extraJackpotProbabilityCollection
                                                                    where p.rtpFk.Equals(rtpFk) &&
                                                                        p.betFk.Equals(betFk) &&
                                                                        p.gameTypeFk.Equals(gameTypeFk) &&
                                                                        p.reelSetFk.Equals(reelSetFk) &&
                                                                        p.extraType.Equals(extraTypeFk) &&
                                                                        p.reelNumber.Equals(reelNumberFk) &&
                                                                        p.symbolIndex.Equals(symbolIndexFk) &&
                                                                        p.jackpotSet.Equals(jackpotSetFk) &&
                                                                        p.jackpotGroup.Equals(jackpotGroupFk) &&
                                                                        p.jackpotOption.Equals(jackpotOptionFk) &&
                                                                        p.betMultiplier.Equals(betMultiplierFk) 
                                                                    select p;
            return returnVal;
        }

        /// <summary>
        /// Get all Selected SetNo
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <returns></returns>
        public IEnumerable<JIXRYReelSetRoot> GetReelSets(byte rtpFk, uint betFk, string gameTypeFk)
        {
            IEnumerable<JIXRYReelSetRoot> returnVal = (from s in _reelSetCollection
                                                       where s.rtpFk.Equals(rtpFk) &&
                                                             s.betFk.Equals(betFk) &&
                                                             s.gameTypeFk.Equals(gameTypeFk)
                                                       select s);
            return returnVal;
        }
        #endregion

        #region Root
        /// <summary>
        /// Get Game Type Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameType"></param>
        /// <returns></returns>
        public JIXRYGameRoot GetGameType(byte rtpFk, uint betFk, string gameType)
        {
            JIXRYGameRoot returnVal = (from s in _gameTypeCollection
                                       where s.rtpFk.Equals(rtpFk) &&
                                          s.betFk.Equals(betFk) &&
                                          s.gameType.Equals(gameType)
                                       select s).First();

            return returnVal;
        }

        /// <summary>
        /// Get Coin Value Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="coinType"></param>
        /// <returns></returns>
        public JIXRYCoinValueRoot GetCoinValueRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string coinType)
        {
            JIXRYCoinValueRoot returnVal = (from s in _coinValueCollection
                                            where s.rtpFk.Equals(rtpFk) &&
                                               s.betFk.Equals(betFk) &&
                                               s.gameTypeFk.Equals(gameTypeFk) &&
                                               s.reelSetFk.Equals(reelSetFk) &&
                                               s.coinType.Equals(coinType)
                                            select s).First();
            return returnVal;                                                           
        }

        /// <summary>
        /// Get Pot Feature Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public JIXRYPotFeatureRoot GetPotFeatureRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName)
        {
            JIXRYPotFeatureRoot returnVal = (from s in _potFeatureCollection
                                            where s.rtpFk.Equals(rtpFk) &&
                                               s.betFk.Equals(betFk) &&
                                               s.gameTypeFk.Equals(gameTypeFk) &&
                                               s.reelSetFk.Equals(reelSetFk) &&
                                               s.nodeName.Equals(nodeName)
                                            select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Mystery Jackpot Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public JIXRYMysteryJackpotRoot GetMysteryJackpotRoot (byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName)
        {
            JIXRYMysteryJackpotRoot returnVal = (from s in _mysteryJackpotCollection
                                                 where s.rtpFk.Equals(rtpFk) &&
                                                    s.betFk.Equals(betFk) &&
                                                    s.gameTypeFk.Equals(gameTypeFk) &&
                                                    s.reelSetFk.Equals(reelSetFk) &&
                                                    s.nodeName.Equals(nodeName)
                                                 select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Extra Spin Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraType"></param>
        /// <param name="reelNumber"></param>
        /// <param name="symbolIndex"></param>
        /// <returns></returns>
        public JIXRYExtraSpinRoot GetExtraSpinRoot (byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex)
        {
            JIXRYExtraSpinRoot returnVal = (from s in _extraSpinCollection
                                                 where s.rtpFk.Equals(rtpFk) &&
                                                    s.betFk.Equals(betFk) &&
                                                    s.gameTypeFk.Equals(gameTypeFk) &&
                                                    s.reelSetFk.Equals(reelSetFk) &&
                                                    s.extraType.Equals(extraType) &&
                                                    s.reelNumber.Equals(reelNumber) &&
                                                    s.symbolIndex.Equals(symbolIndex)
                                                 select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Coin Value Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="coinType"></param>
        /// <param name="reelNumber"></param>
        /// <param name="symbolIndex"></param>
        /// <returns></returns>
        public JIXRYCoinValueRoot GetIngotCreditRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string coinType, byte reelNumber, byte symbolIndex)
        {
            JIXRYCoinValueRoot returnVal = (from s in _coinValueCollection
                                            where s.rtpFk.Equals(rtpFk) &&
                                               s.betFk.Equals(betFk) &&
                                               s.gameTypeFk.Equals(gameTypeFk) &&
                                               s.reelSetFk.Equals(reelSetFk) &&
                                               s.coinType.Equals(coinType) &&
                                               s.reelNumber.Equals(reelNumber) &&
                                               s.symbolIndex.Equals(symbolIndex)
                                            select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Extra Prize Payer Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraType"></param>
        /// <param name="reelNumber"></param>
        /// <param name="symbolIndex"></param>
        /// <returns></returns>
        public JIXRYExtraPrizePayerRoot GetExtraPrizeAdditionRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex)
        {
            JIXRYExtraPrizePayerRoot returnVal = (from s in _extraPrizePayerCollection
                                            where s.rtpFk.Equals(rtpFk) &&
                                               s.betFk.Equals(betFk) &&
                                               s.gameTypeFk.Equals(gameTypeFk) &&
                                               s.reelSetFk.Equals(reelSetFk) &&
                                               s.extraType.Equals(extraType) &&
                                               s.reelNumber.Equals(reelNumber) &&
                                               s.symbolIndex.Equals(symbolIndex)
                                            select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Extra Prize Multiplier Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraType"></param>
        /// <param name="reelNumber"></param>
        /// <param name="symbolIndex"></param>
        /// <returns></returns>
        public JIXRYExtraPrizeMultiplierRoot GetExtraPrizeMultiplierRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex)
        {
            JIXRYExtraPrizeMultiplierRoot returnVal = (from s in _extraPrizeMultiplierCollection
                                                  where s.rtpFk.Equals(rtpFk) &&
                                                     s.betFk.Equals(betFk) &&
                                                     s.gameTypeFk.Equals(gameTypeFk) &&
                                                     s.reelSetFk.Equals(reelSetFk) &&
                                                     s.extraType.Equals(extraType) &&
                                                     s.reelNumber.Equals(reelNumber) &&
                                                     s.symbolIndex.Equals(symbolIndex)
                                                  select s).First();
            return returnVal;
        }

        /// <summary>
        /// Get Extra Jackpot Root
        /// </summary>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        /// <param name="extraType"></param>
        /// <param name="reelNumber"></param>
        /// <param name="symbolIndex"></param>
        /// <param name="jackpotSet"></param>
        /// <param name="jackpotGroup"></param>
        /// <param name="jackpotOption"></param>
        /// <param name="betMultiplier"></param>
        /// <returns></returns>
        public JIXRYExtraJackpotRoot GetExtraJackpotRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier)
        {
            JIXRYExtraJackpotRoot returnVal = (from s in _extraJackpotCollection
                                                       where s.rtpFk.Equals(rtpFk) &&
                                                          s.betFk.Equals(betFk) &&
                                                          s.gameTypeFk.Equals(gameTypeFk) &&
                                                          s.reelSetFk.Equals(reelSetFk) &&
                                                          s.extraType.Equals(extraType) &&
                                                          s.reelNumber.Equals(reelNumber) &&
                                                          s.symbolIndex.Equals(symbolIndex) &&
                                                          s.jackpotSet.Equals(jackpotSet) &&
                                                          s.jackpotGroup.Equals(jackpotGroup) &&
                                                          s.jackpotOption.Equals(jackpotOption) &&
                                                          s.betMultiplier.Equals(betMultiplier)
                                                       select s).First();
            return returnVal;
        }
        #endregion

        #endregion

        #region Deserialize XML
        public void ReadXML(string path)
        {
            if (_docLoaded)
            {
                return;
            }

            _doc.Load(path);
            _docLoaded = true;

            DeserializeXML();
            _filename = System.IO.Path.GetFileName(path);
        }

        private void DeserializeXML()
        {
            XmlNode root = _doc.SelectSingleNode("root");
            string gameCode = root.Attributes["GameCode"].Value;
            string gameName = root.Attributes["GameName"].Value;
            byte totalVarLevelCount = StringToByte(root, "Total_VarLevel_Cnt");
            _checksum = root!.Attributes["checksum"].Value;
            _demo = root!.Attributes["demo"].Value.Equals("true");

            _gameRoot = new JIXRYWeightRoot(gameCode, gameName, totalVarLevelCount);

            int n = root.ChildNodes.Count;

            if (n != totalVarLevelCount)
            {
                throw new Exception("TOTAL VAR LEVEL COUNT NOT MATCH");
            }

            for (int i = 0; i < n; i++)
            {
                XmlNode rtpNode = root.ChildNodes[i];
                DeserializeRTP(rtpNode, (byte)i);
            }

            Debug.Log("All Weightage Xml Loaded");

        }

        /// <summary>
        /// Deserialize all RTP root
        /// </summary>
        /// <param name="node"></param>
        private void DeserializeRTP(XmlNode node, byte index)
        {
            byte level = StringToByte(node, "level");
            string rtpName = node.Attributes["rtpName"].Value;
            byte betOptionCount = StringToByte(node, "BetOption_cnt");

            JIXRYRtpRoot rtp = new JIXRYRtpRoot(level, rtpName, betOptionCount);
            _gameRtpCollections.Add(rtp);

            int n = node.ChildNodes.Count;

            if (n != betOptionCount)
            {
                throw new Exception("TOTAL BET OPTION COUNT NO MATCH");
            }

            for (int i = 0; i < n; i++)
            {
                XmlNode betNode = node.ChildNodes[i];
                DeserializeBet(betNode, level);
            }
        }

        /// <summary>
        /// Deserialize all bet root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        private void DeserializeBet(XmlNode node, byte rtpFk)
        {
            uint betOption = StringToUInt(node, "betOption");
            double totalRtpValue = StringToDouble(node, "TotalRTPValue");

            JIXRYBetRoot bet = new JIXRYBetRoot(rtpFk, betOption, totalRtpValue);
            _gameBetCollection.Add(bet);

            int n = node.ChildNodes.Count;

            for (int i = 0; i < n; i++)
            {
                XmlNode gameNode = node.ChildNodes[i];
                DeserializeGame(gameNode, rtpFk, betOption);
            }
        }

        /// <summary>
        /// Deserialize all game root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        private void DeserializeGame(XmlNode node, byte rtpFk, uint betFk)
        {
            string gameType = node.Attributes["GameType"].Value;
            long totalReelSetWeightCount = StringToLong(node, "TotalReelSet_Weight_Cnt");
            byte reelSetCount = StringToByte(node, "ReelSet_Cnt");

            JIXRYGameRoot game = new JIXRYGameRoot(rtpFk, betFk, gameType, totalReelSetWeightCount, reelSetCount);
            _gameTypeCollection.Add(game);

            int n = node.ChildNodes.Count;
            if (reelSetCount != n)
            {
                throw new Exception("TOTAL REEL SET COUNT NOT MATCH");
            }

            for (int i = 0; i < n; i++)
            {
                XmlNode reelSetNode = node.ChildNodes[i];
                DeserializeReelSet(reelSetNode, rtpFk, betFk, gameType);
            }
        }

        /// <summary>
        /// Deserialize all reel set root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        private void DeserializeReelSet(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk)
        {
            byte reelId = StringToByte(node, "id");
            long totalTypeWeight = StringToLong(node, "TotalTypeWeight_Cnt");
            JIXRYReelSetRoot reelSet = new JIXRYReelSetRoot(rtpFk, betFk, gameTypeFk, reelId, totalTypeWeight);
            _reelSetCollection.Add(reelSet);

            int n = node.ChildNodes.Count;
            if (n != totalTypeWeight)
            {
                throw new Exception("TOTAL REEL COUNT NOT MATCH");
            }

            for (int i = 0; i < n; i++)
            {
                XmlNode featureNode = node.ChildNodes[i];
                HandleFeatureNode(featureNode, rtpFk, betFk, gameTypeFk, reelId);
            }
        }

        /// <summary>
        /// Deserialize feature node root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void HandleFeatureNode(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk)
        {
            string nodeName = node.Name;

            switch (nodeName)
            {
                case "PotFeature_TriggerProb":
                    DeserializePotFeature(node, rtpFk, betFk, gameTypeFk, reelSetFk, nodeName);
                    break;

                case "Ingot_Value":
                case "ReelUpgrade_Ingot":
                    DeserializeNoExtraJackpot(node, rtpFk, betFk, gameTypeFk, reelSetFk);
                    break;

                case "Mystery_Jackpot":
                    DeserializeMysteryJackpot(node, rtpFk, betFk, gameTypeFk, reelSetFk, nodeName);
                    break;

                case "Jackpot_Ingot":
                    DeserializeExtraJackpot(node, rtpFk, betFk, gameTypeFk, reelSetFk);
                    break;

                default:
                    Debug.LogWarning($"Unhandled feature node: {nodeName}");
                    break;
            }
        }

        /// <summary>
        /// Deserialize pot feature root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializePotFeature(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName)
        {
            byte reelNudgeScatter = StringToByte(node, "ReelNudge_Scatter");
            byte extraJackpotScatter = StringToByte(node, "Jackpot_Scatter");
            byte reelUpgradeScatter = StringToByte(node, "ReelUpgrade_Scatter");
            long totalWeightCount = StringToLong(node, "TotalWeight_Cnt");
            byte totalTypeWeightCount = StringToByte(node, "TotalTypeWeight_Cnt");

            JIXRYPotFeatureRoot probability = new JIXRYPotFeatureRoot(rtpFk, betFk, gameTypeFk, reelSetFk, nodeName, reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter, totalWeightCount, totalTypeWeightCount);
            _potFeatureCollection.Add(probability);

            int n = node.ChildNodes.Count;
            for (int i = 0; i < n; i++)
            {
                XmlNode probabilityNode = node.ChildNodes[i];
                DeserializePotFeatureProbability(probabilityNode, rtpFk, betFk, gameTypeFk, reelSetFk, reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter, totalWeightCount, totalTypeWeightCount);
            }
        }

        /// <summary>
        /// Deserialize pot feature probability root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializePotFeatureProbability(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte reelNudgeScatter, byte extraJackpotScatter, byte reelUpgradeScatter, long totalWeightCount, byte totalTypeWeightCount)
        {
            long value = StringToLong(node, "value");
            long weight = StringToLong(node, "weight");

            JIXRYPotFeatureProbabilityRoot probability = new JIXRYPotFeatureProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, reelNudgeScatter, extraJackpotScatter, reelUpgradeScatter, totalWeightCount, totalTypeWeightCount, value, weight);
            _potFeatureProbabilityCollection.Add(probability);   
        }

        /// <summary>
        /// Deserialize coin value root
        /// Deserialize extra spin root
        /// Deserialize extra prize multiplier root
        /// Deserialize extra prize payer root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeNoExtraJackpot(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk)
        {
            string extraType = node.Attributes["type"].Value;
            byte reelNumber = StringToByte(node, "ReelNumber");
            byte symbolIndex = StringToByte(node, "SymbolIndex");
            long totalWeightCount = StringToLong(node, "TotalWeight_Cnt");
            byte totalTypeWeightCount = StringToByte(node, "TotalTypeWeight_Cnt");

            switch (extraType)
            {
                case "Credit":
                    JIXRYCoinValueRoot creditProbability = new JIXRYCoinValueRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount);
                    _coinValueCollection.Add(creditProbability);
                    break;
                case "SpinNo":
                    JIXRYExtraSpinRoot extraSpinProbability = new JIXRYExtraSpinRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount);
                    _extraSpinCollection.Add(extraSpinProbability);
                    break;
                case "Multiplier":
                    JIXRYExtraPrizeMultiplierRoot extraPrizeMultiplierProbability = new JIXRYExtraPrizeMultiplierRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount);
                    _extraPrizeMultiplierCollection.Add(extraPrizeMultiplierProbability);
                    break;
                case "Payer":
                    JIXRYExtraPrizePayerRoot extraPrizePayerProbability = new JIXRYExtraPrizePayerRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount);
                    _extraPrizePayerCollection.Add(extraPrizePayerProbability);
                    break;
            }

            int n = node.ChildNodes.Count;
            for (int i = 0; i < n; i++)
            {
                XmlNode probabilityNode = node.ChildNodes[i];
                DeserializeNoExtraJackpotProbability(probabilityNode, rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount);
            }
        }

        /// <summary>
        /// Deserialize coin value probability root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeNoExtraJackpotProbability(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount)
        {
            long value = StringToLong(node, "value");
            long weight = StringToLong(node, "weight");


            switch (extraType)
            {
                case "Credit":
                    JIXRYCoinValueProbabilityRoot creditProbability = new JIXRYCoinValueProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount, value, weight);
                    _coinValueProbabilityCollection.Add(creditProbability);
                    break;
                case "SpinNo":
                    JIXRYExtraSpinProbabilityRoot extraSpinProbability = new JIXRYExtraSpinProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount, value, weight);
                    _extraSpinProbabilityCollection.Add(extraSpinProbability);
                    break;
                case "Multiplier":
                    JIXRYExtraPrizeMultiplierProbabilityRoot extraPrizeMultiplierProbability = new JIXRYExtraPrizeMultiplierProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount, value, weight);
                    _extraPrizeMultiplierProbabilityCollection.Add(extraPrizeMultiplierProbability);
                    break;
                case "Payer":
                    JIXRYExtraPrizePayerProbabilityRoot extraPrizePayerProbability = new JIXRYExtraPrizePayerProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, totalWeightCount, totalTypeWeightCount, value, weight);
                    _extraPrizePayerProbabilityCollection.Add(extraPrizePayerProbability);
                    break;
            }          
        }

        /// <summary>
        /// Deserialize mystery jackpot root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeMysteryJackpot(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName)
        {
            string jackpotSet = node.Attributes["Jackpot_Set"].Value;
            byte jackpotGroup = StringToByte(node, "Jackpot_Group");
            byte jackpotOption = StringToByte(node, "Jackpot_Option");
            byte betMultiplier = StringToByte(node, "Bet_Multiplier");
            uint denominationRatio = StringToUInt(node, "Denomination_Ratio");
            long totalWeightCount = StringToLong(node, "TotalWeight_Cnt");
            byte totalTypeWeightCount = StringToByte(node, "TotalTypeWeight_Cnt");

            byte jpSet = JackportSetNameToByte(jackpotSet);

            JIXRYMysteryJackpotRoot probability = new JIXRYMysteryJackpotRoot(rtpFk, betFk, gameTypeFk, reelSetFk, nodeName, jpSet, jackpotGroup, jackpotOption, betMultiplier, denominationRatio, totalWeightCount, totalTypeWeightCount);
            _mysteryJackpotCollection.Add(probability);

            int n = node.ChildNodes.Count;
            for (int i = 0; i < n; i++)
            {
                XmlNode probabilityNode = node.ChildNodes[i];
                DeserializeMysteryJackpotProbability(probabilityNode, rtpFk, betFk, gameTypeFk, reelSetFk, jpSet, jackpotGroup, jackpotOption, betMultiplier, denominationRatio, totalWeightCount, totalTypeWeightCount);
            }
        }

        /// <summary>
        /// Deserialize mystery jackpot probability root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeMysteryJackpotProbability(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, uint denominationRatio, long totalWeightCount, byte totalTypeWeightCount)
        {
            long value = StringToLong(node, "value");
            long weight = StringToLong(node, "weight");

            JIXRYMysteryJackpotProbabilityRoot probability = new JIXRYMysteryJackpotProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, jackpotSet, jackpotGroup, jackpotOption, betMultiplier, denominationRatio, totalWeightCount, totalTypeWeightCount, value, weight);
            _mysteryJackpotProbabilityCollection.Add(probability);
        }

        /// <summary>
        /// Deserialize extra jackpot root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeExtraJackpot(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk)
        {
            string extraType = node.Attributes["type"].Value;
            byte reelNumber = StringToByte(node, "ReelNumber");
            byte symbolIndex = StringToByte(node, "SymbolIndex");
            string jackpotSet = node.Attributes["Jackpot_Set"].Value;
            byte jackpotGroup = StringToByte(node, "Jackpot_Group");
            byte jackpotOption = StringToByte(node, "Jackpot_Option");
            byte betMultiplier = StringToByte(node, "Bet_Multiplier");
            long totalWeightCount = StringToLong(node, "TotalWeight_Cnt");
            byte totalTypeWeightCount = StringToByte(node, "TotalTypeWeight_Cnt");

            byte jpSet = JackportSetNameToByte(jackpotSet);

            JIXRYExtraJackpotRoot probability = new JIXRYExtraJackpotRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, jpSet, jackpotGroup, jackpotOption, betMultiplier, totalWeightCount, totalTypeWeightCount);
            _extraJackpotCollection.Add(probability);

            int n = node.ChildNodes.Count;
            for (int i = 0; i < n; i++)
            {
                XmlNode extraJpNode = node.ChildNodes[i];
                DeserializeExtraJackpotProbability(extraJpNode, rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, jpSet, jackpotGroup, jackpotOption, betMultiplier, totalWeightCount, totalTypeWeightCount);
            }
        }

        /// <summary>
        /// Deserialize extra jackpot probability root
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rtpFk"></param>
        /// <param name="betFk"></param>
        /// <param name="gameTypeFk"></param>
        /// <param name="reelSetFk"></param>
        private void DeserializeExtraJackpotProbability(XmlNode node, byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, long totalWeightCount, byte totalTypeWeightCount)
        {
            long value = StringToLong(node, "value");
            long weight = StringToLong(node, "weight");

            JIXRYExtraJackpotProbabilityRoot probability = new JIXRYExtraJackpotProbabilityRoot(rtpFk, betFk, gameTypeFk, reelSetFk, extraType, reelNumber, symbolIndex, jackpotSet, jackpotGroup, jackpotOption, betMultiplier, totalWeightCount, totalTypeWeightCount, value, weight);
            _extraJackpotProbabilityCollection.Add(probability);
        }
        #endregion

        #region Helper
        private byte StringToByte(XmlNode root, string name)
        {
            string val = root!.Attributes[name].Value ?? throw new Exception();
            val = val == string.Empty ? "0" : val;
            return byte.Parse(val);
        }

        private int StringToInt(XmlNode root, string name)
        {
            string val = root!.Attributes[name].Value ?? throw new Exception();
            return int.Parse(val);
        }

        private uint StringToUInt(XmlNode root, string name)
        {
            string val = root!.Attributes[name].Value ?? throw new Exception();
            return uint.Parse(val);
        }

        private long StringToLong(XmlNode root, string name)
        {
            string val = root!.Attributes[name].Value ?? throw new Exception();
            return long.Parse(val);
        }

        private double StringToDouble(XmlNode root, string name)
        {
            string val = root!.Attributes[name].Value ?? throw new Exception();
            return double.Parse(val);
        }

        private byte JackportSetNameToByte(string jackPotSetName)
        {
            switch (jackPotSetName)
            {
                case "A":
                    return 1;
                case "B":
                    return 2;
                case "C":
                    return 3;
                case "D":
                    return 4;
                default:
                    throw new ArgumentException($"Invalid jackpot set name: {jackPotSetName}");
            }
        }
        #endregion      
        /// <summary>
        /// Validate demo
        /// </summary>
        /// <param name="demo"></param>
        /// <returns></returns>
        public bool ValidateDemo(bool demo)
        {
            return _demo == demo;
        }
    }

    #region Struct
    /// <summary>
    /// Main root
    /// </summary>
    public struct JIXRYWeightRoot
    {
        public string gameCode { get; private set; }
        public string gameName { get; private set; }
        public byte totalVarLevelCount { get; private set; }

        public JIXRYWeightRoot(string gameCode, string gameName, byte totalVarLevelCount)
        {
            this.gameCode = gameCode;
            this.gameName = gameName;
            this.totalVarLevelCount = totalVarLevelCount;
        }
    }

    /// <summary>
    /// <rtp></rtp>
    /// </summary>
    public struct JIXRYRtpRoot
    {
        public byte level { get; private set; }
        public string rtpName { get; private set; }
        public byte betOptionCount { get; private set; }

        public JIXRYRtpRoot(byte level, string rtpName, byte betOptionCount)
        {
            this.level = level;
            this.rtpName = rtpName;
            this.betOptionCount = betOptionCount;
        }
    }

    /// <summary>
    /// <bet></bet>
    /// </summary>
    public struct JIXRYBetRoot
    {
        public byte rtpFk { get; private set; }
        public uint betOption { get; private set; }
        public double totalRtpValue { get; private set; }

        public JIXRYBetRoot(byte rtpFk, uint betOption, double totalRtpValue)
        {
            this.rtpFk = rtpFk;
            this.betOption = betOption;
            this.totalRtpValue = totalRtpValue;
        }
    }

    /// <summary>
    /// <game></game>
    /// </summary>
    public struct JIXRYGameRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameType { get; private set; }
        public long totalReelSetWeightCount { get; private set; }
        public byte reelSetCount { get; private set; }

        public JIXRYGameRoot(byte rtpFk, uint betFk, string gameType, long totalReelSetWeightCount, byte reelSetCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameType = gameType;
            this.totalReelSetWeightCount = totalReelSetWeightCount;
            this.reelSetCount = reelSetCount;
        }
    }

    /// <summary>
    /// <ReelSet></ReelSet>
    /// </summary>
    public struct JIXRYReelSetRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte id { get; private set; }
        public long totalTypeWeightCount { get; private set; }

        public JIXRYReelSetRoot(byte rtpFk, uint betFk, string gameTypeFk, byte id, long totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.id = id;        
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PotFeature_TriggerProb></PotFeature_TriggerProb>
    /// </summary>
    public struct JIXRYPotFeatureRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string nodeName { get; private set; }
        public byte reelNudgeScatter { get; private set; }
        public byte extraJackpotScatter { get; private set; }
        public byte reelUpgradeScatter { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYPotFeatureRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName, byte reelNudgeScatter, byte extraJackpotScatter, byte reelUpgradeScatter, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.nodeName = nodeName;
            this.reelNudgeScatter = reelNudgeScatter;
            this.extraJackpotScatter = extraJackpotScatter;
            this.reelUpgradeScatter = reelUpgradeScatter;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYPotFeatureProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public byte reelNudgeScatter { get; private set; }
        public byte extraJackpotScatter { get; private set; }
        public byte reelUpgradeScatter { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYPotFeatureProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte reelNudgeScatter, byte extraJackpotScatter, byte reelUpgradeScatter, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.reelNudgeScatter = reelNudgeScatter;
            this.extraJackpotScatter = extraJackpotScatter;
            this.reelUpgradeScatter = reelUpgradeScatter;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <Coin_Value></Coin_Value>
    /// </summary>
    public struct JIXRYCoinValueRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string coinType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYCoinValueRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string coinType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.coinType = coinType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYCoinValueProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string coinType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYCoinValueProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string coinType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.coinType = coinType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <Mystery_Jackpot></Mystery_Jackpot>
    /// </summary>
    public struct JIXRYMysteryJackpotRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string nodeName { get; private set; }
        public byte jackpotSet { get; private set; }
        public byte jackpotGroup { get; private set; }
        public byte jackpotOption { get; private set; }
        public byte betMultiplier { get; private set; }
        public uint denominationRatio { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYMysteryJackpotRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string nodeName, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, uint denominationRatio, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.nodeName = nodeName;
            this.jackpotSet = jackpotSet;
            this.jackpotGroup = jackpotGroup;
            this.jackpotOption = jackpotOption;
            this.betMultiplier = betMultiplier;
            this.denominationRatio = denominationRatio;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYMysteryJackpotProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public byte jackpotSet { get; private set; }
        public byte jackpotGroup { get; private set; }
        public byte jackpotOption { get; private set; }
        public byte betMultiplier { get; private set; }
        public uint denominationRatio { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYMysteryJackpotProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, uint denominationRatio, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.jackpotSet = jackpotSet;
            this.jackpotGroup = jackpotGroup;
            this.jackpotOption = jackpotOption;
            this.betMultiplier = betMultiplier;
            this.denominationRatio = denominationRatio;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <ExtraSpin></ExtraSpin>
    /// </summary>
    public struct JIXRYExtraSpinRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYExtraSpinRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYExtraSpinProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYExtraSpinProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <ExtraPrizes></ExtraPrizes>Multiplier
    /// </summary>
    public struct JIXRYExtraPrizeMultiplierRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYExtraPrizeMultiplierRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYExtraPrizeMultiplierProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYExtraPrizeMultiplierProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <ExtraPrizes></ExtraPrizes>Payer
    /// </summary>
    public struct JIXRYExtraPrizePayerRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYExtraPrizePayerRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYExtraPrizePayerProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYExtraPrizePayerProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }

    /// <summary>
    /// <Jackpot></Jackpot>
    /// </summary>
    public struct JIXRYExtraJackpotRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public byte jackpotSet { get; private set; }
        public byte jackpotGroup { get; private set; }
        public byte jackpotOption { get; private set; }
        public byte betMultiplier { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }

        public JIXRYExtraJackpotRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, long totalWeightCount, byte totalTypeWeightCount)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.jackpotSet = jackpotSet;
            this.jackpotGroup = jackpotGroup;
            this.jackpotOption = jackpotOption;
            this.betMultiplier = betMultiplier;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
        }
    }

    /// <summary>
    /// <PROBABILITY></PROBABILITY>
    /// </summary>
    public struct JIXRYExtraJackpotProbabilityRoot
    {
        public byte rtpFk { get; private set; }
        public uint betFk { get; private set; }
        public string gameTypeFk { get; private set; }
        public byte reelSetFk { get; private set; }
        public string extraType { get; private set; }
        public byte reelNumber { get; private set; }
        public byte symbolIndex { get; private set; }
        public byte jackpotSet { get; private set; }
        public byte jackpotGroup { get; private set; }
        public byte jackpotOption { get; private set; }
        public byte betMultiplier { get; private set; }
        public long totalWeightCount { get; private set; }
        public byte totalTypeWeightCount { get; private set; }
        public long value { get; private set; }
        public long weight { get; private set; }

        public JIXRYExtraJackpotProbabilityRoot(byte rtpFk, uint betFk, string gameTypeFk, byte reelSetFk, string extraType, byte reelNumber, byte symbolIndex, byte jackpotSet, byte jackpotGroup, byte jackpotOption, byte betMultiplier, long totalWeightCount, byte totalTypeWeightCount, long value, long weight)
        {
            this.rtpFk = rtpFk;
            this.betFk = betFk;
            this.gameTypeFk = gameTypeFk;
            this.reelSetFk = reelSetFk;
            this.extraType = extraType;
            this.reelNumber = reelNumber;
            this.symbolIndex = symbolIndex;
            this.jackpotSet = jackpotSet;
            this.jackpotGroup = jackpotGroup;
            this.jackpotOption = jackpotOption;
            this.betMultiplier = betMultiplier;
            this.totalWeightCount = totalWeightCount;
            this.totalTypeWeightCount = totalTypeWeightCount;
            this.value = value;
            this.weight = weight;
        }
    }
    #endregion
}