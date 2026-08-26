using UnityEngine;
using UnityEngine.UI;
using Weike.LobbyManagement;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYHelpPageJackpotPrizePoolDisplayObject : WkHelpPageSpriteContent
    {
        [SerializeField] private GameObject minorPrize_En;
        [SerializeField] private GameObject minorPrize_Zh;
        [SerializeField] private GameObject miniPrize_En;
        [SerializeField] private GameObject miniPrize_Zh;

        private string _minorPrizeEn;
        private string _minorPrizeZh;
        private string _miniPrizeEn;
        private string _miniPrizeZh;

        private string denomination = string.Empty;
        private string betOption = string.Empty;

        #region Binding Properties
#pragma warning disable CS1591
        public string currency { get; set; } = string.Empty;
#pragma warning restore CS1591
        #endregion

        #region Unity Interface
        protected override void Awake()
        {
            base.Awake();
            _minorPrizeEn = minorPrize_En.GetComponent<Text>().text;
            _minorPrizeZh = minorPrize_Zh.GetComponent<Text>().text;
            _miniPrizeEn = miniPrize_En.GetComponent<Text>().text;
            _miniPrizeZh = miniPrize_Zh.GetComponent<Text>().text;
        }
        #endregion

        #region wkDisplayObject interface

        ///<inheritdoc/>
        protected override void OnAllowedEnable()
        {
            base.OnAllowedEnable();
            InitJackpotPrizePoolValue();
        }

        ///<inheritdoc/>
        protected override void ResetToDefault()
        {
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            InitJackpotPrizePoolValue();
        }
        #endregion

        /// <summary>
        /// Initialize jackpot prize pools value
        /// </summary>
        protected virtual void InitJackpotPrizePoolValue()
        {
            if (owningPlayerController is null) return;
            if (owningPlayerController!.owner is not JIXRYGameManager { gameObject: { activeInHierarchy: bool isActive } }) return;
            if (!isActive) return;
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            denomination = WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)gm.dataModel.denomValue);
            betOption = dm.getTargetBetMultiplier.ToString();

            if (language == WkGameLanguage.EN)
            {
                minorPrize_En.SetActive(true);
                miniPrize_En.SetActive(true);
                minorPrize_Zh.SetActive(false);
                miniPrize_Zh.SetActive(false);

                string minorText = _minorPrizeEn;
                minorText = minorText.Replace("KEY_DENOM", denomination).Replace("KEY_BETMULTIPLIER",betOption).Replace("KEY_PRIZEVALUE",
                    WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)GetMinorPrize()));
                minorPrize_En.GetComponent<Text>().text = minorText;

                string miniText = _miniPrizeEn;
                miniText = miniText.Replace("KEY_DENOM", denomination).Replace("KEY_BETMULTIPLIER", betOption).Replace("KEY_PRIZEVALUE",
                    WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)GetMiniPrize()));
                miniPrize_En.GetComponent<Text>().text = miniText;
            }
            else
            {
                minorPrize_En.SetActive(false);
                miniPrize_En.SetActive(false);
                minorPrize_Zh.SetActive(true);
                miniPrize_Zh.SetActive(true);

                string minorText = _minorPrizeZh;
                minorText = minorText.Replace("KEY_DENOM", denomination).Replace("KEY_BETMULTIPLIER", betOption).Replace("KEY_PRIZEVALUE",
                    WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)GetMinorPrize()));
                minorPrize_Zh.GetComponent<Text>().text = minorText;

                string miniText = _miniPrizeZh;
                miniText = miniText.Replace("KEY_DENOM", denomination).Replace("KEY_BETMULTIPLIER", betOption).Replace("KEY_PRIZEVALUE",
                    WkCoreCurrencyUtils.GetCurrencyOnGameDisplayInString(currency, (ulong)GetMiniPrize()));
                miniPrize_Zh.GetComponent<Text>().text = miniText;
            }
        }

        private long GetMinorPrize()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            if (gm.winManager is not JIXRYWinManager wm)
            {
                return 0;
            }
            int index = wm.GetJackpotPrizeIndex("MINOR");
            uint betMultiplier = dm.getTargetBetMultiplier;
            return GetPrize(index) * betMultiplier;
        }

        private long GetMiniPrize()
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            if (gm.winManager is not JIXRYWinManager wm)
            {
                return 0;
            }
            int index = wm.GetJackpotPrizeIndex("MINI");
            uint betMultiplier = dm.getTargetBetMultiplier;
            return GetPrize(index) * betMultiplier;
        }

        private long GetBasePrize(int index)
        {
            return machineContext?.platformInterface?.GetStdProgressiveBaseAmount(index) ?? 0;
        }

        private long GetPrize(int index)
        {
            return machineContext?.platformInterface?.GetStdProgressiveAmount(index) ?? 0;
        }
    }
}
