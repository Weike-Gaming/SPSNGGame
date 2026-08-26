using UnityEngine;
using Weike.SlotCore;
using UnityEngine.UI;
using TMPro;
using Weike.LobbyManagement;

namespace Weike.Games.JIXRY
{
    public class JIXRYWinStatementDisplayObject : WkWinStatementDisplayObject
    {
        [Header ("Feature & Ingot")]
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
                    featureText.text = language == WkGameLanguage.EN ? $"FREE SPINS FEATURE TRIGGERED" : $"触发免费旋转项目";
                }
                else 
                {
                    ingotSymbolImage.gameObject.SetActive(true);
                }
            }
        }

        protected override void ResetToDefault()
        {
            base.ResetToDefault();
            featureText.gameObject.SetActive(false);
        }
    }
}