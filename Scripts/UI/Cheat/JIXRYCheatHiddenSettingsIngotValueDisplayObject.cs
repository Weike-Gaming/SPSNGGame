using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenSettingsIngotValueDisplayObject : WkCheatHiddenSettingsBaseDisplayObject
    {
        [SerializeField] private byte reelIndex;
        [SerializeField] private byte rowIndex;
        private const int NumRow = 3;
        private string[] _option = { "1", "2", "3" };
        private uint[] _ingotValues = new uint[10];

        public byte selectedBetOption { get; set; }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();

            wkModelCollection.AddModel(cdm);

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            getState("cheat")!.onEnterState += InitDropDownContent;
            getState("fg-cheat")!.onEnterState += InitDropDownContent;
        }

        protected override void InitDropDownContent()
        {
            base.InitDropDownContent();
            dropDown.onValueChanged.RemoveAllListeners();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) { return; }

            _ingotValues = gm.GetPossibleIngotValue(reelIndex).ToArray();

            List<string> options;
            options = _ingotValues.Select(v => v.ToString()).ToList();
            
            dropDown.AddOptions(options);
            dropDown.value = 0;
            ApplySettings();
            dropDown.onValueChanged.AddListener(delegate
            {
                ApplySettings();
            });
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            ApplySettings();
        }

        protected override void ApplySettings()
        {
            base.ApplySettings();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) return;
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            if (dm is null) return;
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel;
            if (cdm is null) return;
            JIXRYReelManager rm = gm.reelManager as JIXRYReelManager;
            if (rm is null) return;
            JIXRYReelData rd = rm.reelData[0] as JIXRYReelData;

            uint betMultiplier = 0;

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                betMultiplier = dm.getSelectedBetMultipliers[cdm.selectedBetOption];
            }
            else
            {
                betMultiplier = dm.getBetMultiplier;
            }

            int index = (reelIndex - 1 ) * (rd.numRows + rd.numDummy) + (rowIndex - 1);
            cdm.predetermineIngotValue[index] = _ingotValues[dropDown.value] * betMultiplier;
        }
    }
}
