using UnityEngine;
using Weike.SlotCore;

namespace Weike.Games.JIXRY
{
    public class JIXRYCheatReturnButtonDisplayObject : WkCheatReturnButtonDisplayObject
    {
        [SerializeField] JIXRYCheatHiddenPanelContentWrapperDisplayObject contentWrapper;

        protected override void PostInitializedDisplayObject()
        {
            base.PostInitializedDisplayObject();

            button.onClickDown.AddListener(contentWrapper.CloseMoreSettings);
        }
    }
}
