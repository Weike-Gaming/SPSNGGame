using System;
using UnityEngine;
using Weike.SlotCore;
using UnityEngine.UI;
using Weike.Core;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenSettingsIsMultiplyDisplayObject : WkDisplayObject
    {
        [SerializeField] private byte reelIndex;
        [SerializeField] private byte rowIndex;
        [SerializeField] private Toggle toggle;

        protected override void OnAllowedEnable()
        {
        }

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel ?? throw new InvalidCastException();

            wkModelCollection.AddModel(cdm);

            Func<string, WkSlotStateCore?> getState = gm.gameState!.GetState<WkSlotStateCore>;

            getState("fg-cheat")!.onEnterState += () =>
            {
                toggle.isOn = false;
            };
            toggle?.onValueChanged.AddListener(delegate (bool isOn)
            {
                SetsMultiply(isOn);
            });
        }

        protected override void ResetToDefault()
        {
            
        }

        private void SetsMultiply(bool isMultiply)
        {
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) return;
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel;
            if (cdm is null) return;
            int index = reelIndex * 4 - rowIndex;
            if (isMultiply)
                cdm.predetermineIsMultiply[index] = 1;
            else
                cdm.predetermineIsMultiply[index] = 0;
        }
    }
}
