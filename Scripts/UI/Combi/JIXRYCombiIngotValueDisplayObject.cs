using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Weike.Common;
using Weike.Core;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    /// <summary>
    /// Handles the individual changing of ingot value if it appears
    /// Attached to the parent of the Dropdown UI element
    /// </summary>
    public class JIXRYCombiIngotValueDisplayObject : WkCombiDisplayObject
    {        
        [Header("Reel Position Setting")]
        [SerializeField] byte reelIndex;
        [SerializeField] byte symbolIndex;

        /// <summary>
        /// Reference to child TMP_Dropdown
        /// </summary>
        private TMP_Dropdown _dropdown = null;

        /// <summary>
        /// List of possible values for the ingot
        /// Get data from JIXRYGameDataModel
        /// </summary>
        private List<long> _possibleValues = new List<long>();

        public uint[] ingotValue { get; set; }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            _dropdown = GetComponentInChildren<TMP_Dropdown>();
            if (!WkAssert.EnsureMsgf(_dropdown != null, $"Reel {reelIndex} - Index {symbolIndex} is missing dropdown component in child"))
                return;

            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();
            JIXRYFreeGameDataModel fgdm = gm.freeGameDataModel as JIXRYFreeGameDataModel ?? throw new InvalidCastException();

            wkModelCollection.AddModel(dm);
            wkModelCollection.AddModel(fgdm);
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            RedrawDropdown();
        }

        private void RedrawDropdown()
        {
            if (_dropdown is null) return;

            _dropdown.ClearOptions();

            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) { return; }

            List<string> options = new List<string>();
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;
            while (rm is null)
            {
                rm = gm.reelManager as JIXRYReelManager;
                return;
            }

            // get list of possible values from XML
            _possibleValues = gm.GetIngotValuesFromXML((byte)(reelIndex + 1));

            // Set dropdown options
            for (int i = 0; i < _possibleValues.Count; i++)
            {
                // dropdown.Add values 
                options.Add(_possibleValues[i].ToString());
            }
            _dropdown.AddOptions(options);
        }

        /// <summary>
        /// Updates the dropdown value change listener to apply settings based on the selected value.
        /// </summary>
        private void OnValueChange()
        {
            _dropdown.onValueChanged.RemoveAllListeners();
            _dropdown.onValueChanged.AddListener((v) => ApplySettings(v));
        }

        protected override void SafeSelected()
        {
            base.SafeSelected();
            if (!gameObject.activeInHierarchy) return;
            StopAllCoroutines();
            OnValueChange();
            _dropdown.gameObject.SetActive(IsIngot());
            _dropdown.RefreshShownValue();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ResetDropDownValue();
        }

        /// <summary>
        /// Updates ReelData[reelIndex].reelIngotValue
        /// Updates GameDataModel.ingotValue
        /// </summary>
        /// <param name="value"></param>
        /// <param name="doCheckWin"></param>
        private void ApplySettings(int value, bool doCheckWin = true)
        {
            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) return;
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;
            if (rm is null) return;
            JIXRYReelData[] rd = rm.reelData as JIXRYReelData[] ?? throw new InvalidCastException();
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel ?? throw new InvalidCastException();

            uint newVal = (uint)_possibleValues[value];

            // Update ReelData[reelIndex].reelIngotValue
            int totalRows = rd[0].numRows + rd[0].numDummy;
            uint[] tmp = new uint[totalRows];
            tmp = rd[reelIndex].reelIngotValue;
            tmp[symbolIndex] = newVal;
            rd[reelIndex].reelIngotValue = tmp;

            // Update GameDataModel.ingotValue
            tmp = new uint[dm.ingotValue.Length];
            tmp = dm.ingotValue;
            dm.ingotValue[reelIndex * totalRows + symbolIndex + (rd[0].numDummy / 2)] = newVal;
            dm.ingotValue = tmp;

            if (doCheckWin)
            {
                gm.SetSkipIngotGeneration();
                gm.CheckWin();
            }
        }

        /// <summary>
        /// Check if current symbol IsIngot.
        /// Used to enable/disable _dropdown
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private bool IsIngot()
        {
            JIXRYCombiGameManager gm = owningPlayerController?.owner as JIXRYCombiGameManager;
            if (gm is null) return false;

            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;

            if (rm is null) return false;

            byte index = rm.GetReelIconIndex(reelIndex, symbolIndex);
            index--; //GetReelIconIndex return 1 to max, BUT this is used in an array that starts a 0.
            WkSymbolInfo symbolInfo = rm.symbolInfo ?? throw new Exception("Missing symbol info reference");
            WkSymbolInfoTemplate symbolInfoTemplate = symbolInfo.symbolTemplate!;
            WkSymbolDetail symbolDetail = symbolInfoTemplate.symbols[index];
            string ingotType = symbolDetail.symbolType;

            if (ingotType.Contains("INGOT"))
                return true;
            else
                return false;
        }

        protected virtual void ResetDropDownValue()
        {
            if (_dropdown is null) { return; }
            _dropdown.ClearOptions();
            _dropdown.value = 0;
        }
    }
}
