using System.Collections.Generic;
using System.Linq;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatHiddenSettingsExtraSpinOptionDisplayObject : WkCheatHiddenSettingsBaseDisplayObject
    {
        private string[] _option = { "1", "2", "3"};

        protected override void InitDropDownContent()
        {
            base.InitDropDownContent();
            dropDown.onValueChanged.RemoveAllListeners();

            JIXRYGameManager gm = owningPlayerController?.owner as JIXRYGameManager;
            if (gm is null) { return; }
            
            List<string> options;
            options = _option.ToList();

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
            JIXRYCheatDataModel cdm = gm.cheatDataModel as JIXRYCheatDataModel;
            if (cdm is null) { return; }
        }
    }
}
