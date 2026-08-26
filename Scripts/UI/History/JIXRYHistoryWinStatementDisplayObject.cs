using UnityEngine;
using Weike.SlotCore;
using UnityEngine.UI;
using TMPro;

namespace Weike.Games.JIXRY
{
    public class JIXRYHistoryWinStatementDisplayObject : WkHistoryWinStatementDisplayObject
    {
        [Header("Feature & Ingot")]
        [SerializeField] TMP_Text featureText;
        [SerializeField] RawImage ingotSymbolImage;

        protected override bool ShowWinCount()
        {
            if (winCount == 0)
            {
                return false;
            }
            return true;
        }

        protected override void UpdateWinStatement()
        {
            base.UpdateWinStatement();
            featureText.gameObject.SetActive(false);

            if (!ShowWinCount())
            {
                if (symbolInfo.IsSymbolScatter(symbolIndex))
                {
                    featureText.gameObject.SetActive(true);
                    featureText.text = "FREE SPINS FEATURE TRIGGERED";
                }
                else
                {
                    ingotSymbolImage.gameObject.SetActive(true);
                }
            }
        }
    }
}