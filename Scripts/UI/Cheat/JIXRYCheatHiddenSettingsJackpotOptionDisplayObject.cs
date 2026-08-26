using System;
using System.Collections.Generic;
using System.Linq;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenSettingsJackpotOptionDisplayObject : WkCheatHiddenSettingsBaseDisplayObject
    {
        private string[] _option = {"No", "Grand", "Major", "Minor", "Mini"};

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();
            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager ?? throw new InvalidCastException();

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
            JIXRYGameDataModel dm = gm.dataModel as JIXRYGameDataModel;
            if (dm is null) { return; }

            List<string> options;
            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                options = _option.Take(3).ToList();
            }
            else
            {
                options = _option.Skip(1).ToList();
            }

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

            if (dm.upcomingPotFeatureGameFlag == (byte)JIXRYStateDataFlag.MAIN_GAME)
            {
                cdm.predetermineJackpotType = (byte)(dropDown.value);
            }
            else
            {
                cdm.predetermineJackpotType = (byte)(dropDown.value + 1);
            }            
        }
    }
}
