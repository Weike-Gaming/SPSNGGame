using UnityEngine;
using UnityEngine.UI;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCombiWinStatementDisplayObject : WkCombiWinStatementDisplayObject
    {
        [Header("Feature & Ingot")]
        [SerializeField] private Text featureText;
        [SerializeField] private RawImage ingotSymbolImage;

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
