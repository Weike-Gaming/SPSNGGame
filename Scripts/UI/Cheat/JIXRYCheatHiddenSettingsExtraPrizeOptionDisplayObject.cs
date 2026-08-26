using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenSettingsExtraPrizeOptionDisplayObject : WkCheatHiddenSettingsBaseDisplayObject
    {
        private string[] _mulOption = { "x2", "x3", "x5" };

        [SerializeField] private bool isReel3;

        protected override void InitDropDownContent()
        {
            base.InitDropDownContent();
            dropDown.onValueChanged.RemoveAllListeners();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) { return; }

            List<string> options = _mulOption.ToList();

            dropDown.AddOptions(options);
            dropDown.value = 0;
            ApplySettings();
            dropDown.onValueChanged.AddListener(delegate
            {
                ApplySettings();
            });
        }

        protected override void ApplySettings()
        {
            base.ApplySettings();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) { return; }
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            if (dm is null) { return; }
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel;
            if (cdm is null) { return; }

            uint betMultiplier = dm.getSelectedBetMultipliers[dm.targetedBetMultiplier];

            cdm.predetermineExtraPrizeMultiplierType = GetMultiplierType(_mulOption[dropDown.value]);
        }

        private byte GetMultiplierType(string value)
        {
            string numberPart = value.StartsWith("x", StringComparison.OrdinalIgnoreCase)
                ? value.Substring(1)
                : value;

            if (byte.TryParse(numberPart, out byte result))
            {
                return result;
            }
            else
            {
                return byte.MaxValue;
            }
        }
    }
}
